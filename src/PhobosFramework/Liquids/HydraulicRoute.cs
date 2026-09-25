using System;

namespace Phobos.Ostranauts.Framework.Liquids;

/// <summary>Bounded authored quadratic resistance, not CFD or a measured pressure observation.</summary>
public static class HydraulicRoute
{
    public static double FlowFraction(int cells,double equivalentCells)
    {
        if(cells<1||double.IsNaN(equivalentCells)||double.IsInfinity(equivalentCells)||equivalentCells<=0) throw new ArgumentOutOfRangeException();
        return 1/Math.Sqrt(1+cells/equivalentCells);
    }
    public static double Share(double budget,int consumers)
    {
        if(consumers<1||double.IsNaN(budget)||double.IsInfinity(budget)||budget<0) throw new ArgumentOutOfRangeException();
        return budget/consumers;
    }
}
