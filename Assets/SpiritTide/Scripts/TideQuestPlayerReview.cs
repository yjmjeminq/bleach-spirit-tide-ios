using System.Collections;
using UnityEngine;
using UnityEngine.UI;
namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        private IEnumerator OpenSideCard(TideSideQuest q)
        {
            game.ReturnTown();yield return null;if(game.Session.Mode==TideMode.Story)yield return ReadStory();
            yield return TapUi("npc-"+q.giver);if(game.Session.Mode==TideMode.Story)yield return ReadStory();
            yield return TapUi("npc-requests");yield return TapUi("quest-act-"+q.act);yield return TapUi("side-select-"+q.id);
        }
        private IEnumerator ReachQuestRoom(int room,bool clear)
        {
            int guard=0;var s=game.Session;
            while(s.IsFighting&&(s.RoomIndex<room||clear&&!s.RoomCleared)&&guard++<6000)
            {
                for(int i=0;i<120&&s.IsFighting&&(s.RoomIndex<room||clear&&!s.RoomCleared);i++)
                {TideCombatDriver.Tick(s,1f/60,false);if(s.RoomIndex<room&&s.AtGate)game.AdvanceRoom();}
                yield return null;if(s.Mode==TideMode.Story)yield return ReadStory();
            }
            Check("quest destination reached with actual normal combat",s.IsFighting&&s.Hp>0&&s.RoomIndex==room&&(!clear||s.RoomCleared));
        }
        private IEnumerator MoveToQuestPoint(Vector2 position)
        {
            int guard=0;while(Vector2.Distance(new Vector2(game.Session.X,game.Session.Y),position)>.4f&&guard++<150)
            {for(int i=0;i<12&&Vector2.Distance(new Vector2(game.Session.X,game.Session.Y),position)>.4f;i++)TideQuestDriver.MoveTo(game.Session,position);yield return null;}
            Check("physical field target is in interaction reach",game.Session.NearInteraction);yield return null;
        }
        private IEnumerator ReviewQuestGameplay()
        {
            string saved=JsonUtility.ToJson(game.Session.Profile);var p=game.Session.Profile;p.uiScale=1.25f;game.ReturnTown();yield return null;
            yield return TapUi("quest-log");yield return TapUi("quest-main");yield return Capture("70-main-quest-book");LeaveToTown();
            int completed=0;int mainMask=p.reportedActs,clearMask=p.clearedDungeons;
            foreach(var q in TideQuests.Data.side)
            {
                p.hero=q.index%6;yield return OpenSideCard(q);yield return Capture("71-side-offer-"+q.id);
                yield return TapUi("side-accept");Check("reading request is not already acceptance "+q.id,TideQuests.State(p,q.id)==null);yield return ReadStory();
                var state=TideQuests.State(p,q.id);Check("actual NPC request is accepted and tracked "+q.id,state!=null&&state.step==0&&p.trackedSide==q.id);
                for(int index=0;index<q.steps.Length;index++)
                {
                    var step=q.steps[index];
                    if(step.kind=="talk")
                    {
                        game.ReturnTown();yield return null;yield return TapUi("npc-"+step.npc);if(game.Session.Mode==TideMode.Story)yield return ReadStory();
                    }
                    else if(step.kind=="cancel"||step.kind=="perfect")
                    {
                        yield return OpenSideCard(q);yield return TapUi("side-go");Check("practice objective enters actual training "+q.id,game.Session.IsTraining);
                        int guard=0;while(state.step==index&&guard++<100)
                        {for(int i=0;i<120&&state.step==index;i++)TideQuestDriver.PracticeTick(game.Session,step.kind);yield return null;}
                        if(step.kind=="perfect")yield return Capture("72-perfect-dodge-training");
                    }
                    else
                    {
                        yield return OpenSideCard(q);yield return TapUi("side-go");Check("side route opens the correct historical act "+q.id,VisibleText("균열 지도 · Act "+q.act));
                        int stage=step.kind=="ward"?2:step.stage;yield return TapUi("dungeon-"+stage);yield return ReadStory();
                        yield return ReachQuestRoom(step.kind=="ward"?1:step.room,step.kind!="ward");
                        if(step.kind=="ward")yield return MoveToQuestPoint(game.Session.WardPosition);
                        else
                        {
                            bool found=false;TideQuestPoint point=default;
                            for(int i=0;i<game.Session.QuestPointCount;i++){var candidate=game.Session.QuestPoint(i);if(candidate.quest!=null&&candidate.quest.id==q.id){point=candidate;found=true;break;}}
                            Check("accepted quest prop exists in its actual room "+q.id,found);yield return MoveToQuestPoint(point.position);
                            if(q.index%3==0)yield return Capture("73-field-request-"+q.id+"-"+index);
                        }
                        yield return TapUi("field-interact");if(game.Session.Mode==TideMode.Story)yield return ReadStory();
                    }
                    game.ReturnTown();yield return null;if(game.Session.Mode==TideMode.Story)yield return ReadStory();
                    Check("touch or actual practice completes intended objective "+q.id+" / "+index,state.step==index+1);
                }
                yield return OpenSideCard(q);int choice=q.choices.Length-1,gold=p.gold,crystals=p.crystals;Button claim=game.Buttons["side-claim-"+choice];CheckHit("side-claim-"+choice);claim.onClick.Invoke();claim.onClick.Invoke();yield return null;
                Check("report reward waits until the conversation finishes "+q.id,p.gold==gold&&!state.claimed);yield return ReadStory();
                Check("report choice is rewarded once without changing main progress "+q.id,state.claimed&&state.choice==choice&&p.gold==gold+q.gold&&p.crystals==crystals+q.crystals&&p.reportedActs==mainMask&&p.clearedDungeons==clearMask);
                Check("chosen report alone unlocks its dialogue "+q.id,TideDialogue.CanRead(p,TideDialogue.Scene(q.finish_scenes[choice])));
                if(q.choices.Length>1)Check("other personal choice stays hidden "+q.id,!TideDialogue.CanRead(p,TideDialogue.Scene(q.finish_scenes[0])));
                if(q.index%3==2)yield return Capture("74-side-complete-act"+q.act);
                yield return TapUi("side-after");Check("NPC acknowledges actual chosen aftermath "+q.id,VisibleText(q.choice_notes[choice])&&DialogueFits());completed++;
            }
            game.ReturnTown();yield return Capture("75-town-after-fifteen-requests");Check("all fifteen side quests completed through live controls",completed==15&&p.sideQuests.Length==15);
            string completedRequests=JsonUtility.ToJson(p);
            var junction=new TideProfile{campaignSchema=1,questSchema=1,campaignAct=2,campaignStage=4,quest=1,level=12,reportedActs=1,clearedDungeons=511,mainInvestigations=1,uiScale=1.25f};TideCatalog.Sanitize(junction);JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(junction),p);game.Session.ClearQuestNotifications();game.ReturnTown();game.Session.AcceptSideQuest("side_a2_1");var joint=TideQuests.Find("side_a2_1");
            yield return OpenSideCard(joint);yield return TapUi("side-go");yield return TapUi("dungeon-1");yield return ReadStory();yield return ReachQuestRoom(0,true);yield return MoveToQuestPoint(game.Session.QuestPoint(0).position);yield return TapUi("field-interact");yield return ReadStory();game.ReturnTown();yield return null;
            yield return TapUi("quest-log");yield return TapUi("quest-main");yield return Capture("77-main-clue-before-departure");LeaveToTown();
            yield return OpenSideCard(joint);yield return TapUi("side-go");yield return TapUi("dungeon-4");yield return ReadStory();yield return ReachQuestRoom(0,true);Check("live main and side investigation appear together",game.Session.QuestPointCount==2&&game.Session.MainInvestigationPending);yield return new WaitForSecondsRealtime(.6f);yield return Capture("76-main-and-side-same-room");
            yield return MoveToQuestPoint(game.Session.QuestPoint(1).position);yield return TapUi("field-interact");yield return ReadStory();Check("optional field completion leaves main gate locked",game.Session.MainInvestigationPending&&!game.Session.GateReady);
            yield return MoveToQuestPoint(game.Session.QuestPoint(0).position);yield return TapUi("field-interact");yield return ReadStory();Check("main field completion opens route and saves checkpoint",game.Session.GateReady&&p.roomCheckpoint==1&&TideQuests.Ready(p,joint));
            JsonUtility.FromJsonOverwrite(completedRequests,p);game.Session.ClearQuestNotifications();game.ReturnTown();yield return null;
            yield return TapUi("npc-byakuya");yield return TapUi("choice-0");yield return TapUi("journal-dialogues");int sideScenes=0;
            for(int act=1;act<=5;act++)
            {
                yield return TapUi("dialogue-act-"+act);var scenes=TideDialogue.Available(p,act);
                for(int i=0;i<scenes.Count;i++)
                {
                    var scene=scenes[i];if(scene.kind.StartsWith("side_"))
                    {
                        Check("completed side scene appears in ordered archive "+scene.id,VisibleText(scene.title));var lines=TideDialogue.Lines(scene.id,TideDialogue.ArchiveHero(p,scene.id));
                        for(int page=1;page<lines.Length;page++)yield return TapUi("dialogue-line-next");sideScenes++;
                    }
                    if(i+1<scenes.Count)yield return TapUi("dialogue-scene-next");
                }
            }
            Check("all sixty played optional conversations can be reread",sideScenes==60);
            JsonUtility.FromJsonOverwrite(saved,p);game.Session.ClearQuestNotifications();game.ReturnTown();yield return null;
        }
    }
}
