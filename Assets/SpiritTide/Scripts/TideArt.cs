using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritTide
{
    [Serializable] public class TideMotionCell{public int cell,x,y,w,h;public float pivot_x;}
    [Serializable] public class TideMotionAtlas{public string key;public int width,height;public TideMotionCell[] cells;}
    [Serializable] public class TideMotionCatalog{public TideMotionAtlas[] atlases;}
    public static class TideArt
    {
        private static readonly Dictionary<string,Sprite> Sprites=new Dictionary<string,Sprite>();
        private static TideMotionCatalog motions;
        public static void Load()
        {
            if(Sprites.Count>0)return;
            foreach(var foe in TideCampaign.Data.foes)
            {
                Sprite sprite;
                if(foe.IsBoss)sprite=Resources.Load<Sprite>("SpiritTide/Art/"+foe.id);
                else
                {
                    var texture=Resources.Load<Texture2D>("SpiritTide/Art/Campaign/"+foe.atlas);
                    if(texture==null)throw new InvalidOperationException("Missing enemy atlas: "+foe.atlas);
                    int w=texture.width/3,h=texture.height/4,col=foe.cell%3,row=foe.cell/3;
                    sprite=Sprite.Create(texture,new Rect(col*w,texture.height-(row+1)*h,w,h),new Vector2(.5f,0),128,0,SpriteMeshType.FullRect);
                    sprite.name=foe.id;
                }
                if(sprite==null)throw new InvalidOperationException("Missing enemy image: "+foe.id);
                Sprites.Add(foe.id,sprite);
            }
        }
        public static Sprite Enemy(string id){return Sprites.TryGetValue(id,out var sprite)?sprite:null;}
        public static Sprite Background(string key)
        {
            if(Sprites.TryGetValue(key,out var sprite))return sprite;
            var texture=Resources.Load<Texture2D>("SpiritTide/Art/Campaign/"+key)??Resources.Load<Texture2D>("SpiritTide/Art/Experience/"+key);
            if(texture==null)return null;
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),128);Sprites.Add(key,sprite);return sprite;
        }
        public static Sprite Grid(string key,int cell,int columns,int rows)
        {
            string cache=key+"#"+cell;if(Sprites.TryGetValue(cache,out var sprite))return sprite;
            var texture=Resources.Load<Texture2D>("SpiritTide/Art/Experience/"+key);if(texture==null)return null;
            int w=texture.width/columns,h=texture.height/rows;
            float inset=key=="town_npcs_v1"&&cell<5&&cell!=2?w*.15f:0;
            sprite=Sprite.Create(texture,new Rect(cell%columns*w+inset,texture.height-(cell/columns+1)*h,w-inset*2,h),new Vector2(.5f,0),128,0,SpriteMeshType.FullRect);sprite.name=cache;Sprites.Add(cache,sprite);return sprite;
        }
        private static TideMotionAtlas MotionAtlas(int hero)
        {
            if(motions==null){var asset=Resources.Load<TextAsset>("SpiritTide/Data/motion_regions");motions=asset==null?new TideMotionCatalog{atlases=new TideMotionAtlas[0]}:JsonUtility.FromJson<TideMotionCatalog>(asset.text);}
            string key=TideCatalog.Characters[hero].id+"_motion_v1";foreach(var atlas in motions.atlases)if(atlas.key==key)return atlas;return null;
        }
        public static Sprite Motion(int hero,int frame)
        {
            var atlas=MotionAtlas(hero);if(atlas==null)return null;int at=Mathf.Clamp(frame,0,8);string key=atlas.key+"@"+at;if(Sprites.TryGetValue(key,out var sprite))return sprite;
            var texture=Resources.Load<Texture2D>("SpiritTide/Art/Experience/"+atlas.key);var c=atlas.cells[at];float sx=texture.width/(float)atlas.width,sy=texture.height/(float)atlas.height;
            sprite=Sprite.Create(texture,new Rect(c.x*sx,texture.height-(c.y+c.h)*sy,c.w*sx,c.h*sy),new Vector2(c.pivot_x,0),128,0,SpriteMeshType.FullRect);sprite.name=key;Sprites.Add(key,sprite);return sprite;
        }
        public static float MotionPivot(int hero,int frame){var atlas=MotionAtlas(hero);return atlas==null?.5f:atlas.cells[Mathf.Clamp(frame,0,8)].pivot_x;}
    }
}
