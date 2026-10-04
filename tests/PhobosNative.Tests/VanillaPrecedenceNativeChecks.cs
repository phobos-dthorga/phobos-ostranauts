using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;
using PhobosAgriculture;

/// <summary>Round 2 of the vanilla-precedence audit (28 September 2026): Agriculture and Shipbreaker let the
/// game's own definitions, power state, eating chain and uninstall rule lead. Runs after the Agriculture
/// definitions have published against vanilla data with no Ship's Water loaded.</summary>
internal static class VanillaPrecedenceNativeChecks
{
    internal static void Run(NativeDefinitions agriculture, Action<bool, string> check)
    {
        // Native power state: the game sets and clears IsPowered only for power info naming a power-on interaction.
        foreach (string prefix in new[] { Definitions.Rack, Definitions.Cooker, IrrigationDefinitions.Supply, WorkupDefinitions.Bench })
        {
            var power = agriculture.Power[prefix + "Power"];
            check(power.strIntPowerOn == prefix + "PowerChange" && power.strIntPowerOff == prefix + "PowerChange" &&
                agriculture.Interactions.TryGetValue(prefix + "PowerChange", out var change) && change.strThemType == "Self",
                "Appliance power info names a self-targeted power-change action, so the game maintains IsPowered: " + prefix);
        }
        check(typeof(JsonPowerInfo).GetProperty("strIntPowerOn") != null || typeof(JsonPowerInfo).GetField("strIntPowerOn") != null,
            "Native power info still carries the power-on interaction name the game checks");
        check(!agriculture.Power.ContainsKey(BulkDefinitions.Tank + "Power") && !agriculture.Interactions.ContainsKey(BulkDefinitions.Tank + "PowerChange"),
            "The unpowered water tank keeps neither power info nor a power-change action");

        // Optional providers gate on the trigger table; the game's lookup returns Blank (always true) for an unknown name.
        // The native lookup's Blank fallback logs through Unity and cannot run here; the decompile shows it.
        check(NativeDefinitions.Trigger(RecyclerCapture.RecyclerTrigger) == null && !DataHandler.dictCTs.ContainsKey(RecyclerCapture.RecyclerTrigger) &&
            DataHandler.dictCTs.ContainsKey("Blank") && !RecyclerCapture.IsRecycler(null),
            "Without Ship's Water the recycler rule is absent, the game's Blank fallback exists, and nothing counts as a recycler");
        check(NativeDefinitions.Trigger("TIsFood") != null && NativeDefinitions.Trigger("") == null, "Existing rules resolve; empty names do not");
        check(!agriculture.Objects.Keys.Any(id => !id.StartsWith("Phobos", StringComparison.Ordinal)), "Agriculture republishes no native object definition by name");

        // Authored food values ride the game's own direct-eating chain.
        foreach (string food in new[] { "PhobosVerdemorrowHearthPotatoes", "PhobosVerdemorrowLettuce" })
        {
            string identity = "Is" + food, reply = food + "AllowDirect";
            check(agriculture.Objects[food].aStartingConds.Contains(identity + "=1x1") && agriculture.Conditions.ContainsKey(identity), "Food carries its identity condition: " + food);
            check(agriculture.Triggers["TIs" + food].aReqs.SequenceEqual(new[] { identity }) && agriculture.Triggers["TIs" + food].fChance == 1, "Identity trigger requires only the identity: " + food);
            var eat = agriculture.Interactions[reply];
            var template = DataHandler.dictInteractions[Definitions.EatTemplate];
            check(eat.CTTestUs == "TIs" + food && eat.LootCTsThem == food + "Effects" && eat.LootCondsThem == template.LootCondsThem &&
                eat.aLootItms.SequenceEqual(template.aLootItms) && agriculture.Loot[food + "Effects"].strType == "trigger",
                "Eating reply is the vanilla reply with our effects loot and the native removal/recently-ate effects: " + food);
            foreach (string parent in Definitions.EatOpeners)
            {
                var opener = DataHandler.dictInteractions[parent];
                int ours = Array.FindIndex(opener.aInverse, e => DefinitionAmendments.ReplyName(e) == reply);
                int vanilla = Array.FindIndex(opener.aInverse, e => DefinitionAmendments.ReplyName(e) == Definitions.EatTemplate);
                check(ours >= 0 && vanilla > ours && opener.aInverse.Count(e => DefinitionAmendments.ReplyName(e) == reply) == 1,
                    "Our reply is listed once before the vanilla direct-eating reply in " + parent + ": " + food);
            }
        }
        var openerBefore = DataHandler.dictInteractions["SeekFoodDirect"].aInverse.ToArray();
        Definitions.Prepare().Publish();
        check(DataHandler.dictInteractions["SeekFoodDirect"].aInverse.SequenceEqual(openerBefore), "Repeated publication inserts no second eating reply");

        // Shipbreaker wall reclamation follows the game's own uninstall rule for the wall.
        var wallRule = NativeDefinitions.Trigger(PhobosShipbreaker.ReclamationGeometry.VanillaWallRule);
        check(wallRule != null && wallRule.aReqs.Contains("IsWall1x1") && wallRule.aReqs.Contains("IsInstalled") && wallRule.aForbids.Contains("IsDamaged") && !wallRule.aForbids.Contains("StatDamage"),
            "The vanilla wall uninstall rule (installed and not damaged, no wear threshold) is the rule G4 now shares");
    }
}
