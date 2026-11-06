using System;
using UnityEngine;
namespace SpiritTide
{
    [Serializable] public sealed class TideSkillIconCrop {public float x,y,width,height;public Rect Bounds=>new Rect(x,y,width,height);}
    [Serializable] public sealed class TideSkillIconDef {public int slot;public string id,label;public TideSkillIconCrop rect;}
    [Serializable] public sealed class TideSkillIconSheet {public int hero,width,height;public string id,resource;public TideSkillIconDef[] icons;}
    [Serializable] public sealed class TideSkillIconData {public int schema;public TideSkillIconSheet[] sheets;}
    public static class TideSkillIcons
    {
        private static TideSkillIconData data;
        private static readonly Sprite[,] sprites=new Sprite[6,5];
        public static TideSkillIconData Data {get {Load();return data;}}
        private static void Load()
        {
            if(data!=null)return;
            var asset=Resources.Load<TextAsset>("SpiritTide/Data/skill_icons");
            if(asset==null)throw new InvalidOperationException("Missing skill icon catalog");
            data=JsonUtility.FromJson<TideSkillIconData>(asset.text);
            if(data==null||data.schema!=1||data.sheets==null||data.sheets.Length!=6)throw new InvalidOperationException("Invalid skill icon catalog");
        }
        public static TideSkillIconDef Definition(int hero,int slot)
        {
            Load();if(hero<0||hero>5||slot<0||slot>4)throw new ArgumentOutOfRangeException();
            var sheet=data.sheets[hero];if(sheet.hero!=hero||sheet.icons==null||sheet.icons.Length!=5||sheet.icons[slot].slot!=slot)throw new InvalidOperationException("Invalid skill icon mapping");
            return sheet.icons[slot];
        }
        public static Sprite Get(int hero,int slot)
        {
            var def=Definition(hero,slot);if(sprites[hero,slot]!=null)return sprites[hero,slot];
            var sheet=data.sheets[hero];var texture=Resources.Load<Texture2D>(sheet.resource);
            if(texture==null||texture.width!=sheet.width||texture.height!=sheet.height)throw new InvalidOperationException("Skill icon texture dimensions changed: "+sheet.id);
            var sprite=Sprite.Create(texture,def.rect.Bounds,new Vector2(.5f,.5f),128,0,SpriteMeshType.FullRect);sprite.name="skill-icon-"+def.id;sprites[hero,slot]=sprite;return sprite;
        }
    }
}
