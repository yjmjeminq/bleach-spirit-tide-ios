using System;
using System.IO;
using UnityEngine;
namespace SpiritTide.EditorTools
{
    public static class TideQuestValidation
    {
        private static TideProfile Ending()=>new TideProfile{campaignSchema=1,questSchema=1,campaignAct=5,campaignStage=6,level=29,quest=3,claimed=true,reportedActs=31,clearedDungeons=TideProgress.AllDungeons,mainInvestigations=31};
        private static void ClearTo(TideSession s,int room)
        {
            for(int i=0;i<100000&&s.IsFighting&&(s.RoomIndex<room||!s.RoomCleared);i++)
            {TideCombatDriver.Tick(s,1f/60,false);if(s.AtGate&&s.RoomIndex<room)s.EnterGate();}
        }
        private static void VisitPoint(TideSession s,TideQuestPoint point)
        {
            for(int i=0;i<1200&&Vector2.Distance(new Vector2(s.X,s.Y),point.position)>.4f;i++)TideQuestDriver.MoveTo(s,point.position);
            s.InteractWorld();
        }
        public static void Run(Action<string,bool> check)
        {
            check("five main chapters and forty-five optional requests",TideQuests.Data.main.Length==5&&TideQuests.Data.side.Length==45);
            var locked=new TideSession(new TideProfile());check("side quests wait for a main request and hide future acts",!locked.AcceptSideQuest("side_a1_1")&&!locked.AcceptSideQuest("side_a5_1"));
            locked.AcceptQuest();check("later side story waits for its main clue",!locked.AcceptSideQuest("side_a3_3"));
            check("unvisited rooms cannot be opened by the side route",!locked.BeginDungeon(1,1,true,0)&&!locked.BeginDungeon(1,1,true,3));
            for(int act=1;act<=5;act++)
            {
                var p=new TideProfile{campaignSchema=1,questSchema=1,campaignAct=act,campaignStage=4,quest=1,level=25,mainInvestigations=(1<<(act-1))-1};var s=new TideSession(p);s.BeginDungeon();ClearTo(s,0);
                check("main field clue remains after room combat "+act,s.IsFighting&&s.RoomCleared&&s.MainInvestigationPending&&p.roomCheckpoint==0);
                s.X=s.GateX;check("room gate cannot bypass main investigation "+act,!s.EnterGate());s.ReturnTown();s.BeginDungeon();check("return before clue resumes unfinished investigation "+act,s.RoomIndex==0&&s.MainInvestigationPending);
                ClearTo(s,0);VisitPoint(s,s.QuestPoint(0));check("actual main interaction saves evidence and checkpoint "+act,(p.mainInvestigations&(1<<(act-1)))!=0&&p.roomCheckpoint==1&&!s.MainInvestigationPending);
                s.ReturnTown();s.BeginDungeon();check("return after clue resumes next room "+act,s.RoomIndex==1);
            }
            int branches=0;
            foreach(var q in TideQuests.Data.side)
            {
                check("side quest has conversations and fair optional rewards "+q.id,q.steps.Length>=2&&q.steps.Length<=3&&q.gold>0&&q.xp>0&&q.choices.Length==q.finish_scenes.Length&&q.choice_notes.Length==q.choices.Length);
                for(int choice=0;choice<q.choices.Length;choice++)
                {
                    var s=new TideSession(Ending());int clears=s.Profile.clearedDungeons,main=s.Profile.reportedActs;
                    check("side request accepts once and not before completion "+q.id+" / "+choice,s.AcceptSideQuest(q.id)&&!s.AcceptSideQuest(q.id)&&!s.ClaimSideQuest(q.id,choice));
                    var state=TideQuests.State(s.Profile,q.id);
                    for(int index=0;index<q.steps.Length;index++)
                    {
                        var step=q.steps[index];
                        if(step.kind=="talk")
                        {
                            s.ReturnTown();check("wrong NPC does not advance request "+q.id,!s.QuestTalk(step.npc=="kon"?"urahara":"kon"));s.QuestTalk(step.npc);
                        }
                        else if(step.kind=="cancel"||step.kind=="perfect")
                        {
                            s.BeginBattle(true,0);for(int i=0;i<5000&&state.step==index;i++)TideQuestDriver.PracticeTick(s,step.kind);s.ReturnTown();
                        }
                        else
                        {
                            int stage=step.stage==0?2:step.stage,room=step.stage==0?1:step.room;
                            int goldBefore=s.Profile.gold,checkpoint=s.Profile.roomCheckpoint;
                            check("only the exact cleared objective room opens "+q.id+" / "+index,
                                !s.BeginDungeon(0,stage,true,room)&&s.BeginDungeon(q.act,stage,true,room)&&s.IsFieldVisit&&s.RoomIndex==room);
                            if(step.kind=="site")
                            {
                                ClearTo(s,room);TideQuestPoint point=default;bool found=false;
                                for(int i=0;i<s.QuestPointCount;i++){var item=s.QuestPoint(i);if(item.quest!=null&&item.quest.id==q.id){point=item;found=true;break;}}
                                check("quest site is reachable through real combat "+q.id+" / "+index,found&&s.Hp>0);VisitPoint(s,point);
                            }
                            else if(step.kind=="ward")
                            {
                                for(int i=0;i<500&&state.step==index;i++){TideQuestDriver.MoveTo(s,s.WardPosition);if(s.NearInteraction)s.InteractWorld();}
                            }
                            else for(int i=0;i<60000&&s.IsFighting&&state.step==index;i++)TideCombatDriver.Tick(s,1f/60,false);
                            check("field visits cannot advance the main route or pay loot "+q.id+" / "+index,
                                !s.EnterGate()&&s.LastXp==0&&s.Profile.gold==goldBefore&&s.Profile.roomCheckpoint==checkpoint);
                            s.ReturnTown();
                        }
                        check("actual objective advances exactly one step "+q.id+" / "+index,state.step==index+1);
                    }
                    int gold=s.Profile.gold,crystals=s.Profile.crystals;
                    check("side report rewards once and keeps chosen response "+q.id+" / "+choice,s.ClaimSideQuest(q.id,choice)&&!s.ClaimSideQuest(q.id,choice)&&state.choice==choice&&state.claimed&&s.Profile.gold==gold+q.gold&&s.Profile.crystals==crystals+q.crystals);
                    check("optional catch-up never changes main ending or first-clear bits "+q.id,s.Profile.clearedDungeons==clears&&s.Profile.reportedActs==main&&s.CampaignFinished);
                    check("selected aftermath appears in town memory "+q.id,TideQuests.TownMemory(s.Profile).Contains(q.choice_keepsakes[choice]));
                    string path=Path.GetFullPath("BuildLogs/side-quest-save-"+q.index+"-"+choice+".json"),error,notice;TideStorage.Save(path,s.Profile,out error);var restored=TideStorage.Load(path,out notice);
                    check("quest choice and reward flag survive checksum reload "+q.id,TideQuests.State(restored,q.id).claimed&&TideQuests.State(restored,q.id).choice==choice);branches++;
                }
            }
            check("all forty-eight optional report outcomes exercised",branches==48);
            var joint=new TideSession(new TideProfile{campaignSchema=1,questSchema=1,campaignAct=2,campaignStage=4,quest=1,level=12,reportedActs=1,clearedDungeons=511,mainInvestigations=1});
            joint.AcceptSideQuest("side_a2_1");joint.BeginDungeon(2,1,true);ClearTo(joint,0);VisitPoint(joint,joint.QuestPoint(0));joint.ReturnTown();joint.BeginDungeon(2,4,false);ClearTo(joint,0);
            check("main and optional clues coexist in a normal ongoing campaign",joint.QuestPointCount==2&&joint.MainInvestigationPending);
            VisitPoint(joint,joint.QuestPoint(1));check("optional interaction cannot satisfy mandatory evidence",TideQuests.Ready(joint.Profile,TideQuests.Find("side_a2_1"))&&joint.MainInvestigationPending&&!joint.GateReady&&joint.Profile.roomCheckpoint==0);
            VisitPoint(joint,joint.QuestPoint(0));check("mandatory evidence releases gate without claiming optional rewards",joint.GateReady&&joint.Profile.roomCheckpoint==1&&!TideQuests.State(joint.Profile,"side_a2_1").claimed);
            var old=new TideProfile{campaignSchema=1,campaignAct=5,campaignStage=6,quest=3,claimed=true,reportedActs=31,clearedDungeons=TideProgress.AllDungeons,mainInvestigations=0};TideCatalog.Sanitize(old);
            check("old ending gets main evidence but no invented side completions",old.mainInvestigations==31&&old.mainEvidence==(1<<25)-1&&old.sideQuests.Length==0);
        }
    }
}
