using UnityEngine;

namespace SpiritTide
{
    public struct TideMotionInput
    {
        public Vector2 ground;
        public float time,height,jumpSpeed,phase,dodge,charge;
        public int facing,serial,hurt,epoch,combo;
        public MoveKind action;
        public bool attacking,charging;
        public static TideMotionInput Capture(TideSession s,float unit)
        {
            return new TideMotionInput{ground=new Vector2(s.X*unit,s.Y*35),time=s.PresentationClock,epoch=s.PresentationEpoch,facing=s.Facing,height=s.Z,jumpSpeed=s.JumpSpeed,
                phase=s.ActionDuration>0?Mathf.Clamp01(s.ActionElapsed/s.ActionDuration):0,action=s.Action,attacking=s.ActionRemaining>0,charging=s.IsCharging,charge=s.HeavyProgress,
                serial=s.AttackSerial,hurt=s.HurtSerial,combo=s.Combo,dodge=s.DodgeRemaining>0?Mathf.Clamp01(s.DodgeElapsed/.32f):0};
        }
    }

    // All channels are continuous. Feet are solved against world-space contacts;
    // torso/head and the two segments of every limb have independent transforms.
    public sealed class TideNaturalMotion
    {
        public readonly Vector2[] Positions=new Vector2[18];
        public readonly float[] Angles=new float[18];
        public readonly Vector2[] Feet=new Vector2[2];
        public readonly bool[] Contacts=new bool[2];
        public float Phase {get;private set;}
        public float Speed {get;private set;}
        public float FootError {get;private set;}
        public float ReachError {get;private set;}
        public float MovementWeight {get;private set;}
        public TideRigDef Rig {get;private set;}
        private readonly Vector2[] anchors=new Vector2[2],previousFeet=new Vector2[2];
        private Vector2 previousGround,velocity,direction=Vector2.right;
        private float previousTime,previousHeight,landing,hurtTime,turnTime,lean,hair,hairVelocity;
        private float transitionTime=1,supportDrop,bodyDrop,weaponAngle,weaponOffset;
        private readonly float[] footRoll=new float[2];
        private Vector2 nearOffset,farOffset,lastNear,lastFar;
        private int epoch,facing,serial,hurt;
        private bool initialized;
        private bool wasTravelling,wasAttacking,wasCharging,wasDodging;

