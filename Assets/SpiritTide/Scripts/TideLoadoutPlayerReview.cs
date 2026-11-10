using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        private IEnumerator ReviewLoadout()
        {
            var session=game.Session;
            string saved=JsonUtility.ToJson(session.Profile);
            var fixture=new TideProfile{campaignSchema=2,level=4,recruitedHeroes=3};
            TideCatalog.Sanitize(fixture);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fixture),session.Profile);
            game.ReturnTown();
            yield return TapUi("pause");yield return TapUi("home-loadout");
            Check("loadout opens the current hero's nine-skill library",session.Mode==TideMode.Loadout&&VisibleText("루키아")&&VisibleText("3 / 9 습득"));
            yield return TapUi("loadout-slot-2");
            yield return TapUi("loadout-skill-8");
            Check("locked skill explains how to unlock and refuses to equip",
                VisibleText("Lv.18 습득")&&!game.Buttons["loadout-equip"].interactable&&session.EquippedSkill(2)==2);
            yield return Capture("87-skill-locked");
            yield return TapUi("loadout-skill-5");yield return TapUi("loadout-equip");
            Check("touch equips the chosen skill in the highlighted slot",session.EquippedSkill(2)==5&&VisibleText("3번 장착"));
            yield return TapUi("loadout-slot-1");yield return TapUi("loadout-skill-5");
            Check("equipping the same technique in a second slot offers a swap",VisibleText("3번 ↔ 2번 교체"));
            yield return TapUi("loadout-equip");
            Check("touch swap keeps each skill in only one slot",session.EquippedSkill(1)==5&&session.EquippedSkill(2)==1);
            yield return Capture("87-skill-loadout");
            LeaveToTown();
            yield return TapUi("hero-1");yield return TapUi("pause");yield return TapUi("home-loadout");
            Check("changing hero shows that hero's own skills and slots",VisibleText("오리히메")&&VisibleText("겹결")&&session.EquippedSkill(1)==1);
            yield return TapUi("loadout-skill-5");yield return TapUi("loadout-equip");
            var restored=TideStorage.Load(Path.Combine(output,"qa-profile.json"),out _);
            Check("game UI saved both loadouts before returning to combat",restored.skillLoadouts[0].skills[1]=="rk_s5"&&
                restored.skillLoadouts[1].skills[0]=="oh_s5"&&restored.level==4&&restored.gold==fixture.gold);

            LeaveToTown();yield return TapUi("hero-0");
            game.StartBattle(true);yield return null;
            yield return TapUi("pause");yield return TapUi("training-loadout");
            yield return TapUi("loadout-slot-2");yield return TapUi("loadout-skill-8");yield return TapUi("loadout-equip");
            Check("training UI allows temporary preview of a later skill",session.EquippedSkill(2)==8&&VisibleText("수련을 종료할 때 초기화"));
            yield return TapUi("close");yield return TapUi("resume");
            int serial=session.AttackSerial;
            yield return TapUi("skill-2");
            var icon=game.Buttons["skill-2"].transform.Find("SkillIcon").GetComponent<Image>();
            Check("equipped slot changes live icon, cast identity and effect",session.AttackSerial==serial+1&&session.Action==MoveKind.Skill8&&
                icon.sprite==TideSkillIcons.Get(0,8)&&game.LastSkillFxId=="rk_s8");
            yield return Capture("87-combat-four-slots");
            game.ReturnTown();
            Check("saved campaign slots return after training",session.EquippedSkill(1)==5&&session.EquippedSkill(2)==1);
            JsonUtility.FromJsonOverwrite(saved,session.Profile);
            session.ClearQuestNotifications();game.ReturnTown();yield return null;
        }
    }
}
