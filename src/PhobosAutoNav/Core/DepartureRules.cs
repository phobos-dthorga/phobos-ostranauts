using System;
using System.Linq;
using System.Collections.Generic;
using Phobos.Ostranauts.Framework.Persistence;
namespace PhobosAutoNav.Core;
internal enum DepartureRecovery { Retain, Detach, Egress, Continue, Finished }
internal static class DepartureRules
{
    internal const double HullGapM=1000, MinimumReserveMS=42;
    internal static readonly string[] Phases={"Prepared","DetachPending","Detached","Departing","Continue","Complete","Cancelled"};
    internal static bool Valid(IReadOnlyDictionary<string,string> fields) =>
        new[]{"owner","ship","console","module","peer","ownPort","peerPort","permission","phase","kind","operation"}.All(k=>fields.TryGetValue(k,out var v)&&ObjectStateStore.SafeValue(v)) &&
        fields["ship"]!=fields["peer"] && Phases.Contains(fields["phase"]) && new[]{"Moor","Dock"}.Contains(fields["kind"]) &&
        new[]{"none","fly","rendezvous","follow","dock","approachdock"}.Contains(fields["operation"]) &&
        (fields["operation"]=="none" || fields.TryGetValue("target",out var target)&&ObjectStateStore.SafeValue(target)&&target!=fields["ship"]&&target!=fields["peer"]);
    internal static DepartureRecovery Reconcile(string phase,bool attached,bool exact,bool otherAttachment,bool orphan)
    {
        if(!Phases.Contains(phase)||otherAttachment||orphan) return DepartureRecovery.Retain;
        if(phase=="Complete"||phase=="Cancelled") return DepartureRecovery.Finished;
        if(attached) return exact&&phase=="Prepared"?DepartureRecovery.Detach:DepartureRecovery.Retain;
        return phase=="Continue"?DepartureRecovery.Continue:DepartureRecovery.Egress;
    }
}
