using UnityEngine;

namespace SpiritTide
{
    public sealed partial class TideSession
    {
        private static readonly float[] ExtraSkillDurations={.50f,.43f,.38f,.62f};
        private enum Reflection { Damage, Spirit, Stance }
        private Reflection reflection;

        private void BeginReflection(float seconds,Reflection effect=Reflection.Damage)
        {
            ReflectRemaining=seconds;
            reflection=effect;
        }

        private void ResolveReflection(TideEnemy source)
        {
            ReflectRemaining=0;
            if(reflection==Reflection.Spirit)Mp=Mathf.Min(100,Mp+22);
            if(reflection==Reflection.Stance)CounterStance=!CounterStance;
            Damage(source,Profile.hero==5?40:32,true);
            if(IsFighting)Changed(reflection==Reflection.Spirit?"반격 성공 · 영압 회복":
                reflection==Reflection.Stance?"반격 성공 · 태세 전환":"반격 성공");
        }

        private void ResolveExtraSkill(int skill)
        {
            switch(Profile.hero)
            {
                case 0:ResolveExtraFrost(skill);break;
                case 1:ResolveExtraFlowers(skill);break;
                case 2:ResolveExtraThunder(skill);break;
                case 3:ResolveExtraHornet(skill);break;
                case 4:ResolveExtraAsh(skill);break;
                case 5:ResolveExtraCrimson(skill);break;
            }
        }

        private void ResolveExtraFrost(int skill)
        {
            switch(skill)
            {
                case 5:
                    Hit(21,4.4f,1.7f,true,e=>e.Slow=Mathf.Max(e.Slow,2.5f),true);
                    break;
                case 6:
                    Move(.23f,new Vector2(Facing,0),8);
                    Hit(38,3.7f,.75f,true,e=>e.Slow=Mathf.Max(e.Slow,1.4f));
                    break;
                case 7:
                    Shield=Mathf.Max(Shield,26+Profile.level*.5f);
                    Hit(16,2.7f,1.2f,true,e=>e.Stun=Mathf.Max(e.Stun,.55f),true);
                    break;
                case 8:
                    foreach(var e in Enemies)
                    {
                        if(!InRange(e,5.2f,1.4f))continue;
                        Damage(e,e.Slow>0?66:27,true);
                        e.Slow=0;
                    }
                    break;
            }
        }

        private void ResolveExtraFlowers(int skill)
        {
            switch(skill)
            {
                case 5:
                    Shield=Mathf.Max(Shield,48+Profile.level);
                    Hit(13,2.9f,1.4f,true,PushAway,true);
                    break;
                case 6:
                    foreach(var e in Enemies)
                        if(InRange(e,7.5f,.65f+Mathf.Abs(e.X-X)*.18f))Damage(e,31,true);
                    break;
                case 7:
                    BeginReflection(1.1f,Reflection.Spirit);
                    break;
                case 8:
                    int healing=(Hp<MaxHp/3f?50:15)+Profile.level;
                    Hp=Mathf.Min(MaxHp,Hp+healing);
                    Shield=Mathf.Max(Shield,12);
                    break;
            }
        }

        private void ResolveExtraThunder(int skill)
        {
            switch(skill)
            {
                case 5:
                    Move(.32f,new Vector2(Facing,0),9);
                    Hit(34,3.1f,1.2f,true,e=>e.Stun=Mathf.Max(e.Stun,.55f),true);
                    break;
                case 6:
                    if(!InRange(Target,3.5f,1.2f))break;
                    Damage(Target,17,true);
                    burstTarget=Target;burstHits=4;burstTick=.07f;
                    break;
                case 7:
                    if(InRange(Target,4.2f,1.25f,true))
                    {
                        float approach=Target.X>=X?1:-1;
                        X=Mathf.Clamp(Target.X+approach*1.15f,-6,IsCampaign?10.5f:6);
                        Facing=Target.X>=X?1:-1;
                    }
                    BeginReflection(.7f);
                    break;
                case 8:
                    Hit(54,4.3f,1.65f,true,e=>
                    {
                        e.Stun=Mathf.Max(e.Stun,1.1f);
                        if(!e.Def.IsBoss&&e.Guard<=0){e.Lift=.1f;e.LiftVelocity=4.5f;}
                    },true);
                    break;
            }
        }

