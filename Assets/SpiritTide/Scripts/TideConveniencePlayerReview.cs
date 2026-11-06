using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        private IEnumerator ReviewConvenience()
        {
            var p=game.Session.Profile;string saved=JsonUtility.ToJson(p);game.ReturnTown();yield return null;
            yield return TapUi("pause");Check("town menu opens a service hub",game.Session.Mode==TideMode.Home);
            yield return TapUi("home-settings");yield return TapUi("hints");Check("keyboard hint preference can be explicit",p.keyboardHints==1);
            yield return TapUi("hints");Check("keyboard hint preference can hide keys",p.keyboardHints==2);
            game.NavigateBack();Check("settings back restores home selection",game.Session.Mode==TideMode.Home&&EventSystem.current.currentSelectedGameObject==game.Buttons["home-settings"].gameObject);
            yield return TapUi("home-gear");
            var gear=new TideGear[48];for(int i=0;i<gear.Length;i++)gear[i]=new TideGear{uid=101+i,name=(i%2==0?"공격":"수호")+" 영장 "+(i+1),slot=i%2==0?"power":"guard",tier=i%5,rarity=i%3};
            p.gear=gear;p.powerGear=101;p.guardGear=102;game.Session.Hp=game.Session.MaxHp;game.Session.Revision++;yield return null;
            string before=JsonUtility.ToJson(p);yield return TapUi("gear-filter-2");yield return TapUi("gear-sort");yield return TapUi("gear-next");yield return TapUi("gear-1");
            string selected=game.Buttons["gear-1"].GetComponentInChildren<Text>().text;
            Check("guard filter only shows comparable health items",game.Buttons["gear-0"].GetComponentInChildren<Text>().text.Contains("최대 체력")&&selected.Contains("최대 체력"));
            Check("gear comparison exposes the actual change",VisibleText("변화")||VisibleText("성능 동일")||VisibleText("현재 착용 중"));
            yield return Capture("81-gear-comparison");yield return TapUi("forge");yield return TapUi("gear-sort");yield return TapUi("close");
            Check("forge back restores filter sort page and selected gear",game.Session.Mode==TideMode.Inventory&&game.Buttons["gear-sort"].GetComponentInChildren<Text>().text.Contains("성능순")&&game.Buttons["gear-1"].GetComponentInChildren<Text>().text==selected);
            Check("browsing and comparing equipment never auto equips or spends",before==JsonUtility.ToJson(p));LeaveToTown();

            JsonUtility.FromJsonOverwrite(saved,p);p.uiScale=1.25f;game.ReturnTown();yield return null;
            yield return TapUi("pause");yield return TapUi("home-journal");yield return TapUi("journal-dialogues");yield return TapUi("dialogue-index");
            yield return TapUi("archive-act-1");yield return TapUi("archive-filter-2");
            Check("companion scene category contains six unlocked leads",game.Buttons["archive-scene-5"].gameObject.activeInHierarchy&&!game.Buttons["archive-next"].interactable);
            string title=game.Buttons["archive-scene-4"].GetComponentInChildren<Text>().text;
            yield return TapUi("archive-scene-4");Check("scene list jumps directly to the selected conversation",VisibleText(title));
            before=JsonUtility.ToJson(p);yield return TapUi("dialogue-line-next");yield return TapUi("dialogue-index");
            Check("scene list keeps its act and category",VisibleText(title)&&!game.Buttons["archive-next"].interactable);yield return Capture("82-dialogue-scene-list");
            yield return TapUi("archive-filter-0");yield return TapUi("archive-next");yield return TapUi("archive-scene-2");yield return TapUi("dialogue-index");
            Check("scene list remembers its page after reading",game.Buttons["archive-prev"].interactable);yield return Capture("82-dialogue-index-page-two");
            Check("scene index and rereading leave progression untouched",before==JsonUtility.ToJson(p));LeaveToTown();

            var fixture=new TideProfile{campaignSchema=1,questSchema=1,campaignAct=1,campaignStage=4,quest=1,level=12,clearedDungeons=7,mainInvestigations=1,uiScale=1.25f};TideCatalog.Sanitize(fixture);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fixture),p);game.Session.ClearQuestNotifications();game.ReturnTown();game.Session.AcceptSideQuest("side_a1_1");
            var q=TideQuests.Find("side_a1_1");yield return OpenSideCard(q);before=JsonUtility.ToJson(p);yield return TapUi("side-go");
            Check("quest map highlights its exact destination",game.Buttons["dungeon-1"].GetComponentInChildren<Text>().text.StartsWith("목적지")&&game.Buttons["depart"].interactable);
            yield return Capture("83-quest-destination");yield return TapUi("map-act-5");yield return TapUi("close");
            Check("map back restores the quest detail and focus",game.Session.Mode==TideMode.Quests&&VisibleText(q.title)&&EventSystem.current.currentSelectedGameObject==game.Buttons["side-go"].gameObject);
            Check("previewing a route has no progression side effects",before==JsonUtility.ToJson(p));yield return TapUi("side-go");yield return TapUi("depart");
            yield return ReadStory();Check("destination primary enters the exact historical dungeon",game.Session.IsCampaign&&game.Session.IsReplay&&game.Session.Dungeon.act==1&&game.Session.Dungeon.number==1);game.ReturnTown();

            game.Session.AcceptSideQuest("side_a1_2");var ward=TideQuests.Find("side_a1_2");yield return OpenSideCard(ward);yield return TapUi("side-go");yield return TapUi("depart");
            Check("first story line cannot go farther back",!game.Buttons["story-previous"].interactable);before=JsonUtility.ToJson(p);
            var lines=TideStory.BeforeDungeon(TideCampaign.Dungeon(1,4),p,false);float clock=game.Session.Clock;
            Check("current destination has a full multi-line briefing",lines.Length>1);
            yield return TapUi("choice-0");yield return Capture("84-story-previous");var stalePrevious=game.Buttons["story-previous"];yield return TapUi("story-previous");stalePrevious.onClick.Invoke();
            Check("previous story line is restored without running completion",game.Session.Mode==TideMode.Story&&VisibleText(lines[0].text)&&before==JsonUtility.ToJson(p)&&game.Session.Clock==clock);
            yield return ReadStory();Check("ward route prefers an accessible current dungeon",game.Session.Dungeon.number==4&&!game.Session.IsReplay);game.ReturnTown();
            TideQuests.State(p,ward.id).step=1;p.roomCheckpoint=1;yield return OpenSideCard(ward);yield return TapUi("side-go");
            Check("route explains a target behind the current saved room",!game.Buttons["depart"].interactable&&!game.Buttons["dungeon-4"].interactable&&VisibleText("정화 후 재현으로 방문"));yield return Capture("83-route-checkpoint-explanation");LeaveToTown();

            yield return TapUi("pause");yield return TapUi("home-settings");p.keyboardHints=2;game.Session.Revision++;yield return null;LeaveToTown();game.StartBattle(true);yield return null;
            bool hidden=true;foreach(Text label in game.Buttons["skill-0"].GetComponentsInChildren<Text>())if(label.text=="A")hidden=false;
            Check("touch hints hide desktop keys in live controls",hidden);
            p.keyboardHints=0;game.Session.Revision++;yield return null;
            bool shown=false;foreach(Text label in game.Buttons["skill-0"].GetComponentsInChildren<Text>())if(label.text=="A")shown=true;
            Check("automatic hints match the current input layout",shown==(!Application.isMobilePlatform&&System.Array.IndexOf(System.Environment.GetCommandLineArgs(),"-spirit-phone-layout")<0));
            p.keyboardHints=2;game.Session.Revision++;yield return null;yield return Capture("85-touch-controls");game.ReturnTown();
            JsonUtility.FromJsonOverwrite(saved,p);game.Session.ClearQuestNotifications();game.ReturnTown();yield return null;
            Check("save success has a visible truthful indicator",VisibleText("자동 저장됨"));
        }
    }
}
