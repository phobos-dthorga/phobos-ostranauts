using System;
using System.Globalization;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Inventory;
using Phobos.Ostranauts.Framework.Processing;
using PhobosShipbreaker.Core;

namespace PhobosShipbreaker;

internal static class PanelConfiguration
{
    internal static string Stamp(CondOwner co)=>ConfigurationStamp.For(co,"PhobosMaterialPort.","PhobosMaterialFilter.","PhobosState.crew-order","PhobosState.FurnaceCoolingMode","PhobosState.Shipbreaker.Capture")+
        (FurnaceRules.Machine(co.strCODef)?FurnaceService.SettingsStamp(co)+"|"+FurnaceService.Get(co).Coolant.Enabled:"")+
        (ProcessingService.IsGrabber(co)?"|"+GUIOrbitDraw.CrossHairTarget?.Ship?.strRegID:"");
    internal static string Peer(CondOwner co,bool sending,bool metals=false)=>PortPairing.Read(sending?CollectorService.Sender(co,RoutingRules.OutputPort(co.strCODef,metals)):CollectorService.Receiver(co)).PeerObjectId;
    internal static bool Apply(ConsoleBinding? binding,CondOwner co,string expected,string action,string? value,out string reason)
    {
        reason=ConsoleText.Get("stale");if(co.bDestroyed||Stamp(co)!=expected)return false;
        bool saved=IndustryService.Run(binding,co.strID,action,value,out reason);if(saved)ConfigurationStamp.SuspendChangedOrder(co);return saved;
    }
}
internal static partial class FurnaceService
{
    // A broken route is still a saved selection. Presentation must permit locating/clearing it.
    internal static string CoolingSelection(CondOwner co)=>PortPairing.Read(Port(co)).PeerObjectId;
    internal static string SettingsStamp(CondOwner co)
    {var b=Get(co).State.Batch;return string.Join("|",b.HeatCapKW.ToString("R",CultureInfo.InvariantCulture),b.RampKPerSecond.ToString("R",CultureInfo.InvariantCulture),b.CoolingCapKW.ToString("R",CultureInfo.InvariantCulture),b.StepMode,b.Phase);}
    internal static bool ApplySettings(ConsoleBinding? binding,CondOwner co,string expected,double heat,double ramp,double cool,out string reason)
    {
        reason=ProcessingService.AccessProblem(co,binding)??"";if(reason.Length>0)return false;
        var s=Get(co);reason=ConsoleText.Get("stale");if(expected!=SettingsStamp(co))return false;
        reason=Text.Get("Furnace.protected");if(s.Protected||!Content.Ready||!Intact(co)||co.HasCond("IsLocked"))return false;
        foreach(var item in new[]{(heat,FurnaceRules.MinPowerSettingKW,FurnaceRules.HeatLimitKW),(ramp,FurnaceRules.MinRamp,FurnaceRules.MaxRamp),(cool,FurnaceRules.MinPowerSettingKW,FurnaceRules.CoolingKW)})
            if(!ThermalMath.Finite(item.Item1)||item.Item1<item.Item2||item.Item1>item.Item3){reason=Text.Get("Furnace.range",item.Item2,item.Item3);return false;}
        var b=s.State.Batch;double oldHeat=b.HeatCapKW,oldRamp=b.RampKPerSecond,oldCool=b.CoolingCapKW;
        b.HeatCapKW=heat;b.RampKPerSecond=ramp;b.CoolingCapKW=cool;Save(s);
        if(s.Protected){b.HeatCapKW=oldHeat;b.RampKPerSecond=oldRamp;b.CoolingCapKW=oldCool;reason=Text.Get("Furnace.protected");return false;}
        ConfigurationStamp.SuspendChangedOrder(co);reason=ConsoleText.Get("applied");return true;
    }
}
