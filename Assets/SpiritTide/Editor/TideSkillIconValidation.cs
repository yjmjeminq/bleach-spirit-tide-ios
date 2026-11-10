using System;
using System.Collections.Generic;
using UnityEngine;
namespace SpiritTide.EditorTools
{
    public static class TideSkillIconValidation
    {
        public static void Run(Action<string,bool> check)
        {
            var names=new HashSet<string>();int count=0;
            foreach(var sheet in TideSkillIcons.Data.sheets)
            {
                var texture=Resources.Load<Texture2D>(sheet.resource);
                check("painted icon atlas keeps authored dimensions "+sheet.id,texture!=null&&texture.width==sheet.width&&texture.height==sheet.height);
                for(int slot=0;slot<5;slot++)
                {
                    var def=TideSkillIcons.Definition(sheet.hero,slot);Rect crop=def.rect.Bounds;Sprite sprite=TideSkillIcons.Get(sheet.hero,slot);
                    check("painted icon matches the real skill "+def.id,def.id==TideCatalog.Characters[sheet.hero].Skill(slot).id&&names.Add(def.id)&&sprite.name=="skill-icon-"+def.id);
                    check("icon crop stays inside its source atlas "+def.id,crop.width>300&&crop.height>300&&crop.xMin>=0&&crop.yMin>=0&&crop.xMax<=texture.width&&crop.yMax<=texture.height&&sprite.rect==crop);
                    if(slot>0)check("neighboring icon crops do not overlap "+def.id,!crop.Overlaps(sheet.icons[slot-1].rect.Bounds));
                    count++;
                }
            }
            check("all thirty playable skills have painted icons",count==30&&names.Count==30);
            var crops=new List<Rect>();
            for(int hero=0;hero<TideCatalog.Characters.Length;hero++)
                for(int skill=CharacterDef.FirstExtraSkill;skill<CharacterDef.SkillCount;skill++)
                {
                    var def=TideSkillIcons.Definition(hero,skill);
                    var sprite=TideSkillIcons.Get(hero,skill);
                    Rect crop=def.rect.Bounds;
                    check("expansion icon matches the real skill "+def.id,def.id==TideCatalog.Characters[hero].Skill(skill).id&&
                        names.Add(def.id)&&sprite.name=="skill-icon-"+def.id&&crop==sprite.rect&&
                        crop.xMin>=0&&crop.yMin>=0&&crop.xMax<=sprite.texture.width&&crop.yMax<=sprite.texture.height&&
                        !crops.Exists(other=>other.Overlaps(crop)));
                    crops.Add(crop);
                }
            check("all fifty-four playable skills have distinct icon crops",names.Count==54&&crops.Count==24);
        }
    }
}
