using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Construction;
using Phobos.Ostranauts.Framework.Registration;
using PhobosShipbreaker;
int checks=0;
void Check(bool result,string why){if(!result)throw new Exception(why);checks++;}
CondOwner Part(string id="Section") {var p=new CondOwner{strCODef=id};p.SetCondAmount("StatMass",80);return p;}
var d=new NativeDefinitions();d.Objects["Section"]=new(){strName="Section",aStartingConds=new[]{"StatMass=1x80"}};d.Objects["Machine"]=new(){strName="Machine",aStartingConds=new[]{"StatMass=1x240"}};
SectionAssembly.Add(d,"PhobosAssembly","Section","SectionTrigger","Machine",3,80,5000,new[]{"Mortorq","Solder"});
string selector=d.Installables["PhobosAssembly"].CTThem;
var good=Part();Check(SectionAssembly.Select(selector,good,true),"Existing section without new progress fields is eligible");
Check(!SectionAssembly.Select(selector,good,false),"Native exclusion cannot be overridden");
Check(SectionAssembly.Select("Foreign",good,true),"Unrelated native selectors untouched");
foreach (string? name in new string?[] { null, "", "Foreign" })
foreach (bool nativeResult in new[] { false, true })
    Check(SectionAssembly.Select(name, null!, nativeResult) == nativeResult,
        "Unnamed or unrelated native triggers preserve their result without reading a target");
foreach(string flag in new[]{"IsInstalled","IsDamaged"}) {good.SetCondAmount(flag,1);Check(!SectionAssembly.Select(selector,good,true),"Invalid section flag: "+flag);good.ZeroCondAmount(flag);}
good.SetCondAmount("StatMass",79);Check(!SectionAssembly.Select(selector,good,true),"Changed mass rejected");good.SetCondAmount("StatMass",80);
good.children.Add(Part("Cargo"));Check(!SectionAssembly.Select(selector,good,true),"Hidden cargo prevents consumption");good.children.Clear();
good.lot.Add(Part());Check(!SectionAssembly.Select(selector,good,true),"Reserved material prevents consumption");good.lot.Clear();
good.aStack.Add(Part());Check(!SectionAssembly.Select(selector,good,true),"Nonseparate sections rejected");good.aStack.Clear();
Check(!SectionAssembly.Select(selector,Part("Foreign"),true),"Identity checked independently of native trigger");
var site=new CondOwner();site.marker=new(){owner=site,strInstalledCO="Machine",strInstallIA="ACTPhobosAssembly"};
SectionAssembly.Initialize(site.marker);Check(site.GetCondAmount("StatInstallProgressMax")==5000&&good.GetCondAmount("StatInstallProgressMax")==0,"New site gets work target without mutating old section");
site.SetCondAmount("StatInstallProgress",123);SectionAssembly.Initialize(site.marker);Check(site.GetCondAmount("StatInstallProgress")==123,"Reinitialization preserves accumulated work");
var finish=new Interaction{strName="MSPhobosAssembly",objUs=site};
Check(!SectionAssembly.Finish(finish,false),"Empty site cannot create machinery");
for(int i=0;i<3;i++){var p=Part();p.objCOParent=site;site.lot.Add(p);if(i<2)Check(!SectionAssembly.Finish(finish,false),"Staged partial bill waits for next delivery");}
var first=site.lot[0];first.bDestroyed=true;Check(!SectionAssembly.Finish(finish,false),"Destroyed delivered section rejected");first.bDestroyed=false;
first.objCOParent=null;Check(!SectionAssembly.Finish(finish,false),"Removed delivery invalidates completion");first.objCOParent=site;
site.lot.Add(first);Check(!SectionAssembly.Finish(finish,false),"Duplicate reference is not extra material");site.lot.RemoveAt(3);
site.marker.strInstallIA="ACTForeign";Check(!SectionAssembly.Finish(finish,false),"Wrong native site action rejected");site.marker.strInstallIA="ACTPhobosAssembly";
Check(!SectionAssembly.Finish(finish,true)&&site.lot.Count==3,"Cancellation leaves native delivery lot intact for native recovery");
Check(SectionAssembly.Finish(finish,false),"Exact full bill admits one completion");Check(!SectionAssembly.Finish(finish,false),"Same finish cannot replay");
SectionAssembly.ResetInteraction(finish);Check(SectionAssembly.Finish(finish,false),"Native pooled interaction reset clears old completion token");
var reloaded=new CondOwner();reloaded.marker=new(){owner=reloaded,strInstalledCO="Machine",strInstallIA="ACTPhobosAssembly"};
foreach(var part in site.lot){var copy=Part(part.strCODef);copy.objCOParent=reloaded;reloaded.lot.Add(copy);}
Check(SectionAssembly.Finish(new(){strName="MSPhobosAssembly",objUs=reloaded},false),"Reloaded site uses same action and retained bill");
Check(SectionAssembly.Finish(new(){strName="MSForeign"},false),"Other native completions unchanged");
DataHandler.dictInstallables=d.Installables;Installables.dictJobBuildOptions["APPS"]=new();Installables.dictJobBuildOptionsListed["APPS"]=new();
var whole=new JsonInstallable{strName="WholeMachine"};Installables.dictJobBuildOptionsListed["APPS"]["Machine"]=whole;
SectionAssembly.PreferAssemblyMenu();Check(Installables.dictJobBuildOptionsListed["APPS"]["Machine"].strName=="PhobosAssembly","Menu selection does not depend on generation order");
DataHandler.dictCOs["Table"]=new(){aInteractions=new[]{"Foreign","PhobosCraft_Legacy","OtherPhobos"}};
SectionAssembly.RetireTableOffers("Legacy");Check(DataHandler.dictCOs["Table"].aInteractions.SequenceEqual(new[]{"Foreign","OtherPhobos"}),"Only superseded table offer removed");

