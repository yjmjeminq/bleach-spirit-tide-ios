using UnityEngine;
using UnityEngine.UI;
namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private sealed class ImpactView
        {
            public int uid,serial,direction;
            public float life,lift;
            public Vector2 at;
            public TideImpactStrength strength;
            public Text number;
            public TideImpactBurst burst;
        }
        private const float ImpactNumberLife=.42f;
        private readonly ImpactView[] impacts=new ImpactView[4];
        private TideStrikeTrail strikeTrail;private TideArc auraArc,targetArc;private TideCastFlash castBeam;private Text chainText;
        private Vector2 impactOffset;
        private float shakeTime,shakeDuration,shakeAmount;
        private int shakeDirection,feelImpactSerial,feelAttackSerial,feelPerfectSerial,feelBreakSerial,feelHurtSerial;
        private AudioClip[] attackSounds=new AudioClip[6],impactSounds=new AudioClip[6];private AudioClip firmImpact,heavyImpact,parrySound,breakSound;private AudioSource ambience;
        private static readonly Color[] HeroColors={new Color(.5f,.86f,1),new Color(1,.8f,.25f),new Color(.77f,.48f,1),new Color(1,.77f,.2f),new Color(.78f,.82f,.86f),new Color(.85f,.25f,.35f)};
        private TideArc MakeArc(string name,Vector2 size)
        {
            var r=Node(name,world,Vector2.zero,Vector2.zero,size);var arc=r.gameObject.AddComponent<TideArc>();arc.raycastTarget=false;return arc;
        }
        private void BuildFeel()
        {
            strikeTrail=weaponSweep.gameObject.AddComponent<TideStrikeTrail>();strikeTrail.raycastTarget=false;
            auraArc=MakeArc("SpiritAura",new Vector2(170,210));
            castBeam=Node("SpiritBeam",world,Vector2.zero,Vector2.zero,new Vector2(84,12)).gameObject.AddComponent<TideCastFlash>();
            castBeam.raycastTarget=false;
            targetArc=MakeArc("TargetRing",new Vector2(105,30));targetArc.Shape(360,.07f);targetArc.color=gold;targetArc.transform.SetSiblingIndex(actorLayer.GetSiblingIndex());
            auraArc.transform.SetSiblingIndex(weaponSweep.GetSiblingIndex());castBeam.transform.SetSiblingIndex(weaponSweep.GetSiblingIndex());
            var chain=Surface("CombatChain",safe,new Vector2(1,1),new Vector2(-175,-128),new Vector2(326,59),surfaceDark).rectTransform;
            chain.SetSiblingIndex(controlsRoot.transform.GetSiblingIndex());
            chainText=Label("",chain,Center,Vector2.zero,new Vector2(300,53),20,gold,true);
            chainText.alignment=TextAnchor.MiddleCenter;
            for(int i=0;i<impacts.Length;i++)
            {
                var v=new ImpactView();v.number=Label("",world,Vector2.zero,Vector2.zero,new Vector2(180,45),29,ink,true);v.number.alignment=TextAnchor.MiddleCenter;
                var outline=v.number.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.02f,.03f,.04f,.95f);outline.effectDistance=new Vector2(1.5f,-1.5f);
                v.burst=Node("ImpactBurst-"+i,world,Vector2.zero,Vector2.zero,new Vector2(180,168)).gameObject.AddComponent<TideImpactBurst>();
                v.burst.raycastTarget=false;v.burst.transform.SetSiblingIndex(v.number.transform.GetSiblingIndex());
                v.burst.gameObject.SetActive(false);
                impacts[i]=v;
            }
            for(int hero=0;hero<6;hero++){attackSounds[hero]=CombatSound(650+hero*95,.062f,false,hero+1);impactSounds[hero]=CombatSound(142+hero*12,.086f,true,hero+8);}
            firmImpact=CombatSound(111,.126f,true,37);heavyImpact=CombatSound(79,.177f,true,41);
            parrySound=CombatSound(1100,.18f,false,50);breakSound=CombatSound(70,.25f,true,60);
            ambience=gameObject.AddComponent<AudioSource>();ambience.clip=CombatSound(75,4,false,71,true);ambience.loop=true;ambience.volume=.025f;ambience.Play();BuildSkillEffects();BuildNaturalMotion();
        }
        private AudioClip CombatSound(float hz,float length,bool impact,int seed,bool ambient=false)
        {
            int n=Mathf.CeilToInt(22050*length);var samples=new float[n];uint state=(uint)seed;float low=0;
            for(int i=0;i<n;i++)
            {
                float t=i/22050f,p=i/(float)n;state=state*1664525+1013904223;float noise=((state>>8)&65535)/32768f-1;low=Mathf.Lerp(low,noise,ambient?.006f:.2f);
                if(impact)
                {
                    float attack=Mathf.Clamp01(t/.0015f),tail=1-p;
                    float body=Mathf.Sin(2*Mathf.PI*hz*t*(1-.36f*p))*.46f*Mathf.Pow(tail,2.6f);
                    float crack=(noise-low)*.32f*Mathf.Exp(-t*99);
                    float weight=Mathf.Sin(2*Mathf.PI*(hz*.53f)*t)*.24f*tail*tail;
                    samples[i]=attack*(body+crack+weight);
                }
                else
                {
                    float envelope=Mathf.Sin(Mathf.Min(1,p*18)*Mathf.PI*.5f)*(1-p)*(1-p);
                    float tone=Mathf.Sin(2*Mathf.PI*hz*(1+p*.15f)*t);
                    samples[i]=ambient?(low*.4f+Mathf.Sin(t*2*Mathf.PI*220)*.012f*Mathf.Sin(t*Mathf.PI*.5f)):envelope*(low*.3f+tone*.11f);
                }
            }
            var clip=AudioClip.Create("SpiritTide-"+(ambient?"Ambience":impact?"Impact":"Cast")+"-"+seed,n,1,22050,false);clip.SetData(samples,0);return clip;
        }
        private void PlayImpactSound()
        {
            var strength=Session.LastImpactStrength;
            AudioClip clip=strength==TideImpactStrength.Heavy?heavyImpact:strength==TideImpactStrength.Firm?firmImpact:impactSounds[Session.Profile.hero];
            float gain=strength==TideImpactStrength.Light?.84f:1;
            audioSource.pitch=1;audioSource.PlayOneShot(clip,gain);ReviewSound(clip,gain);
            KickScreen(strength,Session.LastImpactDirection);
        }
        private void KickScreen(TideImpactStrength strength,int direction)
        {
            shakeAmount=strength==TideImpactStrength.Heavy?6:strength==TideImpactStrength.Firm?3.7f:1.8f;
            shakeDuration=shakeTime=strength==TideImpactStrength.Heavy?.13f:strength==TideImpactStrength.Firm?.095f:.065f;
            shakeDirection=direction;
        }
        private float FeedbackDelta=>SmokeMode&&ReviewStepping?1f/30:Time.unscaledDeltaTime;
        private void UpdateImpactOffset()
        {
            if(Session.IsPaused)
            {
                if(Session.Profile.reducedEffects)impactOffset=Vector2.zero;
                return;
            }
            shakeTime=Mathf.Max(0,shakeTime-FeedbackDelta);
            if(feelImpactSerial!=Session.HitStopEvents)
            {
                bool landed=Session.HitStopEvents>feelImpactSerial;
                feelImpactSerial=Session.HitStopEvents;
                if(landed)PlayImpactSound();
            }
            if(feelHurtSerial!=Session.HurtSerial)
            {
                feelHurtSerial=Session.HurtSerial;
                if(Session.InBattle){KickScreen(TideImpactStrength.Firm,-Session.Facing);audioSource.PlayOneShot(heavyImpact,.5f);}
            }
            if(!Session.InBattle||Session.Profile.reducedEffects||shakeTime<=0){impactOffset=Vector2.zero;return;}
            float left=shakeTime/shakeDuration;
            float pulse=Mathf.Cos((1-left)*Mathf.PI*1.4f)*left*left*shakeAmount;
            impactOffset=new Vector2(-shakeDirection*pulse,-pulse*.26f);
        }
        private void DrawFeel()
        {
            float dt=FeedbackDelta;bool fighting=Session.IsFighting;
            ambience.volume=Session.Profile.soundEnabled?(Session.InBattle?.012f:.025f):0;
            bool defensive=Session.ActionRemaining>0&&(Session.Profile.hero==1&&(Session.Action==MoveKind.Skill0||Session.Action==MoveKind.Skill1||Session.Action==MoveKind.Skill3)||Session.Profile.hero==5&&(Session.Action==MoveKind.Skill0||Session.Action==MoveKind.Skill2||Session.Action==MoveKind.Skill3));
            DrawNaturalActor();
            if(Session.AttackSerial!=feelAttackSerial){feelAttackSerial=Session.AttackSerial;if(fighting){audioSource.pitch=1;audioSource.PlayOneShot(CurrentCastSound(),.8f);ReviewSound(CurrentCastSound(),.8f);}}
            if(Session.PerfectEvents!=feelPerfectSerial){bool play=Session.PerfectEvents>feelPerfectSerial;feelPerfectSerial=Session.PerfectEvents;if(play&&fighting){audioSource.pitch=1;audioSource.PlayOneShot(parrySound);ReviewSound(parrySound);}}
            if(Session.PoiseBreaks!=feelBreakSerial){bool play=Session.PoiseBreaks>feelBreakSerial;feelBreakSerial=Session.PoiseBreaks;if(play&&fighting){audioSource.PlayOneShot(breakSound);ReviewSound(breakSound);}}
            Color color=HeroColors[Session.Profile.hero];color.a=Session.Profile.reducedEffects?.44f:.78f;
            DrawStrikeTrail(fighting,color);
            auraArc.gameObject.SetActive(fighting&&(defensive||Session.Shield>0||Session.Action==MoveKind.Ultimate&&Session.ActionRemaining>0));auraArc.rectTransform.anchoredPosition=actor.anchoredPosition+new Vector2(0,90);auraArc.Shape(360,.025f);auraArc.color=defensive?new Color(.5f,.95f,.7f,.55f):new Color(color.r,color.g,color.b,.38f);
            string counter=Session.PerfectRemaining>0?"정밀 회피 · 공격하면 반격":"";
            string chain=Session.ChainHits>1?Session.ChainHits+" HIT  ·  최고 "+Session.ChainPeak+(counter.Length>0?"\n"+counter:""):counter;
            chainText.transform.parent.gameObject.SetActive(fighting&&!Session.IsTraining&&chain.Length>0);
            Say(chainText,chain);
            targetArc.gameObject.SetActive(Session.InBattle&&Session.Target.Alive);targetArc.rectTransform.anchoredPosition=Project(Session.EnemyX,Session.EnemyY);
            for(int i=0;i<impacts.Length;i++)
            {
                var v=impacts[i];var e=i<Session.Enemies.Count?Session.Enemies[i]:null;
                bool fresh=false;
                if(e==null){v.life=0;}else
                {
                    if(v.uid!=e.Uid){v.uid=e.Uid;v.serial=0;v.life=0;}
                    if(v.serial!=e.DamageSerial)
                    {
                        fresh=true;v.serial=e.DamageSerial;v.life=ImpactNumberLife;v.at=e.Position;v.lift=e.Lift;
                        v.strength=e.ImpactStrength;v.direction=e.ImpactDirection;
                        v.number.fontSize=v.strength==TideImpactStrength.Heavy?36:v.strength==TideImpactStrength.Firm?32:28;
                        v.number.text=Mathf.CeilToInt(e.LastDamage).ToString();
                    }
                }
                if(!fresh&&!Session.IsPaused)v.life=Mathf.Max(0,v.life-dt);
                bool show=Session.InBattle&&v.life>0;v.number.gameObject.SetActive(show);
                if(i<foeViews.Count)foeViews[i].damage.gameObject.SetActive(false);
                if(e!=null)foeViews[i].body.raycastTarget=e.Alive&&Session.IsFighting;
                float age=ImpactNumberLife-v.life;
                Vector2 at=Project(v.at.x,v.at.y)+new Vector2(-v.direction*10,87+v.lift*32);
                v.number.rectTransform.anchoredPosition=at+new Vector2(40,49+age*67);
                v.number.color=new Color(color.r,color.g,color.b,Mathf.Clamp01(v.life/.15f));
                float burstDuration=v.strength==TideImpactStrength.Heavy?.18f:v.strength==TideImpactStrength.Firm?.145f:.105f;
                bool spark=show&&v.strength!=TideImpactStrength.None&&age<burstDuration;
                v.burst.gameObject.SetActive(spark);
                if(spark)
                {
                    v.burst.rectTransform.anchoredPosition=at;
                    v.burst.Frame(age/burstDuration,v.strength,v.direction,v.uid+v.serial*17,color,Session.Profile.reducedEffects);
                }
                if(e!=null&&!e.Alive&&show){var body=foeViews[i];body.root.gameObject.SetActive(true);body.body.color=new Color(.65f,.86f,1,v.life/ImpactNumberLife);body.root.localScale=Vector3.one;body.hp.transform.parent.gameObject.SetActive(false);body.label.gameObject.SetActive(false);}
                else if(e!=null)foeViews[i].root.localScale=Vector3.one;
            }
        }
        private void DrawStrikeTrail(bool fighting,Color color)
        {
            var motion=naturalMotion;
            float phase=Session.ActionPosePhase;
            int facing=motion.DisplayFacing,hero=Session.Profile.hero;
            bool swing=fighting&&Session.HasActionPose&&Session.Action<MoveKind.Skill0&&phase>=.14f&&phase<.78f;
            bool sword=motion.Rig.weapon==2;
            int limb=motion.StrikeLimb;
            Vector2 end=motion.Positions[limb];
            Vector2 normal=(end-motion.Positions[limb-1]).normalized;
            normal=new Vector2(-normal.y,normal.x)*(limb==12?5:3);
            Vector2 inner=sword?motion.Positions[17]:end-normal;
            Vector2 outer=sword?motion.BladeTip:end+normal;
            Vector2 screenOrigin=Project(0,0),root=actor.anchoredPosition-screenOrigin;
            inner=root+new Vector2(inner.x*facing,inner.y);outer=root+new Vector2(outer.x*facing,outer.y);
            float envelope=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.19f,.37f,phase))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.52f,.78f,phase)));
            color.a*=envelope;
            strikeTrail.Draw(Session.PresentationClock,Session.PresentationEpoch,Session.AttackSerial,facing,swing&&hero!=1,sword,screenOrigin,inner,outer,color);

            bool release=swing&&(hero==1||hero==3)&&phase>=.32f&&phase<.67f;
            castBeam.gameObject.SetActive(release);
            if(!release)return;
            float rise=Session.Action==MoveKind.Launcher?.30f:Session.Action==MoveKind.Air?-.24f:0;
            var direction=new Vector2(facing,rise).normalized;
            Vector2 hand=actor.anchoredPosition+new Vector2(end.x*facing,end.y);
            float reach=hero==1?118:68;
            if(hero==1&&Session.Target.Alive&&Mathf.Abs(Session.EnemyX-Session.X)<=7&&Mathf.Abs(Session.EnemyY-Session.Y)<=.9f
                &&(Session.EnemyX-Session.X)*facing>=-.3f)
            {
                Vector2 toTarget=Project(Session.EnemyX,Session.EnemyY)+Vector2.up*(88+Session.Target.Lift*32)-hand;
                if(toTarget.x*facing>8){direction=toTarget.normalized;reach=Mathf.Clamp(toTarget.magnitude,25,312);}
            }
            float length=Mathf.Lerp(18,reach,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.32f,TideSession.ActionContactPhase,phase)));
            castBeam.rectTransform.anchoredPosition=hand+direction*(length*.48f);
            castBeam.rectTransform.sizeDelta=new Vector2(length,hero==1?17:8);
            castBeam.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            castBeam.color=color;
        }
    }

    // The trail lives in world canvas coordinates, so camera movement cannot detach it from the swing.
    public sealed class TideStrikeTrail:MaskableGraphic
    {
        private struct Sample {public Vector2 inner,outer;public float time;}
        private const float Life=.086f;
        private readonly Sample[] samples=new Sample[8];
        private int next,count,epoch=-1,action=-1,facing;
        private float clock,lastSample;
        private Vector2 origin;
        private Color tint;
        private bool sword;

        public void Draw(float time,int worldEpoch,int serial,int direction,bool active,bool blade,Vector2 offset,Vector2 inner,Vector2 outer,Color shade)
        {
            if(!active||epoch!=worldEpoch||action!=serial||facing!=direction)
            {
                if(count>0)SetVerticesDirty();
                count=next=0;
                epoch=worldEpoch;action=serial;facing=direction;
            }
            if(!active)return;
            if(count==0||time-lastSample>.00001f)
            {
                samples[next]=new Sample{inner=inner,outer=outer,time=time};
                next=(next+1)%samples.Length;count=Mathf.Min(count+1,samples.Length);lastSample=time;
            }
            clock=time;origin=offset;tint=shade;sword=blade;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            if(count<2)return;
            Sample previous=default;
            for(int i=0;i<count;i++)
            {
                Sample sample=samples[(next-count+i+samples.Length)%samples.Length];
                if(i==0){AddBand(helper,sample.inner,sample.outer,sample.time);previous=sample;continue;}
                Vector2 from=previous.outer-previous.inner,to=sample.outer-sample.inner;
                float angle=sword?Vector2.SignedAngle(from,to):0;
                int segments=Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(angle)/9),1,8);
                for(int part=1;part<=segments;part++)
                {
                    float t=part/(float)segments;
                    Vector2 start=Vector2.Lerp(previous.inner,sample.inner,t);
                    Vector2 end=sword?start+TideRigGraphic.Rotate(from.normalized,angle*t)*Mathf.Lerp(from.magnitude,to.magnitude,t)
                        :Vector2.Lerp(previous.outer,sample.outer,t);
                    AddBand(helper,start,end,Mathf.Lerp(previous.time,sample.time,t));
                }
                previous=sample;
            }
        }
        private void AddBand(VertexHelper helper,Vector2 from,Vector2 to,float time)
        {
            float alpha=tint.a*Mathf.Clamp01(1-(clock-time)/Life);
            var vertex=UIVertex.simpleVert;int index=helper.currentVertCount;
            vertex.position=origin+Vector2.Lerp(from,to,sword?.73f:0);vertex.color=new Color(tint.r,tint.g,tint.b,0);helper.AddVert(vertex);
            Color center=Color.Lerp(tint,Color.white,.38f);center.a=alpha*.68f;
            vertex.position=origin+Vector2.Lerp(from,to,sword?.94f:.5f);vertex.color=center;helper.AddVert(vertex);
            vertex.position=origin+to;vertex.color=new Color(tint.r,tint.g,tint.b,0);helper.AddVert(vertex);
            if(index==0)return;
            for(int rail=0;rail<2;rail++)
            {
                int a=index-3+rail,b=index+rail;
                helper.AddTriangle(a,a+1,b+1);helper.AddTriangle(a,b+1,b);
            }
        }
    }

    public sealed class TideImpactBurst:MaskableGraphic
    {
        private TideImpactStrength strength;
        private float phase;
        private int direction,seed;
        private Color shade;
        private bool reduced;
        public int VertexCount {get;private set;}

        public void Frame(float progress,TideImpactStrength weight,int facing,int variation,Color tint,bool reducedEffects)
        {
            phase=progress;strength=weight;direction=facing;seed=variation;shade=tint;reduced=reducedEffects;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            int weight=(int)strength;
            float scale=reduced?.70f:1;
            float fade=(1-phase)*(1-phase)*shade.a;
            Color bright=Color.Lerp(shade,Color.white,.76f);
            Color soft=shade;soft.a=0;
            float core=(1-Mathf.Clamp01(phase*3.8f))*scale;
            if(core>0)
            {
                bright.a=core*shade.a;
                float width=(9+weight*3)*core,height=(6+weight*2)*core;
                Vector2 right=new Vector2(width,0),up=new Vector2(0,height);
                Triangle(helper,Vector2.zero,right,up,bright,soft,soft);
                Triangle(helper,Vector2.zero,up,-right,bright,soft,soft);
                Triangle(helper,Vector2.zero,-right,-up,bright,soft,soft);
                Triangle(helper,Vector2.zero,-up,right,bright,soft,soft);
            }
            int count=reduced?4:3+weight*2;
            float travel=Mathf.SmoothStep(0,1,phase);
            bright.a=fade;
            for(int i=0;i<count;i++)
            {
                int variation=(seed+i*29)%11;
                float angle=(Mathf.Lerp(-111,105,(i+.5f)/count)+(variation-5)*4)*Mathf.Deg2Rad;
                var ray=new Vector2(direction*Mathf.Cos(angle),Mathf.Sin(angle));
                Vector2 along=ray*(5+travel*(22+weight*10+variation))*scale;
                Vector2 normal=new Vector2(-ray.y,ray.x)*(1.1f+.55f*weight)*scale;
                Vector2 tip=along+ray*((12+weight*5+variation)*Mathf.Lerp(1,.42f,phase)*scale);
                Triangle(helper,along-normal,along+normal,tip,bright,bright,soft);
            }
            VertexCount=helper.currentVertCount;
        }
        private static void Triangle(VertexHelper helper,Vector2 a,Vector2 b,Vector2 c,Color first,Color second,Color third)
        {
            int start=helper.currentVertCount;
            var vertex=UIVertex.simpleVert;
            vertex.position=a;vertex.color=first;helper.AddVert(vertex);
            vertex.position=b;vertex.color=second;helper.AddVert(vertex);
            vertex.position=c;vertex.color=third;helper.AddVert(vertex);
            helper.AddTriangle(start,start+1,start+2);
        }
    }

    public sealed class TideCastFlash:MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            Rect rect=rectTransform.rect;
            for(int i=0;i<4;i++)
            {
                float along=i==0?0:i==1?.14f:i==2?.71f:1;
                float width=i==0||i==3?0:i==1?.38f:.22f;
                float alpha=i==0||i==3?0:color.a*(i==1?.92f:.6f);
                for(int rail=-1;rail<=1;rail++)
                {
                    Color tint=rail==0?Color.Lerp(color,Color.white,.70f):color;
                    tint.a=rail==0?alpha:0;
                    var vertex=UIVertex.simpleVert;
                    vertex.position=new Vector2(Mathf.Lerp(rect.xMin,rect.xMax,along),rail*rect.height*width);
                    vertex.color=tint;helper.AddVert(vertex);
                }
                if(i==0)continue;
                for(int rail=0;rail<2;rail++)
                {
                    int a=(i-1)*3+rail,b=i*3+rail;
                    helper.AddTriangle(a,a+1,b+1);helper.AddTriangle(a,b+1,b);
                }
            }
        }
    }
}
