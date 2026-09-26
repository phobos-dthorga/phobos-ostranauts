using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Controls;

public sealed class PickerInputGate
{
    private bool active;
    private int released=-2;
    public void Begin()=>active=true;
    public void End(int frame){active=false;released=frame;}
    public bool Captures(int frame)=>active||(long)frame<=(long)released+1;
}
public static class PickerRules
{
    /// <summary>Re-admit against fresh candidates. Order and overlap survive; duplicates and unrelated hits do not.</summary>
    public static string[] Hits(IEnumerable<string> hits,IEnumerable<string> permitted)
    {var allowed=new HashSet<string>(permitted,StringComparer.Ordinal);return hits.Where(allowed.Contains).Distinct(StringComparer.Ordinal).ToArray();}
}
