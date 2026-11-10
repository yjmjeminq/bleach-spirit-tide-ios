using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpiritTide.EditorTools
{
    public static class TideMotionValidation
    {
        [Serializable] private class Metric
        {
            public int hero,frames,worst_frame,slide_frame,actions,slow_steps,strike_contacts,pivot_frame=-1;
            public float maximum_foot_error,maximum_reach_error,maximum_limb_error,maximum_contact_slide,framerate_difference;
            public float landing_pose_step,cancel_wrist_step,cancel_weapon_step,pivot_wrist_step,pivot_blade_step,lane_foot_step;
            public float minimum_pivot_blade_height=999,minimum_pivot_blade_scale=1;
            public float chest_run_peak,chest_landing_peak,chest_settled;
            public float slow_contact_slide,strike_support_error;
            public bool frozen,warp_safe,finite,final_plant,hair_follows_turn,chest_freezes;
            public string worst_state;
        }
        [Serializable] private class Metrics {public string scope="Source-preserving skeleton measurement; visual review is required separately";public Metric[] heroes;}
        public static void Run(Action<string,bool> check)
        {
            var metrics=new List<Metric>();
            for(int hero=0;hero<6;hero++)
            {
                var rig=TideRigData.Get(hero);var tex=TideRigData.Texture(hero);
                check("complete bind texture and eighteen bones "+hero,tex.width==rig.source_width&&tex.height==rig.source_height&&rig.bones.Length==18&&rig.vertices.Length>3000&&rig.vertices.Length<18000);
                var s=Training(hero);var motion=new TideNaturalMotion();
                motion.Reset(rig,TideMotionInput.Capture(s,48));var m=new Metric{hero=hero,finite=true};Vector2[] oldFeet=new Vector2[2];bool[] oldContact=new bool[2];int oldFacing=s.Facing;
                for(int frame=0;frame<720;frame++)
                {
                    Vector2 input=frame<30?Vector2.zero:frame<180?Vector2.right:frame<240?Vector2.zero:frame<390?Vector2.left:frame<450?Vector2.up:Vector2.zero;
                    if(frame==480)s.Jump();if(frame==550)s.Dodge(Vector2.right);if(frame==610)s.Request(MoveKind.Basic);
                    for(int side=0;side<2;side++){oldFeet[side]=motion.Feet[side];oldContact[side]=motion.Contacts[side];}
                    oldFacing=motion.DisplayFacing;s.Step(1f/60,input,false);motion.Tick(rig,TideMotionInput.Capture(s,48));m.frames++;
                    if(motion.MovementWeight>.08f)
                        for(int side=0;side<2;side++)
                        {
                            int beforeSide=oldFacing==motion.DisplayFacing?side:1-side;
                            if(!oldContact[beforeSide]||!motion.Contacts[side])continue;
                            float slide=Vector2.Distance(oldFeet[beforeSide],motion.Feet[side]);
                            if(slide>m.maximum_contact_slide){m.maximum_contact_slide=slide;m.slide_frame=frame;}
                        }
                    if(motion.FootError>m.maximum_foot_error){m.maximum_foot_error=motion.FootError;m.worst_frame=frame;m.worst_state="speed "+motion.Speed+" phase "+motion.Phase+" weight "+motion.MovementWeight;}
                    m.maximum_reach_error=Mathf.Max(m.maximum_reach_error,motion.ReachError);
                    foreach(int b in new[]{4,7,10,13})for(int j=0;j<2;j++)
                    {
                        float rest=Vector2.Distance(rig.bones[b+j].Value,rig.bones[b+j+1].Value),actual=Vector2.Distance(motion.Positions[b+j],motion.Positions[b+j+1]);
                        m.maximum_limb_error=Mathf.Max(m.maximum_limb_error,Mathf.Abs(actual-rest));
                    }
                    foreach(var p in motion.Positions)m.finite&=!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&!float.IsInfinity(p.x)&&!float.IsInfinity(p.y);
                }
                check("continuous limbs stay finite and preserve lengths "+hero,m.finite&&m.maximum_limb_error<.02f);
                check($"planted feet remain on their real world contact {hero}, solve {m.maximum_foot_error:F3} at {m.worst_frame}, slide {m.maximum_contact_slide:F3} at {m.slide_frame}",
                    m.maximum_foot_error<.05f&&m.maximum_contact_slide<.05f);
                s.Pause();var before=(Vector2[])motion.Positions.Clone();float phase=motion.Phase;
                s.Step(1f/30,Vector2.right,true);m.frozen=!motion.Tick(rig,TideMotionInput.Capture(s,48))&&motion.Phase==phase;
                for(int b=0;b<18;b++)m.frozen&=motion.Positions[b]==before[b];check("pause freezes every joint and gait phase "+hero,m.frozen);s.Resume();
                s.HitStopRemaining=.08f;before=(Vector2[])motion.Positions.Clone();s.Step(1f/60,Vector2.right,false);bool hitFrozen=!motion.Tick(rig,TideMotionInput.Capture(s,48));
                for(int b=0;b<18;b++)hitFrozen&=motion.Positions[b]==before[b];check("impact stop freezes every joint "+hero,hitFrozen);
                s.ReturnTown();motion.Tick(rig,TideMotionInput.Capture(s,48));m.warp_safe=motion.Speed==0&&motion.MovementWeight==0;
                check("return resets velocity and world foot anchors "+hero,m.warp_safe);
                var samples=new List<Vector2[]>();
                foreach(int fps in new[]{30,60,120})
                {
                    var c=new TideNaturalMotion();var state=new TideMotionInput{facing=1};c.Reset(rig,state);
                    for(int f=1;f<=fps*3;f++){state.time=f/(float)fps;state.ground=new Vector2(state.time*170,0);c.Tick(rig,state);}
                    samples.Add((Vector2[])c.Positions.Clone());
                }
                for(int n=0;n<2;n++)for(int b=0;b<18;b++)m.framerate_difference=Mathf.Max(m.framerate_difference,Vector2.Distance(samples[n][b],samples[2][b]));
                check("30 60 and 120 Hz produce consistent locomotion "+hero,m.framerate_difference<4);
                foreach(MoveKind action in Enum.GetValues(typeof(MoveKind)))
                {
                    int variants=action==MoveKind.Basic?3:1;
                    for(int combo=0;combo<variants;combo++)
                    {
                        var c=new TideNaturalMotion();var state=new TideMotionInput{facing=1};c.Reset(rig,state);float peak=0,footError=0;bool finite=true;
                        for(int f=1;f<=90;f++)
                        {
                            state.time=f/120f;state.serial=1;state.attacking=f<=72;state.phase=Mathf.Min(1,f/72f);state.action=action;state.combo=combo;
                            state.height=action==MoveKind.Air?.7f:0;c.Tick(rig,state);
                            peak=Mathf.Max(peak,Vector2.Distance(c.Positions[6],rig.bones[6].Value));
                            footError=Mathf.Max(footError,c.FootError);
                            foreach(var p in c.Positions)finite&=!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&p.sqrMagnitude<500*500;
                        }
                        check($"complete articulated action stays finite and grounded {hero} / {action} / {combo}, support {footError:F3}",finite&&peak>5&&footError<.05f);m.actions++;
                    }
                }
                var landingSession=Training(hero);var landingMotion=new TideNaturalMotion();landingMotion.Reset(rig,TideMotionInput.Capture(landingSession,48));
                Vector2 lastHead=landingMotion.Positions[3];int postureFrame=0;
                for(int f=0;f<90;f++)
                {
                    if(f==25)landingSession.Jump();if(f==44)landingSession.Dodge(Vector2.right);
                    landingSession.Step(1f/30,Vector2.right,false);landingMotion.Tick(rig,TideMotionInput.Capture(landingSession,48));
                    float headStep=Vector2.Distance(lastHead,landingMotion.Positions[3]);
                    if(headStep>m.landing_pose_step){m.landing_pose_step=headStep;postureFrame=f;}
                    lastHead=landingMotion.Positions[3];
                }
                check($"running air dodge and landing preserve posture continuity {hero}, step {m.landing_pose_step:F2} at frame {postureFrame}",m.landing_pose_step<11);
                var cancelSession=Training(hero);cancelSession.EnemyX=cancelSession.X+2;var cancelMotion=new TideNaturalMotion();cancelMotion.Reset(rig,TideMotionInput.Capture(cancelSession,48));
                cancelSession.Request(MoveKind.Basic);int cancelGuard=0;
                while(cancelSession.ActionElapsed<cancelSession.ActionDuration*.42f&&cancelGuard++<90){cancelSession.Step(1f/60,Vector2.zero,false);cancelMotion.Tick(rig,TideMotionInput.Capture(cancelSession,48));}
                Vector2 oldWrist=cancelMotion.Positions[6];float oldBlade=cancelMotion.Angles[17];cancelSession.Dodge(Vector2.right);
                while(cancelSession.HitStopRemaining>0)cancelSession.Step(1f/60,Vector2.zero,false);
                cancelSession.Step(1f/60,Vector2.zero,false);cancelMotion.Tick(rig,TideMotionInput.Capture(cancelSession,48));m.cancel_wrist_step=Vector2.Distance(oldWrist,cancelMotion.Positions[6]);m.cancel_weapon_step=Mathf.Abs(Mathf.DeltaAngle(oldBlade,cancelMotion.Angles[17]));
                check($"hit into dodge blends the existing wrist pose {hero}, step {m.cancel_wrist_step:F2}",cancelSession.DodgeRemaining>0&&m.cancel_wrist_step<25);
                check("weapon rotation also blends through a dodge cancel "+hero,rig.weapon==1||m.cancel_weapon_step<25);
                ReviewGroundedTransitions(rig,m);
                ReviewSlowWalkAndImpact(rig,m,check);
                ReviewChestMotion(rig,m,check);
                metrics.Add(m);
            }
            File.WriteAllText("BuildLogs/motion-kinematics.json",JsonUtility.ToJson(new Metrics{heroes=metrics.ToArray()},true));
            ReviewClearStrike(check);
            foreach(var m in metrics)
            {
                int hero=m.hero;
                TideRigDef rig=TideRigData.Get(hero);
                check("ground pivot settles without a wrist jump "+hero,m.pivot_frame>=46&&m.pivot_frame<=47&&m.pivot_wrist_step<24);
                check("blade stays continuous during a running turn "+hero,rig.weapon!=2||m.pivot_blade_step<27);
                check("pivot keeps the long blade above the ground plane "+hero,
                    rig.weapon!=2||m.minimum_pivot_blade_height>0&&m.minimum_pivot_blade_scale<.99f);
                check("depth change keeps the swing foot continuous "+hero,m.lane_foot_step<23);
                check("stopping takes a final step around the supporting foot "+hero,m.final_plant);
                check("separate hair follows the turn and settles "+hero,rig.hair_vertices.Length==0||m.hair_follows_turn);
            }
        }

        private static void ReviewClearStrike(Action<string,bool> check)
        {
            var session=new TideSession(new TideProfile());
            check("first campaign room is available for the finish pose",session.AcceptQuest()&&session.BeginDungeon());
            foreach(var foe in session.Enemies)
            {
                foe.X=session.X+1.5f;foe.Y=session.Y;foe.Hp=1;foe.Stun=2;
            }
            check("clear pose uses a normal basic attack",session.Request(MoveKind.Basic));
            int firstSerial=session.AttackSerial;
            session.Request(MoveKind.Basic);session.Request(MoveKind.Basic);
            for(int f=0;f<60&&!session.RoomCleared;f++)session.Step(1f/60,Vector2.zero,false);
            check("a room-clearing hit retains its contact pose without holding movement",
                session.RoomCleared&&session.ActionRemaining==0&&session.HasActionPose&&Mathf.Abs(session.ActionPosePhase-TideSession.ActionContactPhase)<.001f);
            float start=session.X;
            for(int f=0;f<6;f++)session.Step(1f/60,Vector2.right,false);
            check("the finishing pose follows through while the player can move",
                session.X>start&&session.HasActionPose&&session.ActionPosePhase>TideSession.ActionContactPhase);
            int damage=session.DamageEvents;
            bool second=false,third=false;
            for(int f=0;f<90;f++)
            {
                session.Step(1f/60,Vector2.zero,false);
                second|=session.AttackSerial==firstSerial+1&&session.Combo==1;
                third|=session.AttackSerial==firstSerial+2&&session.Combo==2;
            }
            check("pre-queued taps finish after a room clear without adding hits or advancing the room",
                second&&third&&session.AttackSerial==firstSerial+2&&session.DamageEvents==damage&&session.RoomCleared&&session.RoomIndex==0);
            check("the finishing pose expires",!session.HasActionPose);
        }

        private static TideSession Training(int hero)
        {
            var session=new TideSession(new TideProfile());
            session.Profile.recruitedHeroes=TideProgress.AllHeroes;
            session.SelectHero(hero);
            session.BeginBattle(true);
            return session;
        }

        private static void ReviewSlowWalkAndImpact(TideRigDef rig,Metric metric,Action<string,bool> check)
        {
            var session=Training(rig.hero);
            var motion=new TideNaturalMotion();
            motion.Reset(rig,TideMotionInput.Capture(session,48));
            bool nearStep=false,farStep=false;
            for(int f=0;f<180;f++)
            {
                Vector2 near=motion.Feet[0],far=motion.Feet[1];
                bool nearHeld=motion.Contacts[0],farHeld=motion.Contacts[1];
                session.Step(1f/60,Vector2.right*.22f,false);
                motion.Tick(rig,TideMotionInput.Capture(session,48));
                if(nearHeld&&motion.Contacts[0])metric.slow_contact_slide=Mathf.Max(metric.slow_contact_slide,Vector2.Distance(near,motion.Feet[0]));
                if(farHeld&&motion.Contacts[1])metric.slow_contact_slide=Mathf.Max(metric.slow_contact_slide,Vector2.Distance(far,motion.Feet[1]));
                if(!nearHeld&&motion.Contacts[0]){nearStep=true;metric.slow_steps++;}
                if(!farHeld&&motion.Contacts[1]){farStep=true;metric.slow_steps++;}
            }
            check($"partial stick input makes both feet take short steps {rig.hero}, {metric.slow_steps} steps",nearStep&&farStep&&metric.slow_steps>=3&&metric.slow_contact_slide<.05f);
            bool timed=true,supported=true;
            for(int combo=0;combo<3;combo++)
            {
                session.EnemyX=session.X+1.8f;session.EnemyY=session.Y;
                check("grounded combo can start out of movement "+rig.hero+" / "+combo,session.Request(MoveKind.Basic)&&session.Combo==combo);
                int damage=session.DamageEvents;
                for(int f=0;f<90&&(session.ActionRemaining>0||session.HitStopRemaining>0);f++)
                {
                    session.Step(1f/60,Vector2.zero,false);
                    motion.Tick(rig,TideMotionInput.Capture(session,48));
                    if(damage==session.DamageEvents)continue;
                    damage=session.DamageEvents;metric.strike_contacts++;
                    timed&=Mathf.Abs(session.ActionPosePhase-TideSession.ActionContactPhase)<.0001f;
                    supported&=motion.Contacts[0]||motion.Contacts[1];
                    metric.strike_support_error=Mathf.Max(metric.strike_support_error,motion.FootError);
                }
            }
            check($"all three real hits match the strike pose and a planted foot {rig.hero}, support {metric.strike_support_error:F3}",metric.strike_contacts==3&&timed&&supported&&metric.strike_support_error<.05f);
        }
        private static void ReviewGroundedTransitions(TideRigDef rig,Metric metric)
        {
            var session=Training(rig.hero);
            var motion=new TideNaturalMotion();
            motion.Reset(rig,TideMotionInput.Capture(session,48));
            int support=-1;
            Vector2 planted=Vector2.zero;
            bool tookLastStep=false,keptPlant=true;
            for(int frame=0;frame<88;frame++)
            {
                Vector2 move=frame<8?Vector2.zero:frame<45?Vector2.right:frame<63?Vector2.left:frame<75?Vector2.up:Vector2.zero;
                int facing=motion.DisplayFacing;
                Vector2 wrist=ProjectedWrist(motion);
                float blade=motion.Angles[17]*facing;
                Vector2 near=motion.Feet[0],far=motion.Feet[1];
                session.Step(1f/30,move,false);
                motion.Tick(rig,TideMotionInput.Capture(session,48));
                if(frame>=45&&frame<54)
                {
                    metric.pivot_wrist_step=Mathf.Max(metric.pivot_wrist_step,Vector2.Distance(wrist,ProjectedWrist(motion)));
                    metric.pivot_blade_step=Mathf.Max(metric.pivot_blade_step,Mathf.Abs(Mathf.DeltaAngle(blade,motion.Angles[17]*motion.DisplayFacing)));
                    if(rig.weapon==2)
                    {
                        metric.minimum_pivot_blade_height=Mathf.Min(metric.minimum_pivot_blade_height,motion.BladeTipHeight);
                        metric.minimum_pivot_blade_scale=Mathf.Min(metric.minimum_pivot_blade_scale,motion.BladeScale);
                    }
                    if(motion.DisplayFacing!=facing)
                    {
                        metric.pivot_frame=frame;
                        metric.hair_follows_turn=motion.HairFacing*facing>.25f;
                    }
                }
                if(frame>=63&&frame<70)
                    metric.lane_foot_step=Mathf.Max(metric.lane_foot_step,Mathf.Max(Vector2.Distance(near,motion.Feet[0]),Vector2.Distance(far,motion.Feet[1])));
                if(frame>=75)
                {
                    if(support<0)
                    {
                        support=motion.Contacts[0]?0:1;
                        planted=motion.Feet[support];
                    }
                    keptPlant&=motion.Contacts[support]&&Vector2.Distance(planted,motion.Feet[support])<.02f;
                    tookLastStep|=!motion.Contacts[1-support];
                }
            }
            metric.final_plant=keptPlant&&tookLastStep&&motion.Contacts[0]&&motion.Contacts[1];
            metric.hair_follows_turn&=Mathf.Abs(motion.HairFacing-motion.DisplayFacing)<.001f;
        }

        private static Vector2 ProjectedWrist(TideNaturalMotion motion)
        {
            Vector2 wrist=motion.Positions[6];
            wrist.x*=motion.DisplayFacing;
            return wrist;
        }

        private static void ReviewChestMotion(TideRigDef rig,Metric metric,Action<string,bool> check)
        {
            var session=Training(rig.hero);
            var motion=new TideNaturalMotion();
            motion.Reset(rig,TideMotionInput.Capture(session,48));
            int landed=-1;
            bool bounded=true;
            for(int frame=0;frame<142;frame++)
            {
                if(frame==38)session.Jump();
                float previousHeight=session.Z;
                session.Step(1f/60,frame<24?Vector2.right:Vector2.zero,false);
                motion.Tick(rig,TideMotionInput.Capture(session,48));
                Vector2 offset=motion.ChestOffset;
                bounded&=Mathf.Abs(offset.x)<.91f&&Mathf.Abs(offset.y)<3.15f;
                if(frame<36)metric.chest_run_peak=Mathf.Max(metric.chest_run_peak,offset.magnitude);
                if(previousHeight>0&&session.Z==0)landed=frame;
                if(landed>=0&&frame-landed<12)
                {
                    metric.chest_landing_peak=Mathf.Max(metric.chest_landing_peak,-offset.y);
                    if(!metric.chest_freezes&&offset.y<-.65f)
                    {
                        session.Pause();
                        session.Step(1f/30,Vector2.right,false);
                        bool paused=!motion.Tick(rig,TideMotionInput.Capture(session,48))&&motion.ChestOffset==offset;
                        session.Resume();
                        session.HitStopRemaining=.05f;
                        session.Step(1f/60,Vector2.zero,false);
                        metric.chest_freezes=paused&&!motion.Tick(rig,TideMotionInput.Capture(session,48))&&motion.ChestOffset==offset;
                    }
                }
            }
            metric.chest_settled=motion.ChestOffset.magnitude;
            check("local chest motion follows run and landing "+rig.hero,bounded&&metric.chest_run_peak>.15f&&metric.chest_landing_peak>.7f);
            check("chest motion settles and freezes with the presentation clock "+rig.hero,metric.chest_settled<.05f&&metric.chest_freezes);
            session.ReturnTown();
            motion.Tick(rig,TideMotionInput.Capture(session,48));
            check("new scene clears secondary chest motion "+rig.hero,motion.ChestOffset==Vector2.zero);
        }
    }
}
