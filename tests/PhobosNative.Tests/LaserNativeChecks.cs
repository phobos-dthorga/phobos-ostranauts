using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Phobos.Ostranauts.Framework;
using Phobos.Ostranauts.Framework.Effects;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
using PhobosShipbreaker.Core;

/// <summary>The Ablatine ML-2 mining laser against the game's own data and code: its exterior wall-backed definitions,
/// power, art and firing sheet, and every native contract its work depends on (the mining rule, the rock damage
/// chain, what breaking each mineable object yields, the damage and tile members it calls, and the game's own frame
/// animation).</summary>
internal static class LaserNativeChecks
{
    internal static void Run(NativeDefinitions d, string repo, Action<bool, string> check)
    {
        double Stat(JsonCondOwner co, string key) => EquipmentSaveUpgrade.Amount(co.aStartingConds, key);
        string p = LaserRules.Prefix;
        foreach (string state in MachineFamilies.Forms)
        {
            bool installed = state.StartsWith("Installed", StringComparison.Ordinal), damaged = state.EndsWith("Dmg", StringComparison.Ordinal);
            var co = d.Objects[p + state]; var item = d.Items[co.strItemDef];
            check(item.nCols == 2 && item.aSocketAdds.Length == 4 && co.inventoryWidth == 2 && co.inventoryHeight == 2 && item.fZScale == 0.5f,
                "The ML-2 occupies two by two tiles with the G4's wall-mounted layering: " + state);
            check(item.aSocketAdds.All(s => s == (installed ? "TILExtFixtureAdds" : "TILItemAdds")), "An installed ML-2 is an exterior fixture; a loose one is cargo: " + state);
            check(item.aSocketReqs.Length == 16 && item.aSocketReqs.Select((value, index) => (value, index)).All(x =>
                    x.value == (installed && (x.index == 13 || x.index == 14) ? "TILWall" : "Blank")),
                "An installed ML-2 needs exactly the two hull walls behind it and nothing else: " + state);
            check(item.aSocketForbids.Length == 16 && new[] { 5, 6, 9, 10 }.All(i => item.aSocketForbids[i] == (installed ? "TILObstruction" : "TILItemForbids")),
                "The ML-2 cannot be fitted over an obstruction: " + state);
            check(Stat(co, "StatMass") == LaserRules.MachineKg && Stat(co, "StatBasePrice") == (damaged ? ShipbreakerEconomy.Price(p) / 4 : ShipbreakerEconomy.Price(p)),
                "The ML-2 weighs 120 kg and carries the economy pack price, a quarter when broken: " + state);
            check(co.strNameFriendly.StartsWith("Phobos' Ablatine ML-2 ", StringComparison.Ordinal) && co.strNameFriendly.Contains("Mining Laser"), "The ML-2 carries the Ablatine name: " + state);
            check(co.aSlotsWeHave.Length == 0 && co.strLoot == "Blank" && co.nContainerWidth == 0 && co.strContainerCT == null &&
                !co.aStartingConds.Any(s => s.StartsWith("IsContainer=", StringComparison.Ordinal)), "The ML-2 holds nothing: no feed, tray or compartments: " + state);
            check(!co.aStartingConds.Any(s => s.StartsWith("IsShipWeapon", StringComparison.Ordinal)), "The ML-2 is equipment, never a ship weapon: " + state);
            check(co.mapPoints.Contains("emit,0,16") && co.mapPoints.Contains("use,-8,-40") && co.mapPoints.Contains("PowerA,-8,-24") && co.mapPoints.Contains("PowerB,8,-24"),
                "The emitter is on the outer edge; the crew point is behind the wall and power meets it in the wall row: " + state);
            string runtime = "phobos/shipbreaker/" + LaserDefinitions.Art;
            check(item.strImg == runtime && item.strImgNorm == runtime + "Normal" && item.strImgDamaged == runtime && co.strPortraitImg == runtime && item.objAnimation == null,
                "Every form rests on the plain overhead image; the firing sheet is applied only while it cuts: " + state);
            check((co.jsonPI == p + "Power") == (installed && !damaged) && co.aTickers.Contains("Power") == (installed && !damaged), "Only the intact installed ML-2 draws power: " + state);
            check(co.aInteractions.Count(i => i == IndustrialRules.LocalControls) == (installed ? 1 : 0) && !co.aInteractions.Contains("Inventory"),
                "An installed ML-2 offers its Control Panel and no inventory: " + state);
        }
        var power = d.Power[p + "Power"];
        check(Math.Abs(power.fAmount * Units.SecondsPerHour - LaserRules.IdleKW) < 1e-9 && Math.Abs(power.fOverrideAmount * Units.SecondsPerHour - LaserRules.WorkingKW) < 1e-9 &&
            power.strOverrideCond == LaserRules.Working && power.aInputPts.SequenceEqual(new[] { "PowerA", "PowerB" }) && d.Conditions.ContainsKey(LaserRules.Working),
            "The ML-2 idles at 0.1 kW and draws its working power only under its own working condition");
        check(d.Installables.TryGetValue(p + "LooseInstall", out var install) && install.strBuildType == InstallMenu.Appliances && install.strStartInstall == LaserRules.Installed,
            "The ML-2 installs from the INSTALL menu's APPS tab");
        PowerKinds.Reset();
        check(PowerKinds.Classify(LaserRules.Installed) == PowerKind.Laser && PowerKinds.Classify(LaserRules.Installed + "Dmg") == PowerKind.None &&
            PowerKinds.Classify(p + "Loose") == PowerKind.None, "Only the intact installed ML-2 is routed to the laser's power hooks");
        PowerKinds.Reset();

        // Art: the still image, the firing sheet (one footprint-sized cell per frame) and the beam texture.
        (int Width, int Height) Size(string name)
        {
            var png = File.ReadAllBytes(Path.Combine(repo, "mods", "PhobosShipbreaker", "images", "phobos", "shipbreaker", name + ".png"));
            int Dimension(int offset) => (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
            return (Dimension(16), Dimension(20));
        }
        int cell = LaserRules.Footprint * LaserRules.PixelsPerTile;
        check(Size("PhobosMiningLaser") == (cell, cell) && Size("PhobosMiningLaserNormal") == (cell, cell), "The still image is the two by two footprint at sixteen pixels a tile");
        check(Size("PhobosMiningLaserSheet") == (cell * LaserRules.SheetColumns, cell * LaserRules.SheetRows) && Size("PhobosMiningLaserSheetNormal") == Size("PhobosMiningLaserSheet"),
            "Each cell of the firing sheet and its normal is exactly the footprint, as the game's frame animation requires");
        check(Size("PhobosLaserBeam") == (32, 8), "The beam texture is the small drawn glow");
        check(LaserPresentation.Sheet == "phobos/shipbreaker/PhobosMiningLaserSheet" && LaserPresentation.Beam == "phobos/shipbreaker/PhobosLaserBeam", "Presentation names the exported sheet and beam");
        var animation = LaserPresentation.Animation();
        check(animation.nFrameCount == 8 && animation.strFrameRate == "12" && animation.nSheetColumns == 4 && animation.nSheetRows == 2 && animation.bLoop && !animation.bRandomStartingFrame,
            "The firing animation is eight frames, four by two, looping at a fixed twelve a second");

        // The game's own animation, as its heater uses it, and the members the helper relies on.
        check(DataHandler.dictCOOverlays.TryGetValue("ItmHeater02", out var heater) && heater.objAnimation != null && heater.objAnimation.nSheetColumns == 4 && heater.objAnimation.bLoop,
            "The game's heater still animates through a frame sheet (the precedent this follows)");
        check(SpriteAnimation.Supported && typeof(Item).GetMethod("OnItemAnimationUpdate", BindingFlags.Instance | BindingFlags.NonPublic) != null &&
            typeof(Item).GetField("ItemAnimationUpdate", BindingFlags.Static | BindingFlags.Public)?.FieldType == typeof(UnityEngine.Events.UnityEvent),
            "The game steps item animation from its own per-frame event, which the helper detaches before each change");
        check(typeof(Item).GetMethods().Any(m => m.Name == "SetAlt" && m.GetParameters().Select(x => x.ParameterType).SequenceEqual(
                new[] { typeof(string), typeof(string), typeof(string), typeof(string), typeof(JsonItemAnimation) })) && typeof(Item).GetProperty("ImgOverride") != null,
            "Item.SetAlt still takes an image, its normal, damage image, tint and an animation");
        check(typeof(DataHandler).GetMethod("GetMesh", new[] { typeof(string), typeof(UnityEngine.Transform) }) != null &&
            typeof(DataHandler).GetMethods().Any(m => m.Name == "GetMaterial" && m.GetParameters().Length == 5 && m.GetParameters()[0].ParameterType == typeof(UnityEngine.Renderer)),
            "The game's quad and image-material helpers the beam uses are present");

        // Mining: the laser asks the game's own Mine rule, and every job is one native damage stage.
        var mine = DataHandler.dictCTs[LaserGeometry.MineRule];
        check(mine.aReqs.Contains("IsMineable") && mine.aForbids.Contains("IsOreDeposit") && mine.aTriggers.Contains("TIsDestructable"),
            "The game's Mine rule takes mineable, destructible objects and leaves opened ore deposits to crew");
        check(DataHandler.dictInteractions["ACTMineDeposit"].CTTestThem == "TIsOreDeposit", "Ore deposits keep their own crew action");
        var mineable = DataHandler.dictCOs.Values.Where(c => c.aStartingConds != null && c.aStartingConds.Any(s => s.StartsWith("IsMineable=", StringComparison.Ordinal))).ToArray();
        check(mineable.Length >= 20, "The game's mineable rock, ice and core definitions were found");
        int taken = 0;
        foreach (var rock in mineable)
        {
            var command = (rock.aUpdateCommands ?? Array.Empty<string>()).Where(c => c.StartsWith("Destructable,StatDamage,", StringComparison.Ordinal)).ToArray();
            check(command.Length == 1 && command[0].EndsWith(",StatDamageMax,1.0", StringComparison.Ordinal), "Every mineable object breaks through the destructible damage chain: " + rock.strName);
            string loot = command[0].Split(',')[2];
            bool deposit = rock.aStartingConds.Any(s => s.StartsWith("IsOreDeposit=", StringComparison.Ordinal)), gangue = LaserGeometry.GangueOnly(loot);
            double max = Stat(rock, "StatDamageMax");
            if (deposit || gangue) continue;
            taken++;
            check(LaserRules.AdmitRock(max), "Every worthwhile mineable stage is within the laser's limit: " + rock.strName + " " + max);
        }
        check(taken >= 15, "Rock walls, ice walls and intact cores are all taken");
        check(LaserGeometry.GangueOnly("ACTMineralDestroy") && LaserGeometry.GangueOnly("ACTIceDestroy") && !LaserGeometry.GangueOnly("ACTWallRock011x1Destroy") &&
            !LaserGeometry.GangueOnly("ACTWallRock011x1Dmg") && !LaserGeometry.GangueOnly("ACTWallIce011x1Destroy") && !LaserGeometry.GangueOnly("ACTFloorRockSDamage") &&
            !LaserGeometry.GangueOnly("NoSuchLoot"), "Bare rock floor, ice floor and rubble only leave gangue and are refused; walls and cores are not");
        check(LaserGeometry.Gangue.All(DataHandler.dictCOs.ContainsKey), "The gangue items are the game's own");
        foreach (int n in Enumerable.Range(1, 6))
        {
            string wall = "ItmWallRock0" + n + "1x1";
            check(Stat(DataHandler.dictCOs[wall], "StatDamageMax") == 15 && DataHandler.dictInteractions["MSWallRock0" + n + "1x1Dmg"].objLootModeSwitch == wall + "Dmg",
                "A rock wall cracks into its damaged form after fifteen points: " + wall);
        }
        check(DataHandler.dictInteractions["MSWallRock011x1Destroy"].objLootModeSwitch == "ItmRock01Salvage" && DataHandler.dictLoot["ItmRock01Salvage"].aLoots.Single().Contains("ItmMiningTrash"),
            "A destroyed rock wall rolls the game's own ore-or-gangue table; the laser adds nothing to it");
        check(Stat(DataHandler.dictCOs["ItmWallRock011x1"], "StatDamageMax") + Stat(DataHandler.dictCOs["ItmWallRock011x1Dmg"], "StatDamageMax") <= 45,
            "A whole rock wall is at most the 45 points the guide quotes");

        // The native members the service and Framework call.
        check(typeof(Destructable).GetMethod("DmgLeft", new[] { typeof(string) })?.ReturnType == typeof(double) && typeof(Destructable).GetMethod("DamageCheck", Type.EmptyTypes) != null &&
            typeof(Destructable).GetMethod("ScheduleDamageCheck", Type.EmptyTypes) != null && typeof(Destructable).GetMethod("GetDmgLoot", new[] { typeof(string) }) != null,
            "Destructable exposes the damage it has left, its check and its loot name");
        var ray = typeof(Ostranauts.Ships.DamageSystem).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(m => m.Name == "DamageRay").ToArray();
        check(ray.Length == 1 && ray[0].GetParameters().Length == 7 && typeof(Ship).GetProperty("DamageSystem") != null | typeof(Ship).GetField("DamageSystem") != null, "The game's damage ray, whose per-hit chain NativeDamage mirrors, keeps its shape");
        check(typeof(CrewSim).GetField("BulletTrail", BindingFlags.Static | BindingFlags.Public) != null && typeof(CrewSim).GetMethod("SpawnTrail") != null, "The game's tracer trail is available");
        check(typeof(Ship).GetMethod("GetTileAtWorldCoords1", new[] { typeof(float), typeof(float), typeof(bool), typeof(bool) }) != null &&
            typeof(Tile).GetProperty("IsWall") != null && typeof(Tile).GetProperty("IsShipTileOrSub") != null && typeof(Tile).GetProperty("IsFixture") != null &&
            typeof(Tile).GetField("bPassable") != null && typeof(Ship).GetMethod("GetDockedShipsAndPortIDs", Type.EmptyTypes) != null &&
            typeof(Ship).GetMethod("GetPeople", new[] { typeof(bool) }) != null && typeof(Ship).GetField("Classification")?.FieldType == typeof(Ship.TypeClassification),
            "The tile, attachment, people and classification members the arc reads are present");
        check(DataHandler.dictCOs.TryGetValue(LaserGeometry.MooringPort, out var port) && port.aStartingConds.Any(s => s.StartsWith("IsMooringPort=", StringComparison.Ordinal)),
            "The game's mooring port, which the laser keeps clear of, is the definition it names");
        check(DataHandler.dictLoot["TILExtFixtureAdds"].aCOs.Any(s => s.StartsWith("IsFixtureExt=", StringComparison.Ordinal)), "Exterior fixtures mark their tiles, so our own are seen as in the way");
        // Freed panels use the same native uninstall contract as the G4; the reclamation checks verify that chain.
        // The radiator link (0.61.0): the F6's cooling assemblies are what a head pairs with.
        var radiatorItem = d.Items[d.Objects[FurnaceRules.Radiator + "Installed"].strItemDef]; var portItem = d.Items[d.Objects[FurnaceRules.ThermalPort + "Installed"].strItemDef];
        check(radiatorItem.nCols == 6 && radiatorItem.aSocketAdds.Length == 24 && portItem.nCols == 1 && portItem.aSocketAdds.Length == 1,
            "The cooling assemblies keep the footprints the touching rule reads: six by four and one tile");
        check(FurnaceRules.Cooling(FurnaceRules.Radiator + "Installed") && FurnaceRules.Cooling(FurnaceRules.ThermalPort + "InstalledDmg") && !FurnaceRules.Cooling(LaserRules.Installed) &&
            !FurnaceService.IsEquipmentDefinition(LaserRules.Installed), "A head pairs with the furnace family's cooling assemblies and is not one of that family itself");
        check(Math.Abs(power.fOverrideAmount * Units.SecondsPerHour - LaserRules.WorkingKW) < 1e-9 && LaserRules.HighKW > LaserRules.WorkingKW,
            "The power info still asks for the standard draw; the high setting is scaled from it in the power step, as the G4 scales its cutter");
    }
}
