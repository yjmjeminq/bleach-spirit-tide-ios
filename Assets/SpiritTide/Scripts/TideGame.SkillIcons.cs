using UnityEngine;
using UnityEngine.UI;
namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private readonly Image[] skillIcons=new Image[5];
        private readonly Text[] skillTimers=new Text[5];
        private readonly Image[] skillIconFrames=new Image[5];
        private void BuildSkillIcon(int slot)
        {
            Transform button=skillButtons[slot].transform;
            skillIconFrames[slot]=Picture("SkillIconFrame",button,new Vector2(.5f,.5f),new Vector2(0,22),new Vector2(70,64),slot==4?gold:new Color(.2f,.4f,.47f));
            skillIcons[slot]=Picture("SkillIcon",button,new Vector2(.5f,.5f),new Vector2(0,22),new Vector2(66,60),Color.white,TideSkillIcons.Get(Session.Profile.hero,slot));
            skillIcons[slot].preserveAspect=true;
        }
        private void UpdateSkillIcon(int slot,float remaining)
        {
            skillIcons[slot].sprite=TideSkillIcons.Get(Session.Profile.hero,slot);
            bool mana=Session.Mp>=Session.Hero.Skill(slot).spirit_cost||Session.IsTraining;
            skillIcons[slot].color=mana?Color.white:new Color(.4f,.45f,.5f,.7f);
            skillIconFrames[slot].color=slot==4?gold:new Color(.2f,.4f,.47f);
            Say(skillTimers[slot],!Session.IsTraining&&remaining>0?Mathf.CeilToInt(remaining).ToString():"");
        }
        private void AddHelpSkillIcon(Button button,int slot)
        {
            var image=Picture("HelpSkillIcon",button.transform,new Vector2(.5f,.5f),new Vector2(-49,0),new Vector2(54,54),Color.white,TideSkillIcons.Get(Session.Profile.hero,slot));image.preserveAspect=true;
            Text text=button.GetComponentInChildren<Text>();text.rectTransform.anchoredPosition=new Vector2(29,0);text.rectTransform.sizeDelta=new Vector2(95,64);text.fontSize=19;
            text.text=(ShowKeyboardHints?TideUiInfo.Keys[slot]+"\n":"")+TideSkillIcons.Definition(Session.Profile.hero,slot).label;
        }
    }
}
