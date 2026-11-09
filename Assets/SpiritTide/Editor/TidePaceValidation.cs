using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpiritTide.EditorTools
{
    public static class TidePaceValidation
    {
        private const float Step=1f/60;
        private const int TownPerStage=32;
        private const int MenusPerRequest=22;

        [Serializable] private sealed class ActPace
        {
            public int act,rooms,quests,pages,level,defeats;
            public float fieldSeconds,readingSeconds,menuSeconds;
        }

        [Serializable] private sealed class Pace
        {
            public string method="60 Hz runtime movement, combat, checkpoints, investigations and request steps. Dialogue and menus estimated separately.";
            public string readingRule="Per modal line: greater of 2.8 seconds and 1.6 plus 0.095 seconds per character. Concurrent field calls are excluded.";
            public int menuSecondsPerStage=TownPerStage,menuSecondsPerRequest=MenusPerRequest;
            public float attackInterval,fieldSeconds,readingSeconds,menuSeconds,estimatedMinutes;
            public int rooms,requests,pages,finalLevel,defeats;
            public ActPace[] chapters=new ActPace[5];
        }

        public static void Run(Action<string,bool> check)
        {
            var standard=Measure(.84f,check);var deliberate=Measure(1.05f,check);
            File.WriteAllText("BuildLogs/campaign-pace.json",JsonUtility.ToJson(standard,true));
            File.WriteAllText("BuildLogs/campaign-pace-deliberate.json",JsonUtility.ToJson(deliberate,true));
            check("modeled first journey is near three hours with twenty side requests",standard.estimatedMinutes>=165&&standard.estimatedMinutes<=200);
        }

        private static Pace Measure(float attackInterval,Action<string,bool> check)
        {
            var pace=new Pace{attackInterval=attackInterval};var seen=new HashSet<string>();var s=new TideSession(new TideProfile());var profile=s.Profile;
            var learnedAt=new int[5];learnedAt[0]=profile.level;
            check("fresh story starts as Rukia and learns only the first skill",profile.hero==0&&profile.recruitedHeroes==1
                &&s.SkillAvailable(0)&&!s.SkillAvailable(1)&&!s.SkillAvailable(2)&&!s.SkillAvailable(3)&&!s.SkillAvailable(4)&&!s.SelectHero(1));

            for(int act=1;act<=5;act++)
            {
                var chapter=new ActPace{act=act};pace.chapters[act-1]=chapter;
                check("first journey starts the next chapter "+act,s.AcceptQuest());
                Read(TideQuests.Main(act).accept_scene);
                for(int stage=1;stage<=6;stage++)
                {
                    TideCombatDriver.Prepare(s);
                    // Four optional stories that can be picked up at town on the way to a first clear.
                    var request=stage>=2&&stage<=5?TideQuests.Find("side_a"+act+"_"+(stage+3)):null;
                    if(request!=null)
                    {
                        check("first journey accepts local request "+request.id,s.AcceptSideQuest(request.id));
                        Read(request.offer_scene);chapter.menuSeconds+=MenusPerRequest;
                    }

                    check("first journey departs "+act+"-"+stage,s.BeginDungeon());
                    Read(TideDialogue.DungeonId(s.Dungeon,"entry"),stage!=2&&stage!=4&&stage!=5&&!(act==1&&stage==1));
                    float nextAttack=s.Clock;int frames=0,defeats=0;
                    while(s.IsFighting&&frames++<180000)
                    {
                        if(s.Room.kind=="boss")Read(TideDialogue.DungeonId(s.Dungeon,"intro"));
                        int serial=s.AttackSerial;bool firstLantern=stage==1&&s.EchoAvailable;
                        var state=request==null?null:TideQuests.State(profile,request.id);
                        TideQuestStep step=state!=null&&state.step<request.steps.Length?request.steps[state.step]:null;
                        if(step?.kind=="ward"&&step.room==s.RoomIndex&&s.WardActive&&!s.RoomCleared)
                        {
                            var delta=s.WardPosition-new Vector2(s.X,s.Y);
                            s.Step(Step,delta.normalized,false);
                            if(delta.magnitude<1f)s.InteractWorld();
                        }
                        else TideCombatDriver.Tick(s,Step,attackOpportunity:s.Clock>=nextAttack,visitSites:true);
                        if(serial!=s.AttackSerial)nextAttack=s.Clock+attackInterval;
                        if(firstLantern&&!s.EchoAvailable)Read(TideDialogue.DungeonId(s.Dungeon,"answer"),false);
                        while(s.PendingQuestScene!=null){Read(s.PendingQuestScene);s.ConsumeQuestScene();}
                        if(s.Mode==TideMode.Defeat&&defeats++<3)
                        {
                            TideCombatDriver.Prepare(s);check("first journey resumes the saved room",s.BeginDungeon());
                            nextAttack=s.Clock;
                        }
                    }
                    check("first journey clears "+act+"-"+stage+" with attack interval "+attackInterval+" frames="+frames+" room="+s.RoomIndex+" hp="+s.Hp+" foes="+s.AliveCount,
                        s.Mode==TideMode.Result);
                    chapter.fieldSeconds+=frames*Step;chapter.rooms+=s.Dungeon.rooms.Length;chapter.defeats+=defeats;
                    Read(TideDialogue.DungeonId(s.Dungeon,"clear"));s.ReturnTown();chapter.menuSeconds+=TownPerStage;
                    ObserveSkills();
                    if(request!=null)
                    {
                        check("first journey completes request along its main expedition "+request.id,TideQuests.Ready(profile,request));
                        Read(request.finish_scenes[0]);check("first journey reports request "+request.id,s.ClaimSideQuest(request.id,0));
                        chapter.quests++;ObserveSkills();
                    }
                }
                if(act==3)check("awakening stays locked until the Act 3 report",!s.SkillAvailable(4));
                Read("a"+act+"_report");check("first journey reports complete evidence "+act,TideQuests.ChapterEvidenceReady(profile,act)&&s.ClaimReward());
                if(act==3)check("Act 3 report unlocks the actual awakening input",s.SkillAvailable(4));
                if(act==5)Read("a5_epilogue");
                chapter.level=profile.level;pace.rooms+=chapter.rooms;pace.requests+=chapter.quests;pace.pages+=chapter.pages;pace.defeats+=chapter.defeats;
                pace.fieldSeconds+=chapter.fieldSeconds;pace.readingSeconds+=chapter.readingSeconds;pace.menuSeconds+=chapter.menuSeconds;

                void Read(string id,bool paused=true)
                {
                    if(!seen.Add(id)||!paused)return;
                    foreach(var line in TideDialogue.Lines(id,profile.hero))
                    {
                        chapter.pages++;
                        chapter.readingSeconds+=Mathf.Max(2.8f,1.6f+.095f*line.text.Length);
                    }
                }
            }

            pace.finalLevel=profile.level;pace.estimatedMinutes=(pace.fieldSeconds+pace.readingSeconds+pace.menuSeconds)/60;
            check("all level-gated skills follow the first clear progression",learnedAt[1]>=3&&learnedAt[2]>=5&&learnedAt[3]>=8&&learnedAt[1]<learnedAt[2]&&learnedAt[2]<learnedAt[3]);
            check("first journey finishes thirty main investigations and reunites everyone",s.CampaignFinished
                &&profile.mainEvidence==(1<<25)-1&&profile.mainInvestigations==31&&profile.recruitedHeroes==TideProgress.AllHeroes&&pace.requests==20);
            return pace;

            void ObserveSkills()
            {
                for(int slot=1;slot<4;slot++)if(learnedAt[slot]==0&&s.SkillAvailable(slot))learnedAt[slot]=profile.level;
            }
        }
    }
}
