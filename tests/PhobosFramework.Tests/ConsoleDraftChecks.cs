using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;

internal static class ConsoleDraftChecks
{
    internal static void Run(Action<bool,string> check)
    {
        var gate=new PickerInputGate();check(!gate.Captures(0),"Picker does not swallow normal gameplay input");gate.Begin();
        check(gate.Captures(5)&&gate.Captures(900),"Every pointer/keyboard frame stays captured while picking");gate.End(900);
        check(gate.Captures(900)&&gate.Captures(901)&&!gate.Captures(902),"Closing cannot leak the release click or following frame into gameplay");
        check(PickerRules.Hits(new[]{"wall","bin-a","bin-b","bin-a"},new[]{"bin-a","bin-b"}).SequenceEqual(new[]{"bin-a","bin-b"}),"Overlapping permitted objects require disambiguation and never choose the underlying wall");
        check(PickerRules.Hits(new[]{"missing-bin"},Array.Empty<string>()).Length==0,"Destroyed or no-longer-authorised candidates cannot be selected");
        foreach(var permission in new[]{WorkPermission.Disabled,WorkPermission.Enabled,WorkPermission.Stopped,WorkPermission.Suspended})
        {
            var saved=new StandingOrder{Permission=permission,Recipe="recover-crop",Source="store-a",Destination="store-b",StopReason=permission==WorkPermission.Stopped?"manual":"",Binding="saved-mission"};
            var draft=new OrderDraft("bench",saved);check(!draft.Dirty,"Opening a form is read-only");
            draft.Value.Stock=12;draft.Value.Source="other-store";
            check(draft.Dirty&&saved.Stock==4&&saved.Source=="store-a","Editing drafts never mutates the live order");
            var next=OrderDraft.Commit(draft.Value);
            check(next.Stock==12&&next.Source=="other-store","Apply includes all draft settings");
            check(next.Permission==(permission==WorkPermission.Enabled?WorkPermission.Suspended:permission),"Apply requires explicit resume for enabled orders and preserves other permissions");
            check(permission!=WorkPermission.Stopped||next.StopReason=="manual","Applying does not silently clear manual stops");
            check(next.Binding.Length==0,"Configuration cannot reuse an old mission binding");
            saved.Permission=WorkPermission.Stopped;saved.StopReason="manual";
            check(draft.Expected!=OrderDraft.Fingerprint(saved)||permission==WorkPermission.Stopped,"Independent stops invalidate a stale draft");
        }
        var source=new StandingOrder();var d=new OrderDraft("rack",source);d.Value.Stock=8;d.Value.Stock=source.Stock;
        check(!d.Dirty,"Reverting a field clears the dirty state");
        d.Value.ResumeRoutine=false;check(d.Dirty,"Resume policy participates in stale detection");
        source.Destination="missing-store";var missing=new OrderDraft("rack",source);
        check(missing.Value.Destination=="missing-store","Draft capture retains missing storage identity");
        foreach(float width in new[]{620,760,999,1000,1200,1800,2400})
        {
            check(ConsoleLayout.Narrow(width)==(width<1000),"Responsive transition uses native content units");
            if(!ConsoleLayout.Narrow(width))check(width-2*ConsoleLayout.Margin-ConsoleLayout.ListWidth(width)-20>=550,"Wide layout leaves a usable form width");
        }
    }
}
