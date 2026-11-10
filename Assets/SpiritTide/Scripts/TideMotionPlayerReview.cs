using System;
using System.Collections;
using System.IO;
using UnityEngine;

namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        private IEnumerator MotionAdvance(int frames,Vector2 move)
        {
            for(int f=0;f<frames;f++){game.Session.Step(1f/60,move,false);yield return null;}
        }
        private IEnumerator MotionFinish()
        {
            int guard=0;while((game.Session.ActionRemaining>0||game.Session.Z>0||game.Session.HitStopRemaining>0||game.Session.DodgeRemaining>0)&&guard++<360)
            {game.Session.Step(1f/60,Vector2.zero,false);yield return null;}
            Check("motion action returns to idle without a loop",guard<360);
            yield return MotionAdvance(5,Vector2.zero);
        }
        private IEnumerator MotionContact()
        {
            var session=game.Session;
            int guard=0;
            while(session.ActionRemaining>0&&session.ActionPosePhase<TideSession.ActionContactPhase&&guard++<90)
                yield return MotionAdvance(1,Vector2.zero);
            Check("live action reaches its contact pose",session.ActionRemaining>0&&Mathf.Abs(session.ActionPosePhase-TideSession.ActionContactPhase)<.0001f);
        }
        private void CheckMotionBody(int hero,string action)
        {
            Canvas.ForceUpdateCanvases();var m=game.NaturalMotion;var g=game.ArticulatedActor;bool finite=true;
            for(int i=0;i<g.VertexCount;i+=29){var p=g.SkinnedVertex(i);finite&=!float.IsNaN(p.x)&&!float.IsNaN(p.y)&&p.sqrMagnitude<500*500;}
            Check("rendered articulated pose remains bounded "+hero+" / "+action,finite&&m.FootError<.05f&&Mathf.Abs(m.Positions[0].y-m.Rig.bones[0].y)<40);
            Check("actor preserves face size and upright root "+hero+" / "+action,Mathf.Abs(g.transform.parent.localScale.x)==1&&g.transform.parent.localScale.y==1&&g.transform.parent.localRotation==Quaternion.identity);
            if(m.Rig.weapon==2)
            {
                var hand=g.transform.parent.GetComponentInChildren<TideSwordGrip>();
                Vector3 handle=g.transform.TransformPoint(g.Skin(m.Rig.bones[17].Value,17));
                Check("closed hand renders on the sword hilt "+hero+" / "+action,hand!=null&&hand.VertexCount>30&&
                    Vector3.Distance(handle,hand.transform.TransformPoint(hand.GripCenter))<.025f);
            }
        }
        private void CheckFaceClear(string scenario)
        {
            Canvas.ForceUpdateCanvases();
            var safe=game.transform.Find("GameCanvas/SafeArea");
            var root=game.ArticulatedActor.transform.parent;
            Vector2 face=RectTransformUtility.WorldToScreenPoint(null,root.TransformPoint(game.NaturalMotion.Positions[3]+Vector2.up*17));
            bool clear=true;
            foreach(string path in new[]{"BossHud/EnemyVitals","RoomProgress","AttackTell"})
            {
                var card=(RectTransform)safe.Find(path);
                clear&=!card.gameObject.activeInHierarchy||!RectTransformUtility.RectangleContainsScreenPoint(card,face);
            }
            Check("battle information clears the jumping hero's face "+scenario,clear);
        }
        private IEnumerator ReviewChestMesh(int hero)
        {
            var session=game.Session;
            var rig=game.ArticulatedActor;
            bool reduced=session.Profile.reducedEffects;
            session.Profile.reducedEffects=false;
            for(int i=0;i<90&&(session.Z>0||game.NaturalMotion.ChestOffset.y>-.80f);i++)
            {
                session.Step(1f/60,Vector2.zero,false);
                yield return null;
            }
            Check("the landing drives chest motion on the real body mesh "+hero,session.Z==0&&rig.ChestOffset.y<=-.80f);
            yield return Capture("114-chest-landing-"+hero);
            var active=new Vector2[rig.VertexCount];
            for(int i=0;i<active.Length;i++)active[i]=rig.SkinnedVertex(i);
            session.Profile.reducedEffects=true;
            yield return null;
            Canvas.ForceUpdateCanvases();
            int changed=0;
            bool local=true;
            for(int i=0;i<active.Length;i++)
            {
                if((active[i]-rig.SkinnedVertex(i)).sqrMagnitude<.0009f)continue;
                changed++;
                var vertex=rig.Rig.vertices[i];
                local&=vertex.v>.65f&&vertex.v<.81f&&vertex.u>.28f&&vertex.u<.68f
                    && (vertex.a<=1||vertex.b<=1);
            }
            Check("reduce effects removes the local morph from the real body mesh "+hero,
                rig.ChestOffset==Vector2.zero&&local&&changed>80&&changed<rig.VertexCount*.08f);
            if(hero==4)yield return Capture("114-chest-reduced");
            session.Profile.reducedEffects=reduced;
            yield return null;
        }
        private IEnumerator ReviewNaturalMotion()
        {
            string saved=JsonUtility.ToJson(game.Session.Profile);game.ReviewStepping=true;
            game.Session.Profile.recruitedHeroes=TideProgress.AllHeroes;
            int skillPoses=0;
            try
            {
                for(int hero=0;hero<6;hero++)
                {
                    game.ReturnTown();game.Session.SelectHero(hero);game.StartBattle(true);game.Session.EnemyX=9;game.Session.EnemyY=1.2f;
                    for(int t=0;t<8;t++){game.Session.Step(1f/30,Vector2.zero,false);yield return null;}
                    Check("hero renders an articulated source preserving mesh "+hero,game.ArticulatedActor.Rig.hero==hero&&game.ArticulatedActor.VertexCount>3000);
                    CheckMotionBody(hero,"idle");
                    yield return Capture("100-motion-idle-"+hero);
                    for(int t=0;t<40;t++)
                    {
                        game.Session.Step(1f/30,Vector2.right,false);yield return null;
                        if(t==12||t==26){CheckMotionBody(hero,"run");yield return Capture("101-motion-run-"+hero+"-"+t);}
                    }
                    for(int t=0;t<16;t++){game.Session.Step(1f/30,Vector2.zero,false);yield return null;}
                    game.Session.Request(MoveKind.Basic);
                    for(int t=0;t<5;t++){game.Session.Step(1f/30,Vector2.zero,false);yield return null;}
                    yield return Capture("102-motion-strike-"+hero);
                    for(int t=0;t<25;t++){game.Session.Step(1f/30,Vector2.zero,false);yield return null;}
                    game.Session.Jump();
                    for(int t=0;t<10;t++){game.Session.Step(1f/30,Vector2.zero,false);yield return null;}
                    CheckFaceClear("training "+hero);
                    yield return Capture("103-motion-jump-"+hero);
                    yield return ReviewChestMesh(hero);yield return MotionFinish();
                    game.Session.EnemyX=game.Session.X+2;game.Session.EnemyY=game.Session.Y;
                    for(int combo=0;combo<3;combo++)
                    {
                        Check("real combo input accepted "+hero+" / "+combo,game.Session.Request(MoveKind.Basic));yield return MotionContact();
                        CheckMotionBody(hero,"combo "+combo);
                        var burst=game.GetComponentInChildren<TideImpactBurst>();
                        Check("real contact renders a focused impact "+hero+" / "+combo,burst!=null&&burst.VertexCount>=15);
                        yield return Capture("104-motion-combo-"+hero+"-"+combo);yield return MotionFinish();
                    }
                    game.Session.BeginHeavy();yield return MotionAdvance(24,Vector2.zero);yield return Capture("105-motion-charge-"+hero);game.Session.ReleaseHeavy();yield return MotionAdvance(16,Vector2.zero);
                    CheckMotionBody(hero,"heavy");yield return Capture("105-motion-heavy-"+hero);yield return MotionFinish();
                    game.Session.Request(MoveKind.Launcher);yield return MotionAdvance(10,Vector2.zero);yield return Capture("106-motion-launcher-"+hero);yield return MotionFinish();
                    game.Session.Jump();yield return MotionAdvance(5,Vector2.zero);game.Session.Request(MoveKind.Basic);yield return MotionAdvance(9,Vector2.zero);
                    Check("real jump selects the air action "+hero,game.Session.Action==MoveKind.Air);CheckMotionBody(hero,"air");yield return Capture("106-motion-air-"+hero);yield return MotionFinish();
                    game.Session.Dodge(Vector2.left);yield return MotionAdvance(8,Vector2.zero);CheckMotionBody(hero,"dodge");yield return Capture("107-motion-dodge-"+hero);yield return MotionFinish();
                    game.Session.PerfectRemaining=.4f;game.Session.Request(MoveKind.Basic);yield return MotionAdvance(10,Vector2.zero);Check("perfect opportunity selects counter pose "+hero,game.Session.Action==MoveKind.Counter);yield return Capture("107-motion-counter-"+hero);yield return MotionFinish();
                    for(int slot=0;slot<CharacterDef.SkillCount;slot++)
                    {
                        game.Session.EnemyX=game.Session.X+2;game.Session.EnemyY=game.Session.Y;
                        Check("real skill pose input accepted "+hero+" / "+slot,game.Session.Request((MoveKind)((int)MoveKind.Skill0+slot)));
                        yield return MotionContact();CheckMotionBody(hero,"skill "+slot);
                        yield return Capture("108-motion-skill-"+hero+"-"+slot);yield return MotionFinish();skillPoses++;
                    }
                    var foe=game.Session.Target;game.Session.Shield=0;game.Session.ReflectRemaining=0;foe.Stun=foe.Stagger=foe.Lift=foe.Recovery=0;foe.X=game.Session.X+1;foe.Y=game.Session.Y;foe.Timer=.05f;foe.Telegraph=false;
                    if(!game.Session.TrainingAttacks)game.Session.ToggleTrainingAttacks();int hurt=game.Session.HurtSerial,wait=0;
                    while(game.Session.HurtSerial==hurt&&wait++<300){game.Session.Step(1f/60,Vector2.zero,false);yield return null;}
                    Check("actual enemy hit triggers the hero recoil "+hero,game.Session.HurtSerial>hurt);yield return MotionAdvance(3,Vector2.zero);CheckMotionBody(hero,"hurt");yield return Capture("109-motion-hurt-"+hero);
                    game.Session.ToggleTrainingAttacks();yield return MotionAdvance(25,Vector2.zero);
                    game.Session.Pause();var held=(Vector2[])game.NaturalMotion.Positions.Clone();float phase=game.NaturalMotion.Phase;
                    yield return MotionAdvance(8,Vector2.right);bool frozen=game.NaturalMotion.Phase==phase;
                    for(int b=0;b<18;b++)frozen&=game.NaturalMotion.Positions[b]==held[b];Check("live pause freezes all eighteen bones "+hero,frozen);game.Session.Resume();
                    game.Session.X=6;yield return MotionAdvance(60,Vector2.right);yield return MotionAdvance(35,Vector2.right);
                    Check("holding into the wall stops the gait "+hero,game.NaturalMotion.MovementWeight<.01f&&game.NaturalMotion.Speed<.1f);
                    game.ReturnTown();yield return null;Check("live return resets grounded locomotion "+hero,game.NaturalMotion.Speed==0&&game.NaturalMotion.Rig.hero==hero);
                }
                yield return ReviewFoeMotion();
                yield return ReviewMotionCamera();
                Check("all six articulated heroes and their skill poses reviewed",skillPoses==6*CharacterDef.SkillCount);
                File.WriteAllText(Path.Combine(output,"motion-review.json"),"{\"passed\":true,\"version\":\""+Application.version+"\",\"guid\":\""+Application.buildGUID+"\",\"heroes\":6,\"skill_poses\":"+skillPoses+",\"foes\":70,\"scope\":\"Live Unity mesh rendering and fixed input steps; not iPhone hardware performance\"}");
                if(Screen.width==2556&&Screen.height==1179)yield return NaturalMotionMovie();
            }
            finally {game.ReviewStepping=false;JsonUtility.FromJsonOverwrite(saved,game.Session.Profile);game.Session.ClearQuestNotifications();game.ReturnTown();}
        }
    }
}
