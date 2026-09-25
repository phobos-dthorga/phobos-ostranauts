// Adapted from Auto Navigate 1.2.0 by Gravy / mrkmg (Workshop 3745533691).
// Reconstructed from the author's distributed binary; source repository and licence unverified.
// See THIRD_PARTY_NOTICES.md and docs/auto-navigate-adaptation.md. Not covered by our MIT grant.
#nullable disable
using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using PhobosAutoNav.Core;

namespace PhobosAutoNav;

internal static class AutoNavCore
{
	public enum Phase
	{
		Idle,
		Align,
		Accel,
		Cruise,
		Coast,
		Decel,
		Arrive
	}

	public const double M_TO_AU = 6.684587122268445E-12;

	public const double KM_TO_AU = 6.684587122268445E-09;

	private static readonly MotionTrack Track = new MotionTrack();
    private static NavVector lastAcceleration;
    internal static bool Following;
    internal static bool FaceTarget;
    internal static double? WeaponHeading;
    internal static bool ControlLimited { get; private set; }
    internal static double PredictionHorizon => Track.Horizon;
    internal static double PredictionError => Track.ErrorMS;



	public static volatile Phase CurrentPhase = Phase.Idle;

	public static volatile bool Engaged;

	public static Ship EngagedPlayer;

	public static TargetRef EngagedTarget;

	public static volatile string LastResult;

	public static double CruiseAU = 1.336917424453689E-09;

	public static double ArrSpdAU = 1.336917424453689E-10;

	public static double ArriveAU = ApproachRules.DefaultArrivalKM * KM_TO_AU;

	private static double _elapsedSim;

	private static double _logAccum;

	private static bool _coasting;

    public static CoastSettings FlightCoastSettings { get; private set; }
    internal static bool FlightPrefersTorch { get; private set; }
    internal static double ElapsedSeconds => _elapsedSim;
    internal static bool Coasting => _coasting;
    internal static void AvoidanceStep(double dt)
    { AdvanceDockingClock(dt); Track.Reset(); lastAcceleration=default; }
    internal static void AdvanceDockingClock(double dt)
    { _elapsedSim += dt; _coasting = false; CurrentPhase = Phase.Align; }

    // Loading restores intent only. The first real physics step recomputes
    // guidance from native position/velocity; no old thrust is replayed.
    internal static void RestoreFlight(Ship ship, TargetRef target, FlightSnapshot snapshot)
    {
        EngagedPlayer = ship; EngagedTarget = target;
        Track.Reset(); lastAcceleration = default; Following = snapshot.IsFollowing; FaceTarget = false; WeaponHeading = null;
        CruiseAU = snapshot.CruiseMS * M_TO_AU;
        ArrSpdAU = snapshot.ArrivalMS * M_TO_AU;
        ArriveAU = snapshot.ArrivalKM * KM_TO_AU;
        _elapsedSim = snapshot.ElapsedSeconds; _coasting = snapshot.Coasting;
        FlightCoastSettings = snapshot.Coast; _logAccum = 0;
        FlightPrefersTorch = snapshot.PreferTorch;
        LastResult = null; CurrentPhase = _coasting ? Phase.Coast : Phase.Align;
        Engaged = true;
    }

	public static string PhaseName => CurrentPhase switch
	{
		Phase.Align => Text.Get("Flight.phase.ALIGN"),
		Phase.Accel => Text.Get("Flight.phase.ACCEL"),
		Phase.Cruise => Text.Get("Flight.phase.CRUISE"),
		Phase.Coast => Text.Get("Flight.phase.COAST"),
		Phase.Decel => Text.Get("Flight.phase.DECEL"),
		Phase.Arrive => Text.Get("Flight.phase.ARRIVE"),
		_ => "—",
	};

	public static Ship PlayerShip
	{
		get
		{
			if (!(CrewSim.coPlayer != null))
			{
				return null;
			}
			return CrewSim.coPlayer.ship;
		}
	}

	public static TargetRef LockedTarget()
	{
		return TargetRef.FromCrossHair();
	}

