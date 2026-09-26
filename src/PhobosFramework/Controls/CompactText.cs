using System;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace Phobos.Ostranauts.Framework.Controls;

/// <summary>Explicit horizontal truncation: TMP ellipsis can blank short fields with native font metrics.</summary>
public static class CompactTextRules
{
    public static string Fit(string text,float width,Func<string,float> measure,int maxLines=int.MaxValue)
    {
        if(width<=0||maxLines<=0)return "";
        string Line(string line)
        {
            if(measure(line)<=width)return line;
            const string suffix="...";if(measure(suffix)>width)return "";
            var starts=StringInfo.ParseCombiningCharacters(line);int lo=0,hi=starts.Length;
            while(lo<hi){int mid=(lo+hi+1)/2;string part=mid==starts.Length?line:line.Substring(0,starts[mid]);if(measure(part+suffix)<=width)lo=mid;else hi=mid-1;}
            return (lo==starts.Length?line:line.Substring(0,starts[lo]))+suffix;
        }
        var lines=text.Replace("\r\n","\n").Split('\n');
        if(lines.Length>maxLines){Array.Resize(ref lines,maxLines);lines[maxLines-1]+="...";}
        return string.Join("\n",Array.ConvertAll(lines,Line));
    }
}

/// <summary>Retains the full live caption while fitting individual lines to the actual laid-out width.</summary>
public sealed class CompactText : MonoBehaviour
{
    private TMP_Text label=null!;
    private string source="",rendered="";
    private float width=-1,height=-1,size=-1;
    private void Awake(){label=GetComponent<TMP_Text>();source=label.text;rendered=source;}
    private void LateUpdate()
    {
        float nextWidth=label.rectTransform.rect.width-label.margin.x-label.margin.z;
        float nextHeight=label.rectTransform.rect.height-label.margin.y-label.margin.w;
        bool changed=label.text!=rendered;if(changed)source=label.text;
        if(!changed&&Math.Abs(nextWidth-width)<.1f&&Math.Abs(nextHeight-height)<.1f&&size==label.fontSize)return;
        width=nextWidth;height=nextHeight;size=label.fontSize;
        int lines=height<=0?0:Math.Max(1,(int)(height/(size*FixedTextMetrics.LineHeightEm)));
        rendered=CompactTextRules.Fit(source,width,s=>label.GetPreferredValues(s,float.PositiveInfinity,float.PositiveInfinity).x,lines);
        label.text=rendered;
    }
}
