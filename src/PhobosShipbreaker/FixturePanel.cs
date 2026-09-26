using System.Linq;
using UnityEngine;
namespace PhobosShipbreaker;
internal sealed class FixturePanel
{
    private readonly Settings options;
    internal FixturePanel(ProcessingService service,Settings options)=>this.options=options;
    internal void Update()
    {
        if(CrewSim.Typing||!Input.GetKeyDown(options.ControlsKey)||CrewSim.objInstance==null||!CrewSim.objInstance.FinishedLoading)return;
        if(CrewSim.goUI?.GetComponent<IndustrialPanel>()!=null){CrewSim.LowerUI();return;}
        var machine=ProcessingService.FindMachines().Where(c=>ProcessingService.AccessProblem(c)==null).OrderBy(c=>TileUtils.TileRange(CrewSim.GetSelectedCrew().GetPos(),c.GetPos("use"))).FirstOrDefault();
        if(machine!=null)IndustrialPanel.Open(machine);
    }
    internal void Draw() { }
}
