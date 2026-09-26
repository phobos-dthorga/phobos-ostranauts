using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Phobos.Ostranauts.Framework.Crew;
using Phobos.Ostranauts.Framework.Persistence;
using UnityEngine;
using UnityEngine.UI;

namespace Phobos.Ostranauts.Framework.Controls;

public static class ObjectPresentation
{
    private static ObjectStateStore Store(CondOwner co)=>new(co.mapGUIPropMaps,"display-name",FrameworkInfo.PluginId,1);
    public static string Nickname(CondOwner co)
    {
        if(Store(co).Read(out var data)!=SavedStateStatus.Ready||!data.TryGetValue("name",out var encoded)||encoded=="none")return "";
        try{return Encoding.UTF8.GetString(Enumerable.Range(0,encoded.Length/2).Select(i=>Convert.ToByte(encoded.Substring(i*2,2),16)).ToArray());}catch{return "";}
    }
    public static string Name(CondOwner co){var n=Nickname(co);return n.Length>0?n:co.FriendlyName;}
    public static string ListName(CondOwner co){var n=Name(co);return n.StartsWith("Phobos' ",StringComparison.Ordinal)?n.Substring(8):n;}
    public static string Name(string? id)=>CrewWork.Resolve(id) is CondOwner co?Name(co):ConsoleWidgets.Text(id=="none"||string.IsNullOrEmpty(id)?"not_selected":"missing_selection");
    public static bool Rename(CondOwner co,string expected,string name,out string reason)
    {
        reason=ConsoleWidgets.Text("stale");if(!CrewWork.CanManage(co)||CrewSim.GetSelectedCrew()?.ship!=co.ship||Nickname(co)!=expected)return false;
        name=name.Trim();reason=ConsoleWidgets.Text("nickname_limit");if(name.Length>60||name.Any(char.IsControl))return false;
        string encoded=name.Length==0?"none":BitConverter.ToString(Encoding.UTF8.GetBytes(name)).Replace("-","");
        reason=ConsoleWidgets.Text("protected");return Store(co).TryWrite(new Dictionary<string,string>{{"name",encoded}});
    }
    public static string Location(CondOwner co)
    {
        var pos=co.GetPos("use");var room=co.ship?.GetRoomAtWorldCoords1(pos,false)?.CO;
        return ConsoleWidgets.Text("location",room?.strNameFriendly??co.ship?.publicName??ConsoleWidgets.Text("unavailable"),pos.x.ToString("0"),pos.y.ToString("0"));
    }
    public static string Contents(CondOwner co)
    {
        var contents=CrewLogistics.Contents(co).GroupBy(c=>c.strNameFriendly).Take(3).Select(g=>g.Key+" ×"+g.Count()).ToArray();
        return contents.Length==0?ConsoleWidgets.Text("empty"):string.Join(" · ",contents);
    }
    public static void Picture(Transform parent,CondOwner co,float size=64)
    {
        var r=PanelWidgets.Rect(parent,"Equipment picture");ConsoleWidgets.Size(r,size,size);r.gameObject.AddComponent<Image>().color=new Color(.13f,.18f,.21f);
        Texture? texture=null;
        try {string path=co.Item?.ImgOverride??co.strPortraitImg;if(!string.IsNullOrEmpty(path))texture=DataHandler.LoadPNG(path.EndsWith(".png",StringComparison.OrdinalIgnoreCase)?path:path+".png",bNorm:false);}catch { }
        if(texture!=null&&texture.name!="missing.png"&&(texture.width>2||texture.height>2))
        {var image=PanelWidgets.Rect(r,"Artwork");PanelWidgets.Fill(image,4,4,4,4);var raw=image.gameObject.AddComponent<RawImage>();raw.texture=texture;raw.raycastTarget=false;
            var ratio=image.gameObject.AddComponent<AspectRatioFitter>();ratio.aspectMode=AspectRatioFitter.AspectMode.FitInParent;ratio.aspectRatio=(float)texture.width/texture.height;}
        else
        {
            // Draw a deliberate equipment outline; no dependence on font symbol coverage.
            foreach(var side in new[]{0,1,2,3}){var edge=PanelWidgets.Rect(r,"Placeholder outline");PanelWidgets.Fill(edge,size*.3f,size*.3f,size*.3f,size*.3f);
                if(side<2){edge.anchorMin=new Vector2(side==0?.3f:.7f,.3f);edge.anchorMax=new Vector2(side==0?.3f:.7f,.7f);edge.offsetMin=new Vector2(-1,0);edge.offsetMax=new Vector2(1,0);}
                else {edge.anchorMin=new Vector2(.3f,side==2?.3f:.7f);edge.anchorMax=new Vector2(.7f,side==2?.3f:.7f);edge.offsetMin=new Vector2(0,-1);edge.offsetMax=new Vector2(0,1);}
                var line=edge.gameObject.AddComponent<Image>();line.color=ConsoleWidgets.Slate;line.raycastTarget=false;}
        }
    }
}
