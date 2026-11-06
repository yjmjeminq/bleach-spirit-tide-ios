using System;
using UnityEngine;

namespace SpiritTide.EditorTools
{
    public static class TideJourneyValidation
    {
        public static void Run(Action<string,bool> check)
        {
            check("five authored chapter stakes and midpoint consequences",TideJourney.Data.chapters.Length==5);
            var fresh=new TideSession(new TideProfile());string initial=JsonUtility.ToJson(fresh.Profile);
            check("new players cannot invoke an unencountered boss practice",!fresh.BeginBossPractice(-1)&&!fresh.BeginBossPractice(0)&&!fresh.BeginBossPractice(10)&&!fresh.InBattle);
            string first=TideJourney.Narrative(fresh.Profile);
            check("initial recap contains no later antagonist or recovered original",!first.Contains("그림죠")&&!first.Contains("퀸시")&&!first.Contains("원본을 회수")&&!first.Contains("유하바하"));
            for(int i=0;i<10;i++)check("fresh boss record remains locked "+i,!TideJourney.BossKnown(fresh.Profile,i));
            TideJourney.Next(fresh.Profile);TideJourney.Preparation(fresh.Profile);
            check("reading fresh journey does not accept quests or equip gear",initial==JsonUtility.ToJson(fresh.Profile));
            for(int a=1;a<=5;a++)for(int n=1;n<=6;n++)
            {
                var p=new TideProfile{campaignSchema=1,campaignAct=a,campaignStage=n,quest=1,reportedActs=(1<<(a-1))-1,clearedDungeons=(1<<((a-1)*6+n-1))-1};TideCatalog.Sanitize(p);
                string saved=JsonUtility.ToJson(p),proof=TideJourney.Proof(p);var now=TideJourney.Current(p);
                check("recap cites only completed field evidence "+a+" / "+n,n>1?proof==TideCampaign.Dungeon(a,n-1).resolution:a>1?proof==TideCampaign.Dungeon(a-1,6).resolution:proof.Contains("아직 현장에서"));
                check("resume describes actual dungeon and checkpoint "+a+" / "+n,TideJourney.Next(p).Contains(now.title)&&TideJourney.Next(p).Contains("1 / "+now.rooms.Length+"방"));
                TideJourney.Narrative(p);TideJourney.Preparation(p);
                check("journey inspection is read only "+a+" / "+n,saved==JsonUtility.ToJson(p));
                if(n==4)check("midpoint next action names mandatory field evidence "+a,TideJourney.Next(p).Contains(TideQuests.Main(a).inspect_title));
            }
            var resumed=new TideSession(new TideProfile{campaignSchema=1,campaignAct=2,campaignStage=2,quest=1,roomCheckpoint=1,reportedActs=1,clearedDungeons=127});
            check("journey resume uses the saved second room",TideJourney.Next(resumed.Profile).Contains("2 / "+TideJourney.Current(resumed.Profile).rooms.Length+"방"));
            for(int i=0;i<10;i++)
            {
                var p=new TideProfile{campaignSchema=1,campaignAct=5,campaignStage=6,reportedActs=31,clearedDungeons=TideProgress.AllDungeons,claimed=true,quest=3,level=30,potions=0};var s=new TideSession(p);string saved=JsonUtility.ToJson(p);int revision=s.SaveRevision;
                check("completed boss record can start a real practice "+i,s.BeginBossPractice(i)&&!s.IsCampaign&&!s.IsTraining&&s.IsFighting&&s.EnemyId==TideCatalog.Bosses[i].id);
                s.Hp=50;check("boss practice healing is free with zero inventory "+i,s.UsePotion()&&s.Hp>50&&p.potions==0&&s.SaveRevision==revision);
                s.X=0;s.Y=0;s.EnemyX=1;s.EnemyY=0;s.Hp=1;
                for(int t=0;t<600&&s.IsFighting;t++)s.Step(1f/60,Vector2.zero,false);
                check("defeat explains the attack that actually hit "+i,s.Mode==TideMode.Defeat&&s.LastDamageFoe==TideCatalog.Bosses[i].id&&!string.IsNullOrEmpty(s.LastDamageCue)&&TideJourney.DefeatAdvice(s).Contains(s.LastDamageCue));
                float clock=s.Clock;
                check("defeat can inspect preparation without restarting "+i,s.OpenMenu(TideMode.Journey));
                for(int t=0;t<60;t++)s.Step(1f/60,Vector2.one,true);
                check("preparation does not resume a defeated battle "+i,s.Clock==clock&&s.Hp==0);
                check("practice can restart from its guide "+i,s.BeginBossPractice(i)&&string.IsNullOrEmpty(s.LastDamageCue));
                for(int t=0;t<18000&&s.IsFighting;t++)TideCombatDriver.Tick(s);
                check("practice victory has no rewards or campaign writes "+i,s.Mode==TideMode.Result&&s.LastXp==0&&s.LastGold==0&&s.LastGearUid==0&&s.SaveRevision==revision&&saved==JsonUtility.ToJson(p));
                s.ReturnTown();check("practice return preserves the full saved campaign "+i,saved==JsonUtility.ToJson(p));
            }
        }
    }
}