	public static void BeginFlight(Ship player, TargetRef target, CoastSettings coastSettings, bool preferTorch)
	{
		ShipSitu shipSitu = player?.objSS;
		if (shipSitu != null && (shipSitu.bOrbitLocked || shipSitu.bBOLocked || shipSitu.bIsBO))
		{
			try
			{
				if (!shipSitu.bIsBO)
				{
					shipSitu.UnlockFromBO();
				}
				player.UnlockFromOrbit();
			}
			catch (Exception ex)
			{
				Plugin.Verbose("unlock failed: " + ex.Message);
			}
		}
		player?.objSS?.ResetNavData();
		EngagedPlayer = player;
		EngagedTarget = target;
		_elapsedSim = 0.0;
		_logAccum = 0.0;
		_coasting = false;
        Track.Reset(); lastAcceleration = default; Following = FaceTarget = ControlLimited = false; WeaponHeading = null;
        FlightCoastSettings = coastSettings;
        FlightPrefersTorch = preferTorch;
		LastResult = null;
		CurrentPhase = Phase.Align;
		Engaged = true;
		Plugin.Verbose("flight engaged -> " + target.DisplayName);
	}

	public static void EndFlight(Ship player, string result)
	{
        // A reactor-control failure must not skip releasing RCS/navigation.
        try { Plugin.Service.Torch.Release(); }
        catch (Exception ex) { Plugin.Verbose("torch release failed: " + ex.Message); }
		Engaged = false;
		EngagedPlayer = null;
		EngagedTarget = null;
		CurrentPhase = ((result == "ARRIVED") ? Phase.Arrive : Phase.Idle);
		LastResult = result;
		if (player != null)
		{
			try
			{
				player.objSS?.ResetNavData();
				player.Maneuver(0f, 0f, 0f, 0, 1E-10f);
			}
			catch
			{
			}
		}
		try
		{
			CrewSim.ResetTimeScale();
		}
		catch
		{
		}
		Plugin.Verbose("flight ended: " + result);
	}

	public static void ResetStatics()
	{
        Track.Reset(); lastAcceleration = default; Following = FaceTarget = ControlLimited = false; WeaponHeading = null;
        FlightPrefersTorch = false;
		Engaged = false;
        _coasting = false;
		EngagedPlayer = null;
		EngagedTarget = null;
		CurrentPhase = Phase.Idle;
		LastResult = null;
	}

