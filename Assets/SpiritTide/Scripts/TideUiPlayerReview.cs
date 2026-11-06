using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        private void LeaveToTown(){for(int i=0;i<8&&game.Session.Mode!=TideMode.Town;i++)game.NavigateBack();Check("back path reaches town",game.Session.Mode==TideMode.Town);}
        private bool VisibleText(string fragment)
        {
            foreach(Text label in game.GetComponentsInChildren<Text>())if(label.text.Contains(fragment))return true;return false;
        }
        private void Tap(string id)
        {
            CheckHit(id);Button button=game.Buttons[id];
            var pointer=new PointerEventData(EventSystem.current){pointerId=-1,button=PointerEventData.InputButton.Left,position=HitPoint(button)};
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerUpHandler);
            ExecuteEvents.Execute(button.gameObject,pointer,ExecuteEvents.pointerClickHandler);
        }
        private IEnumerator ReviewUi()
        {
            Check("game owns keyboard submission once",!EventSystem.current.sendNavigationEvents);
            game.Session.X=2.3f;game.Session.Y=.6f;
            yield return Capture("20-town-objective");Tap("npc-tessai");Click("choice-0");
            yield return Capture("20-inventory");Tap("close");
            Check("service back returns to its NPC",game.Session.Mode==TideMode.Dialogue&&VisibleText("수련장에 들어간다"));
            game.NavigateBack();
            Check("closing service preserves town position",game.Session.Mode==TideMode.Town&&Mathf.Abs(game.Session.X-2.3f)<.01f&&Mathf.Abs(game.Session.Y-.6f)<.01f);
            Click("pause");yield return Capture("80-home");Click("home-settings");yield return Capture("20-settings");
            Vector2 behind=RectTransformUtility.WorldToScreenPoint(null,game.Buttons["pause"].transform.position);
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=behind},hits);
            Check("modal shade intercepts background touches",hits.Count>0&&hits[0].gameObject.name=="Menus");
            EventSystem.current.SetSelectedGameObject(game.Buttons["hud-size"].gameObject);game.ActivateMenuSelection();
            Check("keyboard activation changes control preset once",game.Session.Profile.controlScale==1.1f);
            Click("hud-opacity");Click("sound");Click("font");
            Check("readability and sound preferences apply",game.Session.Profile.controlOpacity==1&&!game.Session.Profile.soundEnabled&&game.Session.Profile.uiScale>1);
            yield return Capture("20-settings-large");
            EventSystem.current.SetSelectedGameObject(game.Buttons["close"].gameObject);game.CycleMenuFocus(true);
            Check("reverse tab wraps to last menu action",EventSystem.current.currentSelectedGameObject==game.Buttons["help"].gameObject);game.CycleMenuFocus(false);
            Check("forward tab wraps back to first menu action",EventSystem.current.currentSelectedGameObject==game.Buttons["close"].gameObject);
            Click("help");yield return Capture("21-controls");
            Check("help returns to original settings",game.Session.Mode==TideMode.Help);Click("back");Check("nested help back",game.Session.Mode==TideMode.Settings);
            game.NavigateBack();game.NavigateBack();
            Check("settings back keeps same town position",game.Session.Mode==TideMode.Town&&Mathf.Abs(game.Session.X-2.3f)<.01f);
            yield return Capture("20-town-large");
            for(int hero=0;hero<6;hero++)
            {
                Click("hero-"+hero);Click("pause");Click("help");Click("help-skills");
                for(int slot=0;slot<5;slot++)
                {
                    Click("help-slot-"+slot);yield return Capture("21-skill-"+hero+"-"+slot);
                    Check("skill details match selected hero and slot "+hero+" / "+slot,VisibleText(game.Session.Hero.Skill(slot).name)&&VisibleText(TideUiInfo.Skill(hero,slot)));
                }
                LeaveToTown();
            }
            Click("hero-5");Click("npc-tessai");Click("choice-1");Click("skill-3");yield return new WaitForSecondsRealtime(.3f);
            Check("Unohana stance is visible",game.Session.CounterStance&&VisibleText("검격 태세"));yield return Capture("22-large-training");
            Click("pause");Click("hud-opacity");Click("resume");yield return Capture("22-soft-training");
            Check("soft control preset renders at selected alpha",Mathf.Abs(game.transform.Find("GameCanvas/SafeArea/TouchControls").GetComponent<CanvasGroup>().alpha-.65f)<.01f);
            Transform actors=game.transform.Find("GameCanvas/SafeArea/World/Actors");game.Session.Y=1;yield return null;
            Check("deeper player draws behind enemy",actors.Find("Player").GetSiblingIndex()<actors.Find("target-0").GetSiblingIndex());game.Session.Y=-1;yield return null;
            Check("nearer player draws in front of enemy",actors.Find("Player").GetSiblingIndex()>actors.Find("target-0").GetSiblingIndex());game.Session.Y=0;
            while(game.Session.ActionRemaining>0)yield return null;
            Click("skill-4");yield return new WaitForSecondsRealtime(.1f);Click("pause");
            float remaining=game.Session.ActionRemaining,clock=game.Session.Clock;int damage=game.Session.DamageEvents;
            Check("pause keeps the current cast",remaining>0&&game.Session.IsPaused);
            Click("help");yield return Capture("22-paused-help");game.NavigateBack();
            Check("help back preserves battle and action",game.Session.Mode==TideMode.Pause&&game.Session.IsPaused&&game.Session.ActionRemaining==remaining&&game.Session.Clock==clock);
            Check("pause hides training character switching",!game.Buttons["hero-0"].gameObject.activeInHierarchy);
            Click("resume");yield return new WaitForSecondsRealtime(.6f);
            Check("closing help resumes pending skill hit once",game.Session.Mode==TideMode.Training&&game.Session.DamageEvents==damage+1);
            Check("landed attack has a visible damage number",VisibleText("85"));yield return Capture("23-combat-feedback");
            Click("pause");Click("retreat");
            Check("training exits directly with quest intact",game.Session.Mode==TideMode.Town&&game.Session.Quest==QuestState.Available&&game.Session.Profile.gold==800&&game.Session.Profile.potions==3);
            // Restore through the same settings controls; the story regression starts fresh.
            Click("pause");Click("home-settings");Click("hud-size");Click("hud-opacity");Click("sound");Click("font");Click("resume");game.NavigateBack();Click("hero-0");
            Check("defaults restored without changing progress",game.Session.Profile.controlScale==1&&Mathf.Abs(game.Session.Profile.controlOpacity-.85f)<.01f&&game.Session.Profile.soundEnabled&&game.Session.Profile.uiScale==1&&game.Session.Quest==QuestState.Available);
        }
    }
}
