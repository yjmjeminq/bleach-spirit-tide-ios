using UnityEngine;
using UnityEngine.UI;
namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private static readonly Vector2 SkillArtCenter=new Vector2(0,17);
        private static readonly Vector2 SkillArtSize=new Vector2(49,49);
        private readonly Image[] skillIcons=new Image[TideLoadouts.SlotCount];
        private readonly Text[] skillTimers=new Text[TideLoadouts.SlotCount];
        private readonly Image[] skillIconFrames=new Image[TideLoadouts.SlotCount];
        private readonly GameObject[] skillLocks=new GameObject[TideLoadouts.SlotCount];
        private void BuildSkillIcon(int slot)
        {
            var control=skillButtons[slot];
            Transform button=control.transform;
            Surface("SkillCaption",button,Center,new Vector2(0,-25),new Vector2(82,36),new Color(.105f,.20f,.24f,.91f)).transform.SetAsFirstSibling();
            Vector2 medallion=new Vector2(56,56);
            Picture("SkillIconDisk",button,Center,SkillArtCenter,medallion,new Color(.15f,.28f,.32f,.95f),touchDisc);
            var icon=Node("SkillIcon",button,Center,SkillArtCenter,SkillArtSize).gameObject.AddComponent<TideSkillMedallion>();
            icon.sprite=TideSkillIcons.Get(Session.Profile.hero,Session.EquippedSkill(slot));
            icon.raycastTarget=false;skillIcons[slot]=icon;
            skillIconFrames[slot]=Picture("SkillIconFrame",button,Center,SkillArtCenter,medallion,new Color(.45f,.70f,.77f,.85f),touchRim);
            control.targetGraphic=skillIconFrames[slot];
            var badge=Picture("SkillLocked",button,Center,new Vector2(26,27),new Vector2(24,24),new Color(.06f,.11f,.14f,.96f),circle);
            skillLocks[slot]=badge.gameObject;
            Picture("LockBody",badge.transform,Center,new Vector2(0,-3),new Vector2(11,8),gold);
            Stroke("LockShackle",badge.transform,new Vector2(-3,1),new Vector2(-3,5),1.5f,gold);
            Stroke("LockShackle",badge.transform,new Vector2(-3,5),new Vector2(3,5),1.5f,gold);
            Stroke("LockShackle",badge.transform,new Vector2(3,5),new Vector2(3,1),1.5f,gold);
        }
        private void UpdateSkillIcon(int slot,int skill,bool available,float remaining)
        {
            skillIcons[slot].sprite=TideSkillIcons.Get(Session.Profile.hero,skill);
            bool mana=Session.Mp>=Session.Hero.Skill(skill).spirit_cost||Session.IsTraining;
            bool ready=available&&mana&&(Session.IsTraining||remaining<=0);
            skillIcons[slot].color=mana&&available?Color.white:new Color(.4f,.45f,.5f,.7f);
            Color glow=skill==CharacterDef.Awakening?gold:TideSkillFx.Get(Session.Profile.hero,skill).core;
            skillIconFrames[slot].color=ready?glow:new Color(.46f,.58f,.63f,.62f);
            skillLocks[slot].SetActive(!available);
            Say(skillTimers[slot],!Session.IsTraining&&remaining>0?Mathf.CeilToInt(remaining).ToString():"");
        }
        private void AddHelpSkillIcon(Button button,int slot)
        {
            int skill=Session.EquippedSkill(slot);
            var image=Picture("HelpSkillIcon",button.transform,Center,new Vector2(-67,0),new Vector2(62,62),Color.white,TideSkillIcons.Get(Session.Profile.hero,skill));image.preserveAspect=true;
            Text text=button.GetComponentInChildren<Text>();text.rectTransform.anchoredPosition=new Vector2(34,0);text.rectTransform.sizeDelta=new Vector2(126,64);text.fontSize=20;
            text.text=(slot+1)+"번"+(ShowKeyboardHints?" · "+TideUiInfo.Keys[slot]:"")+"\n"+TideSkillIcons.Definition(Session.Profile.hero,skill).label;
        }
    }
}
