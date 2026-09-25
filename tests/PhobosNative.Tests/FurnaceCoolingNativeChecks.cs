using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Ostranauts.Trading;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Persistence;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

internal static class FurnaceCoolingNativeChecks
{
    internal static void Run(NativeDefinitions prepared, string repo, Action<bool,string> check)
    {
        var item = prepared.Items[FurnaceRules.ThermalPort + "Installed"];
        check(item.nCols == 1 && item.aSocketAdds.Length == 1 && item.aSocketReqs.Length == 9 && item.aSocketReqs[4] == "TILFloor", "Port is one native tile on the existing sealed floor");
        check(item.aSocketAdds[0] == "TILFixtureAdds" && !prepared.Objects[FurnaceRules.ThermalPort + "Installed"].aStartingConds.Any(s => s.StartsWith("IsFloor")), "Port does not manufacture a floor or pressure boundary");
        var adds = DataHandler.dictLoot["TILFloor"].aCOs;
        var broken = DataHandler.dictLoot["TILFloorDmgAdds"].aCOs;
        check(adds.Any(s=>s.StartsWith("IsFloorSealed=")) && broken.Any(s=>s.StartsWith("IsEVATile=")), "Support checks follow real sealed and damaged native floor sockets");
        var floor = new DataCO(DataHandler.dictCOs["ItmFloorGrate01"]);
        check(floor.HasCond("IsFloorGrate") && floor.HasCond("IsInstalled") && !floor.HasCond("IsDamaged"), "Native intact floor is recognized by physical support lookup");
        check(new DataCO(DataHandler.dictCOs["ItmFloorGrate01Dmg"]).HasCond("IsDamaged"), "Damaged floor cannot pass intact support lookup");
        var port = EquipmentEconomy.Machines.Single(s => s.Prefix == FurnaceRules.ThermalPort);
        var radiator = EquipmentEconomy.Machines.Single(s => s.Prefix == FurnaceRules.Radiator);
        check(port.Price == radiator.Price && port.Install == radiator.Install && port.Uninstall == radiator.Uninstall && port.Repair == radiator.Repair &&
            port.Dismantle == radiator.Dismantle && port.RepairBill.SequenceEqual(radiator.RepairBill) && port.Salvage.SequenceEqual(radiator.Salvage) && port.BrokenSalvage.SequenceEqual(radiator.BrokenSalvage), "Complete underside assembly retains exterior radiator economics");
        foreach (string state in new[] { "Installed", "InstalledDmg", "Loose", "LooseDmg" })
        {
            var co = prepared.Objects[FurnaceRules.ThermalPort + state];
            check(EquipmentSaveUpgrade.Amount(co.aStartingConds, "StatMass") == 100 && co.inventoryWidth == 1 && co.inventoryHeight == 1, "All port forms retain complete assembly mass and intended dimensions");
            check(FurnaceRules.Cooling(co.strName) && IndustrialRules.Equipment(co.strName), "Port uses shared furnace cooling classification");
            if (state.StartsWith("Installed")) check(co.aInteractions.Contains(IndustrialRules.LocalControls), "Installed ports expose checked local controls");
        }
        // PNG header dimensions, without loading the Unity renderer.
        foreach (var pair in new[] { ("",16), ("Normal",16), ("Portrait",256) })
        {
            byte[] png = File.ReadAllBytes(Path.Combine(repo,"mods/PhobosShipbreaker/images/phobos/shipbreaker/PhobosFurnaceThermalPort" + pair.Item1 + ".png"));
            int Size(int offset) => png[offset] << 24 | png[offset+1] << 16 | png[offset+2] << 8 | png[offset+3];
            check(Size(16) == pair.Item2 && Size(20) == pair.Item2, "Port derivatives match native world and portrait scale");
        }
        foreach (string kind in new[] { FurnaceRules.Radiator, FurnaceRules.ThermalPort })
        {
            var a = new Dictionary<string,Dictionary<string,string>>(); var b = new Dictionary<string,Dictionary<string,string>>();
            var f = new MaterialPort("furnace", "PhobosFurnace.Cooling", a); var c = new MaterialPort(kind, "PhobosFurnace.Cooling", b);
            check(PortPairing.TryLink(f,c,out _), "Either cooling installation can bind the unchanged logical port");
            check(!PortPairing.TryLink(f,new MaterialPort("second",c.PortId,new()),out _) &&
                !PortPairing.TryLink(new MaterialPort("second-furnace",f.PortId,new()),c,out _), "Neither duplicate sink nor duplicate consumer can share a cooling connection");
            var store = new ObjectStateStore(b,"Furnace",Text.Owner,1);
            check(store.TryWrite(FurnaceCooling.Save(12345.6789)), "Native map retains hot cooling state");
            var save = new JsonItem { aGPMSettings = b.Select(m => new JsonGUIPropMap { strName=m.Key, dictGUIPropMap=DataHandler.ConvertDictToStringArray(m.Value) }).ToArray() };
            var loaded = JsonConvert.DeserializeObject<JsonItem>(JsonConvert.SerializeObject(save))!;
            var maps = loaded.aGPMSettings.ToDictionary(m=>m.strName,m=>DataHandler.ConvertStringArrayToDict(m.dictGUIPropMap));
            var loadedPort = new MaterialPort(c.ObjectId,c.PortId,maps);
            check(PortPairing.Matches(f,loadedPort), "Legacy radiator and new port links survive native serialization");
            var savedStore = new ObjectStateStore(maps,"Furnace",Text.Owner,1);
            check(savedStore.Read(out var fields) == SavedStateStatus.Ready && FurnaceCooling.TryRead(fields,out var heat) && heat == 12345.6789, "Hot store survives native serialization without copying or resetting energy");
        }
    }
}