	public static void SteerFlight(Ship player, TargetRef target, double fTime)
	{
		ShipSitu shipSitu = player?.objSS;
		if (shipSitu == null || player.bDestroyed)
		{
			EndFlight(null, "ABORTED");
		}
		else
		{
			if (fTime <= 0.0)
			{
				return;
			}
			try
			{
				if (player.IsDocked())
				{
					EndFlight(player, "DOCKED");
					return;
				}
			}
			catch
			{
			}
			if (Plugin.AbortOnManualThrust == null || Plugin.AbortOnManualThrust.Value)
			{
				try
				{
					GUIOrbitDraw instance = GUIOrbitDraw.Instance;
					if (instance != null && instance.PlayerThrusting)
					{
						EndFlight(player, "MANUAL");
						return;
					}
				}
				catch
				{
				}
			}
			_elapsedSim += fTime;
			float num = ((Plugin.MaxFlightSimHours != null) ? Plugin.MaxFlightSimHours.Value : 48f);
			if (num > 0f && _elapsedSim > (double)num * Phobos.Ostranauts.Framework.Units.SecondsPerHour)
			{
				EndFlight(player, "TIMEOUT");
				return;
			}
			if (!target.Resolve(out var px, out var py, out var vx, out var vy))
			{
				EndFlight(player, "TGT LOST");
				return;
			}
			if (player.GetRCSRemain() <= 0.0)
			{
				EndFlight(player, "NO FUEL");
				return;
			}

            foreach (double value in new[] { px, py, vx, vy, shipSitu.vPosx, shipSitu.vPosy,
                shipSitu.vVelX, shipSitu.vVelY, shipSitu.fRot, shipSitu.fW, CruiseAU, ArrSpdAU, ArriveAU,
                (double)Plugin.ArrivalSpeedTolerance.Value,
                Plugin.RotAccelMax.Value, Plugin.RotSpeedMax.Value, Plugin.MaxFlightSimHours.Value })
            {
                if (!ArrivalBrake.Finite(value)) { EndFlight(player, "INVALID FLIGHT DATA"); return; }
            }
            double cruise = Math.Max(CruiseAU / M_TO_AU, 1), arrival = Math.Min(ArrSpdAU / M_TO_AU, cruise);
            double stop = EffectiveArriveAU(player, target) / M_TO_AU;
            var offset = new NavVector((px - shipSitu.vPosx) / M_TO_AU, (py - shipSitu.vPosy) / M_TO_AU);
            var velocity = new NavVector((shipSitu.vVelX - vx) / M_TO_AU, (shipSitu.vVelY - vy) / M_TO_AU);
            double tolerance = Plugin.ArrivalSpeedTolerance.Value;
            if (!Following && offset.Length <= stop * ApproachRules.ArrivalBandMultiplier && velocity.Length <= arrival + tolerance)
            { EndFlight(player, "ARRIVED"); return; }
            double throttle = ReadShipThrottle(player), full = player.RCSAccelMax / M_TO_AU;
            if (throttle <= 0) { EndFlight(player, "THROTTLE ZERO"); return; }
            if (!Track.Observe(-velocity, StarSystem.fEpoch, lastAcceleration))
            { EndFlight(player, "INVALID FLIGHT DATA"); return; }
            // Relative observations cancel common gravity; subtract our known delivered control.
            var acceleration = Track.Acceleration;
            double hull = CollisionManager.GetCollisionDistanceAU(player.objSS, target.TargetSitu) / M_TO_AU;
            if (!PredictiveGuidance.TryPlan(offset, velocity, acceleration, lastAcceleration, full * throttle,
                cruise, arrival, stop, hull, fTime, Track.Horizon, Following, out var plan))
            { EndFlight(player, "INVALID FLIGHT DATA"); return; }
            ControlLimited = plan.Limited;
            double error = plan.Correction.Length;
            double lateral = Math.Abs(velocity.X * offset.Unit.Y - velocity.Y * offset.Unit.X);
            if (!CoastRules.TryDecide(_coasting, cruise, error, lateral, stop, plan.Braking,
                FlightCoastSettings, out var coast)) { EndFlight(player, "INVALID FLIGHT DATA"); return; }
            _coasting = coast.Coasting && acceleration.Length < .001 && !Following;
            var demand = _coasting ? default : plan.Acceleration;
            var correction = plan.Correction;
            var torch = Plugin.Service.Torch;
            double available = 0;
            bool useTorch = !_coasting && torch.Available(player, FlightPrefersTorch, fTime, out available);
            // Do not rely on a future torch burn to recover from today's closing speed.
            double safe = TorchRules.SafeSpeed(Math.Max(0, offset.Length - stop * ApproachRules.ArrivalBandMultiplier),
                velocity.Length, arrival, full * throttle, fTime);
            if (useTorch && !plan.Braking && velocity.Length >= safe) useTorch = false;
            var legCorrection = plan.Braking ? correction.Unit * Math.Max(correction.Length, velocity.Length - arrival) : correction;
            if (useTorch && PredictiveGuidance.TorchWorthwhile(legCorrection, shipSitu.fRot, shipSitu.fW,
                Plugin.RotAccelMax.Value * throttle, Plugin.RotSpeedMax.Value, available, full * throttle,
                plan.Horizon, Plugin.TorchMinimumCorrectionMS.Value, out var delay) &&
                PredictiveGuidance.TorchSequenceSafe(offset, velocity, acceleration, plan.RequestedAcceleration,
                    hull, full * throttle, available, delay, fTime))
            {
                double torchHeading = -Math.Atan2(correction.X, correction.Y);
                double torchError = WrapPi(torchHeading - shipSitu.fRot);
                if (TorchRules.Aligned(torchError, shipSitu.fW, fTime + TorchRules.ZoneRefreshSeconds))
                {
                    double burn = TorchRules.BurnAcceleration(plan.RequestedAcceleration.X * fTime, plan.RequestedAcceleration.Y * fTime, shipSitu.fRot, available, fTime);
                    if (!plan.Braking) burn = Math.Min(burn, Math.Max(0, safe - velocity.Length) / fTime);
                    if (burn > 0 && torch.Burn(player, burn, fTime))
                    {
                        player.Maneuver(0, 0, 0, 0, (float)fTime);
                        lastAcceleration = new NavVector(shipSitu.vAccIn.x / M_TO_AU, shipSitu.vAccIn.y / M_TO_AU);
                        CurrentPhase = plan.Braking ? Phase.Decel : Phase.Accel;
                        return;
                    }
                }
                else
                {
                    torch.Align();
                    float turning = ComputeRotInput(shipSitu, torchError, fTime, TorchRules.MaximumHeadingRadians / 2);
                    // RCS keeps correcting while torch turns; a target cannot make us coast indefinitely by changing heading.
                    ApplyPredictiveRcs(player, demand, turning, full, fTime);
                    CurrentPhase = plan.Braking ? Phase.Decel : Phase.Align;
                    return;
                }
            }
            torch.Cut();
            double desiredHeading = FaceTarget && WeaponHeading.HasValue && !plan.Braking && !plan.Limited ? WeaponHeading.Value : -Math.Atan2(offset.X, offset.Y);
            double face = WrapPi(desiredHeading - shipSitu.fRot);
            float turn = _coasting && !FaceTarget ? (float)CoastRules.CoastRotation(shipSitu.fW, fTime, Plugin.RotAccelMax.Value) :
                ComputeRotInput(shipSitu, face, fTime, FlightCoastSettings.BurnHeadingToleranceDegrees * CoastRules.DegreesToRadians);
            ApplyPredictiveRcs(player, demand, turn, full, fTime);
            CurrentPhase = _coasting || plan.Holding ? Phase.Coast : plan.Braking ? Phase.Decel : Phase.Accel;
            _logAccum += fTime;
            if (_logAccum >= 5)
            {
                _logAccum = 0;
                Plugin.Verbose($"predictive range={offset.Length:0.0}m relative={velocity.Length:0.00}m/s horizon={plan.Horizon:0.0}s error={Track.ErrorMS:0.00}m/s limited={plan.Limited}");
            }
        }
    }

