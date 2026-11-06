using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpiritTide.EditorTools
{
    public static class TideMotionValidation
    {
        [Serializable] private class Metric {public int hero,frames,worst_frame;public float maximum_foot_error,maximum_reach_error,maximum_limb_error,maximum_contact_slide,framerate_difference,landing_pose_step,cancel_wrist_step,cancel_weapon_step;public int actions;public bool frozen,warp_safe;public bool finite;public string worst_state;}
        [Serializable] private class Metrics {public string scope="Source-preserving skeleton measurement; visual review is required separately";public Metric[] heroes;}
        public static void Run(Action<string,bool> check)
        {
            var metrics=new List<Metric>();
            for(int hero=0;hero<6;hero++)
            {
                var rig=TideRigData.Get(hero);var tex=TideRigData.Texture(hero);
                check("complete bind texture and eighteen bones "+hero,tex.width==rig.source_width&&tex.height==rig.source_height&&rig.bones.Length==18&&rig.vertices.Length>3000&&rig.vertices.Length<18000);
                var s=new TideSession(new TideProfile{hero=hero});s.BeginBattle(true);var motion=new TideNaturalMotion();
                motion.Reset(rig,TideMotionInput.Capture(s,48));var m=new Metric{hero=hero,finite=true};Vector2[] oldFeet=new Vector2[2];bool[] oldContact=new bool[2];int oldFacing=s.Facing;
                for(int frame=0;frame<720;frame++)
                {
                    Vector2 input=frame<30?Vector2.zero:frame<180?Vector2.right:frame<240?Vector2.zero:frame<390?Vector2.left:frame<450?Vector2.up:Vector2.zero;
                    if(frame==480)s.Jump();if(frame==550)s.Dodge(Vector2.right);if(frame==610)s.Request(MoveKind.Basic);
                    for(int side=0;side<2;side++){oldFeet[side]=motion.Feet[side];oldContact[side]=motion.Contacts[side];}
                    oldFacing=s.Facing;s.Step(1f/60,input,false);motion.Tick(rig,TideMotionInput.Capture(s,48));m.frames++;
                    if(oldFacing==s.Facing&&motion.MovementWeight>.08f)
                        for(int side=0;side<2;side++)if(oldContact[side]&&motion.Contacts[side])m.maximum_contact_slide=Mathf.Max(m.maximum_contact_slide,Vector2.Distance(oldFeet[side],motion.Feet[side]));
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
                check("planted feet remain on their real world contact "+hero,m.maximum_foot_error<.05f&&m.maximum_contact_slide<.05f);
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
                foreach(MoveKind action in new[]{MoveKind.Basic,MoveKind.Heavy,MoveKind.Launcher,MoveKind.Air,MoveKind.Counter,MoveKind.Skill0,MoveKind.Skill1,MoveKind.Skill2,MoveKind.Skill3,MoveKind.Ultimate})
                {
                    int variants=action==MoveKind.Basic?3:1;
                    for(int combo=0;combo<variants;combo++)
                    {
                        var c=new TideNaturalMotion();var state=new TideMotionInput{facing=1};c.Reset(rig,state);float peak=0;bool finite=true;
                        for(int f=1;f<=90;f++)
                        {
                            state.time=f/120f;state.serial=1;state.attacking=f<=72;state.phase=Mathf.Min(1,f/72f);state.action=action;state.combo=combo;
                            state.height=action==MoveKind.Air?.7f:0;c.Tick(rig,state);
                            peak=Mathf.Max(peak,Vector2.Distance(c.Positions[6],rig.bones[6].Value));
                            foreach(var p in c.Positions)finite&=!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&p.sqrMagnitude<500*500;
                        }
                        check("complete articulated action stays finite "+hero+" / "+action+" / "+combo,finite&&peak>5);m.actions++;
                    }
                }
                var landingSession=new TideSession(new TideProfile{hero=hero});landingSession.BeginBattle(true);var landingMotion=new TideNaturalMotion();landingMotion.Reset(rig,TideMotionInput.Capture(landingSession,48));
                Vector2 lastHead=landingMotion.Positions[3];
                for(int f=0;f<90;f++)
                {
                    if(f==25)landingSession.Jump();if(f==44)landingSession.Dodge(Vector2.right);
                    landingSession.Step(1f/30,Vector2.right,false);landingMotion.Tick(rig,TideMotionInput.Capture(landingSession,48));
                    m.landing_pose_step=Mathf.Max(m.landing_pose_step,Vector2.Distance(lastHead,landingMotion.Positions[3]));lastHead=landingMotion.Positions[3];
                }
                check("running air dodge and landing preserve posture continuity "+hero,m.landing_pose_step<11);
                var cancelSession=new TideSession(new TideProfile{hero=hero});cancelSession.BeginBattle(true);cancelSession.EnemyX=cancelSession.X+2;var cancelMotion=new TideNaturalMotion();cancelMotion.Reset(rig,TideMotionInput.Capture(cancelSession,48));
                cancelSession.Request(MoveKind.Basic);int cancelGuard=0;
                while(cancelSession.ActionElapsed<cancelSession.ActionDuration*.42f&&cancelGuard++<90){cancelSession.Step(1f/60,Vector2.zero,false);cancelMotion.Tick(rig,TideMotionInput.Capture(cancelSession,48));}
                Vector2 oldWrist=cancelMotion.Positions[6];float oldBlade=cancelMotion.Angles[17];cancelSession.Dodge(Vector2.right);
                while(cancelSession.HitStopRemaining>0)cancelSession.Step(1f/60,Vector2.zero,false);
                cancelSession.Step(1f/60,Vector2.zero,false);cancelMotion.Tick(rig,TideMotionInput.Capture(cancelSession,48));m.cancel_wrist_step=Vector2.Distance(oldWrist,cancelMotion.Positions[6]);m.cancel_weapon_step=Mathf.Abs(Mathf.DeltaAngle(oldBlade,cancelMotion.Angles[17]));
                check("hit into dodge blends the existing wrist pose "+hero,cancelSession.DodgeRemaining>0&&m.cancel_wrist_step<25);
                check("weapon rotation also blends through a dodge cancel "+hero,rig.weapon==1||m.cancel_weapon_step<25);
                metrics.Add(m);
            }
            File.WriteAllText("BuildLogs/motion-kinematics.json",JsonUtility.ToJson(new Metrics{heroes=metrics.ToArray()},true));
        }
    }
}
