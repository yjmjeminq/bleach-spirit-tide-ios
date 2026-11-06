using UnityEngine;
using UnityEngine.UI;
namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private sealed class ImpactView{public int uid,serial;public float life;public Vector2 at;public Text number;public Image[] rays=new Image[6];}
        private readonly ImpactView[] impacts=new ImpactView[4];
        private TideArc weaponArc,auraArc,targetArc;private Image castBeam;private Text chainText;private Image poiseFill;
        private Vector2 impactOffset;private float shakeTime,hurtPoseTime;private int feelAttackSerial,feelPerfectSerial,feelBreakSerial,feelHurtSerial;
        private AudioClip[] attackSounds=new AudioClip[6],impactSounds=new AudioClip[6];private AudioClip heavyImpact,parrySound,breakSound;private AudioSource ambience;
        private static readonly Color[] HeroColors={new Color(.5f,.86f,1),new Color(1,.8f,.25f),new Color(.77f,.48f,1),new Color(1,.77f,.2f),new Color(.78f,.82f,.86f),new Color(.85f,.25f,.35f)};
        private TideArc MakeArc(string name,Vector2 size)
        {
            var r=Node(name,world,Vector2.zero,Vector2.zero,size);var arc=r.gameObject.AddComponent<TideArc>();arc.raycastTarget=false;return arc;
        }
        private void BuildFeel()
        {
            weaponArc=MakeArc("WeaponArc",new Vector2(190,170));auraArc=MakeArc("SpiritAura",new Vector2(170,210));castBeam=Picture("SpiritBeam",world,Vector2.zero,Vector2.zero,new Vector2(160,14),Color.white);
            targetArc=MakeArc("TargetRing",new Vector2(105,30));targetArc.Shape(360,.07f);targetArc.color=gold;targetArc.transform.SetSiblingIndex(actorLayer.GetSiblingIndex());
            foreach(var value in new Transform[]{weaponArc.transform,auraArc.transform,castBeam.transform})value.SetSiblingIndex(slash.GetSiblingIndex());
            chainText=Label("",safe,new Vector2(.5f,1),new Vector2(0,-43),new Vector2(590,84),30,gold,true);chainText.alignment=TextAnchor.MiddleCenter;chainText.transform.SetSiblingIndex(menuRoot.transform.GetSiblingIndex());
            var track=Picture("PoiseTrack",safe,new Vector2(.5f,1),new Vector2(0,-155),new Vector2(640,4),new Color(.12f,.2f,.26f));track.transform.SetSiblingIndex(menuRoot.transform.GetSiblingIndex());
            poiseFill=Picture("PoiseFill",track.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(640,4),new Color(.4f,.75f,1));poiseFill.type=Image.Type.Filled;poiseFill.fillMethod=Image.FillMethod.Horizontal;
            progressText.rectTransform.anchoredPosition=new Vector2(0,-184);progressText.rectTransform.sizeDelta=new Vector2(720,30);foreach(var marker in roomMarkers)marker.rectTransform.anchoredPosition+=Vector2.down*6;
            for(int i=0;i<impacts.Length;i++)
            {
                var v=new ImpactView();v.number=Label("",world,Vector2.zero,Vector2.zero,new Vector2(180,45),29,ink,true);v.number.alignment=TextAnchor.MiddleCenter;
                var outline=v.number.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.02f,.03f,.04f,.95f);outline.effectDistance=new Vector2(1.5f,-1.5f);
                for(int j=0;j<v.rays.Length;j++)v.rays[j]=Picture("ImpactRay-"+i+"-"+j,world,Vector2.zero,Vector2.zero,new Vector2(35,4),Color.white);
                impacts[i]=v;
            }
            for(int hero=0;hero<6;hero++){attackSounds[hero]=CombatSound(650+hero*95,.075f,false,hero+1);impactSounds[hero]=CombatSound(130+hero*17,.11f,true,hero+8);}
            heavyImpact=CombatSound(80,.18f,true,41);parrySound=CombatSound(1100,.18f,false,50);breakSound=CombatSound(70,.25f,true,60);
            ambience=gameObject.AddComponent<AudioSource>();ambience.clip=CombatSound(75,4,false,71,true);ambience.loop=true;ambience.volume=.025f;ambience.Play();
        }
        private AudioClip CombatSound(float hz,float length,bool impact,int seed,bool ambient=false)
        {
            int n=Mathf.CeilToInt(22050*length);var samples=new float[n];uint state=(uint)seed;float low=0;
            for(int i=0;i<n;i++)
            {
                float t=i/22050f,p=i/(float)n;state=state*1664525+1013904223;float noise=((state>>8)&65535)/32768f-1;low=Mathf.Lerp(low,noise,ambient?.006f:.2f);
                float envelope=ambient?.6f:Mathf.Sin(Mathf.Min(1,p*18)*Mathf.PI*.5f)*Mathf.Pow(1-p,impact?3:2);
                float tone=Mathf.Sin(2*Mathf.PI*(hz*(impact?1-p*.55f:1+p*.15f))*t);
                samples[i]=ambient?(low*.4f+Mathf.Sin(t*2*Mathf.PI*220)*.012f*Mathf.Sin(t*Mathf.PI*.5f)):envelope*(impact?noise*.22f+tone*.38f:low*.3f+tone*.11f);
            }
            var clip=AudioClip.Create("SpiritTide-"+(ambient?"Ambience":impact?"Impact":"Cast")+"-"+seed,n,1,22050,false);clip.SetData(samples,0);return clip;
        }
        private void PlayImpactSound()
        {
            bool heavy=Session.Action==MoveKind.Heavy||Session.Action==MoveKind.Ultimate||Session.Action==MoveKind.Counter;
            audioSource.pitch=.94f+(Session.DamageEvents%5)*.025f;audioSource.PlayOneShot(heavy?heavyImpact:impactSounds[Session.Profile.hero]);ReviewSound(heavy?heavyImpact:impactSounds[Session.Profile.hero]);shakeTime=Mathf.Max(shakeTime,heavy?.14f:.07f);
        }
        private void DrawFeel()
        {
            float dt=SmokeMode&&ReviewStepping?1f/30:Time.unscaledDeltaTime;bool fighting=Session.IsFighting;bool moving=Joystick.Value.sqrMagnitude>.01f||keyboardMove.sqrMagnitude>.01f;
            ambience.volume=Session.Profile.soundEnabled?(Session.InBattle?.012f:.025f):0;
            shakeTime=Mathf.Max(0,shakeTime-dt);float shake=Session.Profile.reducedEffects?0:Mathf.Min(1,shakeTime/.1f)*3;impactOffset=new Vector2(Mathf.Sin(Time.unscaledTime*125),Mathf.Cos(Time.unscaledTime*91))*shake;
            if(!Session.InBattle)impactOffset=Vector2.zero;
            if(Session.HurtSerial!=feelHurtSerial){feelHurtSerial=Session.HurtSerial;shakeTime=.16f;hurtPoseTime=.22f;audioSource.PlayOneShot(heavyImpact,.5f);}hurtPoseTime=Mathf.Max(0,hurtPoseTime-dt);
            int frame=-1;
            if(fighting&&hurtPoseTime>0)frame=6;
            else if(Session.DodgeRemaining>0)frame=5;
            else if(Session.Z>.1f)frame=7;
            else if(Session.ActionRemaining>0){float p=Session.ActionElapsed/Mathf.Max(.01f,Session.ActionDuration);frame=Session.Action==MoveKind.Ultimate?8:p<.35f?2:p<.65f?3:4;}
            else if(moving)frame=(int)((Session.InBattle?Session.Clock:Time.unscaledTime)*7)%2;
            bool defensive=Session.ActionRemaining>0&&(Session.Profile.hero==1&&(Session.Action==MoveKind.Skill0||Session.Action==MoveKind.Skill1||Session.Action==MoveKind.Skill3)||Session.Profile.hero==5&&(Session.Action==MoveKind.Skill0||Session.Action==MoveKind.Skill2||Session.Action==MoveKind.Skill3));
            if(defensive)frame=8;
            if(frame==4&&Session.Profile.hero==5)frame=2;
            if(frame==5)frame=0;
            var pose=frame>=0?TideArt.Motion(Session.Profile.hero,frame):null;actorImage.sprite=pose??heroSprites[Session.Profile.hero];actor.sizeDelta=pose!=null?new Vector2(pose.rect.width/pose.rect.height*192,192):new Vector2(128,192);actor.pivot=new Vector2(pose!=null?TideArt.MotionPivot(Session.Profile.hero,frame):.5f,0);
            float phase=Session.ActionRemaining>0?Session.ActionElapsed/Mathf.Max(.01f,Session.ActionDuration):0,drive=Mathf.Sin(Mathf.Clamp01(phase)*Mathf.PI);
            actor.anchoredPosition+=new Vector2(Session.Facing*drive*12,Session.DodgeRemaining>0?-8:0);
            if(pose==null){actor.localRotation=Quaternion.Euler(0,0,Session.Profile.reducedEffects?0:Session.DodgeRemaining>0?-Session.Facing*14:-Session.Facing*drive*7);actor.localScale=new Vector3(Session.Facing*(1+drive*.035f),1-drive*.025f,1);}
            if(Session.DodgeRemaining>0){actor.localRotation=Quaternion.Euler(0,0,-Session.Facing*12);actor.localScale=new Vector3(Session.Facing*1.05f,.92f,1);}
            if(Session.AttackSerial!=feelAttackSerial){feelAttackSerial=Session.AttackSerial;if(fighting){audioSource.pitch=1;audioSource.PlayOneShot(attackSounds[Session.Profile.hero],.8f);ReviewSound(attackSounds[Session.Profile.hero],.8f);}}
            if(Session.PerfectEvents!=feelPerfectSerial){bool play=Session.PerfectEvents>feelPerfectSerial;feelPerfectSerial=Session.PerfectEvents;if(play&&fighting){audioSource.pitch=1;audioSource.PlayOneShot(parrySound);ReviewSound(parrySound);}}
            if(Session.PoiseBreaks!=feelBreakSerial){bool play=Session.PoiseBreaks>feelBreakSerial;feelBreakSerial=Session.PoiseBreaks;if(play&&fighting){audioSource.PlayOneShot(breakSound);ReviewSound(breakSound);}}
            bool arc=fighting&&!defensive&&Session.ActionRemaining>0&&phase>.15f&&phase<.85f;
            weaponArc.gameObject.SetActive(arc);castBeam.gameObject.SetActive(arc&&(Session.Profile.hero==1||Session.Profile.hero==3));slash.gameObject.SetActive(false);
            Color color=HeroColors[Session.Profile.hero];color.a=Session.Profile.reducedEffects?.55f:.85f;weaponArc.color=color;weaponArc.Shape(Session.Profile.hero==1?360:Session.Profile.hero==3?65:Session.Action==MoveKind.Ultimate?300:140,Session.Action==MoveKind.Heavy?.13f:.07f,Session.Profile.hero==1?3:24);
            weaponArc.rectTransform.anchoredPosition=actor.anchoredPosition+new Vector2(Session.Facing*35,85);weaponArc.rectTransform.localScale=new Vector3(Session.Facing,1,1);weaponArc.rectTransform.localRotation=Quaternion.Euler(0,0,(phase-.5f)*-Session.Facing*90);
            weaponArc.rectTransform.sizeDelta=Session.Action==MoveKind.Ultimate?new Vector2(330,260):Session.Profile.hero==3?new Vector2(170,80):new Vector2(190,170);
            castBeam.rectTransform.anchoredPosition=actor.anchoredPosition+new Vector2(Session.Facing*(70+phase*70),90);castBeam.rectTransform.sizeDelta=new Vector2(Session.Profile.hero==3?180:90,Session.Profile.hero==3?4:30);castBeam.color=color;
            auraArc.gameObject.SetActive(fighting&&(defensive||Session.Shield>0||Session.Action==MoveKind.Ultimate&&Session.ActionRemaining>0));auraArc.rectTransform.anchoredPosition=actor.anchoredPosition+new Vector2(0,90);auraArc.Shape(360,.025f);auraArc.color=defensive?new Color(.5f,.95f,.7f,.55f):new Color(color.r,color.g,color.b,.38f);
            chainText.gameObject.SetActive(Session.InBattle&&!Session.IsTraining);Say(chainText,Session.ChainHits>1?Session.ChainHits+" HIT · 최고 "+Session.ChainPeak+(Session.PerfectRemaining>0?"\n정밀 회피 · 반격 기회":""):Session.PerfectRemaining>0?"정밀 회피 · 공격하면 반격":"");
            targetArc.gameObject.SetActive(Session.InBattle&&Session.Target.Alive);targetArc.rectTransform.anchoredPosition=Project(Session.EnemyX,Session.EnemyY);
            bool poise=Session.InBattle&&Session.Target.Def!=null&&Session.Target.Def.IsBoss&&Session.AliveCount>0;poiseFill.transform.parent.gameObject.SetActive(poise);poiseFill.fillAmount=Session.Target.Poise/100;
            for(int i=0;i<impacts.Length;i++)
            {
                var v=impacts[i];var e=i<Session.Enemies.Count?Session.Enemies[i]:null;
                if(e==null){v.life=0;}else
                {
                    if(v.uid!=e.Uid){v.uid=e.Uid;v.serial=0;v.life=0;}
                    if(v.serial!=e.DamageSerial){v.serial=e.DamageSerial;v.life=.42f;v.at=e.Position;v.number.text=Mathf.CeilToInt(e.LastDamage).ToString();}
                }
                v.life=Mathf.Max(0,v.life-dt);bool show=Session.InBattle&&v.life>0;v.number.gameObject.SetActive(show);
                if(i<foeViews.Count)foeViews[i].damage.gameObject.SetActive(false);
                if(e!=null)foeViews[i].body.raycastTarget=e.Alive&&Session.IsFighting;
                float spread=(.42f-v.life)*120;Vector2 at=Project(v.at.x,v.at.y)+new Vector2(0,82);
                v.number.rectTransform.anchoredPosition=at+new Vector2(45,60+spread*.5f);v.number.color=new Color(color.r,color.g,color.b,Mathf.Clamp01(v.life/.15f));
                for(int j=0;j<v.rays.Length;j++){var ray=v.rays[j];ray.gameObject.SetActive(show&&v.life>.2f);float angle=j*60+17;ray.rectTransform.anchoredPosition=at+new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad))*(12+spread);ray.rectTransform.localRotation=Quaternion.Euler(0,0,angle);ray.rectTransform.sizeDelta=new Vector2(30+spread*.2f,3);ray.color=v.life>.36f?Color.white:color;}
                if(e!=null&&!e.Alive&&show){var body=foeViews[i];body.root.gameObject.SetActive(true);body.body.color=new Color(.65f,.86f,1,v.life/.42f);body.root.localScale=Vector3.one*(.8f+v.life*.45f);body.hp.transform.parent.gameObject.SetActive(false);body.label.gameObject.SetActive(false);}
                else if(e!=null)foeViews[i].root.localScale=Vector3.one;
            }
        }
    }
}