    private static void ApplyPredictiveRcs(Ship player, NavVector acceleration, float turn, double full, double dt)
    {
        double cos = Math.Cos(player.objSS.fRot), sin = Math.Sin(player.objSS.fRot);
        ApplyRcs(player, (acceleration.X * cos + acceleration.Y * sin) / full,
            (-acceleration.X * sin + acceleration.Y * cos) / full, turn, dt);
        lastAcceleration = new NavVector(player.objSS.vAccRCS.x / M_TO_AU, player.objSS.vAccRCS.y / M_TO_AU);
    }

    // Read-only snapshot: panel refreshes must not advance target physics.
    public static bool TryReadApproach(Ship player, TargetRef target, double requestedKM,
        out ApproachPlan plan, out double relativeSpeedMS)
    {
        plan = default;
        relativeSpeedMS = 0;
        try
        {
            var own = player?.objSS;
            var other = target?.TargetSitu;
            if (own == null || other == null) return false;
            double dx = other.vPosx - own.vPosx, dy = other.vPosy - own.vPosy;
            double vx = (own.vVelX - other.vVelX) / M_TO_AU;
            double vy = (own.vVelY - other.vVelY) / M_TO_AU;
            relativeSpeedMS = Math.Sqrt(vx * vx + vy * vy);
            return ArrivalBrake.Finite(relativeSpeedMS) && ApproachRules.TryPlan(
                Math.Sqrt(dx * dx + dy * dy) / KM_TO_AU, requestedKM,
                CollisionManager.GetCollisionDistanceAU(own, other) / KM_TO_AU, out plan);
        }
        catch { return false; }
    }

    // Phobos: shared read-only preflight for Fly, Resume and panel readiness.
    internal static bool TryReadAdmission(Ship player, TargetRef target, double requestedKM,
        double arrivalMS, double throttle, double reactionSeconds, out BrakingRoom room)
    {
        room = default;
        if (!TryReadApproach(player, target, requestedKM, out var plan, out _)) return false;
        var own = player.objSS;
        var other = target.TargetSitu;
        try
        {
            return ApproachAdmission.TryEvaluate((other.vPosx - own.vPosx) / M_TO_AU,
                (other.vPosy - own.vPosy) / M_TO_AU, (own.vVelX - other.vVelX) / M_TO_AU,
                (own.vVelY - other.vVelY) / M_TO_AU,
                CollisionManager.GetCollisionDistanceAU(own, other) / M_TO_AU,
                plan.EffectiveArrivalKM * 1000, arrivalMS, player.RCSAccelMax / M_TO_AU,
                throttle, reactionSeconds, out room);
        }
        catch { return false; }
    }

    private static void ApplyRcs(Ship ship, double x, double y, double turn, double dt)
    {
        double share = x == 0 && y == 0 ? 1 : RcsBudget.CombinedRotationShare;
        if (!RcsBudget.TryLimit(x, y, turn, ReadShipThrottle(ship), share, out var command))
        { EndFlight(ship, "INVALID FLIGHT DATA"); return; }
        ship.Maneuver((float)command.X, (float)command.Y, (float)command.Turn, 0, (float)dt);
    }

