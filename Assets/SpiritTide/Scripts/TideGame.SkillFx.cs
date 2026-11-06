using UnityEngine;
namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private sealed class SkillFxView
        {
            public TideSkillFxGraphic ground,air;public TideSkillFxDef def;public Vector2 source,target;
            public float started,duration;public int facing,seed;public bool active;
        }
        private SkillFxView[] skillFxPool;
        private SkillFxView[] skillStatusFx;
        private AudioClip[] skillSounds;
        private RectTransform skillGroundRoot,skillAirRoot;
        private Material skillGlowMaterial;
        private int skillFxSerial,skillFxCursor;
        public int SkillFxActiveCount {get;private set;}
        public int SkillFxMeshVertices {get;private set;}
        public string LastSkillFxId {get;private set;}
        public int SkillFxPoolCapacity=>TideSkillFx.PoolSize;
        private TideSkillFxGraphic NewSkillGraphic(string name,Transform parent)
        {
            RectTransform rect=Stretch(name,parent);var graphic=rect.gameObject.AddComponent<TideSkillFxGraphic>();
            graphic.raycastTarget=false;graphic.material=skillGlowMaterial;rect.gameObject.SetActive(false);return graphic;
        }
        private SkillFxView NewSkillView(string name)
        {return new SkillFxView{ground=NewSkillGraphic(name+"-ground",skillGroundRoot),air=NewSkillGraphic(name+"-air",skillAirRoot)};}
        private void BuildSkillEffects()
        {
            var shader=Resources.Load<Shader>("SpiritTide/Vfx/SkillGlow");
            if(shader==null)throw new System.InvalidOperationException("Missing skill glow shader");
            skillGlowMaterial=new Material(shader){name="Spirit Tide shared skill glow"};
            skillGroundRoot=Stretch("SkillGroundFX",world);skillGroundRoot.SetSiblingIndex(warning.GetSiblingIndex());
            skillAirRoot=Stretch("SkillAirFX",world);skillAirRoot.SetSiblingIndex(warningText.transform.GetSiblingIndex());
            skillFxPool=new SkillFxView[TideSkillFx.PoolSize];for(int i=0;i<skillFxPool.Length;i++)skillFxPool[i]=NewSkillView("SkillFx-"+i);
            skillStatusFx=new SkillFxView[4];for(int i=0;i<skillStatusFx.Length;i++)skillStatusFx[i]=NewSkillView("SkillStatus-"+i);
            skillSounds=new AudioClip[30];for(int i=0;i<skillSounds.Length;i++)skillSounds[i]=TideSkillFx.Sound(TideSkillFx.All[i]);
        }
        private AudioClip CurrentCastSound()
        {
            int slot=(int)Session.Action-(int)MoveKind.Skill0;
            return slot>=0&&slot<5&&skillSounds!=null?skillSounds[Session.Profile.hero*5+slot]:attackSounds[Session.Profile.hero];
        }
        private void SetSkillView(SkillFxView view,bool visible,float age)
        {
            view.ground.gameObject.SetActive(visible);view.air.gameObject.SetActive(visible);if(!visible)return;
            Vector2 half=world.rect.size*.5f,source=Project(view.source.x,view.source.y)-half,target=Project(view.target.x,view.target.y)-half;
            view.ground.Frame(view.def,age,view.duration,source,target,view.facing,true,Session.Profile.reducedEffects,view.seed);
            view.air.Frame(view.def,age,view.duration,source+Vector2.up*90,target+Vector2.up*90,view.facing,false,Session.Profile.reducedEffects,view.seed);
        }
        private void ResetSkillEffects()
        {
            if(skillFxPool==null)return;
            foreach(var fx in skillFxPool){fx.active=false;SetSkillView(fx,false,0);}
            foreach(var fx in skillStatusFx)SetSkillView(fx,false,0);
            skillFxSerial=Session.AttackSerial;SkillFxActiveCount=SkillFxMeshVertices=0;LastSkillFxId=null;
        }
        private void DrawSkillEffects()
        {
            if(skillFxPool==null)return;
            if(!Session.InBattle)
            {
                foreach(var fx in skillFxPool){fx.active=false;SetSkillView(fx,false,0);}
                foreach(var fx in skillStatusFx)SetSkillView(fx,false,0);
                skillFxSerial=Session.AttackSerial;SkillFxActiveCount=SkillFxMeshVertices=0;return;
            }
            if(skillFxSerial!=Session.AttackSerial)
            {
                skillFxSerial=Session.AttackSerial;int slot=(int)Session.Action-(int)MoveKind.Skill0;
                if(Session.IsFighting&&Session.ActionRemaining>0&&slot>=0&&slot<5)
                {
                    var fx=skillFxPool[skillFxCursor++%skillFxPool.Length];fx.active=true;fx.def=TideSkillFx.Get(Session.Profile.hero,slot);
                    fx.source=new Vector2(Session.X,Session.Y);fx.target=new Vector2(Session.EnemyX,Session.EnemyY);fx.facing=Session.Facing;
                    fx.started=Session.Clock-Session.ActionElapsed;fx.duration=Session.ActionDuration;fx.seed=skillFxSerial*31+Session.Profile.hero*73;
                    LastSkillFxId=fx.def.id;
                }
            }
            SkillFxActiveCount=SkillFxMeshVertices=0;
            foreach(var fx in skillFxPool)
            {
                float age=Session.Clock-fx.started;fx.active&=age<=fx.duration+(fx.def==null?0:fx.def.tail)&&age>=0;
                SetSkillView(fx,fx.active,age);
                if(fx.active){SkillFxActiveCount++;SkillFxMeshVertices+=fx.air.VertexCount+fx.ground.VertexCount;}
            }
            // These effects follow real gameplay state, including cloud detonation and stance changes.
            int hero=Session.Profile.hero;
            for(int i=0;i<skillStatusFx.Length;i++)
            {
                var fx=skillStatusFx[i];bool active=false;float age=1;
                if(hero==3&&i<Session.Enemies.Count&&Session.Enemies[i].Alive&&Session.Enemies[i].Mark>0)
                {active=true;fx.def=TideSkillFx.Get(3,0);fx.source=fx.target=Session.Enemies[i].Position;fx.duration=100000;age=1+Mathf.Repeat(Session.Clock,1000);}
                else if(hero==4&&i<2&&Session.CloudTime[i]>0)
                {active=true;fx.def=TideSkillFx.Get(4,1);fx.source=Session.Clouds[i]-new Vector2(Session.Facing*1.5f,0);fx.target=Session.Clouds[i];fx.duration=6;age=5-Session.CloudTime[i]+.1f;}
                else if(i==0&&(hero==1&&(Session.Shield>0||Session.ReflectRemaining>0)||hero==5&&Session.CounterStance||hero==2&&Session.PerfectRemaining>0))
                {active=true;fx.def=TideSkillFx.Get(hero,hero==1?(Session.ReflectRemaining>0?3:0):3);fx.source=new Vector2(Session.X,Session.Y);fx.target=fx.source;fx.duration=100000;age=1+Mathf.Repeat(Session.Clock,1000);}
                fx.facing=Session.Facing;fx.seed=911+i;SetSkillView(fx,active,age);
            }
        }
        private void OnDestroy()
        {
            if(skillGlowMaterial!=null)Destroy(skillGlowMaterial);
            if(skillSounds!=null)foreach(var clip in skillSounds)if(clip!=null)Destroy(clip);
        }
    }
}