        private static float Damp(float current,float target,float rate,float dt)=>Mathf.LerpUnclamped(current,target,1-Mathf.Exp(-rate*dt));
        private static Vector2 Damp(Vector2 current,Vector2 target,float rate,float dt)=>Vector2.LerpUnclamped(current,target,1-Mathf.Exp(-rate*dt));
        private static float Smooth(float value){value=Mathf.Clamp01(value);return value*value*value*(value*(value*6-15)+10);}
        private static float Angle(Vector2 value)=>Mathf.Atan2(value.y,value.x)*Mathf.Rad2Deg;
        private Vector2 Rest(int bone)=>Rig.bones[bone].Value;
        private Vector2 Point(int bone,Vector2 value)=>Positions[bone]+TideRigGraphic.Rotate(value-Rest(bone),Angles[bone]);
        public void Reset(TideRigDef rig,TideMotionInput input)
        {
            Rig=rig;previousGround=input.ground;previousTime=input.time;epoch=input.epoch;facing=input.facing;
            serial=input.serial;hurt=input.hurt;previousHeight=input.height;velocity=Vector2.zero;
            Phase=Speed=MovementWeight=landing=hurtTime=turnTime=lean=hair=hairVelocity=0;
            nearOffset=farOffset=Vector2.zero;transitionTime=1;supportDrop=0;bodyDrop=1;weaponAngle=weaponOffset=0;
            for(int i=0;i<18;i++){Positions[i]=Rest(i);Angles[i]=0;}
            for(int i=0;i<2;i++){Feet[i]=anchors[i]=input.ground+new Vector2(Rig.soles[i].x*facing,Rig.soles[i].y);Contacts[i]=true;previousFeet[i]=Feet[i];}
            wasTravelling=false;wasAttacking=input.attacking;wasCharging=input.charging;wasDodging=input.dodge>0;initialized=true;Evaluate(input,0);
        }
        public bool Tick(TideRigDef rig,TideMotionInput input)
        {
            if(!initialized||Rig!=rig||epoch!=input.epoch||(input.ground-previousGround).sqrMagnitude>48*48)
            {Reset(rig,input);return true;}
            float elapsed=input.time-previousTime;if(elapsed<=.000001f)return false;
            float dt=Mathf.Min(.05f,elapsed);Vector2 delta=input.ground-previousGround;
            Vector2 measured=delta/Mathf.Max(.00001f,elapsed);
            velocity=Damp(velocity,measured,18,dt);Speed=velocity.magnitude;
            if(delta.sqrMagnitude>.000001f)direction=delta.normalized;
            MovementWeight=Damp(MovementWeight,Mathf.Clamp01(measured.magnitude/72),12,dt);
            if(measured.magnitude<.5f&&MovementWeight<.01f)MovementWeight=0;
            float run=Mathf.SmoothStep(0,1,Mathf.InverseLerp(110,270,Speed));
            float stride=Mathf.Lerp(112,188,run)*Rig.stride*Mathf.Lerp(1,.24f,Mathf.Abs(direction.y));
            bool travelling=measured.magnitude>.5f;
            if(travelling&&!wasTravelling)
            {
                float support=Mathf.Lerp(.58f,.32f,run);
                Phase=Mathf.Clamp(support*.5f-Vector2.Dot(Feet[0]-previousGround,direction)/stride,.015f,support-.025f);
                for(int i=0;i<2;i++)anchors[i]=Feet[i];
            }
            wasTravelling=travelling;
            Phase=Mathf.Repeat(Phase+delta.magnitude/Mathf.Max(1,stride),1);
            if(facing!=input.facing){facing=input.facing;turnTime=.17f;for(int i=0;i<2;i++)previousFeet[i]=Feet[i];}
            if(previousHeight>.02f&&input.height<=.02f){landing=.19f;for(int i=0;i<2;i++){Contacts[i]=true;anchors[i]=input.ground+new Vector2(Rig.soles[i].x*facing,Rig.soles[i].y);}}
            if(input.hurt!=hurt){hurt=input.hurt;hurtTime=.24f;}
            if(input.serial!=serial||input.attacking!=wasAttacking||input.charging!=wasCharging||(input.dodge>0)!=wasDodging)
            {
                serial=input.serial;wasAttacking=input.attacking;wasCharging=input.charging;wasDodging=input.dodge>0;transitionTime=0;
                Targets(input,run,out Vector2 a,out Vector2 b,out _,out float blade,out _);
                nearOffset=lastNear-a;farOffset=lastFar-b;weaponOffset=Mathf.DeltaAngle(blade,weaponAngle);
            }
            transitionTime+=dt;landing=Mathf.Max(0,landing-dt);hurtTime=Mathf.Max(0,hurtTime-dt);turnTime=Mathf.Max(0,turnTime-dt);
            Evaluate(input,dt);previousGround=input.ground;previousTime=input.time;previousHeight=input.height;
            return true;
        }
        private static float Track(float phase,float a,float b,float c,float d,float e)
        {
            // Cubic Hermite tangents carry the swing through the hit marker at .4.
            float t0,t1,p0,p1,m0,m1;
            if(phase<.22f){t0=0;t1=.22f;p0=a;p1=b;m0=0;m1=(c-a)/.4f;}
            else if(phase<.4f){t0=.22f;t1=.4f;p0=b;p1=c;m0=(c-a)/.4f;m1=(d-b)/.5f;}
            else if(phase<.72f){t0=.4f;t1=.72f;p0=c;p1=d;m0=(d-b)/.5f;m1=(e-c)/.6f;}
            else {t0=.72f;t1=1;p0=d;p1=e;m0=(e-c)/.6f;m1=0;}
            float span=t1-t0,t=Mathf.Clamp01((phase-t0)/span),t2=t*t,t3=t2*t;
            return (2*t3-3*t2+1)*p0+(t3-2*t2+t)*span*m0+(-2*t3+3*t2)*p1+(t3-t2)*span*m1;
        }
        private static Vector2 Track(float p,Vector2 a,Vector2 b,Vector2 c,Vector2 d)=>new Vector2(Track(p,a.x,b.x,c.x,d.x,a.x),Track(p,a.y,b.y,c.y,d.y,a.y));
        private void Targets(TideMotionInput input,float run,out Vector2 near,out Vector2 far,out float body,out float weapon,out float kick)
        {
            int h=Rig.hero;float p=input.phase;
            float cycle=Mathf.Sin(Phase*Mathf.PI*2),swing=cycle*MovementWeight;
            Vector2 chest=Rest(1);
            near=chest+new Vector2(h==1?24:15,h==5?-43:-28);
            far=chest+new Vector2(h==1?43:30,h==1?-20:-32);
            if(h==2){near=chest+new Vector2(16,-7);far=chest+new Vector2(33,4);}
            if(h==3){near=chest+new Vector2(15,-11);far=chest+new Vector2(30,-4);}
            near+=new Vector2(swing*(h==2?11:7),-Mathf.Abs(swing)*3);
            far+=new Vector2(-swing*7,-Mathf.Abs(swing)*2);
            body=-run*MovementWeight*(h==2||h==3?13:9);weapon=h==3?0:55;kick=0;
            if(input.charging){float q=Mathf.Clamp01(input.charge);near+=new Vector2(-18*q,25*q);far+=new Vector2(-8*q,12*q);body+=7*q;weapon=Mathf.Lerp(weapon,170,q);}
            if(!input.attacking)return;
            bool skill=input.action>=MoveKind.Skill0;int slot=(int)input.action-(int)MoveKind.Skill0;
            bool heavy=input.action==MoveKind.Heavy||input.action==MoveKind.Counter;
            float wind=heavy?18:10,reach=heavy?61:52;
            Vector2 na=near,fa=far;
            near=Track(p,na,chest+new Vector2(-wind,25),chest+new Vector2(reach,-9),chest+new Vector2(33,-30));
            far=Track(p,fa,chest+new Vector2(9,4),chest+new Vector2(-16,-15),fa);
            body+=Track(p,0,7,heavy?-17:-10,-5,0);
            weapon=Track(p,weapon,heavy?188:162,95,38,weapon);
            if(h==1)
            {
                near=Track(p,na,chest+new Vector2(10,-9),chest+new Vector2(62,slot==2?-4:13),chest+new Vector2(37,7));
                far=Track(p,fa,chest+new Vector2(20,10),chest+new Vector2(48,slot==1?34:20),chest+new Vector2(35,5));
                body=Track(p,0,4,-5,-2,0);
            }
            if(h==2)
            {
                bool leg=!skill&&input.combo==2||skill&&(slot==0||slot==4);
                kick=leg?Mathf.Clamp01(Track(p,0,.2f,1,.4f,0)):0;
                near=Track(p,na,chest+new Vector2(-10,7),chest+new Vector2(55,5+input.combo*5),na);
                far=Track(p,fa,chest+new Vector2(16,7),chest+new Vector2(skill&&slot==1?36:30,skill&&slot==1?-53:0),fa);
                if(leg)body+=kick*14;
                if(skill&&slot==2)body-=Mathf.Sin(p*Mathf.PI)*12;
            }
            if(h==3)
            {
                near=Track(p,na,chest+new Vector2(-7,-5),chest+new Vector2(65,slot==2?6:-7),chest+new Vector2(32,-15));
                far=Track(p,fa,chest+new Vector2(10,-8),chest+new Vector2(22,-3),fa);weapon=0;
            }
            if(h==4){weapon=Track(p,55,175,92,40,55);body+=Track(p,0,8,-9,-6,0);}
            if(h==5){weapon=Track(p,55,155,88,44,55);body*=.65f;}
            if(skill)
            {
                bool heal=h==1&&(slot==1||slot==4)||h==5&&(slot==0||slot==3);
                bool ground=h==0&&slot==1||h==2&&slot==1;
                if(heal)
                {
                    near=Track(p,na,chest+new Vector2(13,4),chest+new Vector2(28,30),chest+new Vector2(18,20));
                    far=Track(p,fa,chest+new Vector2(27,4),chest+new Vector2(45,25),chest+new Vector2(35,15));body=0;weapon=55;
                }
                else if(ground)
                {
                    near=Track(p,na,chest+new Vector2(17,21),chest+new Vector2(26,-55),chest+new Vector2(24,-30));body+=Track(p,0,-3,-10,-6,0);
                }
                else if(slot==3||slot==4)
                {
                    float lift=slot==4?25:13;
                    far=Track(p,fa,chest+new Vector2(14,lift),chest+new Vector2(38,lift+16),chest+new Vector2(32,6));
                    if(h==0||h==4)near+=Vector2.up*Track(p,0,8,lift,8,0);
                }
            }
            if(!skill&&input.action==MoveKind.Basic&&h!=1&&h!=2&&h!=3)
            {
                if(input.combo==1)
                {
                    near=Track(p,na,chest+new Vector2(15,-36),chest+new Vector2(48,24),chest+new Vector2(18,20));
                    weapon=Track(p,55,25,145,168,55);
                }
                else if(input.combo==2)
                {
                    near=Track(p,na,chest+new Vector2(-10,-9),chest+new Vector2(64,0),chest+new Vector2(48,-2));
                    far=Track(p,fa,chest+new Vector2(10,-9),chest+new Vector2(32,-6),fa);weapon=Track(p,55,105,92,80,55);
                }
            }
            if(h==1&&skill)
            {
                if(slot==0||slot==3)
                {
                    near=Track(p,na,chest+new Vector2(8,0),chest+new Vector2(slot==0?43:24,21),chest+new Vector2(34,12));
                    far=Track(p,fa,chest+new Vector2(23,4),chest+new Vector2(slot==0?47:29,35),chest+new Vector2(35,19));
                }
            }
            if(h==5&&skill)
            {
                if(slot==2){near=Track(p,na,chest+new Vector2(5,-8),chest+new Vector2(22,4),chest+new Vector2(16,-8));weapon=Track(p,55,15,4,25,55);}
                if(slot==3){near=Track(p,na,chest+new Vector2(-8,-14),chest+new Vector2(18,-22),na);far=Track(p,fa,chest+new Vector2(11,-4),chest+new Vector2(36,-2),fa);weapon=Track(p,55,115,105,75,55);body=Track(p,0,3,-5,-3,0);}
            }
            if(h==4&&skill&&slot==1){far=Track(p,fa,chest+new Vector2(6,-2),chest+new Vector2(49,12),fa);near=Track(p,na,chest+new Vector2(7,-20),chest+new Vector2(18,-28),na);weapon=55;}
            if(input.action==MoveKind.Air){near=Track(p,na,chest+new Vector2(4,28),chest+new Vector2(54,-22),chest+new Vector2(28,-30));weapon=Track(p,55,185,80,30,55);body-=Mathf.Sin(p*Mathf.PI)*7;}
            if(input.action==MoveKind.Counter){near=Track(p,na,chest+new Vector2(-4,5),chest+new Vector2(63,5),na);body+=Track(p,0,11,-15,-4,0);}
            if(input.action==MoveKind.Launcher){weapon=Track(p,55,36,145,163,55);near=Track(p,na,chest+new Vector2(13,-37),chest+new Vector2(40,27),chest+new Vector2(21,24));}
        }
        private void Evaluate(TideMotionInput input,float dt)
        {
            FootError=ReachError=0;float run=Mathf.SmoothStep(0,1,Mathf.InverseLerp(110,270,Speed));
            float stride=Mathf.Lerp(112,188,run)*Rig.stride*Mathf.Lerp(1,.24f,Mathf.Abs(direction.y)),support=Mathf.Lerp(.58f,.32f,run);
            Targets(input,run,out Vector2 near,out Vector2 far,out float desiredLean,out float weapon,out float kick);
            if(hurtTime>0){float recoil=Mathf.Sin(Mathf.Clamp01(hurtTime/.24f)*Mathf.PI);desiredLean+=recoil*12;near+=new Vector2(-12,8)*recoil;far+=new Vector2(-9,5)*recoil;}
            if(input.dodge>0){float q=Mathf.Sin(input.dodge*Mathf.PI);desiredLean-=16*q;near+=new Vector2(-17,-5)*q;far+=new Vector2(-22,-7)*q;}
            bool air=input.height>.02f;
            float breathe=Mathf.Sin(input.time*2.2f)*.45f;
            float drop=1+MovementWeight*Mathf.Lerp(12,13,run)+Mathf.Sin(Phase*4*Mathf.PI)*MovementWeight*1.2f;
            if(landing>0)drop+=Mathf.Sin(landing/.19f*Mathf.PI)*9;
            if(turnTime>0)drop+=Mathf.Sin(turnTime/.17f*Mathf.PI)*6;
            if(input.charging)drop+=Mathf.Clamp01(input.charge)*4;
            if(air){desiredLean+=Mathf.Clamp(input.jumpSpeed,-7,7)*-.8f;drop=3;}
            lean=dt>0?Damp(lean,desiredLean,22,dt):desiredLean;
            float pelvisTilt=MovementWeight*Mathf.Sin(Phase*2*Mathf.PI)*2.5f+lean*.23f;
            bodyDrop=dt>0?Damp(bodyDrop,drop,28,dt):drop;
            Positions[0]=Rest(0)+new Vector2(0,breathe-bodyDrop);Angles[0]=pelvisTilt;
            for(int side=0;side<2;side++)
            {
                float c=Mathf.Repeat(Phase+side*.5f,1),off,lift=0,roll=0;
                bool contact=c<support&&MovementWeight>.05f;
                if(contact)off=(support*.5f-c)*stride;
                else
                {
                    float t=Mathf.Clamp01((c-support)/(1-support));off=(Smooth(t)-.5f)*support*stride;
                    lift=Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),1.3f)*Mathf.Lerp(15,29,run);roll=-Mathf.Sin(t*Mathf.PI)*13;
                }
                Vector2 restSole=Rig.soles[side].Value;
                Vector2 baseOffset=new Vector2(restSole.x*facing*Mathf.Lerp(1,.28f,MovementWeight),restSole.y);
                Vector2 wanted=input.ground+baseOffset+direction*off*MovementWeight+Vector2.up*lift*MovementWeight;
                if(contact)
                {
                    if(!Contacts[side])anchors[side]=wanted;
                    wanted=anchors[side];
                }
                if(MovementWeight<.05f){wanted=Damp(Feet[side],input.ground+new Vector2(restSole.x*facing,restSole.y),20,dt);contact=(wanted-(input.ground+new Vector2(restSole.x*facing,restSole.y))).sqrMagnitude<.5f;}
                if(air)
                {
                    float tuck=Mathf.Lerp(14,25,Mathf.InverseLerp(-2,5,input.jumpSpeed));
                    wanted=input.ground+new Vector2((side==0?21:-16)*facing,restSole.y+tuck+(side==0?5:0));contact=false;roll=side==0?-12:15;
                }
                if(side==0&&kick>.01f){wanted=Vector2.Lerp(wanted,input.ground+new Vector2(67*facing,95),kick);contact=false;roll=-18*kick;}
                if(turnTime>0){float t=1-turnTime/.17f;wanted=Vector2.Lerp(previousFeet[side],wanted,Smooth(t))+Vector2.up*Mathf.Sin(t*Mathf.PI)*5;contact=false;}
                Feet[side]=wanted;Contacts[side]=contact;
                Vector2 local=new Vector2((wanted.x-input.ground.x)*facing,wanted.y-input.ground.y);
                footRoll[side]=roll;
            }
            // Solve support height before the spine and arms. Lowering the pelvis
            // keeps the planted ankle reachable without stretching the legs.
            float requiredDrop=0;
            for(int side=0;side<2;side++)
            {
                if(air||!Contacts[side])continue;int b=10+side*3;
                Vector2 sole=new Vector2((Feet[side].x-input.ground.x)*facing,Feet[side].y-input.ground.y);
                Vector2 ankle=sole-TideRigGraphic.Rotate(Rig.soles[side].Value-Rest(b+2),footRoll[side]);
                Vector2 hip=Point(0,Rest(b));float length=Vector2.Distance(Rest(b),Rest(b+1))+Vector2.Distance(Rest(b+1),Rest(b+2))-.05f;
                float dx=hip.x-ankle.x,ceiling=ankle.y+Mathf.Sqrt(Mathf.Max(0,length*length-dx*dx));
                requiredDrop=Mathf.Max(requiredDrop,hip.y-ceiling);
            }
            supportDrop=Mathf.Max(requiredDrop,Damp(supportDrop,requiredDrop,16,dt));
            Positions[0]+=Vector2.down*supportDrop;
            Positions[1]=Point(0,Rest(1));Angles[1]=lean;
            Positions[2]=Point(1,Rest(2));Angles[2]=lean*.6f;
            Positions[3]=Positions[2];Angles[3]=lean*.2f;
            float decay=Mathf.Exp(-transitionTime/.06f);near+=nearOffset*decay;far+=farOffset*decay;
            near+=Positions[1]-Rest(1);far+=Positions[1]-Rest(1);lastNear=near;lastFar=far;
            Arm(4,near,-1);Arm(7,far,-1);
            float hairTarget=Mathf.Clamp(-velocity.x/55,-6,6)+Mathf.Sin(input.time*2.4f)*.65f;
            if(dt>0){hair=Mathf.SmoothDamp(hair,hairTarget,ref hairVelocity,.18f,80,dt);}
            Positions[16]=Positions[3];Angles[16]=Angles[3]+hair;
            for(int side=0;side<2;side++)
            {
                Vector2 restSole=Rig.soles[side].Value;
                Vector2 local=new Vector2((Feet[side].x-input.ground.x)*facing,Feet[side].y-input.ground.y);
                Leg(10+side*3,local,footRoll[side],side);
                if(Contacts[side])
                {
                    Vector2 achieved=Point(12+side*3,restSole);Vector2 world=input.ground+new Vector2(achieved.x*facing,achieved.y);
                    FootError=Mathf.Max(FootError,Vector2.Distance(world,Feet[side]));
                }
            }
            weaponAngle=weapon+weaponOffset*decay;
            Positions[17]=Point(6,Rest(6)+(Rig.weapon==1?new Vector2(-2.5f,-5):new Vector2(0,-4)));Angles[17]=Rig.weapon==1?Angles[6]-108:weaponAngle;
        }
        private void Arm(int bone,Vector2 target,float bend)
        {
            Vector2 shoulder=Point(1,Rest(bone));
            Solve(shoulder,(Rest(bone+1)-Rest(bone)).magnitude,(Rest(bone+2)-Rest(bone+1)).magnitude,target,bend,out Vector2 elbow,out Vector2 wrist);
            Positions[bone]=shoulder;Positions[bone+1]=elbow;Positions[bone+2]=wrist;
            Angles[bone]=Mathf.DeltaAngle(Angle(Rest(bone+1)-Rest(bone)),Angle(elbow-shoulder));
            Angles[bone+1]=Mathf.DeltaAngle(Angle(Rest(bone+2)-Rest(bone+1)),Angle(wrist-elbow));
            Angles[bone+2]=Angles[bone+1];
        }
        private void Leg(int bone,Vector2 sole,float roll,int side)
        {
            Vector2 hip=Point(0,Rest(bone));Vector2 ankle=sole-TideRigGraphic.Rotate(Rig.soles[side].Value-Rest(bone+2),roll);
            float upper=(Rest(bone+1)-Rest(bone)).magnitude,lower=(Rest(bone+2)-Rest(bone+1)).magnitude;
            ReachError=Mathf.Max(ReachError,Mathf.Max(0,Vector2.Distance(hip,ankle)-upper-lower));
            Solve(hip,upper,lower,ankle,1,out Vector2 knee,out Vector2 end);
            Positions[bone]=hip;Positions[bone+1]=knee;Positions[bone+2]=end;
            Angles[bone]=Mathf.DeltaAngle(Angle(Rest(bone+1)-Rest(bone)),Angle(knee-hip));
            Angles[bone+1]=Mathf.DeltaAngle(Angle(Rest(bone+2)-Rest(bone+1)),Angle(end-knee));Angles[bone+2]=roll;
        }
        public static void Solve(Vector2 root,float upper,float lower,Vector2 target,float bend,out Vector2 joint,out Vector2 end)
        {
            Vector2 delta=target-root;float distance=Mathf.Clamp(delta.magnitude,Mathf.Abs(upper-lower)+.001f,upper+lower-.001f);
            Vector2 along=delta.sqrMagnitude>.000001f?delta.normalized:Vector2.down;
            float x=(upper*upper-lower*lower+distance*distance)/(2*distance),h=Mathf.Sqrt(Mathf.Max(0,upper*upper-x*x));
            joint=root+along*x+new Vector2(-along.y,along.x)*h*Mathf.Sign(bend);end=root+along*distance;
        }
    }
}
