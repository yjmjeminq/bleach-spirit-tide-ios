using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace SpiritTide.EditorTools
{
    public static class TideValidation
    {
        [Serializable] private class Row{public string name;public bool passed;}
        [Serializable] private class Report{public bool passed;public Row[] checks;public string scope;public int fullHealthDungeons,fullHealthRooms;}
        private static readonly List<Row> Rows=new List<Row>();private static int dungeonCount,roomCount;
        private static void Check(string name,bool value){Rows.Add(new Row{name=name,passed=value});if(!value)throw new InvalidOperationException(name);}
        private static void Step(TideSession s,float seconds){for(int i=0;i<Mathf.CeilToInt(seconds*60);i++)s.Step(1f/60,Vector2.zero,false);}
        private static TideSession Practice(int hero=0,bool training=true,int boss=0){var s=new TideSession(new TideProfile{hero=hero});s.BeginBattle(training,boss);s.X=0;s.EnemyX=1;return s;}
        public static void Run()
        {
            Rows.Clear();dungeonCount=roomCount=0;
            try
            {
                TideCatalog.Load();TideStory.Load();TideArt.Load();
                Check("six heroes / thirty dungeons / seventy foes",TideCatalog.Characters.Length==6&&TideCampaign.Data.stages.Length==30&&TideCampaign.Data.foes.Length==70);
                int rooms=0;var encountered=new HashSet<string>();
                foreach(var d in TideCampaign.Data.stages)
                {
                    Check("dungeon art and briefing "+d.index,TideArt.Background(d.background)!=null&&TideStory.BeforeDungeon(d,new TideProfile{campaignSchema=1,campaignAct=d.act,campaignStage=d.number},false).Length>0);
                    foreach(var r in d.rooms){rooms++;foreach(string id in r.foes){encountered.Add(id);if(r.kind=="troops")Check("waves contain only troops "+d.index+" "+id,TideCampaign.Foe(id).rank=="regular");}}
                }
                Check("114 playable rooms and all 70 foe IDs are reachable",rooms==114&&encountered.Count==70);
                foreach(var f in TideCampaign.Data.foes)Check("foe sprite and tell "+f.id,TideArt.Enemy(f.id)!=null&&!string.IsNullOrEmpty(f.brain)&&!string.IsNullOrEmpty(f.tell));
                for(int hero=0;hero<6;hero++)
                {
                    Check("hero v2 image "+hero,Resources.Load<Sprite>("SpiritTide/Art/"+TideCatalog.Characters[hero].id+"_bikini_v2")!=null);
                    var s=new TideSession(new TideProfile{hero=hero});Check("departure needs accepted quest "+hero,!s.BeginDungeon());
                    for(int act=1;act<=5;act++)
                    {
                        Check("accept once "+hero+" / "+act,s.AcceptQuest()&&!s.AcceptQuest()&&!s.ClaimReward());
                        for(int n=1;n<=6;n++)
                        {
                            TideCombatDriver.Prepare(s);Check("start correct dungeon "+hero+" / "+act+"-"+n,s.BeginDungeon()&&s.Dungeon.act==act&&s.Dungeon.number==n&&s.Hp==s.MaxHp);
                            int frame=0,lastRoom=0;
                            while(s.IsFighting&&frame++<60000){TideCombatDriver.Tick(s);lastRoom=Mathf.Max(lastRoom,s.RoomIndex);}
                            Check("full health input-only clear "+hero+" / "+act+"-"+n+" frames="+frame+" hp="+s.Hp+" room="+s.RoomIndex+" enemies="+s.AliveCount,s.Mode==TideMode.Result&&s.Hp>0&&lastRoom==s.Dungeon.rooms.Length-1);
                            Check("one first-clear gear and real XP "+hero+" / "+act+"-"+n,s.LastXp==s.Dungeon.xp&&s.LastGold==s.Dungeon.gold&&s.LastGearUid==1000+s.Dungeon.index&&(s.Profile.clearedDungeons&(1<<s.Dungeon.index))!=0);
                            dungeonCount++;roomCount+=s.Dungeon.rooms.Length;s.ReturnTown();
                        }
                        int gold=s.Profile.gold;Check("act report pays once and links next "+hero+" / "+act,s.Quest==QuestState.Ready&&s.ClaimReward()&&!s.ClaimReward()&&s.Profile.gold==gold+250+(act-1)*100&&s.Profile.campaignAct==Mathf.Min(5,act+1));
                    }
                    Check("campaign finished all bits and leveled "+hero,s.CampaignFinished&&s.Profile.clearedDungeons==TideProgress.AllDungeons&&s.Profile.level>10&&s.Profile.gear.Length==33);
                    int mask=s.Profile.reportedActs,items=s.Profile.gear.Length;Check("cleared dungeon replay available "+hero,s.BeginDungeon(1,1,true));
                    for(int i=0;i<60000&&s.IsFighting;i++)TideCombatDriver.Tick(s);
                    Check("replay rewards reduced and first loot never duplicated "+hero,s.Mode==TideMode.Result&&s.LastGearUid==0&&s.LastXp==24&&s.Profile.gear.Length==items&&s.Profile.reportedActs==mask);s.ReturnTown();
                }
                TestCombat();TestCheckpoint();TestStorage();TideUiValidation.Run(Check);TideExperienceValidation.Run(Check);TideDialogueValidation.Run(Check);TideQuestValidation.Run(Check);TideSkillFxValidation.Run(Check);Save(true);
            }
            catch{Save(false);throw;}
        }
        private static void TestCombat()
        {
            var ice=Practice();ice.Request(MoveKind.Skill1);Step(ice,.6f);Check("Rukia cold slows",ice.EnemySlow>0);
            var ward=Practice(1);ward.Hp=70;ward.Request(MoveKind.Skill1);Step(ward,.6f);Check("Orihime heals",ward.Hp>70);ward.Request(MoveKind.Skill0);Step(ward,.6f);Check("Orihime shield",ward.Shield>0);
            var fist=Practice(2);fist.Request(MoveKind.Skill2);Step(fist,.8f);Check("Yoruichi actual multi hit",fist.DamageEvents==3);
            var sting=Practice(3);sting.Request(MoveKind.Skill0);Step(sting,.6f);Check("Soi Fon per-target mark",sting.Mark==1);sting.Request(MoveKind.Skill3);Step(sting,.6f);Check("Soi Fon consumes mark",sting.Mark==0&&sting.LastDamage>=70);
            var ash=Practice(4);ash.Request(MoveKind.Skill1);Step(ash,.6f);Check("Rangiku installs ash",ash.CloudTime[0]>0||ash.CloudTime[1]>0);ash.Request(MoveKind.Skill3);Step(ash,.6f);Check("Rangiku detonates",ash.CloudTime[0]==0&&ash.CloudTime[1]==0&&ash.LastDamage>=61);
            var healer=Practice(5);healer.Request(MoveKind.Skill3);Step(healer,.6f);Check("Unohana stance",healer.CounterStance);healer.Hp=80;healer.Request(MoveKind.Skill0);Step(healer,.6f);Check("Unohana recovery",healer.Hp>80);
            for(int boss=0;boss<10;boss++)
            {
                var s=Practice(0,false,boss);Step(s,1.15f);Check("boss unique warning "+boss,s.Telegraph&&!string.IsNullOrEmpty(s.Target.Cue));
                if(boss==0){s.Target.Attacks=1;}
                if(boss==7)Check("Aizen has distinguishable decoys",s.Target.Decoys);
                if(boss==9){Check("Yhwach previews before locking",!s.Target.FutureLocked);Step(s,.9f);Check("Yhwach confirms future line",s.Target.FutureLocked);}
                if(boss==1){Vector2 locked=s.WarningPosition;s.Step(.05f,Vector2.up,false);Check("Fisher lure fixed at tell",s.WarningPosition==locked);Step(s,1.2f);Check("Fisher body opening",s.BodyExposed);}
            }
            var multi=new TideSession(new TideProfile());multi.AcceptQuest();multi.BeginDungeon();multi.X=2;multi.Y=0;foreach(var e in multi.Enemies){e.X=3;e.Y=0;}multi.Request(MoveKind.Ultimate);Step(multi,.6f);Check("area skill damages every live troop",multi.DamageEvents>=2);
            Check("combat prevents equipment and hero swaps",!multi.EquipGear(2)&&!multi.SelectHero(4));
            float clock=multi.Clock;multi.Pause();Step(multi,3);Check("multi enemy pause freezes clock",multi.Clock==clock);multi.Resume();
            var gear=new TideSession(new TideProfile());gear.Profile.crystals=3;int old=gear.Profile.gold;Check("forge pays both resources exactly once",gear.UpgradeGear(1)&&gear.Profile.gold==old-120&&gear.Profile.crystals==0&&TideProgress.Gear(gear.Profile,1).upgrade==1);old=gear.Profile.gold;Check("failed forge cannot spend gold",!gear.UpgradeGear(1)&&gear.Profile.gold==old);
            Check("equipment bonus feeds damage stat",gear.EquipGear(1)&&gear.AttackBonus==3);
        }
        private static void TestCheckpoint()
        {
            var s=new TideSession(new TideProfile());s.AcceptQuest();s.BeginDungeon();Check("locked exit cannot skip room",!s.EnterGate());
            for(int i=0;i<12000&&s.IsFighting&&!s.RoomCleared;i++)TideCombatDriver.Tick(s);
            Check("room clear saves next room immediately",s.RoomCleared&&s.Profile.roomCheckpoint==1&&s.Profile.gold==800&&s.Profile.xp==0);
            s.X=-3;Check("gate requires physical approach",!s.EnterGate());s.ReturnTown();s.BeginDungeon();Check("town return resumes next room",s.RoomIndex==1&&s.Hp==s.MaxHp);
            s.Hp=1;s.X=s.EnemyX;s.Y=s.EnemyY;for(int i=0;i<240&&s.IsFighting;i++)s.Step(1f/60,Vector2.zero,false);Check("defeat retains room checkpoint",s.Mode==TideMode.Defeat&&s.Profile.roomCheckpoint==1);s.BeginDungeon();Check("retry restores actual room and full health",s.RoomIndex==1&&s.Hp==s.MaxHp&&s.Enemies.TrueForAll(e=>e.Hp==e.MaxHp));
        }
        private static void TestStorage()
        {
            string folder=Path.GetFullPath("BuildLogs/SaveChecks-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);string path=Path.Combine(folder,"profile.json"),error,notice;
            var p=new TideProfile();var s=new TideSession(p);s.AcceptQuest();p.roomCheckpoint=1;p.crystals=9;s.UpgradeGear(1);p.gold=831;Check("atomic first save",TideStorage.Save(path,p,out error));p.gold=952;Check("atomic replacement save",TideStorage.Save(path,p,out error));var loaded=TideStorage.Load(path,out notice);
            Check("campaign gear preferences checkpoint roundtrip",loaded.gold==952&&loaded.roomCheckpoint==1&&loaded.campaignSchema==1&&TideProgress.Gear(loaded,1).upgrade==1);
            File.WriteAllText(path,"broken");loaded=TideStorage.Load(path,out notice);Check("verified backup recovers damaged primary",loaded.gold==831&&!string.IsNullOrEmpty(notice));Check("save after corruption succeeds",TideStorage.Save(path,loaded,out error));Check("corrupt primary preserved",Directory.GetFiles(folder,"*.corrupt-*").Length>0);
            var old=new TideProfile{quest=3,checkpoint=2,claimed=true,gold=890,potions=4};TideCatalog.Sanitize(old);Check("old Act1 completion migrates to Act2 without extra payout",old.campaignAct==2&&old.campaignStage==1&&old.quest==0&&old.reportedActs==1&&old.clearedDungeons==63&&old.gold==890&&old.potions==4);
            var broken=new TideProfile{campaignSchema=1,gear=new TideGear[]{null},hero=999,gold=-1};TideCatalog.Sanitize(broken);Check("corrupt inventory and values repaired",broken.gear.Length>0&&broken.hero==5&&broken.gold==0);
        }
        private static void Save(bool passed){TideBuilder.Write("BuildLogs/model-validation.json",new Report{passed=passed,checks=Rows.ToArray(),fullHealthDungeons=dungeonCount,fullHealthRooms=roomCount,scope="Unity model/resources: six input-only complete campaigns with normal HP, real XP, loot, upgrades and manual gate approach. Native iPhone validation remains deferred."});}
    }
}
