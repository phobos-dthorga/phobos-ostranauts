using PhobosShipbreaker.Core;
namespace PhobosShipbreaker;
internal sealed class CollectorPanel
{
    internal CollectorPanel(CollectorService service) { }
    internal bool Show(CondOwner port)=>RoutingRules.IsReceiver(port.strCODef)&&CollectorService.EndpointAccess(port)==null&&IndustrialPanel.Open(port);
    internal bool ShowSource(CondOwner source,bool metals=false)=>(!metals||ProcessingService.IsReclaimer(source))&&RoutingRules.IsSender(source.strCODef)&&CollectorService.EndpointAccess(source)==null&&IndustrialPanel.Open(source);
    internal void Reset() { }
    internal void Draw() { }
}
