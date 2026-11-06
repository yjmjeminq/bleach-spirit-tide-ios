using System;
using UnityEngine;

namespace SpiritTide
{
    [Serializable] public sealed class TideRigPoint {public float x,y;public Vector2 Value=>new Vector2(x,y);}
    [Serializable] public sealed class TideRigVertex {public TideRigPoint p;public float u,v,w;public int a,b;}
    [Serializable] public sealed class TideRigDef
    {
        public int hero,source_width,source_height,weapon;public string id,resource,hair_resource;
        public float height,stride;public bool hair;
        public TideRigPoint[] bones,soles;public TideRigVertex[] vertices,hair_vertices;public int[] triangles,hair_triangles;
    }
    [Serializable] public sealed class TideRigCatalog {public int schema;public TideRigDef[] rigs;}
    public static class TideRigData
    {
        private static TideRigCatalog data;
        private static readonly Texture2D[] textures=new Texture2D[6],hairTextures=new Texture2D[6];
        public static TideRigCatalog Data
        {
            get
            {
                if(data==null)
                {
                    var asset=Resources.Load<TextAsset>("SpiritTide/Data/motion_rigs");
                    if(asset==null)throw new InvalidOperationException("Missing motion rig definitions");
                    data=JsonUtility.FromJson<TideRigCatalog>(asset.text);
                    if(data.schema!=1||data.rigs.Length!=6)throw new InvalidOperationException("Incomplete motion rigs");
                }
                return data;
            }
        }
        public static TideRigDef Get(int hero)=>Data.rigs[Mathf.Clamp(hero,0,5)];
        public static Texture2D Texture(int hero,bool hair=false)
        {
            if(hair){if(string.IsNullOrEmpty(Get(hero).hair_resource))return null;if(hairTextures[hero]==null)hairTextures[hero]=Resources.Load<Texture2D>(Get(hero).hair_resource);return hairTextures[hero];}
            if(textures[hero]==null)textures[hero]=Resources.Load<Texture2D>(Get(hero).resource);
            if(textures[hero]==null)throw new InvalidOperationException("Missing bind-pose texture");
            return textures[hero];
        }
    }
}
