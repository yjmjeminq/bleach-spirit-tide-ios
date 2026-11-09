using System;
using System.Collections.Generic;
using UnityEngine;
namespace SpiritTide.EditorTools
{
    public static class TideExperienceValidation
    {
        private static void Step(TideSession s,float time){for(int i=0;i<Mathf.CeilToInt(time*60);i++)s.Step(1f/60,Vector2.zero,false);}
        public static void Run(Action<string,bool> check)
        {
            var backgrounds=new HashSet<string>();var layouts=new HashSet<string>();var hazards=new HashSet<string>();
            foreach(var d in TideCampaign.Data.stages)
            {
                backgrounds.Add(d.background);check("causal reason and resolution "+d.index,!string.IsNullOrWhiteSpace(d.reason)&&!string.IsNullOrWhiteSpace(d.resolution)&&d.reason!=d.resolution);
                foreach(var r in d.rooms){layouts.Add(r.layout);if(!string.IsNullOrEmpty(r.hazard))hazards.Add(r.hazard);check("room world rule is defined "+d.index+" / "+r.title,!string.IsNullOrEmpty(r.layout)&&(!r.ward||r.kind=="troops"||r.kind=="mixed"));}
            }
            check("fifteen distinct combat settings, six formations, five environment rules",backgrounds.Count==15&&layouts.Count==6&&hazards.Count==5);
            for(int act=1;act<=5;act++)for(int hero=0;hero<6;hero++)foreach(int stage in new[]{1,4,6})
            {
                var p=new TideProfile{campaignSchema=1,campaignAct=act,campaignStage=stage,quest=1,hero=hero};TideCatalog.Sanitize(p);
                foreach(string npc in new[]{"urahara","tessai","kon","byakuya","kukaku"})
                {
                    string text=TideStory.NpcText(npc,p);bool ids=false;foreach(var clue in TideExperience.Data.clues)ids|=text.Contains(clue.id);
                    check("localized meaningful town reply "+act+" / "+hero+" / "+stage+" / "+npc,text.Length>20&&!ids);
                }
                var voice=TideExperience.Voice(hero,act);check("companion act voice has all beats "+act+" / "+hero+" / "+stage,!string.IsNullOrEmpty(voice.departure)&&!string.IsNullOrEmpty(voice.after_mid)&&!string.IsNullOrEmpty(voice.after_boss));
            }
            for(int act=0;act<4;act++)check("next act has proven clue "+act,TideStory.Data.acts[act].boss_clue==TideStory.Data.acts[act+1].entry_clue);
            foreach(var scene in TideExperience.Data.encounters)check("boss has introduction and resolution "+scene.id,scene.intro.Length>=2&&scene.outro.Length>=1);
            var cancel=new TideSession(new TideProfile());cancel.BeginBattle(true);cancel.X=0;cancel.EnemyX=1;cancel.Request(MoveKind.Basic);Step(cancel,.2f);int serial=cancel.AttackSerial;
            check("connected basic cancels directly into skill",cancel.Request(MoveKind.Skill0)&&cancel.CancelEvents==1&&cancel.AttackSerial==serial+1);Step(cancel,.7f);check("cancelled recovery never duplicates the first hit",cancel.DamageEvents==2);
            var hit=new TideSession(new TideProfile());hit.AcceptQuest();hit.BeginDungeon();hit.X=1;hit.Y=0;hit.EnemyX=2;hit.EnemyY=0;var e=hit.Target;hit.Request(MoveKind.Heavy);for(int i=0;i<60&&hit.DamageEvents==0;i++)hit.Step(1f/60,Vector2.zero,false);
            float x=e.X,clock=hit.Clock;check("heavy connects with hit stop and stagger",hit.HitStopRemaining>0&&e.Stagger>0&&e.KnockVelocity>0);hit.Step(1f/60,Vector2.zero,false);check("hit stop freezes simulation but counts down",hit.Clock==clock&&e.X==x);Step(hit,.24f);check("knockback moves the actual foe",e.X>x+.1f);
            var launch=new TideSession(new TideProfile());launch.AcceptQuest();launch.BeginDungeon();launch.X=1;launch.EnemyX=2;launch.EnemyY=0;launch.Request(MoveKind.Launcher);Step(launch,.3f);check("launcher has real airborne state",launch.Target.Lift>0&&launch.Target.LiftVelocity>0);launch.Jump();Step(launch,.2f);launch.Request(MoveKind.Basic);Step(launch,.4f);check("air follow-up is a real move",launch.AttackSerial>=2&&launch.Action==MoveKind.Air);
            var poise=new TideSession(new TideProfile());poise.BeginBattle(false,1);poise.X=0;poise.EnemyX=1;poise.Target.Poise=20;poise.Request(MoveKind.Heavy);Step(poise,.4f);check("boss poise break opens a real vulnerability",poise.PoiseBreaks==1&&poise.Target.Exposed>0&&poise.Target.Stun>0);
            var phase=new TideSession(new TideProfile());phase.BeginBattle(true,5);phase.Target.Hp=phase.Target.MaxHp*.69f;Step(phase,.02f);check("Ulquiorra has second phase",phase.Target.Phase==2);phase.Target.Hp=phase.Target.MaxHp*.34f;Step(phase,.02f);check("Ulquiorra has third phase",phase.Target.Phase==3);
            var ward=new TideSession(new TideProfile{campaignSchema=1,campaignAct=1,campaignStage=2,quest=1});ward.BeginDungeon();
            for(int i=0;i<30000&&ward.RoomIndex==0;i++)TideCombatDriver.Tick(ward);
            check("ambush room starts with a second wave pending",ward.RoomIndex==1&&ward.Enemies.Count==2&&ward.ReinforcementsPending&&ward.WardActive);
            int uid=ward.Enemies[0].Uid;for(int i=0;i<18000&&ward.Enemies[0].Uid==uid&&ward.IsFighting;i++)TideCombatDriver.Tick(ward,1f/60,false);
            check("reinforcements arrive after first wave, gate stays locked",ward.IsFighting&&ward.Enemies[0].Uid!=uid&&!ward.RoomCleared&&!ward.ReinforcementsPending);
            var device=new TideSession(new TideProfile{campaignSchema=1,campaignAct=1,campaignStage=2,roomCheckpoint=1,quest=1});device.BeginDungeon();device.X=device.WardPosition.x;device.Y=device.WardPosition.y;device.Mp=50;
            check("ward is an actionable counter to environment",device.InteractWorld()&&!device.WardActive&&device.Mp==65);float timer=device.EnvironmentTimer;Step(device,.8f);check("disabled ward cannot rearm a hazard",!device.EnvironmentWarning&&device.EnvironmentTimer==timer);
            var rescue=new TideSession(new TideProfile{campaignSchema=1,campaignAct=1,campaignStage=1,quest=1,rescuedDungeons=3});int gold=rescue.Profile.gold,potions=rescue.Profile.potions;check("optional town report pays once",rescue.ReportRescue(1)&&!rescue.ReportRescue(1)&&rescue.Profile.gold==gold+100&&rescue.Profile.potions==potions+1&&rescue.Profile.crystals==2);
            var missed=new TideSession(new TideProfile{campaignSchema=1,campaignAct=1,campaignStage=2,quest=1,clearedDungeons=1});missed.BeginDungeon(1,1,true);for(int i=0;i<15000&&missed.IsFighting&&!missed.RoomCleared;i++)TideCombatDriver.Tick(missed,1f/60,false);check("missed rescue remains available in replay",missed.IsReplay&&missed.EchoAvailable);missed.X=missed.EchoX;check("replay rescue is recoverable once",missed.InteractWorld()&&!missed.InteractWorld()&&(missed.Profile.rescuedDungeons&1)!=0);
            var npcTexture=Resources.Load<Texture2D>("SpiritTide/Art/Experience/town_npcs_v1");check("NPC atlas retains its original pixel aspect",npcTexture.width==1536&&npcTexture.height==1024);
        }
    }
}
