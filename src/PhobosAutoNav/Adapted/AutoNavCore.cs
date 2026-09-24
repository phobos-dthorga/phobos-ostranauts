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

	private const double SAFETY = 0.85;

	private const double MAX_TGO = 2592000.0;

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

	public static void BeginFlight(Ship player, TargetRef target)
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
		LastResult = null;
		CurrentPhase = Phase.Align;
		Engaged = true;
		Plugin.Verbose("flight engaged -> " + target.DisplayName);
	}

	public static void EndFlight(Ship player, string result)
	{
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
		Engaged = false;
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
                (double)Plugin.ArrivalSpeedTolerance.Value, Plugin.CoastTolerance.Value,
                Plugin.RotAccelMax.Value, Plugin.RotSpeedMax.Value, Plugin.MaxFlightSimHours.Value })
            {
                if (!ArrivalBrake.Finite(value)) { EndFlight(player, "INVALID FLIGHT DATA"); return; }
            }
			double num2 = Math.Max(CruiseAU, M_TO_AU);
			double num3 = Math.Max(0.0, Math.Min(ArrSpdAU, num2));
			double num4 = (double)((Plugin.ArrivalSpeedTolerance != null) ? Plugin.ArrivalSpeedTolerance.Value : 5f) * M_TO_AU;
			double num5 = px - vx * fTime - shipSitu.vPosx;
			double num6 = py - vy * fTime - shipSitu.vPosy;
			double num7 = Math.Sqrt(num5 * num5 + num6 * num6);
			double num8 = shipSitu.vVelX - vx;
			double num9 = shipSitu.vVelY - vy;
			double num10 = Math.Sqrt(num8 * num8 + num9 * num9);
			double num11 = EffectiveArriveAU(player, target);
            if (!ArrivalBrake.Finite(num7) || !ArrivalBrake.Finite(num10) || !ArrivalBrake.Finite(num11))
            { EndFlight(player, "INVALID FLIGHT DATA"); return; }
			// Phobos: crossing a distance boundary is not proof of completed braking.
            if (ArrivalBrake.NeedsBrake(num7, num11, num10, num3, num4))
            {
                if (!ArrivalBrake.TryCommand(num8 / M_TO_AU, num9 / M_TO_AU, shipSitu.fRot,
                    player.RCSAccelMax / M_TO_AU, ReadShipThrottle(player), num3 / M_TO_AU, fTime,
                    out var command))
                {
                    EndFlight(player, "BRAKE UNAVAILABLE");
                    return;
                }
                CurrentPhase = Phase.Decel;
                player.Maneuver((float)command.X, (float)command.Y, 0f, 0, (float)fTime);
                return;
            }
            if (num7 <= num11 || (num3 <= num4 && num7 <= num11 * ApproachRules.ArrivalBandMultiplier && num10 <= num4))
			{
				EndFlight(player, "ARRIVED");
				return;
			}
			float num12 = ReadShipThrottle(player);
			if (num12 <= 0f)
			{
				EndFlight(player, "THROTTLE ZERO");
                return;
			}
			double rCSAccelMax = player.RCSAccelMax;
			if (rCSAccelMax <= 0.0)
			{
				return;
			}
			double num13 = (double)num12 * rCSAccelMax;
			double num14 = num5 / num7;
			double num15 = num6 / num7;
			double v = Math.Max(0.0, num8 * num14 + num9 * num15);
			double num16 = Math.Min(2592000.0, EstimateTGo(num7 - num11, v, num2, num3, num13));
			if (!target.ResolveAt(num16, out var px2, out var py2, out var _, out var _))
			{
				EndFlight(player, "TGT LOST");
				return;
			}
			double num17 = px2 - (px + vx * num16);
			double num18 = py2 - (py + vy * num16);
			double num19 = Math.Sqrt(num17 * num17 + num18 * num18);
			double num20 = 0.5 * num7;
			if (num19 > num20 && num19 > 0.0)
			{
				num17 *= num20 / num19;
				num18 *= num20 / num19;
				num19 = num20;
			}
			double num21 = num5 + num17;
			double num22 = num6 + num18;
			double num23 = Math.Sqrt(num21 * num21 + num22 * num22);
			if (num23 < 1E-15)
			{
				return;
			}
			double num24 = num21 / num23;
			double num25 = num22 / num23;
			float num26 = (float)(0.0 - Math.Atan2(num21, num22));
			double num27 = WrapPi(num26 - shipSitu.fRot);
			double num28 = num8;
			double num29 = num9;
			double num30 = num28 * num24 + num29 * num25;
			double num31 = Math.Max(0.0, num23 - num11);
			double num32 = Math.Sqrt(num3 * num3 + 2.0 * num13 * 0.85 * num31);
			double num33 = Math.Min(num2, num32);
			if ((num33 - num3) * fTime > num31)
			{
				num33 = num3 + num31 / fTime;
			}
			double num34 = num28 - num30 * num24;
			double num35 = num29 - num30 * num25;
			double num36 = Math.Sqrt(num34 * num34 + num35 * num35);
			double num37 = num33 - num30;
			double num38 = Math.Sqrt(num37 * num37 + num36 * num36);
			double num39 = Math.Max(0.1, (Plugin.CoastTolerance != null) ? Plugin.CoastTolerance.Value : 3f) * M_TO_AU;
			if (_coasting && num38 > num39)
			{
				_coasting = false;
			}
			else if (!_coasting && num38 < num39 * 0.2)
			{
				_coasting = true;
			}
			double num40 = 0.0;
			double num41 = 0.0;
			if (!_coasting)
			{
				double num42 = Math.Min(num13, num36 / fTime);
				if (num36 > 1E-18)
				{
					num40 = (0.0 - num34) / num36 * num42;
					num41 = (0.0 - num35) / num36 * num42;
				}
				double num43 = Math.Sqrt(Math.Max(0.0, num13 * num13 - num42 * num42));
				double num44 = Math.Max(0.0 - num43, Math.Min(num43, (num33 - num30) / fTime));
				num40 += num24 * num44;
				num41 += num25 * num44;
			}
			double num45 = num36 / M_TO_AU;
			double num46 = num30 / M_TO_AU;
			double num47 = num2 / M_TO_AU;
			double num48 = Math.Abs(num27) * 57.2957795;
			if (_coasting)
			{
				CurrentPhase = Phase.Coast;
			}
			else if (num32 < num2)
			{
				CurrentPhase = Phase.Decel;
			}
			else if (num48 > 15.0 || (num45 > 2.0 && num46 < num47 * 0.5))
			{
				CurrentPhase = Phase.Align;
			}
			else if (num30 >= num33 * 0.9)
			{
				CurrentPhase = Phase.Cruise;
			}
			else
			{
				CurrentPhase = Phase.Accel;
			}
			float fR = ComputeRotInput(shipSitu, num27, fTime, _coasting ? 0.087 : 0.0017);
			double num49 = Math.Cos(shipSitu.fRot);
			double num50 = Math.Sin(shipSitu.fRot);
			double num51 = (num40 * num49 + num41 * num50) / rCSAccelMax;
			double num52 = ((0.0 - num40) * num50 + num41 * num49) / rCSAccelMax;
			player.Maneuver((float)num51, (float)num52, fR, 0, (float)fTime);
			if (Plugin.VerboseLogging != null && Plugin.VerboseLogging.Value)
			{
				_logAccum += fTime;
				if (_logAccum >= 5.0)
				{
					_logAccum = 0.0;
					Plugin.Verbose("steer[" + PhaseName + "]: range=" + (num7 / KM_TO_AU).ToString("0.#") + "km tGo=" + num16.ToString("0") + "s lead=" + (num19 / KM_TO_AU).ToString("0.##") + "km in=" + (num30 / M_TO_AU).ToString("0.#") + " cross=" + (num36 / M_TO_AU).ToString("0.#") + " vDes=" + (num33 / M_TO_AU).ToString("0.#") + "m/s");
				}
			}
		}
	}

	private static double EstimateTGo(double d, double v0, double cruise, double arrSpd, double aMax)
	{
		if (d <= 0.0)
		{
			return 0.0;
		}
		if (aMax <= 0.0)
		{
			return d / Math.Max(v0, 1E-12);
		}
		if (v0 > cruise)
		{
			v0 = cruise;
		}
		double num = Math.Max(0.0, (cruise * cruise - v0 * v0) / (2.0 * aMax));
		double num2 = Math.Max(0.0, (cruise * cruise - arrSpd * arrSpd) / (2.0 * aMax));
		if (num + num2 <= d)
		{
			return (cruise - v0) / aMax + (cruise - arrSpd) / aMax + (d - num - num2) / cruise;
		}
		double val = (2.0 * aMax * d + v0 * v0 + arrSpd * arrSpd) / 2.0;
		double num3 = Math.Sqrt(Math.Max(val, Math.Max(v0 * v0, arrSpd * arrSpd)));
		return Math.Max(0.0, num3 - v0) / aMax + Math.Max(0.0, num3 - arrSpd) / aMax;
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

    public static double EffectiveArriveAU(Ship player, TargetRef target) =>
        TryReadApproach(player, target, ArriveAU / KM_TO_AU, out var plan, out _)
            ? plan.EffectiveArrivalKM * KM_TO_AU : double.NaN;
	public static bool HasFuelForFlight(Ship player, TargetRef target)
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
			if (!target.Resolve(out var px, out var py, out var vx, out var vy))
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

	private static float ComputeRotInput(ShipSitu ps, double headErr, double fTime, double deadband = 0.0017)
	{
		if (Plugin.UseThrusterRotation != null && !Plugin.UseThrusterRotation.Value)
		{
			ps.fRot = (float)((double)ps.fRot + headErr);
			ps.fW = 0f;
			ps.fA = 0f;
			return 0f;
		}
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
			ps.fW = 0f;
			ps.fA = 0f;
			return 0f;
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
