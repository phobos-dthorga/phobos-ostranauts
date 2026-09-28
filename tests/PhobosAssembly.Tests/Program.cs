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
// Site appearance derives from native lots/work without granting permission or editing saves.
var appearance=new SectionAssemblyAppearance("early","mid",64);
SectionAssembly.SetAppearance("PhobosAssembly",appearance);
foreach(var key in new[]{"early","earlyNormal","mid","midNormal"}) DataHandler.textures[key+".png"]=new();
CondOwner VisualSite() {
 var co=new CondOwner{Item=new()};co.marker=new(){owner=co,strInstalledCO="Machine",strInstallIA="ACTPhobosAssembly"};return co;
}
void Deliver(CondOwner co) {var part=Part();part.objCOParent=co;co.lot.Add(part);}
bool Middle(CondOwner co) {Check(SectionAssembly.TryAppearance(co.marker!,out var descriptor,out var mid)&&descriptor==appearance,"Registered marker resolves appearance");return mid;}
var visual=VisualSite(); var nativeMaterial=visual.Item!.rend.sharedMaterial;
Check(!Middle(visual),"Empty placement is unfinished");
Deliver(visual);Deliver(visual);visual.SetCondAmount("StatInstallProgress",10000);
Check(!Middle(visual),"Work cannot substitute for missing sections");
Deliver(visual);visual.SetCondAmount("StatInstallProgress",0);Check(!Middle(visual),"Full delivered bill waits for work to start");
visual.SetCondAmount("StatInstallProgress",1);Check(Middle(visual),"Work on a full delivered bill shows intermediate");
foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,-1d}){visual.SetCondAmount("StatInstallProgress",invalid);Check(!Middle(visual),"Invalid progress stays early");}
visual.SetCondAmount("StatInstallProgress",5000);Check(Middle(visual),"Even complete work never paints a finished machine before native completion");
foreach(string flag in new[]{"IsInstalled","IsDamaged"}) {visual.lot[0].SetCondAmount(flag,1);Check(!Middle(visual),"Ineligible delivered part cannot advance visuals");visual.lot[0].ZeroCondAmount(flag);}
var savedPart=visual.lot[0];visual.lot[0]=visual.lot[1];Check(!Middle(visual),"Duplicate delivery stays early");visual.lot[0]=savedPart;
savedPart.objCOParent=null;Check(!Middle(visual),"Removed delivery retreats visual stage");savedPart.objCOParent=visual;
savedPart.bDestroyed=true;Check(!Middle(visual),"Destroyed delivery stays early");savedPart.bDestroyed=false;
visual.children.Add(Part("Cargo"));Check(!Middle(visual),"Unexpected cargo does not present an accepted assembly bill");visual.children.Clear();
var restored=VisualSite();foreach(var p in visual.lot)Deliver(restored);restored.SetCondAmount("StatInstallProgress",5000);
Check(Middle(restored),"Reload derives intermediate from retained parts and work without new saved state");
UnityEngine.Time.unscaledTime=1;SectionAssembly.Initialize(visual.marker);
var view=visual.marker!.GetComponent<SectionAssemblyView>()!;
var privateMaterial=visual.Item.rend.sharedMaterial;
Check(privateMaterial!=nativeMaterial&&privateMaterial.mainTexture==DataHandler.textures["mid.png"],"Marker owns its selected intermediate material");
Check(privateMaterial.shader==nativeMaterial.shader&&privateMaterial.renderQueue==nativeMaterial.renderQueue&&visual.Item.rend.propertyBlock=="Native visibility/selection","Native shader, queue and property block remain intact");
Check(nativeMaterial.mainTexture!=privateMaterial.mainTexture,"Shared finished material untouched");
int writes=privateMaterial.writes,loads=DataHandler.loads;visual.SetCondAmount("StatInstallProgress",0);
view.Refresh(1.01f);Check(privateMaterial.writes==writes,"Routine refresh frequency limited");
view.Refresh(1.11f);Check(privateMaterial.mainTexture==DataHandler.textures["early.png"],"Work reset selects early on next bounded refresh");
writes=privateMaterial.writes;loads=DataHandler.loads;view.Refresh(1.22f);view.Refresh(1.33f);
Check(privateMaterial.writes==writes&&DataHandler.loads==loads,"Unchanged stages do not reload textures or rewrite material");
Check(visual.GetCondAmount("StatInstallProgress")==0&&visual.lot.Count==3&&visual.lot.All(p=>p.objCOParent==visual),"Rendering preserves work and physical lots");
view.Refresh(.5f);Check(privateMaterial.writes==writes,"A reset real-time clock is safe and unchanged stages still suppress writes");
visual.marker.strInstallIA="ACTForeign";view.Refresh(1.44f);
Check(visual.Item.rend.sharedMaterial==nativeMaterial&&privateMaterial.destroyed,"Marker reuse for another job restores and releases only owned material");
visual.marker.strInstallIA="ACTPhobosAssembly";view.Refresh(1.55f);
var replaced=visual.Item.rend.sharedMaterial;var externalMaterial=new UnityEngine.Material();visual.Item.rend.sharedMaterial=externalMaterial;view.Refresh(1.66f);
Check(replaced.destroyed&&visual.Item.rend.sharedMaterial!=externalMaterial,"Replacement renderer material is adopted without retaining old clone");
view.Release();Check(visual.Item.rend.sharedMaterial==externalMaterial,"Disable/cancel cleanup restores the latest native material");
view.Release();Check(visual.Item.rend.sharedMaterial==externalMaterial,"Repeated cleanup is harmless");
view.Refresh(1.67f); // Inside the interval: explicit cleanup never reattaches itself.
Check(visual.Item.rend.sharedMaterial==externalMaterial,"Cleanup keeps native material until a permitted refresh");
visual.Item.bPlaceholder=false;view.Refresh(1.77f);Check(visual.Item.rend.sharedMaterial==externalMaterial,"Completed/whole machine never receives construction art");
visual.Item.bPlaceholder=true;visual.marker.strInstalledCO="Other";view.Refresh(1.88f);Check(visual.Item.rend.sharedMaterial==externalMaterial,"Foreign output on reused marker is untouched");
visual.marker.strInstalledCO="Machine";
var unavailable=new SectionAssemblyAppearance("missing","wrong",64);SectionAssembly.SetAppearance("PhobosAssembly",unavailable);
DataHandler.textures["wrong.png"]=new(){width=96};DataHandler.textures["wrongNormal.png"]=new(){width=96};
int failures=0;Phobos.Ostranauts.Framework.FrameworkLifecycle.Log=_=>failures++;
view.Refresh(2);loads=DataHandler.loads;view.Refresh(2.2f);
Check(failures==1&&DataHandler.loads==loads&&visual.Item.rend.sharedMaterial==externalMaterial,"Missing art falls back without repeated loads or logs");
visual.SetCondAmount("StatInstallProgress",1);view.Refresh(2.4f);Check(failures==2&&visual.Item.rend.sharedMaterial==externalMaterial,"Wrong-size art preserves native footprint and falls back");
SectionAssembly.SetAppearance("PhobosAssembly",appearance);view.Refresh(2.6f);
var prior=visual.Item.rend.sharedMaterial;SectionAssembly.Reset();view.Refresh(2.8f);
Check(prior.destroyed&&visual.Item.rend.sharedMaterial==externalMaterial,"World registration reset releases active appearance");
Check(SectionAssembly.Select(selector,Part("Foreign"),true),"World reload clears registrations");
// The same policy covers two-section D4/R4 without a furnace-only three-part assumption.
d.Objects["Machine"].aStartingConds=new[]{"StatMass=1x160"};
SectionAssembly.Add(d,"PhobosAssembly","Section","SectionTrigger","Machine",2,80,5000,Array.Empty<string>());
SectionAssembly.SetAppearance("PhobosAssembly",appearance);
var pair=VisualSite();Deliver(pair);pair.SetCondAmount("StatInstallProgress",50);Check(!Middle(pair),"One of two sections stays early");
Deliver(pair);Check(Middle(pair),"Two-section jobs advance with work and their full bill");
SectionAssembly.Initialize(pair.marker);var pairView=pair.marker!.GetComponent<SectionAssemblyView>()!;var pairMaterial=pair.Item!.rend.sharedMaterial;
pair.bDestroyed=true;pairView.Refresh(3);Check(pairMaterial.destroyed,"Destroyed or cancelled sites release their private material");
SectionAssembly.Reset();
Console.WriteLine($"PASS: {checks} production assembly/recovery checks with native adapters doubled; Unity hauling and inventory interaction remain unverified.");
