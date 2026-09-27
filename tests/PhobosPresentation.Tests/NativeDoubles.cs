using System.Collections.Generic;

public sealed class Ship
{
    public int LoadState = 2;
    public readonly List<CondOwner> Objects = new();
    public int Reads;
    public List<CondOwner> GetCOs(object? filter, bool bSubObjects, bool bAllowDocked, bool bAllowLocked)
    {
        if (bSubObjects || bAllowDocked || !bAllowLocked) throw new System.Exception("Discovery scope changed");
        Reads++; return new(Objects);
    }
}
public sealed class CondOwner
{
    public Ship? ship;
    public CondOwner? objCOParent;
    public bool bDestroyed, Installed = true;
    public string Id = "fixture";
}
namespace Phobos.Ostranauts.Framework.Diagnostics
{
    internal static class Performance
    { internal static readonly object ShipCandidates = new(); internal static void Increment(object counter, int count) { } }
}
namespace Phobos.Ostranauts.Framework.Controls
{
    public sealed class CompactText
    { public string Source = ""; public int Changes; public void SetSource(string value) { if (Source != value) { Source = value; Changes++; } } }
}
namespace UnityEngine
{
    public readonly record struct Color(float R);
    public sealed class GameObject
    { private readonly Dictionary<System.Type,object> components = new();
      public T? GetComponent<T>() where T:class => components.TryGetValue(typeof(T),out var c) ? c as T : null;
      public T AddComponent<T>() where T:new() { var c=new T(); components[typeof(T)]=c!; return c; }
      public bool activeSelf; public int Writes; public void SetActive(bool active) { Writes++; activeSelf = active; } }
}
namespace UnityEngine.UI
{
    public class Selectable
    { private bool enabled; public int Writes; public bool interactable { get => enabled; set { enabled = value; Writes++; } } }
    public sealed class Graphic
    { private UnityEngine.Color current; public int Writes; public UnityEngine.Color color { get => current; set { current = value; Writes++; } } }
}
namespace TMPro
{
    public sealed class TMP_Text
    {
        public object? Component;
        private string caption = "";
        public int Writes, Lookups;
        public string text { get => caption; set { caption = value; Writes++; } }
        public T? GetComponent<T>() where T : class { Lookups++; return Component as T; }
    }
}

internal static class CrewSim { internal static bool bJustClickedInput; }
namespace UnityEngine { public class MonoBehaviour {} }
namespace UnityEngine.UI {
 public sealed class Button : Selectable {
  public readonly UnityEngine.GameObject gameObject = new(); public bool Active=true, ParentAllows=true;
  public T? GetComponent<T>() where T:class => gameObject.GetComponent<T>();
  public bool IsActive()=>Active; public bool IsInteractable()=>interactable && ParentAllows;
 }
}
namespace UnityEngine.EventSystems {
 public interface IPointerClickHandler { void OnPointerClick(PointerEventData data); }
 public sealed class PointerEventData {
  public enum InputButton { Left, Right, Middle }
  public InputButton button; public bool Used; public void Use()=>Used=true;
 }
}
