using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Screen-only veil, brackets and signal-like line. Never changes object layers, lights or targets.</summary>
public sealed class PickerGraphic : MaskableGraphic
{
    private Rect[] boxes=Array.Empty<Rect>();
    private PickerRegion[] shade=Array.Empty<PickerRegion>();
    private Vector2 from,to;
    private bool connected,hasOrigin;
    private float phase;
    public void Present(Rect[] candidates,Vector2 origin,Vector2 pointer,bool valid,bool showLine)
    {
        candidates=candidates.Distinct().ToArray();
        var bounds=rectTransform.rect;
        if(!boxes.SequenceEqual(candidates)||lastSize!=bounds.size)
        {boxes=candidates;lastSize=bounds.size;shade=PickerMask.Outside(bounds.width,bounds.height,boxes.Select(r=>new PickerRegion(r.x-bounds.x,r.y-bounds.y,r.width,r.height)));}
        from=origin;to=pointer;connected=valid;hasOrigin=showLine;phase=Time.unscaledTime*12;SetVerticesDirty();
    }
    private Vector2 lastSize;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var bounds=rectTransform.rect;
        foreach(var r in shade)Quad(vh,new Rect(r.X+bounds.x,r.Y+bounds.y,r.Width,r.Height),new Color(0,0,0,.65f));
        var ink=connected?new Color(.22f,.85f,.94f):ConsoleWidgets.Amber;
        foreach(var box in boxes)
        {
            float arm=Math.Min(10,Math.Min(box.width,box.height)/3);
            foreach(float x in new[]{box.xMin,box.xMax})foreach(float y in new[]{box.yMin,box.yMax})
            {Segment(vh,new Vector2(x,y),new Vector2(x+(x==box.xMin?arm:-arm),y),2,ConsoleWidgets.Amber);Segment(vh,new Vector2(x,y),new Vector2(x,y+(y==box.yMin?arm:-arm)),2,ConsoleWidgets.Amber);}
        }
        if(!hasOrigin)return;
        var bend=new Vector2(to.x,from.y);Signal(vh,from,bend,ink);Signal(vh,bend,to,ink);
        Quad(vh,new Rect(from.x-4,from.y-4,8,8),ConsoleWidgets.Slate);
        if(connected){float radius=7+2*Mathf.Sin(phase);Segment(vh,to-new Vector2(radius,0),to+new Vector2(radius,0),2,ink);Segment(vh,to-new Vector2(0,radius),to+new Vector2(0,radius),2,ink);}
    }
    private void Signal(VertexHelper vh,Vector2 a,Vector2 b,Color ink)
    {
        int steps=connected?Math.Min(160,Math.Max(1,(int)(Vector2.Distance(a,b)/8))):1;Vector2 prior=a,normal=new Vector2(b.y-a.y,a.x-b.x).normalized;
        for(int i=1;i<=steps;i++){var point=Vector2.Lerp(a,b,(float)i/steps);if(connected&&i<steps)point+=normal*(2*Mathf.Sin(i*1.8f-phase));Segment(vh,prior,point,1.5f,ink);prior=point;}
    }
    private static void Quad(VertexHelper vh,Rect r,Color color)
    {int start=vh.currentVertCount;vh.AddVert(new Vector3(r.xMin,r.yMin),color,Vector2.zero);vh.AddVert(new Vector3(r.xMin,r.yMax),color,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMax),color,Vector2.zero);vh.AddVert(new Vector3(r.xMax,r.yMin),color,Vector2.zero);vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);}
    private static void Segment(VertexHelper vh,Vector2 a,Vector2 b,float width,Color color)
    {Vector2 n=new Vector2(b.y-a.y,a.x-b.x).normalized*(width/2);int s=vh.currentVertCount;vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);vh.AddTriangle(s,s+1,s+2);vh.AddTriangle(s,s+2,s+3);}
}
