using System;
using System.Linq;
using Phobos.Ostranauts.Framework.Controls;
using Phobos.Ostranauts.Framework.Crew;

internal static class ConsoleDraftChecks
{
    internal static void Run(Action<bool,string> check)
    {
        // Use exaggerated glyph widths to reproduce narrow fields and a font with tall metrics.
        float Measure(string s)=>new System.Globalization.StringInfo(s).LengthInTextElements*9;
        foreach(var caption in new[]{"Short", "A duplicate storage name that extends past the field", "First line too long\nSecond line too long", "Café\r\nRéserves", "A🙂e\u0301界 long name"})
        foreach(float width in new[]{0,20,30,70,160,340})
        {
            string fitted=CompactTextRules.Fit(caption,width,Measure);
            check(fitted.Split('\n').All(line=>Measure(line)<=width),"Each fixed caption line fits its own boundary without shrinking the font");
            check(!fitted.Contains('\r'),"Native/catalog newline differences do not create extra lines");
        }
        check(CompactTextRules.Fit("e\u0301e\u0301🙂ABC",45,Measure)=="e\u0301e\u0301...","Truncation preserves complete accented/emoji text elements");
        check(CompactTextRules.Fit("First\nSecond",100,Measure)=="First\nSecond","Short multi-line rows keep both lines");
        check(CompactTextRules.Fit("",100,Measure)=="","Icon-only controls do not acquire text");
        check(CompactTextRules.Fit("First\nSecond",100,Measure,1)=="First...","Single-line notices cannot spill into the action bar or outside the frame");
        check(CompactTextRules.Fit("First\nSecond",100,Measure,0)=="","Collapsed fields cannot render a stray line");
        var holes=new[]{new PickerRegion(10,10,30,30),new PickerRegion(25,20,40,35),new PickerRegion(-10,70,30,60),new PickerRegion(120,0,10,10)};
        var dim=PickerMask.Outside(100,100,holes);
        for(int y=0;y<100;y++)for(int x=0;x<100;x++)
        {
            bool Contains(PickerRegion r)=>x+.5f>=r.X&&x+.5f<r.X+r.Width&&y+.5f>=r.Y&&y+.5f<r.Y+r.Height;
            check(dim.Count(Contains)==(holes.Any(Contains)?0:1),"Overlapping and offscreen candidates have clear openings; all other pixels are dimmed exactly once");
        }
        check(PickerMask.Outside(100,100,Array.Empty<PickerRegion>()).Length==1,"An empty candidate set dims the whole view");
        check(PickerMask.Outside(0,100,holes).Length==0,"Unlaid-out canvases do not generate invalid geometry");
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
