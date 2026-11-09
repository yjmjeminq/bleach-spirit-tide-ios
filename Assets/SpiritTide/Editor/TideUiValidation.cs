using System;
using System.IO;
using UnityEngine;

namespace SpiritTide.EditorTools
{
    public static class TideUiValidation
    {
        private static void Advance(TideSession s,float time){for(int frame=0;frame<Mathf.CeilToInt(time*60);frame++)s.Step(1f/60,Vector2.zero,false);}
        private static TideSession Battle(int hero=0,bool training=false)
        {
            var s=new TideSession(new TideProfile{hero=hero,quest=(int)QuestState.Active});s.BeginBattle(training);s.X=0;s.EnemyX=1;return s;
        }
        public static void Run(Action<string,bool> check)
        {
            var plain=Battle();var paused=Battle();plain.Request(MoveKind.Ultimate);paused.Request(MoveKind.Ultimate);Advance(plain,.1f);Advance(paused,.1f);
            float remaining=paused.ActionRemaining,mp=paused.Mp,cooldown=paused.Cooldowns[4],clock=paused.Clock;
            paused.Pause();Advance(paused,4);
            check("pause preserves paid cast and all combat time",paused.ActionRemaining==remaining&&paused.Mp==mp&&paused.Cooldowns[4]==cooldown&&paused.Clock==clock);
            check("help opens inside paused battle",paused.OpenMenu(TideMode.Help)&&paused.IsPaused&&!paused.IsFighting);
            Advance(paused,2);paused.Resume();Advance(paused,1);Advance(plain,1);
            check("resume resolves same ultimate exactly once",paused.DamageEvents==1&&paused.EnemyHp==plain.EnemyHp&&Mathf.Abs(paused.Mp-plain.Mp)<.01f&&Mathf.Abs(paused.ActionRemaining-plain.ActionRemaining)<.001f);
            var buffer=Battle(0,true);buffer.Request(MoveKind.Basic);Advance(buffer,.05f);buffer.Request(MoveKind.Skill0);buffer.Pause();buffer.Resume();Advance(buffer,1);
            check("pause discards queued inputs without deleting current hit",buffer.AttackSerial==1&&buffer.DamageEvents==1);
            var charge=Battle(0,true);charge.BeginHeavy();Advance(charge,.3f);charge.Pause();charge.Resume();Advance(charge,1);
            check("pause cancels an uncommitted heavy charge",!charge.IsCharging&&charge.AttackSerial==0);
            var burst=Battle(2,true);burst.Request(MoveKind.Skill2);Advance(burst,.22f);burst.Pause();Advance(burst,3);burst.Resume();Advance(burst,.7f);
            check("pause preserves later hits of multi-hit skill",burst.DamageEvents==3);
            burst.Pause();check("paused training cannot replace active hero",!burst.SelectHero(4));
            foreach(float width in new[]{1280f,1345f,1560f,1700f})foreach(float scale in new[]{1f,1.1f})
            {
                bool separate=true,inside=true,largeEnough=true,nearAttack=true;
                Rect joystick=new Rect((TideHudLayout.JoystickCenter-TideHudLayout.JoystickSize*.5f)*scale,TideHudLayout.JoystickSize*scale);
                Vector2 attack=TideHudLayout.Bounds(0,width,scale).center;
                for(int i=0;i<TideHudLayout.Ids.Length;i++)
                {
                    Rect a=TideHudLayout.Bounds(i,width,scale);
                    inside&=a.xMin>=0&&a.xMax<=width&&a.yMin>=0&&a.yMax<450;
                    largeEnough&=a.width>=84&&a.height>=84;
                    separate&=!a.Overlaps(joystick);
                    if(i>=5)nearAttack&=Vector2.Distance(attack,a.center)<=180*scale;
                    for(int j=0;j<i;j++)separate&=!a.Overlaps(TideHudLayout.Bounds(j,width,scale));
                }
                check("control preset stays separate and inside "+width+" / "+scale,separate&&inside&&largeEnough);
                check("every skill is in the same thumb cluster as attack "+width+" / "+scale,nearAttack);
                float bodyRight=width*.23f+6*TideHudLayout.UnitX(width,scale)+160;
                float nearestSkill=TideHudLayout.Bounds(5,width,scale).xMin;
                check("first boss remains clear of action panel "+width+" / "+scale,bodyRight<=nearestSkill-23);
            }
            string legacy=Path.GetFullPath("BuildLogs/legacy-ui-"+Guid.NewGuid().ToString("N")+".json");
            File.Copy("Assets/SpiritTide/Editor/Fixtures/profile-v1-0.1.0.json",legacy);string notice,error;
            TideProfile profile=TideStorage.Load(legacy,out notice);
            check("0.1.0 save preserves progress and receives control defaults",profile.gold==890&&profile.campaignAct==2&&profile.reportedActs==1&&profile.potions==4&&profile.controlScale==1&&Mathf.Abs(profile.controlOpacity-.85f)<.001f&&profile.soundEnabled&&profile.keyboardHints==0);
            check("restored completed Act1 save has next act guidance",new TideSession(profile).Status.Contains("Act 2"));
            check("restored midboss checkpoint starts second half",new TideSession(new TideProfile{quest=(int)QuestState.Active,checkpoint=1}).Profile.campaignStage==4);
            profile.controlScale=1.1f;profile.controlOpacity=.65f;profile.soundEnabled=false;profile.keyboardHints=2;
            bool saved=TideStorage.Save(legacy,profile,out error);TideProfile reloaded=TideStorage.Load(legacy,out notice);
            check("control preferences persist without changing quest or gold",saved&&reloaded.gold==890&&reloaded.campaignAct==2&&reloaded.reportedActs==1&&reloaded.controlScale==1.1f&&reloaded.controlOpacity==.65f&&!reloaded.soundEnabled&&reloaded.keyboardHints==2);
            var bad=new TideProfile{controlScale=float.NaN,controlOpacity=float.PositiveInfinity};TideCatalog.Sanitize(bad);
            check("invalid control preferences have readable defaults",bad.controlScale==1&&bad.controlOpacity==.85f);
            for(int hero=0;hero<6;hero++)for(int slot=0;slot<5;slot++)check("skill explanation exists "+hero+" / "+slot,!string.IsNullOrWhiteSpace(TideUiInfo.Skill(hero,slot)));
        }
    }
}
