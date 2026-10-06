using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Phobos.Ostranauts.Framework.Trading;

/// <summary>Framework 0.123.0 fair gig deadlines against the game's own code and data: the methods the patches hook,
/// the fields the allowance writes and reads, the torch rating the fleet uses, and the gig templates whose far deliveries
/// the game times by distance. No gig is generated and no game session runs.</summary>
internal static class GigNativeChecks
{
    internal static void Run(string native, Action<bool, string> check)
    {
        const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        check(typeof(GigManager).GetMethod("CheckLocal", any, null, new[] { typeof(JsonJobSave), typeof(CondOwner), typeof(double) }, null)?.ReturnType == typeof(bool),
            "The game still prices a gig offer through CheckLocal(JsonJobSave, CondOwner, double)");
        check(typeof(GUIJobs).GetMethod("GetDisplayGigText", any, null, new[] { typeof(JsonJobSave) }, null)?.ReturnType == typeof(string),
            "The Gig Nexus still builds an offer's text in GetDisplayGigText(JsonJobSave)");
        foreach (var field in new[] { "fTimeMult", "bTaken", "strRegIDPickup", "strRegIDDropoff", "fEpochExpired" })
            check(typeof(JsonJobSave).GetField(field, any) != null || typeof(JsonJobSave).GetProperty(field, any) != null, "A gig still keeps " + field);
        check(typeof(GigManager).GetMethod("GetTier", any) != null && typeof(GigManager).GetMethod("TakeJob", any) != null,
            "The game still sets the deadline and the speed bonuses from the gig's own time allowance");
        check(typeof(Ostranauts.ShipGUIs.NavStation.NavModTorchDrive).GetMethod("GetLimiterSafetyMax", new[] { typeof(Ship) })?.ReturnType == typeof(float) &&
              typeof(Ship).GetMethod("GetMaxTorchThrust", new[] { typeof(float) }) != null && typeof(Ship).GetField("fFusionThrustMax") != null,
            "The game still rates a torch drive by its 2 g limiter and full thrust");

        // The game's own templates: far deliveries carry a positive duration the allowance scales.
        string jobs = Path.Combine(native, "jobs", "jobs", "jobs.json");
        var templates = Newtonsoft.Json.JsonConvert.DeserializeObject<JsonJob[]>(File.ReadAllText(jobs))!;
        var courier = templates.FirstOrDefault(t => t.strName == "MVPCourierTestDeliverCO");
        check(courier != null && courier.fDuration > 0, "The game's courier gig has a duration in hours for the allowance to scale");
        check(GigTimeRules.GameHoursPerAu == 70 && GigTimeRules.FerryHoursPerAu == 80 && Math.Abs(GigTimeRules.FarAu * 149597872.0 - 5000) < 1e-2,
            "Framework reads the game's own figures: 70 hours per AU for gigs, 80 for its ferry, far beyond 5,000 km");
    }
}
