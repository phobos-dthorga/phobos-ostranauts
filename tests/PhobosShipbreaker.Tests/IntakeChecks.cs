using Phobos.Ostranauts.Framework.Inventory;
using System;
using System.Linq;
using System.Text.Json;
using PhobosShipbreaker.Core;

internal static class IntakeChecks
{
    internal static void Run(Action<bool, string> check, Action<Action, string> throws, JsonElement[] recipes)
    {
        foreach (double a in new[] { 0.0, 90, 180, 270, -90 })
        {
            var c = IntakeRules.Rotate(0, -2, a); var p = IntakeRules.Rotate(0, -4.5, a);
            check(IntakeRules.Connected(10, 20, a, 10+c.X, 20+c.Y, a, 10+p.X, 20+p.Y, a+180), "All cardinal hull orientations connect");
            check(IntakeRules.Connected(10, 20, a, 10+c.X, 20+c.Y, a+180, 10+p.X, 20+p.Y, a+180), "Symmetric chute can be reversed");
            check(!IntakeRules.Connected(10, 20, a, 10+c.X, 20+c.Y, a, 11+p.X, 20+p.Y, a+180), "Offset processor does not teleport materials");
            check(!IntakeRules.Connected(10, 20, a, 10+c.X, 20+c.Y, a, 10+p.X, 20+p.Y, a), "Processor mouth must face chute");
            check(!IntakeRules.Connected(10, 20, a, 10+c.X, 20+c.Y, a+90, 10+p.X, 20+p.Y, a+180), "Perpendicular chute is disconnected");
        }
        var clock = new TransferClock("panel-A", 5);
        check(clock.Advance("panel-A", 2, true) && clock.Progress == 2, "Powered intake advances");
        check(clock.Advance("panel-A", 20, false) && clock.Progress == 2, "Blackout earns no transfer work");
        foreach (double elapsed in new[] { -1, 61, double.NaN, double.PositiveInfinity })
            check(!clock.Advance("panel-A", elapsed, true) && clock.Progress == 2, "Clock discontinuity cannot finish a transfer");
        check(!clock.Advance("panel-B", 3, true) && clock.Progress == 2, "Replacement panel cannot inherit pending motion");
        check(clock.Advance("panel-A", 10, true) && clock.Complete && clock.Progress == 5, "Completed motion caps credit");
        check(new TransferClock("panel-A", 5).Progress == 0, "Reload/re-arm starts a fresh delay without transforming material");
        foreach (double seconds in new[] { 0, 61, double.NaN, double.NegativeInfinity })
            throws(() => new TransferClock("wall", seconds), "Invalid intake setting rejected");
        foreach (var pair in new[] { (Id: "PhobosBuildHullChute", Kg: IntakeRules.ChuteKg), (Id: "PhobosBuildExteriorGrabber", Kg: IntakeRules.GrabberKg) })
        {
            var recipe = recipes.Single(r => r.GetProperty("id").GetString() == pair.Id);
            double input = recipe.GetProperty("ingredients").EnumerateArray().Sum(i => i.GetProperty("count").GetInt32() * i.GetProperty("unitMassKg").GetDouble());
            double output = recipe.GetProperty("outputs").EnumerateArray().Sum(i => i.GetProperty("count").GetInt32() * i.GetProperty("unitMassKg").GetDouble());
            check(ProcessRules.MassMatches(input, pair.Kg) && ProcessRules.MassMatches(output, pair.Kg), pair.Id + " retains its full material bill");
        }
        check(Command.Parse("phobosshipbreaker feed machine").Action == CommandAction.Feed, "Manual feed has an unambiguous console entry");
        check(Command.Parse("phobosshipbreaker products machine").Action == CommandAction.Products, "Products use the same checked service");
    }
}
