// Only the renderer boundary is doubled; the view and stage selection are production code.
using System;
using System.Collections.Generic;
namespace UnityEngine;
public class Object {public bool destroyed;public static void Destroy(Object item)=>item.destroyed=true;}
public class MonoBehaviour:Object {public GameObject gameObject=null!;}
public class GameObject {
 public CondOwner? owner;private Dictionary<Type,object> components=new();
 public T? GetComponent<T>() where T:class=>owner as T ?? owner?.marker as T ?? (components.TryGetValue(typeof(T),out var x)?x as T:null);
 public T AddComponent<T>() where T:MonoBehaviour,new(){var x=new T{gameObject=this};components[typeof(T)]=x;return x;}
}
public static class Time {public static float unscaledTime;}
public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
public class Transform {public Vector3 position;}
public enum FilterMode {Point}
public class Texture2D {public int width=64,height=64;public FilterMode filterMode;}
public class Material:Object {
 public static int created; public int writes;public int renderQueue=123;public string shader="NativePlaceholder";
 public Dictionary<string,Texture2D?> textures=new();public Dictionary<string,float> floats=new();
 public Texture2D? mainTexture=>textures.TryGetValue("_MainTex",out var t)?t:null;
 public Material(){textures["_MainTex"]=new();}
 public Material(Material x){created++;textures=new(x.textures);floats=new(x.floats);renderQueue=x.renderQueue;shader=x.shader;}
 public void SetTexture(string k,Texture2D? t){textures[k]=t;writes++;}
 public void SetFloat(string k,float x){floats[k]=x;writes++;}
}
public class Renderer {public Material sharedMaterial=new();public string propertyBlock="Native visibility/selection";}
