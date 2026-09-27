using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Registration;

internal static class ItemHandlingChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var packs = new[] { PhobosShipbreaker.Content.Prepare(), PhobosAgriculture.Definitions.Prepare(), PhobosAutoNav.EquipmentContent.Prepare() };
        foreach (var d in packs)
        foreach (var co in d.Objects.Values.Where(c => c.strName.StartsWith("Phobos",StringComparison.Ordinal)))
        {
            var flags=(co.aStartingConds??Array.Empty<string>()).Select(s=>s.Split('=')[0]).ToArray();
            var actions=co.aInteractions??Array.Empty<string>();
            var slots=(co.mapSlotEffects??Array.Empty<string>()).Where((s,i)=>i%2==0).ToArray();
            bool installed=flags.Contains("IsInstalled"), system=flags.Contains("IsSystem");
            if (system) { check(!actions.Contains("PickupItem"),"No pickup on internal compartment: "+co.strName); continue; }
            if (installed)
                check(!actions.Contains("PickupItem")&&!actions.Contains("DropItem")&&!slots.Contains("drag"),"Installed equipment cannot be carried: "+co.strName);
            else
            {
                check(actions.Contains("PickupItem")&&actions.Contains("DropItem"),"Loose cargo has native pickup/drop: "+co.strName);
                check(actions.Contains("PickupItemStack")== (co.nStackLimit>1),"Stack action matches physical stack limit: "+co.strName);
                if(flags.Contains("IsCumbersome")) check(slots.SequenceEqual(new[]{"drag"}),"Bulky cargo is drag-only: "+co.strName);
                else check(slots.Length>0,"Loose portable cargo has a usable slot: "+co.strName);
            }
        }
        var industrial=packs[0];
        foreach(string id in new[]{"PhobosShipbreakerSection","PhobosScrapReclaimerSection","PhobosFurnaceSection"})
        {
            var co=industrial.Objects[id];
            check(co.aStartingConds.Any(s=>s.StartsWith("IsCumbersome=")),"Heavy section marked cumbersome: "+id);
            check(industrial.Installables.Values.Any(j=>j.strActionCO==id&&j.strJobType=="dismantle"),"Section retains dismantling: "+id);
            var saved=new JsonCondOwnerSave { strID="original",strCODef=id,strSlotName="heldR",aConds=new[]{"StatMass=1x80","StatDamage=1x3","IsCumbersome=1x0"},aCondZeroes=new[]{"IsCumbersome","IsPristine"} };
            var upgraded=ItemHandling.Upgrade(saved);
            check(upgraded!=saved&&upgraded.strID==saved.strID&&upgraded.strSlotName=="heldR"&&upgraded.aConds.Contains("StatDamage=1x3")&&upgraded.aConds.Contains("StatMass=1x80"),"Handling correction preserves identity, placement, wear and mass");
            check(EquipmentSaveUpgrade.Amount(upgraded.aConds,"IsCumbersome")==1&&!upgraded.aCondZeroes.Contains("IsCumbersome"),"Saved conditions receive effective cumbersome flag");
            check(saved.aConds.Contains("IsCumbersome=1x0")&&saved.aCondZeroes.Contains("IsCumbersome"),"Source save DTO is untouched");
            check(ReferenceEquals(upgraded,ItemHandling.Upgrade(upgraded)),"Handling upgrade is idempotent");
            check(ItemHandling.LegacyHand(upgraded),"Existing saved hand placement is recognized");
            upgraded.strSlotName="drag";check(!ItemHandling.LegacyHand(upgraded),"Drag placement grants no hand exception");
            saved.aConds=new[]{"DEFAULT"};check(ItemHandling.Upgrade(saved).aConds.Contains("DEFAULT"),"Compressed defaults remain intact");
        }
        var foreign=new JsonCondOwnerSave{strCODef="ItmScrapSteel",aConds=new[]{"DEFAULT"},strSlotName="heldR"};
        check(ReferenceEquals(foreign,ItemHandling.Upgrade(foreign))&&!ItemHandling.LegacyHand(foreign),"Foreign items never receive handling migration");
        var broken=packs[1].Objects[PhobosAgriculture.BulkDefinitions.Tank+"InstalledDmg"];
        check(!broken.aInteractions.Any(a=>a.StartsWith("PhobosAgriculture_bulk-")),"Broken reservoir offers no operating work");
        check(typeof(Slots).GetMethod("UnSlotItem",new[]{typeof(string),typeof(CondOwner),typeof(bool)})?.ReturnType==typeof(CondOwner),"Legacy release hook matches native unslot boundary");
    }
}