    public static double EffectiveArriveAU(Ship player, TargetRef target) =>
        TryReadApproach(player, target, ArriveAU / KM_TO_AU, out var plan, out _)
            ? plan.EffectiveArrivalKM * KM_TO_AU : double.NaN;
	public static bool HasFuelForFlight(Ship player, TargetRef target, bool readOnly = false)
	{
		if (player?.objSS == null || target == null)
		{
			return false;
		}
		try
		{
			if (player.GetRCSRemain() <= 0.0)
			{
				return false;
			}
			double px, py, vx, vy;
            if (readOnly)
            {
                var situ = target.TargetSitu;
                if (situ == null) return false;
                px = situ.vPosx; py = situ.vPosy; vx = situ.vVelX; vy = situ.vVelY;
            }
            else if (!target.Resolve(out px, out py, out vx, out vy))
			{
				return false;
			}
			double num = px - player.objSS.vPosx;
			double num2 = py - player.objSS.vPosy;
			double num3 = Math.Max(0.0, Math.Sqrt(num * num + num2 * num2) - EffectiveArriveAU(player, target));
			double num4 = player.objSS.vVelX - vx;
			double num5 = player.objSS.vVelY - vy;
			double num6 = Math.Sqrt(num4 * num4 + num5 * num5);
			double num7 = Math.Max(CruiseAU, M_TO_AU);
			double num8 = Math.Max(0.0, Math.Min(ArrSpdAU, num7));
			double num9 = Math.Min(num7, Math.Sqrt(Math.Max(0.0, player.RCSAccelMax * 0.85 * num3)));
			double num10 = num6 + num9 + Math.Max(0.0, num9 - num8);
			bool result = player.DeltaVRemainingRCS >= num10 * 1.05;
			Plugin.Verbose("fuel: have=" + (player.DeltaVRemainingRCS / M_TO_AU).ToString("0.#") + "m/s dV, need~" + (num10 / M_TO_AU).ToString("0.#") + "m/s -> " + result);
			return result;
		}
		catch (Exception ex)
		{
			Plugin.Verbose("fuel check failed: " + ex.Message);
			return false;
		}
	}

	public static bool AutoDockBusy()
	{
		try
		{
			Type type = Type.GetType("AutoDock.AutoDockCore, AutoDock");
			if (type == null)
			{
				return false;
			}
			FieldInfo field = type.GetField("Engaged");
			bool flag = default(bool);
			int num;
			if (field != null)
			{
				object value = field.GetValue(null);
				if (value is bool)
				{
					flag = (bool)value;
					num = 1;
				}
				else
				{
					num = 0;
				}
			}
			else
			{
				num = 0;
			}
			return (byte)((uint)num & (flag ? 1u : 0u)) != 0;
		}
		catch
		{
			return false;
		}
	}

	private static double WrapPi(double a)
	{
		a %= Math.PI * 2.0;
		if (a > Math.PI)
		{
			a -= Math.PI * 2.0;
		}
		else if (a < -Math.PI)
		{
			a += Math.PI * 2.0;
		}
		return a;
	}

	private static float ComputeRotInput(ShipSitu ps, double headErr, double fTime, double deadband)
	{
		double num = ((Plugin.RotAccelMax != null) ? ((double)Plugin.RotAccelMax.Value) : 0.5);
		double val = ((Plugin.RotSpeedMax != null) ? ((double)Plugin.RotSpeedMax.Value) : 0.6);
		if (num <= 0.0 || fTime <= 0.0)
		{
			return 0f;
		}
		double num2 = ps.fW;
		double num3 = Math.Abs(headErr);
		if (num3 < deadband && Math.Abs(num2) < 0.002)
		{
            return (float)Math.Max(-num, Math.Min(num, -num2 / (2 * fTime)));
		}
		double val2 = Math.Sqrt(2.0 * num * 0.8 * num3);
		double num4 = (double)Math.Sign(headErr) * Math.Min(val, val2);
		double num5 = (num4 - num2) / (2.0 * fTime);
		if (num5 > num)
		{
			num5 = num;
		}
		else if (num5 < 0.0 - num)
		{
			num5 = 0.0 - num;
		}
		return (float)num5;
	}

    private static float ReadShipThrottle(Ship player) => Plugin.Service.Throttle;
}
