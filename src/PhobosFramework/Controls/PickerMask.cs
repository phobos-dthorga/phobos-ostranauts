using System;
using System.Collections.Generic;
using System.Linq;

namespace Phobos.Ostranauts.Framework.Controls;

public readonly struct PickerRegion
{
    public readonly float X,Y,Width,Height;
    public PickerRegion(float x,float y,float width,float height){X=x;Y=y;Width=width;Height=height;}
}
public static class PickerMask
{
    /// <summary>Non-overlapping dark regions around a union of visible candidate rectangles.</summary>
    public static PickerRegion[] Outside(float width,float height,IEnumerable<PickerRegion> openings)
    {
        if(width<=0||height<=0)return Array.Empty<PickerRegion>();
        var holes=openings.Select(r=>new PickerRegion(Math.Max(0,r.X),Math.Max(0,r.Y),Math.Min(width,r.X+r.Width)-Math.Max(0,r.X),Math.Min(height,r.Y+r.Height)-Math.Max(0,r.Y)))
            .Where(r=>r.Width>0&&r.Height>0).ToArray();
        var ys=holes.SelectMany(r=>new[]{r.Y,r.Y+r.Height}).Concat(new[]{0f,height}).Distinct().OrderBy(v=>v).ToArray();
        var result=new List<PickerRegion>();
        for(int i=1;i<ys.Length;i++)
        {
            float bottom=ys[i-1],top=ys[i],x=0;
            foreach(var h in holes.Where(r=>r.Y<top&&r.Y+r.Height>bottom).OrderBy(r=>r.X))
            {if(h.X>x)result.Add(new PickerRegion(x,bottom,h.X-x,top-bottom));x=Math.Max(x,h.X+h.Width);}
            if(x<width)result.Add(new PickerRegion(x,bottom,width-x,top-bottom));
        }
        return result.ToArray();
    }
}
