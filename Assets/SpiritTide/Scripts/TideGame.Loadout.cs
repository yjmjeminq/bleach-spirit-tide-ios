using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private int loadoutSlot,loadoutSkill;

        private void OpenSkillLoadout()
        {
            loadoutSlot=0;
            loadoutSkill=Session.EquippedSkill(0);
            OpenMenu(TideMode.Loadout);
        }

        private void BuildSkillLoadout()
        {
            int hero=Session.Profile.hero;
            MenuTitle("기술 장착 · "+TideStory.HeroName(hero));
            for(int i=0;i<TideLoadouts.SlotCount;i++)
            {
                int slot=i,skill=Session.EquippedSkill(i);
                string id="loadout-slot-"+i;
                var tile=Button(id,"",panel,Center,new Vector2((i-1.5f)*238,136),new Vector2(222,86),()=>
                {
                    loadoutSlot=slot;loadoutSkill=Session.EquippedSkill(slot);pendingFocus=id;Refresh();
                },i==loadoutSlot);
                string number=(i+1)+"번"+(ShowKeyboardHints?" · "+TideUiInfo.Keys[i]:"");
                SkillTile(tile,hero,skill,number,Session.SkillAvailable(skill),true);
            }

            int learned=0;
            for(int i=0;i<CharacterDef.SkillCount;i++)if(TideProgress.SkillLearned(Session.Profile,i))learned++;
            string count=Session.IsTraining?"수련용 · 모든 기술 사용 가능":learned+" / "+CharacterDef.SkillCount+" 습득";
            Label("기술 목록 · "+count,panel,Center,new Vector2(-168,78),new Vector2(628,28),20,muted,true);

            for(int i=0;i<CharacterDef.SkillCount;i++)
            {
                int skill=i,slot=Session.EquippedSlot(i);
                string id="loadout-skill-"+i;
                var tile=Button(id,"",panel,Center,new Vector2(-380+i%3*213,22-i/3*94),new Vector2(202,84),()=>
                {
                    loadoutSkill=skill;pendingFocus=id;Refresh();
                },i==loadoutSkill);
                bool available=Session.SkillAvailable(i);
                string caption=!available?TideProgress.LearningBadge(i)+" 습득":slot>=0?(slot+1)+"번 장착":Session.IsTraining?"수련 가능":"습득 완료";
                SkillTile(tile,hero,i,caption,available,false);
            }

            BuildSkillDetail(hero);
            string tip=Session.IsTraining?"수련에서 바꾼 배치는 수련을 종료할 때 초기화됩니다."
                :"위에서 슬롯을 고른 뒤 아래 기술을 선택하세요. 장착한 기술을 고르면 두 슬롯이 교체됩니다.";
            if(!Session.IsTraining&&!string.IsNullOrEmpty(TideStorage.Diagnostic))tip=Session.Status;
            Label(tip,panel,Center,new Vector2(0,-242),new Vector2(962,35),20,muted);
        }

        private void SkillTile(Button button,int hero,int skill,string caption,bool available,bool equippedRow)
        {
            Transform tile=button.transform;
            Color tint=available?Color.white:new Color(.6f,.67f,.72f);
            float iconX=equippedRow?-69:-64;
            var icon=Picture("LoadoutIcon",tile,Center,new Vector2(iconX,0),new Vector2(64,60),tint,TideSkillIcons.Get(hero,skill));
            icon.preserveAspect=true;
            Text name=button.GetComponentInChildren<Text>();
            name.text=TideSkillIcons.Definition(hero,skill).label;
            name.rectTransform.anchoredPosition=new Vector2(33,equippedRow?-12:12);
            name.rectTransform.sizeDelta=new Vector2(122,32);
            name.alignment=TextAnchor.MiddleLeft;
            name.fontSize=22;
            Label(caption,tile,Center,new Vector2(33,equippedRow?16:-16),new Vector2(122,28),18,
                !available||skill==CharacterDef.Awakening||equippedRow?gold:muted,true);
        }

        private void BuildSkillDetail(int hero)
        {
            const float x=322;
            var skill=Session.Hero.Skill(loadoutSkill);
            bool available=Session.SkillAvailable(loadoutSkill);
            int currentSlot=Session.EquippedSlot(loadoutSkill);
            Surface("SkillDetail",panel,Center,new Vector2(x,-71),new Vector2(322,295),surfaceDark);
            var icon=Picture("SkillDetailIcon",panel,Center,new Vector2(213,34),new Vector2(70,66),
                available?Color.white:new Color(.65f,.71f,.76f),TideSkillIcons.Get(hero,loadoutSkill));
            icon.preserveAspect=true;
            Label(skill.name,panel,Center,new Vector2(360,45),new Vector2(208,54),23,ink,true);
            string requirement=loadoutSkill==CharacterDef.Awakening?"각성 · ":"";
            Label(requirement+(available?"영압 "+skill.spirit_cost+" · "+skill.cooldown_seconds+"초":TideProgress.SkillRequirement(loadoutSkill)),
                panel,Center,new Vector2(360,14),new Vector2(208,28),17,available?muted:gold);
            Label(TideUiInfo.Skill(hero,loadoutSkill),panel,Center,new Vector2(x,-63),new Vector2(282,109),21,ink);

            string applyText=!available?"습득 후 장착":currentSlot==loadoutSlot?"현재 슬롯에 장착됨":
                currentSlot<0?(loadoutSlot+1)+"번 슬롯에 장착":(currentSlot+1)+"번 ↔ "+(loadoutSlot+1)+"번 교체";
            var apply=Button("loadout-equip",applyText,panel,Center,new Vector2(x,-169),new Vector2(286,84),()=>
            {
                if(!Session.EquipSkill(loadoutSlot,loadoutSkill))return;
                if(!Session.IsTraining){Persist();lastPersistRevision=Session.SaveRevision;}
                pendingFocus="loadout-slot-"+loadoutSlot;
                Refresh();
            },true);
            apply.GetComponentInChildren<Text>().fontSize=24;
            apply.interactable=available&&currentSlot!=loadoutSlot;
        }
    }
}
