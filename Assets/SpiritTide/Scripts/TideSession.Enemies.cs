using System;
using UnityEngine;

namespace SpiritTide
{
    public sealed class TideEnemy
    {
        public int Uid,Pattern,Attacks,Mark,Guard,ReturnHits,DamageSerial,Phase=1;
        public float Stagger,KnockVelocity,Lift,LiftVelocity,Poise=100;
        public TideFoeDef Def;
        public float X,Y,Hp,MaxHp,Stun,Slow,Flash,Exposed,Timer=1,TellDuration=1,Recovery,LastDamage;
        public bool Telegraph,SecondArea,Decoys,FutureLocked;
        public Vector2 Warning,Warning2;
        public bool Alive=>Def!=null&&Hp>0;
        public Vector2 Position=>new Vector2(X,Y);
        public string Cue="";
    }
    public sealed partial class TideSession
    {
        private TideEnemy Spawn(string id,int ordinal,int total,int stageIndex)
        {
            TideFoeDef def=TideCampaign.Foe(id);
            var e=new TideEnemy{Uid=nextEnemyUid++,Def=def,X=2.5f+ordinal*1.25f,Y=total==1?0:(ordinal-(total-1)*.5f)*.65f,Guard=def.brain=="guard"?2:0,Timer=1.1f+ordinal*.35f};
            float pressure=IsCampaign?Mathf.Max(1,Room.pressure):1;
            e.MaxHp=def.base_hp*(1+Mathf.Max(0,stageIndex)*.11f)*pressure;
            e.Hp=e.MaxHp;ApplyFormation(e,ordinal,total);Enemies.Add(e);return e;
        }
        private void ChooseTarget()
        {
            if(target!=null&&target.Alive&&Mathf.Abs(target.X-X)<7)return;
            target=null;float best=float.MaxValue;
            foreach(var e in Enemies)if(e.Alive){float distance=Mathf.Abs(e.X-X)+Mathf.Abs(e.Y-Y)*2;if(distance<best){target=e;best=distance;}}
        }
        private string Cue(TideEnemy e,int pattern)
        {
            string id=e.Def.id;
            if(id=="a1_mid_shrieker")return pattern==1?"사역체 폭발 · 두 원 밖으로":pattern==2?"음파 · 점프":"발톱 강타 · 위아래로";
            if(id=="a1_boss_grand_fisher")return pattern==1?"의사 미끼 · 원 밖으로":pattern==2?"낮게 휘두르기 · 점프":"발톱 휩쓸기 · 위아래로";
            if(id=="a2_mid_shawlong")return "교차 절단 · 사선 사이로 이동";
            if(id=="a2_boss_grimmjow")return pattern==1?"판테라 도약 · 원 밖으로":"소니도 돌진 · 사선에서 이탈";
            if(id=="a3_mid_nnoitra")return "다완 연격 · 마지막 공격까지 회피";
            if(id=="a3_boss_ulquiorra")return pattern==1?"착지 충격 · 원 밖으로":"빛의 창 · 사선 밖으로";
            if(id=="a4_mid_gin")return "신창 · 긴 직선 밖으로";
            if(id=="a4_boss_aizen")return "환영 대조 · 진한 실체 표식만 실제 공격";
            if(id=="a5_mid_haschwalth")return pattern==1?"되돌림 예고 · 고리 밖으로":"왕의 방패 · 큰 공격 뒤를 노리기";
            if(id=="a5_boss_yhwach")return pattern==6?"가능한 미래 → 확정된 사선 회피":"왕의 검 · 정면에서 이탈";
            return pattern==1?"지정 구역 · 원 밖으로":pattern==2?"낮은 공격 · 점프":pattern==3?"원거리 사선 · 위아래로":pattern==4?"교차 사선 · 틈으로 이동":"근접 예고 · 측면으로";
        }
        private void StartTell(TideEnemy e,int pattern,float duration)
        {
            e.Pattern=pattern;e.Warning=pattern==0?e.Position:new Vector2(X,Y);e.Warning2=e.Warning+new Vector2(2.1f,.65f);
            e.SecondArea=e.Def.id=="a1_mid_shrieker"&&pattern==1||e.Def.id=="a3_mid_nnoitra"&&e.Hp<e.MaxHp*.5f;
            e.Decoys=e.Def.id=="a4_boss_aizen";e.FutureLocked=pattern!=6;
            if(e.Def.id=="a5_boss_yhwach"&&pattern==6)e.Warning=new Vector2(X,Mathf.Clamp(Y+(e.Attacks%2==0?.4f:-.4f),-1.1f,1.1f));
            e.TellDuration=duration;e.Timer=duration;e.Telegraph=true;e.Cue=Cue(e,pattern);Revision++;
        }
        private int NextPattern(TideEnemy e)
        {
            switch(e.Def.id)
            {
                case "a1_mid_shrieker":return e.Attacks%3;
                case "a1_boss_grand_fisher":return new[]{1,0,2}[e.Attacks%3];
                case "a2_mid_shawlong":return e.Attacks%2==0?4:0;
                case "a2_boss_grimmjow":return e.Attacks%3==1?1:3;
                case "a3_mid_nnoitra":return e.Attacks%3==2?4:0;
                case "a3_boss_ulquiorra":return e.Phase==3?(e.Attacks%2==0?4:1):e.Phase==2?(e.Attacks%2==0?3:1):e.Attacks%3==2?1:3;
                case "a4_mid_gin":return 3;
                case "a4_boss_aizen":return 1;
                case "a5_mid_haschwalth":return e.Attacks%3==2?1:0;
                case "a5_boss_yhwach":return e.Attacks%3==2?0:6;
            }
            switch(e.Def.brain){case "ranged":return e.Def.attack.Contains("폭발")?1:3;case "zone":case "fly":return 1;case "dash":return 3;case "guard":return e.Guard>0?0:2;default:return e.Def.IsNamed&&e.Attacks%3==2?2:0;}
        }
        private bool AreaHits(TideEnemy e,Vector2 location)
        {
            float dx=X-location.x,dy=Y-location.y;
            switch(e.Pattern)
            {
                case 1:return dx*dx+dy*dy<1.15f*1.15f;
                case 2:return Mathf.Abs(dy)<.65f&&Z<.5f;
                case 3:return Mathf.Abs(dy)<.5f;
                case 4:return Mathf.Abs(dy-dx*.2f)<.35f||Mathf.Abs(dy+dx*.2f)<.35f;
                case 6:return e.FutureLocked&&Mathf.Abs(dy)<.45f;
                default:return Mathf.Abs(dx)<3&&Mathf.Abs(dy)<.85f;
            }
        }
        public bool InTelegraph(TideEnemy e)=>e.Alive&&e.Telegraph&&(AreaHits(e,e.Warning)||e.SecondArea&&AreaHits(e,e.Warning2));
        private void StepEnemy(TideEnemy e,float dt)
        {
            if(!IsFighting||e.Stun>0||e.Stagger>0||e.Lift>.05f)return;
            if(e.Recovery>0){e.Recovery=Mathf.Max(0,e.Recovery-dt);return;}
            if(e.Telegraph)
            {
                e.Timer-=dt;
                if(e.Pattern==6&&!e.FutureLocked&&e.Timer<e.TellDuration*.45f){e.FutureLocked=true;e.Cue="확정된 미래 · 진한 사선 밖으로";Revision++;}
                if(e.Timer>0)return;
                if(AreaHits(e,e.Warning)||e.SecondArea&&AreaHits(e,e.Warning2))TakeDamage(e,EnemyDamage(e));
                if(!IsFighting)return;
                e.Telegraph=false;e.Attacks++;
                bool opening=e.Def.id=="a1_boss_grand_fisher"&&e.Pattern==1||e.Def.id=="a3_boss_ulquiorra"&&e.Pattern==1||e.Def.id=="a4_boss_aizen";
                e.Recovery=opening?1.3f:e.Def.id=="a3_mid_nnoitra"&&e.Attacks%3!=0?.35f:.85f;e.Exposed=opening?1.3f:0;
                if(e.Guard==0&&e.Def.brain=="guard")e.Exposed=2;
                if(e.Def.brain=="dash"||e.Def.id=="a2_boss_grimmjow"){e.X=Mathf.Clamp(e.Warning.x+(e.X<X?-1:1)*1.5f,-5.5f,9);e.Y=e.Warning.y;}
                e.Timer=.9f+(e.Def.IsBoss?0:.2f);Revision++;return;
            }
            float pace=e.Slow>0?.45f:1;float dxPlayer=X-e.X;
            bool distant=e.Def.brain=="ranged"||e.Def.brain=="zone";
            if(!distant||Mathf.Abs(dxPlayer)>6)
            {
                float desired=e.Def.brain=="dash"?3:1.9f;if(Mathf.Abs(dxPlayer)>desired)e.X=Mathf.MoveTowards(e.X,X-Mathf.Sign(dxPlayer)*desired,dt*(e.Def.brain=="dash"?1.8f:1.1f)*pace);
                e.Y=Mathf.MoveTowards(e.Y,Y,dt*.7f*pace);
            }
            else if(Mathf.Abs(dxPlayer)<2.5f)e.X=Mathf.Clamp(e.X-Mathf.Sign(dxPlayer)*dt*.8f,-5.5f,9);
            e.Timer-=dt;if(e.Timer>0)return;
            if(!distant&&!e.Def.IsBoss&&Mathf.Abs(dxPlayer)>4){e.Timer=.25f;return;}
            int pattern=NextPattern(e);float duration=e.Def.IsBoss?1.15f:1.05f;
            if(e.Hp<e.MaxHp*.35f)duration-=.18f;if(pattern==6)duration=1.5f;if(e.Def.id=="a3_boss_ulquiorra"&&e.Phase>=2)duration-=.08f;
            StartTell(e,pattern,duration);
        }
        private float EnemyDamage(TideEnemy e)
        {
            int stage=IsCampaign?Dungeon.index:0;return (e.Def.IsBoss?e.Def.rank=="boss"?19:14:e.Def.IsNamed?12:8)+stage*.9f;
        }
    }
}
