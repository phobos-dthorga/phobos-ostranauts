// Adapted from Auto Navigate 1.2.0 by Gravy / mrkmg (Workshop 3745533691).
// Reconstructed from the author's distributed binary; source repository and licence unverified.
// See THIRD_PARTY_NOTICES.md and docs/auto-navigate-adaptation.md. Not covered by our MIT grant.
#nullable disable
using System;
using Ostranauts.ShipGUIs.Utilities;
using Ostranauts.Ships;
using Ostranauts.Utils.Models;

namespace PhobosAutoNav;

internal sealed class TargetRef
{
	private Ship _ship;

	private string _shipRegID;

	private BodyOrbit _body;

	private IStellarObject _stellar;

	private double _fx;

	private double _fy;

	private bool _isFixed;

	public string DisplayName { get; private set; } = Text.Get("Flight.target");

	public ShipSitu TargetSitu
	{
		get
		{
			Ship ship = LiveShip();
			if (ship != null)
			{
				return ship.objSS;
			}
			return _stellar?.objSS;
		}
	}

	public BodyOrbit Body => _body;

    // Phobos: reconnect by registration ID in the newly loaded world, never by display name.
    public string ShipId => _shipRegID;
    public static TargetRef FromShipId(string id)
    {
        var ship = CrewSim.system?.GetShipByRegID(id);
        return ship == null || ship.bDestroyed || ship.HideFromSystem || ship.IsStationHidden() ? null :
            new TargetRef { _ship = ship, _shipRegID = id, DisplayName = string.IsNullOrEmpty(ship.publicName) ? id : ship.publicName };
    }

	public static TargetRef FromCrossHair()
	{
		NavPOI crossHairTarget = GUIOrbitDraw.CrossHairTarget;
		if (crossHairTarget == null)
		{
			return null;
		}
		TargetRef targetRef = new TargetRef
		{
			DisplayName = crossHairTarget.name
		};
		if (crossHairTarget.Ship != null)
		{
			targetRef._ship = crossHairTarget.Ship;
			targetRef._shipRegID = crossHairTarget.Ship.strRegID;
			return targetRef;
		}
		if (crossHairTarget.bodyOrbit != null)
		{
			targetRef._body = crossHairTarget.bodyOrbit;
			return targetRef;
		}
		if (crossHairTarget.stellarObj != null)
		{
			targetRef._stellar = crossHairTarget.stellarObj;
			return targetRef;
		}
		double fTargetFuture = crossHairTarget.fTargetFuture;
		crossHairTarget.fTargetFuture = 0.0;
		Point sXY = crossHairTarget.GetSXY(out var _, out var _);
		crossHairTarget.fTargetFuture = fTargetFuture;
		targetRef._fx = sXY.X;
		targetRef._fy = sXY.Y;
		targetRef._isFixed = true;
		return targetRef;
	}

	private Ship LiveShip()
	{
		if (_shipRegID == null)
		{
			return null;
		}
        // Phobos: the qualified ID must resolve in the current world, never
        // a cached object from a removed/replaced ship or a previous save.
        _ship = CrewSim.system?.GetShipByRegID(_shipRegID);
		if (_ship == null || _ship.bDestroyed || _ship.HideFromSystem || _ship.IsStationHidden())
		{
			return null;
		}
		return _ship;
	}

	public bool Resolve(out double px, out double py, out double vx, out double vy)
	{
		px = (py = (vx = (vy = 0.0)));
		try
		{
			if (_isFixed)
			{
				px = _fx;
				py = _fy;
				return true;
			}
			if (_body != null)
			{
				_body.UpdateTime(StarSystem.fEpoch);
				px = _body.dXReal;
				py = _body.dYReal;
				vx = _body.dVelX;
				vy = _body.dVelY;
				return true;
			}
			ShipSitu targetSitu = TargetSitu;
			if (targetSitu == null)
			{
				return false;
			}

			px = targetSitu.vPosx;
			py = targetSitu.vPosy;
			vx = targetSitu.vVelX;
			vy = targetSitu.vVelY;
			return true;
		}
		catch (Exception ex)
		{
			Plugin.Verbose("target resolve failed: " + ex.Message);
			return false;
		}
	}

	public bool ResolveAt(double dtAhead, out double px, out double py, out double vx, out double vy)
	{
		px = (py = (vx = (vy = 0.0)));
		if (dtAhead < 0.0)
		{
			dtAhead = 0.0;
		}
		try
		{
			if (_isFixed)
			{
				px = _fx;
				py = _fy;
				return true;
			}
			if (_body != null)
			{
				Point point = PredictBody(dtAhead);
				Point point2 = PredictBody(dtAhead + 0.5);
				Point point3 = PredictBody(Math.Max(0.0, dtAhead - 0.5));
				double num = dtAhead + 0.5 - Math.Max(0.0, dtAhead - 0.5);
				px = point.X;
				py = point.Y;
				vx = (point2.X - point3.X) / num;
				vy = (point2.Y - point3.Y) / num;
				return true;
			}
			ShipSitu targetSitu = TargetSitu;
			if (targetSitu == null)
			{
				return false;
			}

			Point predictedPosition = targetSitu.GetPredictedPosition(dtAhead);
			Point predictedPosition2 = targetSitu.GetPredictedPosition(dtAhead + 0.5);
			Point predictedPosition3 = targetSitu.GetPredictedPosition(Math.Max(0.0, dtAhead - 0.5));
			double num2 = dtAhead + 0.5 - Math.Max(0.0, dtAhead - 0.5);
			px = predictedPosition.X;
			py = predictedPosition.Y;
			vx = (predictedPosition2.X - predictedPosition3.X) / num2;
			vy = (predictedPosition2.Y - predictedPosition3.Y) / num2;
			return true;
		}
		catch (Exception ex)
		{
			Plugin.Verbose("target predict failed: " + ex.Message);
			return false;
		}
	}

	private Point PredictBody(double dt)
	{
		_body.UpdateTime(StarSystem.fEpoch + dt, bCorrectTimes: true, bCalcV: false);
		Point result = new Point(_body.dXReal, _body.dYReal);
		_body.UpdateTime(StarSystem.fEpoch, bCorrectTimes: true, bCalcV: false);
		return result;
	}
}
