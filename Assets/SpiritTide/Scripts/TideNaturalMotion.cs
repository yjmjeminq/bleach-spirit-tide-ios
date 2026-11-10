using UnityEngine;

namespace SpiritTide
{
    public struct TideMotionInput
    {
        public Vector2 ground,fullPace;
        public float time,height,jumpSpeed,phase,dodge,charge;
        public int facing,serial,hurt,epoch,combo;
        public MoveKind action;
        public bool attacking,charging;
        public static TideMotionInput Capture(TideSession s,float unit)
        {
            return new TideMotionInput{ground=new Vector2(s.X*unit,s.Y*35),fullPace=new Vector2(unit,35*.65f)*s.MovementPace,time=s.PresentationClock,epoch=s.PresentationEpoch,facing=s.Facing,height=s.Z,jumpSpeed=s.JumpSpeed,
                phase=s.ActionPosePhase,action=s.Action,attacking=s.HasActionPose,charging=s.IsCharging,charge=s.HeavyProgress,
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
        public float ShoeProjection=>MovementWeight*Mathf.Abs(direction.x);
        public int DisplayFacing=>facing;
        public float HairFacing {get;private set;}
        public Vector2 ChestOffset=>chestMotion.LocalOffset(facing);
        public float BladeScale {get;private set;}=1;
        public Vector2 BladeDirection=>TideRigGraphic.Rotate(bladeAxis,Angles[17]);
        public Vector2 BladeTip=>Positions[17]+TideRigGraphic.Rotate(bladeTip,Angles[17])*BladeScale;
        public float BladeTipHeight=>BladeTip.y;
        public int StrikeLimb {get;private set;}=6;
        public TideRigDef Rig {get;private set;}
        private const float SettleDuration=.18f;
        private const float HairTurnDuration=.13f;
        public const float SwordHandLength=5.5f;
        private readonly TideChestMotion chestMotion=new TideChestMotion();
        private readonly Vector2[] anchors=new Vector2[2],takeoff=new Vector2[2],swingFrom=new Vector2[2],touchdown=new Vector2[2];
        private readonly Vector2[] strikeFrom=new Vector2[2],strikeTo=new Vector2[2];
        private readonly bool[] strikeContacts=new bool[2];
        private readonly float[] swingStart=new float[2],lastCycle=new float[2],stanceEnd=new float[2],legSpan=new float[2];
        private Vector2 previousGround,velocity,direction=Vector2.right,inputHeading=Vector2.right;
        private TideMotionInput previousInput;
        private Vector2 stepFrom,stepTo,bladeTip,bladeAxis;
        private float previousTime,landing,hurtTime,turnAge,turnHold,turnWeight,lean,hair,hairVelocity,flightTime,stepAge;
        private float hairFacingFrom,hairTurnAge;
        private float paceFraction,fullHorizontalPace;
        private float transitionTime=1,supportDrop,bodyDrop,bodyShift,spineLead,weaponAngle,weaponOffset,strikeStartPhase;
        private readonly float[] footRoll=new float[2];
        private Vector2 nearOffset,farOffset,lastNear,lastFar;
        private int epoch,facing,poseFacing,serial,hurt,settleFoot,strikeSerial=-1,strikeLead;
        private bool initialized;
        private bool wasTravelling,wasAttacking,wasCharging,wasDodging,wasAirborne,wasGroundedAttack,weaponTurning;

        private static float Damp(float current,float target,float rate,float dt)=>Mathf.LerpUnclamped(current,target,1-Mathf.Exp(-rate*dt));
        private static Vector2 Damp(Vector2 current,Vector2 target,float rate,float dt)=>Vector2.LerpUnclamped(current,target,1-Mathf.Exp(-rate*dt));
        private static float Smooth(float value){value=Mathf.Clamp01(value);return value*value*value*(value*(value*6-15)+10);}
        private static float Angle(Vector2 value)=>Mathf.Atan2(value.y,value.x)*Mathf.Rad2Deg;
        private Vector2 Rest(int bone)=>Rig.bones[bone].Value;
        private Vector2 Point(int bone,Vector2 value)=>Positions[bone]+TideRigGraphic.Rotate(value-Rest(bone),Angles[bone]);
        private float RunWeight=>Mathf.SmoothStep(0,1,Mathf.InverseLerp(.60f,.96f,paceFraction))*Mathf.Lerp(1,.15f,Mathf.Abs(direction.y));
        private float Stride(float run)
        {
            float walk=Mathf.Lerp(30,fullHorizontalPace*.63f,Mathf.InverseLerp(0,.80f,paceFraction));
            return Mathf.Lerp(walk,fullHorizontalPace*.74f,run)*Rig.stride*Mathf.Lerp(1,.64f,Mathf.Abs(direction.y));
        }
        private static float SupportFraction(float run)=>Mathf.Lerp(.64f,.46f,run);
        private Vector2 RestSole(int side)=>new Vector2(Rig.soles[side].x*facing,Rig.soles[side].y);

        public void Reset(TideRigDef rig,TideMotionInput input)
        {
            if(Rig!=rig)
            {
                bladeTip=Vector2.zero;
                if(rig.weapon==2)
                {
                    Vector2 grip=rig.bones[17].Value;
                    foreach(var vertex in rig.vertices)
                    {
                        if(vertex.a!=17||vertex.b!=17)continue;
                        Vector2 edge=vertex.p.Value-grip;
                        if(edge.y<bladeTip.y)bladeTip=edge;
                    }
                    bladeAxis=bladeTip.normalized;
                }
            }
            Rig=rig;previousInput=input;previousGround=input.ground;previousTime=input.time;epoch=input.epoch;poseFacing=facing=input.facing;
            serial=input.serial;hurt=input.hurt;velocity=Vector2.zero;direction=inputHeading=Vector2.right*facing;
            Phase=Speed=MovementWeight=landing=hurtTime=turnAge=turnHold=turnWeight=lean=hair=hairVelocity=flightTime=0;
            fullHorizontalPace=input.fullPace.x>0?input.fullPace.x:172.8f;paceFraction=0;
            nearOffset=farOffset=Vector2.zero;transitionTime=1;supportDrop=bodyShift=spineLead=0;bodyDrop=1;weaponAngle=weaponOffset=0;
            settleFoot=-1;strikeSerial=-1;stepAge=SettleDuration;
            HairFacing=hairFacingFrom=facing;hairTurnAge=HairTurnDuration;BladeScale=1;
            for(int i=0;i<18;i++){Positions[i]=Rest(i);Angles[i]=0;}
            for(int i=0;i<2;i++)
            {
                takeoff[i]=RestSole(i);Feet[i]=anchors[i]=input.ground+takeoff[i];Contacts[i]=true;
                swingFrom[i]=touchdown[i]=Feet[i];swingStart[i]=lastCycle[i]=0;stanceEnd[i]=SupportFraction(0);
                int hip=10+i*3;
                legSpan[i]=Vector2.Distance(Rest(hip),Rest(hip+1))+Vector2.Distance(Rest(hip+1),Rest(hip+2));
            }
            wasTravelling=false;wasAttacking=input.attacking;wasCharging=input.charging;wasDodging=input.dodge>0;
            wasAirborne=input.height>.02f;wasGroundedAttack=false;initialized=true;weaponTurning=false;
            Evaluate(input,0,false,false);
        }
        public bool Tick(TideRigDef rig,TideMotionInput input)
        {
            if(!initialized||Rig!=rig||epoch!=input.epoch)
            {Reset(rig,input);return true;}
            float elapsed=input.time-previousTime;if(elapsed<=.000001f)return false;
            Vector2 delta=input.ground-previousGround;
            if(delta.sqrMagnitude>48*48)
            {
                if(!input.attacking||input.serial!=serial){Reset(rig,input);return true;}
                // A lunge's combat position changes on the hit. Carry the pose and
                // its contacts along so the cast keeps its follow-through.
                for(int i=0;i<2;i++){Feet[i]+=delta;anchors[i]+=delta;strikeFrom[i]+=delta;strikeTo[i]+=delta;swingFrom[i]+=delta;touchdown[i]+=delta;}
                stepFrom+=delta;stepTo+=delta;previousGround=input.ground;
                previousInput.ground=input.ground;
                chestMotion.Rebase(delta);
            }
            // Bound the solver step so touchdown and lift-off use the same
            // ground sample at 30, 60 and 120 Hz.
            int steps=Mathf.Max(1,Mathf.CeilToInt(Mathf.Min(elapsed,.05f)*120-.001f));
            for(int step=1;step<=steps;step++)
            {
                if(step==steps){AdvancePose(input);continue;}
                float t=step/(float)steps;
                var sample=input;
                sample.ground=Vector2.LerpUnclamped(previousInput.ground,input.ground,t);
                sample.time=Mathf.LerpUnclamped(previousInput.time,input.time,t);
                sample.height=Mathf.LerpUnclamped(previousInput.height,input.height,t);
                sample.jumpSpeed=Mathf.LerpUnclamped(previousInput.jumpSpeed,input.jumpSpeed,t);
                sample.phase=Mathf.LerpUnclamped(previousInput.serial==input.serial?previousInput.phase:0,input.phase,t);
                sample.charge=Mathf.LerpUnclamped(previousInput.charge,input.charge,t);
                sample.dodge=Mathf.LerpUnclamped(previousInput.dodge,input.dodge>0?input.dodge:previousInput.dodge>0?1:0,t);
                AdvancePose(sample);
            }
            previousInput=input;
            return true;
        }

        private void AdvancePose(TideMotionInput input)
        {
            float elapsed=input.time-previousTime;
            float dt=Mathf.Min(.05f,elapsed);
            Vector2 delta=input.ground-previousGround;
            Vector2 measured=delta/Mathf.Max(.00001f,elapsed);
            velocity=Damp(velocity,measured,18,dt);Speed=velocity.magnitude;
            if(measured.sqrMagnitude>.25f)
            {
                Vector2 heading=measured.normalized;
                inputHeading=heading;
                direction=Damp(direction,heading,18,dt);
            }
            bool air=input.height>.02f,dodging=input.dodge>0;
            bool groundedAttack=input.attacking&&!air&&!dodging;
            bool travelling=measured.magnitude>.5f&&!air&&!dodging&&(!groundedAttack||input.phase>.72f);
            if(input.fullPace.x>0)fullHorizontalPace=input.fullPace.x;
            float depthPace=input.fullPace.y>0?input.fullPace.y:81.9f;
            float effort=travelling?new Vector2(measured.x/fullHorizontalPace,measured.y/depthPace).magnitude:0;
            paceFraction=Damp(paceFraction,Mathf.Clamp01(effort),18,dt);
            MovementWeight=Damp(MovementWeight,Mathf.Clamp01(effort/.57f),13,dt);
            if(!travelling&&MovementWeight<.01f)MovementWeight=0;
            bool turned=UpdateFacing(input,dt);
            if(turned&&groundedAttack)strikeSerial=-1;
            if(turned){hairFacingFrom=HairFacing;hairTurnAge=0;}
            hairTurnAge=Mathf.Min(HairTurnDuration,hairTurnAge+dt);
            HairFacing=Mathf.Lerp(hairFacingFrom,facing,Smooth(hairTurnAge/HairTurnDuration));
            if(air&&!wasAirborne||dodging&&!wasDodging)
            {
                for(int i=0;i<2;i++)takeoff[i]=Feet[i]-previousGround;
                flightTime=0;
            }
            flightTime+=dt;
            if(wasAirborne&&!air&&!dodging)
            {
                landing=.19f;
                for(int i=0;i<2;i++){Feet[i]=anchors[i]=input.ground+RestSole(i);Contacts[i]=true;}
            }
            if(!travelling&&!air&&!dodging&&!groundedAttack&&(wasTravelling||wasDodging||wasAirborne||wasGroundedAttack||turned))
                BeginSettle(input.ground);
            if(travelling&&!wasTravelling)direction=inputHeading;
            float run=RunWeight,stride=Stride(run);
            if(travelling&&!wasTravelling)
            {
                float support=SupportFraction(run);
                int plant=Contacts[0]||!Contacts[1]?0:1;
                float start=Mathf.Clamp(support*.5f-Vector2.Dot(Feet[plant]-previousGround,direction)/stride,.015f,support-.025f);
                Phase=Mathf.Repeat(start-plant*.5f,1);
                for(int i=0;i<2;i++)anchors[i]=Feet[i];
                settleFoot=-1;
            }
            if(travelling)Phase=Mathf.Repeat(Phase+delta.magnitude/stride,1);
            if(input.hurt!=hurt){hurt=input.hurt;hurtTime=.24f;}
            bool blend=input.serial!=serial||input.attacking!=wasAttacking||input.charging!=wasCharging||dodging!=wasDodging;
            serial=input.serial;wasAttacking=input.attacking;wasCharging=input.charging;wasDodging=dodging;wasAirborne=air;
            transitionTime=blend?0:transitionTime+dt;
            landing=Mathf.Max(0,landing-dt);hurtTime=Mathf.Max(0,hurtTime-dt);stepAge=Mathf.Min(SettleDuration,stepAge+dt);
            Evaluate(input,dt,travelling,blend);
            wasGroundedAttack=groundedAttack;wasTravelling=travelling;previousGround=input.ground;previousTime=input.time;
        }

        private bool UpdateFacing(TideMotionInput input,float dt)
        {
            if(input.facing==facing)
            {
                turnAge=0;
                turnHold=Mathf.Max(0,turnHold-dt);
                if(turnHold==0)turnWeight=Damp(turnWeight,0,input.attacking?32:22,dt);
                return false;
            }
            turnAge+=dt;
            float commit=input.attacking?.037f:.058f;
            turnWeight=Mathf.Max(turnWeight,Smooth(turnAge/commit));
            if(turnAge<commit)return false;
            facing=input.facing;
            bodyShift=-bodyShift;
            spineLead=-spineLead;
            turnHold=input.attacking?.016f:.032f;
            // Mirroring exchanges the screen-side legs. Their world contacts stay put.
            (Feet[0],Feet[1])=(Feet[1],Feet[0]);
            (Contacts[0],Contacts[1])=(Contacts[1],Contacts[0]);
            (anchors[0],anchors[1])=(anchors[1],anchors[0]);
            (takeoff[0],takeoff[1])=(takeoff[1],takeoff[0]);
            (swingFrom[0],swingFrom[1])=(swingFrom[1],swingFrom[0]);
            (touchdown[0],touchdown[1])=(touchdown[1],touchdown[0]);
            (swingStart[0],swingStart[1])=(swingStart[1],swingStart[0]);
            (lastCycle[0],lastCycle[1])=(lastCycle[1],lastCycle[0]);
            (stanceEnd[0],stanceEnd[1])=(stanceEnd[1],stanceEnd[0]);
            if(Rig.weapon==2)weaponAngle=-weaponAngle;
            Phase=Mathf.Repeat(Phase+.5f,1);
            if(settleFoot>=0)settleFoot=1-settleFoot;
            return true;
        }

        private void BeginSettle(Vector2 ground)
        {
            int plant=Contacts[0]!=Contacts[1]?Contacts[0]?0:1
                : (Feet[0]-ground-RestSole(0)).sqrMagnitude<=(Feet[1]-ground-RestSole(1)).sqrMagnitude?0:1;
            if(!Contacts[plant])anchors[plant]=ground+RestSole(plant);
            settleFoot=1-plant;
            stepFrom=Feet[settleFoot];
            stepTo=ground+RestSole(settleFoot);
            stepAge=0;
        }
        private static float Track(float phase,float a,float b,float c,float d,float e)
        {
            const float anticipation=.20f,contact=TideSession.ActionContactPhase,recovery=.71f;
            float ab=(b-a)/anticipation,bc=(c-b)/(contact-anticipation),cd=(d-c)/(recovery-contact),de=(e-d)/(1-recovery);
            float atB=Tangent(ab,bc),atC=Tangent(bc,cd),atD=Tangent(cd,de);
            float t0,t1,p0,p1,m0,m1;
            if(phase<anticipation){t0=0;t1=anticipation;p0=a;p1=b;m0=0;m1=atB;}
            else if(phase<contact){t0=anticipation;t1=contact;p0=b;p1=c;m0=atB;m1=atC;}
            else if(phase<recovery){t0=contact;t1=recovery;p0=c;p1=d;m0=atC;m1=atD;}
            else {t0=recovery;t1=1;p0=d;p1=e;m0=atD;m1=0;}
            float span=t1-t0,t=Mathf.Clamp01((phase-t0)/span),t2=t*t,t3=t2*t;
            return (2*t3-3*t2+1)*p0+(t3-2*t2+t)*span*m0+(-2*t3+3*t2)*p1+(t3-t2)*span*m1;
        }
        private static float Tangent(float before,float after)=>before*after>0?2*before*after/(before+after):0;
        private static Vector2 Track(float p,Vector2 a,Vector2 b,Vector2 c,Vector2 d)=>new Vector2(Track(p,a.x,b.x,c.x,d.x,a.x),Track(p,a.y,b.y,c.y,d.y,a.y));
        private bool Kicks(TideMotionInput input)
        {
            if(Rig.hero!=2)return false;
            return input.action==MoveKind.Basic&&input.combo==2||input.action==MoveKind.Heavy||input.action==MoveKind.Launcher||input.action==MoveKind.Air
                ||input.action==MoveKind.Skill0||input.action==MoveKind.Ultimate||input.action==MoveKind.Skill5;
        }

        private void SwordStrike(TideMotionInput input,Vector2 ready,Vector2 balance,float guard,
            out Vector2 near,out Vector2 far,out float lean,out float weapon,out float stance)
        {
            float p=input.phase;
            Vector2 chest=Rest(1);
            float swing=Rig.hero==4?7:Rig.hero==5?-5:0;
            bool rising=input.action==MoveKind.Launcher||input.action==MoveKind.Basic&&input.combo==1;
            bool thrust=input.action==MoveKind.Counter||input.action==MoveKind.Basic&&input.combo==2;
            bool heavy=input.action==MoveKind.Heavy;
            bool aerial=input.action==MoveKind.Air;
            Vector2 coil=new Vector2(-26,25+swing),hit=new Vector2(38+swing,5),through=new Vector2(26,-29);
            float wind=172+swing,contact=101,finish=49,drive=-13;
            stance=.91f;
            if(rising)
            {
                coil=new Vector2(-21,-49);hit=new Vector2(35,11+swing);through=new Vector2(14,36+swing);
                wind=25;contact=118;finish=164;drive=-9;stance=.77f;
            }
            else if(thrust)
            {
                coil=new Vector2(-16,-20);hit=new Vector2(50+swing*.4f,-7);through=new Vector2(44,-10);
                wind=103;contact=92;finish=85;drive=-16;stance=1.04f;
            }
            else if(heavy||aerial)
            {
                coil=new Vector2(-26,40);hit=new Vector2(40+swing,3);through=new Vector2(24,-38);
                wind=208;contact=105;finish=56;drive=-18;stance=1.12f;
            }
            near=Track(p,ready,chest+coil,chest+hit,chest+through);
            far=Track(p,balance,chest+new Vector2(20,-15),chest+new Vector2(thrust?14:20,-36),chest+new Vector2(16,-39));
            weapon=Track(p,guard,wind,contact,finish,guard);
            lean=Track(p,0,heavy?13:8,drive,rising?-3:-7,0);
        }

        private void Targets(TideMotionInput input,float run,out Vector2 near,out Vector2 far,out float body,out float weapon,out float kick,out float stance)
        {
            int h=Rig.hero;float p=input.phase;
            float swing=-Mathf.Cos(Phase*Mathf.PI*2)*MovementWeight*Mathf.Lerp(1,.55f,Mathf.Abs(direction.y));
            Vector2 chest=Rest(1);
            near=chest+new Vector2(h==1?24:15,h==5?-43:-28);
            far=chest+new Vector2(h==1?43:30,h==1?-20:-32);
            if(h==2){near=chest+new Vector2(16,-7);far=chest+new Vector2(33,4);}
            if(h==3){near=chest+new Vector2(15,-11);far=chest+new Vector2(30,-4);}
            float armDrive=Mathf.Lerp(h==5?10:13,h==2?33:h==3?28:24,run);
            Vector2 runNear=new Vector2(6+swing*armDrive,-45+Mathf.Max(0,swing)*Mathf.Lerp(6,20,run));
            Vector2 runFar=new Vector2(10-swing*armDrive,-44+Mathf.Max(0,-swing)*Mathf.Lerp(6,18,run));
            float guard=Rig.weapon==2?(h==4?64:h==5?56:59):55;
            if(Rig.weapon==2)
            {
                near=chest+new Vector2(h==4?10:6,h==5?-43:-39);
                far=chest+new Vector2(19,-42);
                runNear=new Vector2(h==4?17:13,-43)+new Vector2(swing*5,swing*4);
                runFar=new Vector2(12-swing*(armDrive+6),-43+Mathf.Max(0,-swing)*Mathf.Lerp(7,18,run));
            }
            near=Vector2.Lerp(near,chest+runNear,MovementWeight);
            far=Vector2.Lerp(far,chest+runFar,MovementWeight);
            body=-MovementWeight*Mathf.Lerp(3,h==2||h==3?24:21,run)*Mathf.Lerp(.42f,1,Mathf.Abs(direction.x));
            body+=swing*Mathf.Lerp(1.8f,2.8f,run);
            weapon=h==3?0:guard+MovementWeight*Mathf.Lerp(7,18,run)+swing*2.5f;
            kick=stance=0;StrikeLimb=6;
            if(input.charging)
            {
                float q=Mathf.Clamp01(input.charge);
                near=Vector2.Lerp(near,chest+new Vector2(-26,36),q);far+=new Vector2(-8*q,12*q);
                body+=10*q;weapon=Mathf.Lerp(weapon,198,q);
            }
            if(!input.attacking)return;
            bool skill=input.action>=MoveKind.Skill0;int slot=(int)input.action-(int)MoveKind.Skill0;
            if(Rig.weapon==2&&!skill)
            {
                SwordStrike(input,near,far,weapon,out near,out far,out float strikeLean,out weapon,out stance);
                body+=strikeLean;
                return;
            }
            bool heavy=input.action==MoveKind.Heavy||input.action==MoveKind.Counter;
            stance=h==1?.48f:heavy?1.16f:skill?.72f:1;
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
                bool leg=Kicks(input);
                kick=leg?Mathf.Clamp01(Track(p,0,.2f,1,.4f,0)):0;
                near=Track(p,na,chest+new Vector2(-10,7),chest+new Vector2(55,5+input.combo*5),na);
                far=Track(p,fa,chest+new Vector2(16,7),chest+new Vector2(skill&&slot==1?36:30,skill&&slot==1?-53:0),fa);
                if(leg){body+=kick*18;StrikeLimb=12;}
                if(skill&&(slot==2||slot==7))body-=Mathf.Sin(p*Mathf.PI)*12;
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
                bool heal=h==1&&(slot==1||slot==4||slot==5||slot==7||slot==8)||h==5&&(slot==0||slot==3||slot==5);
                bool ground=h==0&&slot==1||h==2&&(slot==1||slot==8);
                if(heal)
                {
                    near=Track(p,na,chest+new Vector2(13,4),chest+new Vector2(28,30),chest+new Vector2(18,20));
                    far=Track(p,fa,chest+new Vector2(27,4),chest+new Vector2(45,25),chest+new Vector2(35,15));body=0;weapon=55;stance=.16f;
                }
                else if(ground)
                {
                    near=Track(p,na,chest+new Vector2(17,21),chest+new Vector2(26,-55),chest+new Vector2(24,-30));body+=Track(p,0,-3,-10,-6,0);stance=.52f;
                }
                else if(slot==3||slot==4||slot==7||slot==8)
                {
                    float lift=slot==4||slot==8?25:13;
                    far=Track(p,fa,chest+new Vector2(14,lift),chest+new Vector2(38,lift+16),chest+new Vector2(32,6));
                    if(h==0||h==4)near+=Vector2.up*Track(p,0,8,lift,8,0);
                }
            }
            if(input.action==MoveKind.Basic&&h==1)
            {
                if(input.combo==1)
                {
                    near=Track(p,na,chest+new Vector2(17,-5),chest+new Vector2(23,14),na);
                    far=Track(p,fa,chest+new Vector2(13,18),chest+new Vector2(62,28),chest+new Vector2(35,11));
                    StrikeLimb=9;
                }
                else if(input.combo==2)
                {
                    near=Track(p,na,chest+new Vector2(3,12),chest+new Vector2(57,16),chest+new Vector2(36,5));
                    far=Track(p,fa,chest+new Vector2(10,-14),chest+new Vector2(54,6),chest+new Vector2(34,-4));
                    body=Track(p,0,7,-12,-4,0);stance=.65f;
                }
            }
            if(input.action==MoveKind.Basic&&h==2)
            {
                if(input.combo==1)
                {
                    near=Track(p,na,chest+new Vector2(17,9),chest+new Vector2(25,13),na);
                    far=Track(p,fa,chest+new Vector2(-7,5),chest+new Vector2(61,6),chest+new Vector2(43,-13));
                    StrikeLimb=9;
                }
                else if(input.combo==2)
                {
                    near=Track(p,na,chest+new Vector2(3,15),chest+new Vector2(15,11),chest+new Vector2(10,-6));
                    far=Track(p,fa,chest+new Vector2(18,10),chest+new Vector2(-21,-23),fa);
                }
            }
            if(input.action==MoveKind.Basic&&h==3&&input.combo!=0)
            {
                bool finish=input.combo==2;
                near=Track(p,na,chest+new Vector2(finish?-16:5,finish?-22:13),chest+new Vector2(finish?70:59,finish?1:19),chest+new Vector2(34,-12));
                body+=Track(p,0,finish?10:4,finish?-13:-7,-3,0);
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
                if(slot==2||slot==7){near=Track(p,na,chest+new Vector2(5,-8),chest+new Vector2(22,4),chest+new Vector2(16,-8));weapon=Track(p,55,15,4,25,55);}
                if(slot==3){near=Track(p,na,chest+new Vector2(-8,-14),chest+new Vector2(18,-22),na);far=Track(p,fa,chest+new Vector2(11,-4),chest+new Vector2(36,-2),fa);weapon=Track(p,55,115,105,75,55);body=Track(p,0,3,-5,-3,0);}
            }
            if(h==4&&skill&&(slot==1||slot==5)){far=Track(p,fa,chest+new Vector2(6,-2),chest+new Vector2(49,12),fa);near=Track(p,na,chest+new Vector2(7,-20),chest+new Vector2(18,-28),na);weapon=55;}
            if(input.action==MoveKind.Air)
            {
                near=Track(p,na,chest+new Vector2(4,28),chest+new Vector2(h==2?24:54,h==2?12:-22),chest+new Vector2(28,-30));
                if(h==1)far=Track(p,fa,chest+new Vector2(15,18),chest+new Vector2(46,-14),fa);
                weapon=Track(p,55,185,80,30,55);body-=Mathf.Sin(p*Mathf.PI)*7;
            }
            if(input.action==MoveKind.Counter){near=Track(p,na,chest+new Vector2(-4,5),chest+new Vector2(63,5),na);body+=Track(p,0,11,-15,-4,0);}
            if(input.action==MoveKind.Launcher)
            {
                weapon=Track(p,55,36,145,163,55);
                near=Track(p,na,chest+new Vector2(13,-37),chest+new Vector2(h==2?22:40,27),chest+new Vector2(21,24));
                if(h==1)far=Track(p,fa,chest+new Vector2(19,-30),chest+new Vector2(50,36),fa);
                stance=.86f;
            }
        }
        private void BeginStrikeStance(TideMotionInput input,float stance)
        {
            strikeSerial=input.serial;
            strikeStartPhase=Mathf.Clamp(input.phase,.045f,.28f);
            bool kick=Kicks(input);
            strikeLead=kick||Contacts[1]&&!Contacts[0]?0:1;
            for(int side=0;side<2;side++)
            {
                Vector2 rest=RestSole(side);
                strikeFrom[side]=Feet[side];
                strikeContacts[side]=Contacts[side];
                Vector2 at=Contacts[side]?Feet[side]:new Vector2(Feet[side].x,input.ground.y+rest.y);
                if(side==strikeLead&&stance>.2f&&!kick)
                {
                    float front=rest.x*facing+(side==0?38:20)*stance;
                    float advance=Mathf.Clamp((input.ground.x-at.x)*facing+front,0,34*stance);
                    at.x+=advance*facing;
                    at.y=input.ground.y+rest.y;
                }
                strikeTo[side]=at;
            }
        }
        private void PlaceFeet(TideMotionInput input,float dt,bool travelling,float run,float airWeight,float kick,float stance)
        {
            bool air=input.height>.02f,dodging=input.dodge>0;
            bool striking=input.attacking&&!air&&!dodging&&!travelling;
            if(striking&&(strikeSerial!=input.serial||!wasGroundedAttack||wasTravelling))BeginStrikeStance(input,stance);
            float stride=Stride(run);
            for(int side=0;side<2;side++)
            {
                Vector2 rest=RestSole(side),wanted;
                bool contact;
                float roll=0;
                if(air||dodging)
                {
                    float tuck=Mathf.Lerp(16,25,Mathf.InverseLerp(-2,5,input.jumpSpeed));
                    var tucked=new Vector2((side==0?20:-16)*facing,rest.y+tuck+(side==0?5:0));
                    Vector2 pose=Vector2.Lerp(rest,tucked,airWeight);
                    if(dodging)
                    {
                        float burst=Mathf.Sin(input.dodge*Mathf.PI);
                        pose+=direction*((side==0?19:-24)*burst)+Vector2.up*((side==0?7:15)*burst);
                    }
                    wanted=input.ground+Vector2.Lerp(takeoff[side],pose,Smooth(flightTime/.10f));
                    roll=(side==0?-12:15)*airWeight;
                    contact=false;
                }
                else if(travelling)
                {
                    float cycle=Mathf.Repeat(Phase+side*.5f,1);
                    bool newStep=!wasTravelling||cycle+1e-5f<lastCycle[side];
                    if(newStep)stanceEnd[side]=SupportFraction(run);
                    lastCycle[side]=cycle;
                    float hipX=input.ground.x+Point(0,Rest(10+side*3)).x*facing;
                    if(Contacts[side]&&cycle<stanceEnd[side])
                    {
                        // When accelerating, release a trailing heel before it
                        // pulls the hips down to the limit of the planted leg.
                        float trailing=(hipX-anchors[side].x)*direction.x;
                        if(trailing>legSpan[side]*.37f&&(Contacts[1-side]||run>.2f)
                            ||Mathf.Abs(anchors[side].x-hipX)>legSpan[side]*.67f)
                            stanceEnd[side]=cycle;
                    }
                    float support=stanceEnd[side];
                    contact=cycle<support;
                    if(contact)
                    {
                        if(!Contacts[side])anchors[side]=Vector2.MoveTowards(Feet[side],touchdown[side],360*dt);
                        wanted=anchors[side];
                        roll=Mathf.Lerp(11,0,Smooth(cycle/.14f))-21*Smooth(Mathf.InverseLerp(support*.63f,support,cycle));
                    }
                    else
                    {
                        float swing=Mathf.Clamp01((cycle-support)/(1-support));
                        float lead=Mathf.Min(stride*support*.45f,legSpan[side]*.30f)*MovementWeight;
                        rest.x*=Mathf.Lerp(1,.43f,MovementWeight);
                        Vector2 landing=input.ground+rest+direction*(lead+stride*(1-cycle));
                        if(Contacts[side]||!wasTravelling)
                        {
                            swingFrom[side]=Feet[side];touchdown[side]=landing;
                            swingStart[side]=swing;
                        }
                        else touchdown[side]=Vector2.MoveTowards(touchdown[side],landing,190*dt);
                        float progress=Mathf.InverseLerp(swingStart[side],1,swing);
                        float arc=Mathf.Sin(Mathf.Pow(progress,.77f)*Mathf.PI);
                        float lift=Mathf.Lerp(8,21,run)*MovementWeight*Mathf.Lerp(1,.61f,Mathf.Abs(direction.y));
                        wanted=Vector2.Lerp(swingFrom[side],touchdown[side],Smooth(progress))+Vector2.up*(arc*lift);
                        roll=Mathf.Lerp(-21,11,Smooth(progress))-7*arc;
                    }
                    roll*=Mathf.Lerp(.40f,1,MovementWeight)*Mathf.Lerp(1,.51f,Mathf.Abs(direction.y));
                }
                else if(striking)
                {
                    float progress=Smooth(Mathf.InverseLerp(strikeStartPhase,TideSession.ActionContactPhase-.025f,input.phase));
                    Vector2 span=strikeTo[side]-strikeFrom[side];
                    bool stepping=span.sqrMagnitude>.04f;
                    wanted=Vector2.Lerp(strikeFrom[side],strikeTo[side],progress);
                    if(stepping&&side==strikeLead)wanted.y+=Mathf.Sin(progress*Mathf.PI)*Mathf.Min(8,span.magnitude*.23f);
                    contact=progress>=1||strikeContacts[side]&&(!stepping||progress<=0);
                    if(contact)anchors[side]=wanted;
                }
                else
                {
                    bool step=side==settleFoot&&stepAge<SettleDuration&&(stepTo-stepFrom).sqrMagnitude>1;
                    if(side==settleFoot&&!step)anchors[side]=stepTo;
                    float phase=stepAge/SettleDuration;
                    wanted=step?Vector2.Lerp(stepFrom,stepTo,Smooth(phase))+Vector2.up*(Mathf.Sin(phase*Mathf.PI)*7):anchors[side];
                    contact=!step;
                }
                if(side==0&&kick>.01f)
                {
                    wanted=Vector2.Lerp(wanted,input.ground+new Vector2(66*facing,input.action==MoveKind.Launcher?115:88),kick);
                    contact=false;roll=-18*kick;
                }
                Feet[side]=wanted;Contacts[side]=contact;footRoll[side]=roll;
            }
        }

        private void Evaluate(TideMotionInput input,float dt,bool travelling,bool blendAction)
        {
            FootError=ReachError=0;float run=RunWeight;
            Targets(input,run,out Vector2 near,out Vector2 far,out float desiredLean,out float weapon,out float kick,out float stance);
            if(hurtTime>0){float recoil=Mathf.Sin(Mathf.Clamp01(hurtTime/.24f)*Mathf.PI);desiredLean+=recoil*12;near+=new Vector2(-12,8)*recoil;far+=new Vector2(-9,5)*recoil;}
            float dodge=Mathf.Sin(input.dodge*Mathf.PI);
            desiredLean-=16*dodge;near+=new Vector2(-17,-5)*dodge;far+=new Vector2(-22,-7)*dodge;
            bool air=input.height>.02f;
            float airWeight=Smooth(Mathf.InverseLerp(.02f,.92f,input.height));
            desiredLean-=Mathf.Clamp(input.jumpSpeed,-7,7)*.8f*airWeight;
            near+=new Vector2(-4,10)*airWeight;far+=new Vector2(-8,8)*airWeight;
            if(turnWeight>.002f)
            {
                Vector2 chest=Rest(1);
                float guardY=input.attacking?-25:-37;
                near=Vector2.Lerp(near,chest+new Vector2(3,guardY),turnWeight);
                far=Vector2.Lerp(far,chest+new Vector2(9,-36),turnWeight);
                desiredLean=Mathf.Lerp(desiredLean,3,turnWeight);
                if(Rig.weapon==2)weapon=Mathf.LerpAngle(weapon,0,turnWeight);
            }
            if(blendAction)
            {
                nearOffset=lastNear-near;farOffset=lastFar-far;weaponOffset=Mathf.DeltaAngle(weapon,weaponAngle);
            }
            float breathe=Mathf.Sin(input.time*2.2f)*.45f;
            float load=Mathf.Cos((Phase-.09f)*4*Mathf.PI);
            float drop=1+MovementWeight*(Mathf.Lerp(.35f,3.4f,run)+load*Mathf.Lerp(1.65f,3.1f,run));
            if(landing>0)drop+=Mathf.Sin(landing/.19f*Mathf.PI)*9;
            drop+=4*turnWeight+13*dodge;
            if(input.charging)drop+=Mathf.Clamp01(input.charge)*4;
            float shift=0;
            if(input.attacking&&!air)
            {
                shift=Track(input.phase,0,-6,16,10,0)*stance-kick*13;
                drop+=Track(input.phase,0,5,8,5,0)*stance;
            }
            drop=Mathf.Lerp(drop,3,airWeight);
            lean=dt>0?Damp(lean,desiredLean,input.attacking?34:22,dt):desiredLean;
            float pelvisTilt=MovementWeight*Mathf.Sin(Phase*2*Mathf.PI)*2.5f+lean*.23f;
            bodyDrop=dt>0?Damp(bodyDrop,drop,28,dt):drop;
            Positions[0]=Rest(0)+new Vector2(0,breathe-bodyDrop);Angles[0]=pelvisTilt;
            PlaceFeet(input,dt,travelling,run,airWeight,kick,stance);
            if(!air&&input.dodge==0)
            {
                float center=((Feet[0].x+Feet[1].x)*.5f-input.ground.x)*facing;
                shift+=Mathf.Clamp(center,-15,15)*(1-MovementWeight*.65f)*.45f;
                if(travelling)shift+=MovementWeight*(2+Mathf.Sin(Phase*4*Mathf.PI)*2.4f)*direction.x*facing;
            }
            bodyShift=dt>0?Damp(bodyShift,shift,input.attacking?42:17,dt):shift;
            Positions[0].x+=bodyShift;
            // Solve support height before the spine and arms. Lowering the pelvis
            // keeps the planted ankle reachable without stretching the legs.
            float requiredDrop=0;
            for(int side=0;side<2;side++)
            {
                if(air||!Contacts[side])continue;int b=10+side*3;
                Vector2 sole=new Vector2((Feet[side].x-input.ground.x)*facing,Feet[side].y-input.ground.y);
                Vector2 ankle=sole-TideRigGraphic.Rotate(Rig.soles[side].Value-Rest(b+2),footRoll[side]);
                Vector2 hip=Point(0,Rest(b));float length=legSpan[side]-.05f;
                float dx=hip.x-ankle.x,ceiling=ankle.y+Mathf.Sqrt(Mathf.Max(0,length*length-dx*dx));
                requiredDrop=Mathf.Max(requiredDrop,hip.y-ceiling);
            }
            supportDrop=Mathf.Max(requiredDrop,Damp(supportDrop,requiredDrop,16,dt));
            Positions[0]+=Vector2.down*supportDrop;
            float carry=MovementWeight*Mathf.Lerp(2.5f,13,run)*Mathf.Max(0,direction.x*facing)*(1-turnWeight)*(1-airWeight);
            spineLead=dt>0?Damp(spineLead,carry,20,dt):carry;
            Positions[1]=Point(0,Rest(1))+new Vector2(spineLead,0);Angles[1]=lean;
            var chestCenter=input.ground+new Vector2(Positions[1].x*facing,Positions[1].y+input.height*54);
            chestMotion.Advance(chestCenter,dt,Rig.hero);
            Positions[2]=Point(1,Rest(2));Angles[2]=lean*.6f;
            Positions[3]=Positions[2];Angles[3]=lean*.10f-MovementWeight*Mathf.Sin(Phase*2*Mathf.PI)*.95f;
            float decay=Mathf.Exp(-transitionTime/(input.attacking?.047f:.06f));near+=nearOffset*decay;far+=farOffset*decay;
            lastNear=near;lastFar=far;
            Vector2 chestOffset=Positions[1]-Rest(1);
            near+=chestOffset;far+=chestOffset;
            if(turnWeight>.005f||poseFacing!=facing)
            {
                // Limit the hand in screen direction while the body pivots.
                // The mirror changes the limb's local X, not its momentum.
                Vector2 nearBefore=Positions[Rig.weapon==2?17:6],farBefore=Positions[9];
                nearBefore.x*=poseFacing*facing;farBefore.x*=poseFacing*facing;
                near=Vector2.MoveTowards(nearBefore,near,525*dt);
                far=Vector2.MoveTowards(farBefore,far,525*dt);
            }
            Arm(4,near,-1,Rig.weapon==2?SwordHandLength:0);Arm(7,far,-1);
            poseFacing=facing;
            float hairTarget=Mathf.Clamp(-velocity.x*facing/55,-6,6)+Mathf.Sin(input.time*2.4f)*.65f;
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
            float blade=weapon+weaponOffset*decay;
            bool turningBlade=Rig.weapon==2&&!input.attacking&&!input.charging&&(turnWeight>.02f||weaponTurning);
            if(turningBlade)
            {
                weaponAngle=Mathf.MoveTowardsAngle(weaponAngle,blade,650*dt);
                weaponTurning=turnWeight>.02f||Mathf.Abs(Mathf.DeltaAngle(weaponAngle,blade))>.1f;
            }
            else{weaponAngle=blade;weaponTurning=false;}
            Positions[17]=Rig.weapon==2?Positions[6]+(Positions[6]-Positions[5]).normalized*SwordHandLength
                :Point(6,Rest(6)+new Vector2(-2.5f,-5));
            Angles[17]=Rig.weapon==1?Angles[6]-108:weaponAngle;
            // The long blade points into the plane briefly during a pivot.
            // Project that part of its reach around the grip.
            float bladeDrop=-TideRigGraphic.Rotate(bladeTip,weaponAngle).y;
            BladeScale=turningBlade&&!air&&bladeDrop>1?Mathf.Clamp01((Positions[17].y-3)/bladeDrop):1;
        }
        private void Arm(int bone,Vector2 target,float bend,float handLength=0)
        {
            Vector2 shoulder=Point(1,Rest(bone));
            float forearmLength=(Rest(bone+2)-Rest(bone+1)).magnitude;
            Solve(shoulder,(Rest(bone+1)-Rest(bone)).magnitude,forearmLength+handLength,target,bend,out Vector2 elbow,out Vector2 hand);
            Vector2 wrist=elbow+(hand-elbow)*(forearmLength/(forearmLength+handLength));
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
