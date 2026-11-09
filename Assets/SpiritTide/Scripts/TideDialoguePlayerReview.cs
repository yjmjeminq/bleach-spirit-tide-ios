using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        private void ReviewDialogueFonts()
        {
            var probeObject=new GameObject("DialogueFontProbe",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));probeObject.SetActive(false);
            var probe=probeObject.GetComponent<Text>();probe.font=Resources.Load<Font>("SpiritTide/UI/Pretendard-Regular");probe.horizontalOverflow=HorizontalWrapMode.Wrap;probe.verticalOverflow=VerticalWrapMode.Truncate;
            foreach(var scene in TideDialogue.Data.scenes)for(int hero=0;hero<6;hero++)
            {
                bool storyFits=true,archiveFits=true;
                foreach(var line in TideDialogue.Lines(scene.id,hero))
                {
                    probe.text=line.text;probe.fontSize=40;probe.rectTransform.sizeDelta=new Vector2(704,180);storyFits&=probe.preferredHeight<=180;
                    probe.fontSize=31;probe.rectTransform.sizeDelta=new Vector2(950,112);archiveFits&=probe.preferredHeight<=112;
                }
                Check("all dialogue pages fit maximum story font "+scene.id+" / "+hero,storyFits);
                Check("all dialogue pages fit maximum archive font "+scene.id+" / "+hero,archiveFits);
            }
            Destroy(probeObject);
        }

        private IEnumerator ReviewVoicePresentation()
        {
            ReviewDialogueFonts();
            var session=game.Session;
            string saved=JsonUtility.ToJson(session.Profile);
            var ending=new TideProfile{campaignSchema=1,campaignAct=5,campaignStage=6,quest=3,level=29,
                reportedActs=31,clearedDungeons=TideProgress.AllDungeons,claimed=true,uiScale=1.25f};
            TideCatalog.Sanitize(ending);
            string fixture=JsonUtility.ToJson(ending);
            for(int hero=0;hero<6;hero++)
            {
                JsonUtility.FromJsonOverwrite(fixture,session.Profile);game.ReturnTown();yield return null;
                yield return TapUi("hero-"+hero);
                int act=hero%5+1;
                yield return TapUi("npc-kukaku");yield return TapUi("choice-0");
                yield return TapUi("map-act-"+act);yield return TapUi("dungeon-1");
                Check("selected fighter takes the field for the spoken response "+hero,
                    session.IsReplay&&session.Dungeon.act==act&&session.Profile.hero==hero);
                yield return ReviewFirstLantern(act,true);
            }
            JsonUtility.FromJsonOverwrite(saved,session.Profile);
            session.ClearQuestNotifications();game.ReturnTown();yield return null;
        }

        private IEnumerator ReviewDialogueLibrary()
        {
            string saved=JsonUtility.ToJson(game.Session.Profile);
            ReviewDialogueFonts();
            int main=0;foreach(var record in game.Session.Profile.dialogueHistory)if(record.id.EndsWith("_entry")||record.id.EndsWith("_intro")||record.id.EndsWith("_clear")||record.id.EndsWith("_report")||record.id.StartsWith("main_a"))main++;
            Check("native campaign records all one hundred and ten main scenes",main==110);
            foreach(string npc in new[]{"urahara","tessai","kon","byakuya","kukaku"})
            {game.ReturnTown();yield return TapUi("npc-"+npc);Check("ending town reply remains readable "+npc,DialogueFits());game.NavigateBack();}

            // Real optional dialogue controls for every hero in every act, with
            // progress fixtures only; these do not claim new campaign victories.
            for(int act=1;act<=5;act++)for(int hero=0;hero<6;hero++)
            {
                JsonUtility.FromJsonOverwrite(saved,game.Session.Profile);var p=game.Session.Profile;p.campaignAct=act;p.campaignStage=1;p.quest=1;p.claimed=false;p.reportedActs=(1<<(act-1))-1;p.hero=hero;p.uiScale=1.25f;TideCatalog.Sanitize(p);game.ReturnTown();
                yield return TapUi("npc-kon");yield return TapUi("choice-1");
                Check("companion conversation opens for current act and hero "+act+" / "+hero,game.Session.Mode==TideMode.Story&&DialogueFits());
                if(act==4&&hero==4)yield return Capture("60-rangiku-before-gin");
                while(game.Session.Mode==TideMode.Story){Check("companion conversation page fits "+act+" / "+hero+" / "+game.StoryPage,DialogueFits());Click("choice-0");yield return null;}
                Check("companion conversation is remembered "+act+" / "+hero,TideDialogue.Seen(p,"a"+act+"_party_"+TideCatalog.Characters[hero].id)!=null);
            }
            JsonUtility.FromJsonOverwrite(saved,game.Session.Profile);var completed=game.Session.Profile;completed.uiScale=1.25f;completed.rescueReports=31;
            int originalLead=TideDialogue.Seen(completed,"a1_d1_entry").hero;completed.hero=(originalLead+1)%6;game.ReturnTown();
            yield return TapUi("npc-byakuya");yield return TapUi("choice-0");yield return TapUi("journal-dialogues");
            string profileBefore=JsonUtility.ToJson(completed);int visited=0;
            for(int act=1;act<=5;act++)
            {
                yield return TapUi("dialogue-act-"+act);var scenes=TideDialogue.Available(completed,act);
                for(int index=0;index<scenes.Count;index++)
                {
                    var scene=scenes[index];Check("archive title follows story order "+scene.id,VisibleText(scene.title));
                    var lines=TideDialogue.Lines(scene.id,TideDialogue.ArchiveHero(completed,scene.id));
                    for(int page=0;page<lines.Length;page++)
                    {
                        Text current=null;foreach(var t in game.GetComponentsInChildren<Text>())if(t.name=="DialogueArchiveText")current=t;
                        Check("archive renders exact speaker-page text "+scene.id+" / "+page,current!=null&&current.text==lines[page].text&&current.preferredHeight<=current.rectTransform.rect.height);
                        if(page==0&&(scene.id=="a1_d1_entry"||scene.id=="a4_d3_clear"||scene.id=="a5_report"))yield return Capture("61-archive-"+scene.id);
                        if(page==lines.Length-1&&scene.id=="a1_d1_entry")Check("archive retains original lead after switching hero",current.text==TideExperience.Voice(originalLead,1).departure&&completed.hero!=originalLead);
                        if(page+1<lines.Length)yield return TapUi("dialogue-line-next");
                    }
                    visited++;if(index+1<scenes.Count)yield return TapUi("dialogue-scene-next");
                }
            }
            Check("all completed main companion and actual rescue scenes are navigable",visited==151);
            Check("reading archive does not alter rewards progress or history",profileBefore==JsonUtility.ToJson(completed));LeaveToTown();

            var fresh=new TideProfile{hero=3};TideCatalog.Sanitize(fresh);JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(fresh),completed);game.ReturnTown();
            yield return TapUi("npc-kon");Check("companion dialogue waits for the current act quest",!game.Buttons["choice-1"].interactable);LeaveToTown();
            yield return TapUi("npc-byakuya");yield return TapUi("choice-0");yield return TapUi("journal-dialogues");yield return TapUi("dialogue-act-5");
            Check("unplayed act archive hides future names and dialogue",VisibleText("아직 열린 대화가 없습니다.")&&!VisibleText("유하바하"));yield return Capture("62-archive-locked");LeaveToTown();
            game.Session.AcceptQuest();game.StartBattle(false);yield return null;
            Check("story pause preserves battle clock",game.Session.Mode==TideMode.Story);float clock=game.Session.Clock;yield return null;Check("story cannot advance combat simulation",Mathf.Abs(game.Session.Clock-clock)<.0001f);
            yield return TapUi("choice-2");Check("abandoning briefing does not record it",TideDialogue.Seen(completed,"a1_d1_entry")==null&&game.Session.Mode==TideMode.Town);
            game.StartBattle(false);yield return null;yield return TapUi("choice-1");
            Check("skip records scene once and begins actual battle",TideDialogue.Seen(completed,"a1_d1_entry")!=null&&game.Session.IsFighting&&completed.dialogueHistory.Length==1);
            JsonUtility.FromJsonOverwrite(saved,completed);game.ReturnTown();yield return null;
        }
    }
}