var port=new CondOwner{strCODef="PortInstalled"};var container=new Container(port);var cargo=Part("Supply");cargo.objCOParent=port;container.ContainedCOs.Add(cargo);cargo.SetCondAmount("IsLotItem",1);
var stacked=Part("Supply");cargo.aStack.Add(stacked);stacked.SetCondAmount("IsLotItem",1);
Check(CoolingCargo.HasCargo(port),"Legacy container contents visible to recovery");
ProcessingService.Problem="far";Check(!CoolingCargo.Open(port,out _)&&cargo.HasCond("IsLotItem"),"Out-of-reach access preserves markers");ProcessingService.Problem=null;
FurnaceService.Problem="hot";Check(!CoolingCargo.Open(port,out _)&&cargo.HasCond("IsLotItem"),"Hot/protected access preserves markers");FurnaceService.Problem=null;
port.lot.Add(cargo);Check(!CoolingCargo.Open(port,out _)&&cargo.HasCond("IsLotItem"),"Actual work lot never released by recovery");port.lot.Clear();
cargo.lot.Add(Part());Check(!CoolingCargo.Open(port,out _),"Nested reserved material stays protected");cargo.lot.Clear();
port.active=new();Check(!CoolingCargo.Open(port,out _),"Active operation blocks recovery");port.active=null;
Check(CoolingCargo.Open(port,out _)&&!cargo.HasCond("IsLotItem")&&!stacked.HasCond("IsLotItem"),"Explicit recovery clears orphan flags on retained head and stack");
Check(ReferenceEquals(cargo.objCOParent,port)&&container.ContainedCOs.Single()==cargo&&cargo.aStack.Single()==stacked,"Recovery preserves identity, parentage and stack membership");
Check(CrewSim.inventoryGUI!.opened==port,"Native inventory displays actual saved container");
var replacement=new CondOwner{strCODef="PortInstalledDmg"};var replacementContainer=new Container(replacement);
Check(!CoolingCargo.Restoring(replacementContainer,cargo),"Ordinary deposits blocked");
int depth=CoolingCargo.BeginTransition(port,replacement);
Check(CoolingCargo.Restoring(replacementContainer,cargo),"Native damage transition can retain exact captured cargo");
Check(!CoolingCargo.Restoring(replacementContainer,Part("NewCargo")),"Transition does not admit new items");
Check(!CoolingCargo.Restoring(container,cargo),"Transition grant is destination-specific");
CoolingCargo.EndTransition(depth);Check(!CoolingCargo.Restoring(replacementContainer,cargo),"Finalizer removes temporary permission including failed native transitions");
int wrong=CoolingCargo.BeginTransition(port,new(){strCODef="RadiatorLoose"});Check(!CoolingCargo.Restoring(replacementContainer,cargo),"Unrelated cooling family receives no transfer grant");CoolingCargo.EndTransition(wrong);
container.ContainedCOs.Clear();Check(!CoolingCargo.HasCargo(port)&&!CoolingCargo.Open(port,out _),"Empty new or recovered cooling equipment offers no storage access");
SectionAssembly.Reset();Check(SectionAssembly.Select(selector,Part("Foreign"),true),"World reload clears registrations");
Console.WriteLine($"PASS: {checks} production assembly/recovery checks with native adapters doubled; Unity hauling and inventory interaction remain unverified.");