        private void ResolveExtraHornet(int skill)
        {
            switch(skill)
            {
                case 5:
                    if(!InRange(Target,7.5f,1.85f,true))break;
                    Target.Mark=1;
                    Damage(Target,19,true);
                    break;
                case 6:
                    foreach(var e in Enemies)
                    {
                        if(!InRange(e,5.8f,.85f))continue;
                        e.Guard=Mathf.Max(0,e.Guard-1);
                        Damage(e,36,true);
                    }
                    break;
                case 7:
                    Hit(19,3.3f,1.05f,true);
                    DodgeRemaining=.32f;DodgeElapsed=.05f;
                    dodgeDirection=new Vector2(-Facing,Y>0?-.45f:.45f).normalized;
                    break;
                case 8:
                    var marked=InRange(Target,4.8f,1.2f)&&Target.Mark>0?Target:null;
                    Hit(25,4.8f,1.2f,true);
                    if(marked!=null){burstTarget=marked;burstHits=4;burstTick=.06f;}
                    break;
            }
        }

        private void PlaceAsh(Vector2 at)
        {
            int cloud=CloudTime[0]<CloudTime[1]?0:1;
            Clouds[cloud]=at;
            CloudTime[cloud]=5;
        }

        private void PushAway(TideEnemy enemy)
        {
            if(!enemy.Def.IsBoss&&enemy.Guard<=0)enemy.KnockVelocity=enemy.X<X?-4:4;
        }

        public Vector2 AshPlacement()
        {
            Vector2 player=new Vector2(X,Y);
            Vector2 offset=Target.Alive?Target.Position-player:new Vector2(Facing*2,0);
            return player+Vector2.ClampMagnitude(offset,7.5f);
        }

        public Vector2 AshAttraction(Vector2 origin)
        {
            Vector2 at=new Vector2(X,Y);
            float distance=float.PositiveInfinity;
            for(int i=0;i<Clouds.Length;i++)
            {
                float gap=(Clouds[i]-origin).sqrMagnitude;
                if(CloudTime[i]<=0||gap>=distance)continue;
                distance=gap;at=Clouds[i];
            }
            return at;
        }

        private void ResolveExtraAsh(int skill)
        {
            Vector2 player=new Vector2(X,Y);
            switch(skill)
            {
                case 5:
                    PlaceAsh(AshPlacement());
                    break;
                case 6:
                    Hit(20,5.8f,2,true,e=>
                    {
                        Vector2 pulled=Vector2.MoveTowards(e.Position,AshAttraction(e.Position),e.Def.IsBoss?.6f:1.6f);
                        e.X=pulled.x;e.Y=pulled.y;e.KnockVelocity=0;
                        e.Slow=Mathf.Max(e.Slow,1.8f);
                    },true);
                    break;
                case 7:
                    bool near=false;
                    for(int i=0;i<Clouds.Length;i++)near|=CloudTime[i]>0&&(Clouds[i]-player).sqrMagnitude<6.25f;
                    Shield=Mathf.Max(Shield,near?39:17);
                    Hit(17,2.8f,1.4f,true,PushAway,true);
                    break;
                case 8:
                    if(CloudTime[0]<=0&&CloudTime[1]<=0){Hit(20,3.4f,1.55f,true,null,true);break;}
                    for(int i=0;i<Clouds.Length;i++)
                    {
                        if(CloudTime[i]<=0)continue;
                        CloudTime[i]=0;
                        foreach(var e in Enemies)
                            if(e.Alive&&(e.Position-Clouds[i]).sqrMagnitude<6.25f)Damage(e,38,true);
                    }
                    break;
            }
        }

        private void ResolveExtraCrimson(int skill)
        {
            switch(skill)
            {
                case 5:
                    Hp=Mathf.Min(MaxHp,Hp+15+Profile.level);
                    if(!CounterStance)Shield=Mathf.Max(Shield,19);
                    break;
                case 6:
                    foreach(var e in Enemies)
                    {
                        if(!InRange(e,4.7f,1.15f))continue;
                        if(CounterStance)e.Guard=Mathf.Max(0,e.Guard-1);
                        Damage(e,CounterStance?47:30,true);
                    }
                    break;
                case 7:
                    BeginReflection(.95f,Reflection.Stance);
                    break;
                case 8:
                    if(!CounterStance)Hp=Mathf.Min(MaxHp,Hp+30+Profile.level);
                    Hit(CounterStance?57:32,4.9f,1.45f,true,null,true);
                    break;
            }
        }
    }
}
