using UnityEngine;
namespace SpiritTide
{
    public enum TideImpactStrength { None, Light, Firm, Heavy }

    public sealed partial class TideSession
    {
        public float HitStopRemaining,ComboRemaining,DungeonDamageTaken,DungeonStartedAt;
        public int ChainHits,ChainPeak,CancelEvents,PerfectEvents,HitStopEvents,PoiseBreaks,HurtSerial;
        public TideImpactStrength LastImpactStrength {get;private set;}
        public int LastImpactDirection {get;private set;}=1;
        private int actionDamageStart;
        private float impactClock=-1;
        public string ClearRank=>DungeonDamageTaken<=0&&ChainPeak>=20?"SS":DungeonDamageTaken<MaxHp*.25f?"S":DungeonDamageTaken<MaxHp*.7f?"A":"B";
        private void ResetFightFeel()
        {
            HitStopRemaining=ComboRemaining=DungeonDamageTaken=0;
            ChainHits=ChainPeak=CancelEvents=PerfectEvents=HitStopEvents=PoiseBreaks=0;
            LastImpactStrength=TideImpactStrength.None;impactClock=-1;DungeonStartedAt=Clock;
        }
        private void RegisterImpact(TideEnemy e)
        {
            bool heavy=Action==MoveKind.Heavy||Action==MoveKind.Counter,ultimate=Action==MoveKind.Ultimate;
            bool finisher=Action==MoveKind.Basic&&Combo==2;
            bool firm=finisher||Action==MoveKind.Launcher||Action>=MoveKind.Skill0;
            var strength=heavy||ultimate?TideImpactStrength.Heavy:firm&&e.Guard==0?TideImpactStrength.Firm:TideImpactStrength.Light;
            float pause=ultimate?.075f:heavy?.053f:strength==TideImpactStrength.Light?.015f:finisher?.037f:.024f;
            e.ImpactDirection=Mathf.Abs(e.X-X)<.15f?Facing:e.X>X?1:-1;
            e.ImpactStrength=strength;
            if(impactClock!=PresentationClock||strength>LastImpactStrength)
            {
                impactClock=PresentationClock;LastImpactStrength=strength;LastImpactDirection=e.ImpactDirection;
            }
            HitStopRemaining=Mathf.Max(HitStopRemaining,pause);HitStopEvents++;
            ChainHits=ComboRemaining>0?ChainHits+1:1;ChainPeak=Mathf.Max(ChainPeak,ChainHits);ComboRemaining=2.6f;
            if(e.Def.IsBoss)
            {
                if(e.Exposed<=0)
                {
                    e.Poise-=heavy?28:ultimate?35:Action>=MoveKind.Skill0?12:3;
                    if(e.Poise<=0){e.Poise=100;e.Stun=1.3f;e.Exposed=1.3f;e.Telegraph=false;e.Guard=0;PoiseBreaks++;Changed("자세 붕괴 · 잠시 드러난 빈틈을 공략하세요.");}
                }
            }
            else if(e.Guard<=0)
            {
                e.Stagger=Mathf.Max(e.Stagger,heavy?.32f:Action>=MoveKind.Skill0?.2f:.08f);
                e.KnockVelocity=Facing*(ultimate?4.5f:heavy?3.2f:finisher?1.95f:1.25f);
                if(heavy||ultimate){e.Telegraph=false;e.Timer=Mathf.Max(e.Timer,.65f);}
                if(Action==MoveKind.Launcher){e.Lift=.1f;e.LiftVelocity=5.5f;e.Stagger=.6f;}
                if(Action==MoveKind.Air&&e.Lift>.15f)e.LiftVelocity=Mathf.Max(e.LiftVelocity,2.2f);
            }
        }
        private void StepFeel(float dt)
        {
            ComboRemaining=Mathf.Max(0,ComboRemaining-dt);if(ComboRemaining<=0)ChainHits=0;
            foreach(var e in Enemies)
            {
                e.Stagger=Mathf.Max(0,e.Stagger-dt);e.X=Mathf.Clamp(e.X+e.KnockVelocity*dt,-5.8f,10);e.KnockVelocity=Mathf.MoveTowards(e.KnockVelocity,0,dt*12);
                if(e.Lift>0||e.LiftVelocity>0){e.Lift=Mathf.Max(0,e.Lift+e.LiftVelocity*dt);e.LiftVelocity-=14*dt;if(e.Lift<=0)e.LiftVelocity=0;}
                if(e.Def!=null&&e.Def.id=="a3_boss_ulquiorra")e.Phase=e.Hp>e.MaxHp*.7f?1:e.Hp>e.MaxHp*.35f?2:3;
            }
        }
    }
}
