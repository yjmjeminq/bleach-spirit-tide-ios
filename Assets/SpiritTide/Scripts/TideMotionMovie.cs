using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        [Serializable] private class MotionFrameRow {public int hero,frame,serial,action,combo,facing;public float clock,phase,foot_error;public TideRigPoint root,near_foot,far_foot;public bool near_contact,far_contact;public TideRigPoint[] joints;}
        [Serializable] private class MotionTrace {public string version,guid;public MotionFrameRow[] frames;}
        private static TideRigPoint MotionPoint(Vector2 p)=>new TideRigPoint{x=p.x,y=p.y};
        private IEnumerator ReviewFoeMotion()
        {
            game.ReturnTown();game.StartBattle(true);if(!game.Session.TrainingAttacks)game.Session.ToggleTrainingAttacks();
            var families=new HashSet<TideFoeMotionGraphic.Anatomy>();int count=0;
            foreach(var def in TideCampaign.Data.foes)
            {
                // A training fixture swaps only the encounter identity. Movement,
                // telegraphs, attacks and player damage use the normal enemy AI.
                var e=game.Session.Target;e.Def=def;e.Uid=10000+count;e.X=1.8f;e.Y=0;e.Hp=e.MaxHp=def.base_hp;e.Attacks=0;e.DamageSerial=0;
                e.Stun=e.Stagger=e.Lift=e.Recovery=e.Exposed=0;e.Telegraph=false;e.Timer=.05f;game.Session.Revision++;
                yield return MotionAdvance(3,Vector2.zero);Canvas.ForceUpdateCanvases();var body=game.GetComponentInChildren<TideFoeMotionGraphic>();
                Check("canonical foe has a bounded live motion surface "+def.id,body!=null&&body.sprite.name==def.id&&body.VertexCount==425);CheckHit("target-0");count++;
                if(families.Add(body.BodyType))
                {
                    yield return MotionAdvance(32,Vector2.zero);yield return Capture("110-motion-foe-"+body.BodyType+"-tell");
                    yield return MotionAdvance(210,Vector2.zero);Check("normal AI drives the enemy motion events "+body.BodyType,e.Attacks>0);
                    yield return Capture("111-motion-foe-"+body.BodyType+"-follow");
                    Vector2 sample=body.Deform(new Vector2(.2f,.72f));game.Session.Pause();yield return MotionAdvance(5,Vector2.right);
                    Check("pause freezes enemy surface "+body.BodyType,body.Deform(new Vector2(.2f,.72f))==sample);game.Session.Resume();
                }
            }
            Check("all seventy canonical enemies use continuous motion",count==70&&families.Count==7);game.ReturnTown();yield return null;
        }
        private IEnumerator ReviewMotionCamera()
        {
            game.ReturnTown();var profile=new TideProfile{hero=5,quest=1,campaignSchema=1,campaignAct=1,campaignStage=1};TideCatalog.Sanitize(profile);
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(profile),game.Session.Profile);game.ReturnTown();game.StartBattle(false);yield return ReadStory();yield return null;
            UnityEngine.UI.Image backdrop=null;foreach(var image in game.GetComponentsInChildren<UnityEngine.UI.Image>())if(image.name=="Scenery")backdrop=image;
            Check("camera review finds the actual painted backdrop",backdrop!=null);
            foreach(int side in new[]{1,-1})
            {
                game.Session.X=side>0?10.5f:-6;yield return MotionAdvance(160,Vector2.zero);var rect=backdrop.rectTransform;
                Check("painted scenery covers the full world at map edge "+side,rect.offsetMin.x<=0&&rect.offsetMax.x>=0&&rect.offsetMin.y<=0&&rect.offsetMax.y>=0);
                yield return Capture("112-motion-camera-edge-"+side);
                Vector2 at=rect.anchoredPosition;game.Session.Pause();yield return MotionAdvance(5,Vector2.right);
                Check("pause freezes the follow camera at map edge "+side,rect.anchoredPosition==at);game.Session.Resume();
            }
            game.ReturnTown();yield return null;
        }
        private IEnumerator NaturalMotionMovie()
        {
            string folder=Path.Combine(output,"MotionFrames");Directory.CreateDirectory(folder);game.BeginReviewAudio();const int perHero=225;int index=0;var trace=new List<MotionFrameRow>();
            Canvas canvas=game.GetComponentInChildren<Canvas>();Camera camera=Camera.main;var mode=canvas.renderMode;Camera oldCamera=canvas.worldCamera;float plane=canvas.planeDistance;
            var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;camera.targetTexture=rt;
                for(int hero=0;hero<6;hero++)
                {
                    game.ReturnTown();game.Session.SelectHero(hero);game.StartBattle(true);game.Session.EnemyX=6;game.Session.EnemyY=0;int initialSerial=game.Session.AttackSerial,lastSerial=initialSerial;var actions=new HashSet<MoveKind>();var combos=new HashSet<int>();
                    for(int frame=0;frame<perHero;frame++,index++)
                    {
                        var s=game.Session;game.ReviewTime=index/30f;
                        Vector2 move=frame>=8&&frame<45?Vector2.right:frame>=45&&frame<63?Vector2.left:frame>=63&&frame<75?Vector2.up:Vector2.zero;
                        if(frame==76){s.EnemyX=s.X+2;s.EnemyY=s.Y;}
                        if(frame==80||frame==96||frame==112)s.Request(MoveKind.Basic);
                        if(frame==128)s.Request(MoveKind.Heavy);
                        if(frame==148)s.Jump();if(frame==153)s.Request(MoveKind.Basic);
                        if(frame==171)s.Dodge(Vector2.left);if(frame==183)s.Request(MoveKind.Ultimate);
                        s.Step(1f/30,move,false);yield return null;
                        if(s.AttackSerial!=lastSerial){lastSerial=s.AttackSerial;actions.Add(s.Action);if(s.Action==MoveKind.Basic)combos.Add(s.Combo);}
                        var motion=game.NaturalMotion;var joints=new TideRigPoint[18];for(int b=0;b<18;b++)joints[b]=MotionPoint(motion.Positions[b]);
                        trace.Add(new MotionFrameRow{hero=hero,frame=frame,serial=s.AttackSerial,action=(int)s.Action,combo=s.Combo,facing=s.Facing,clock=s.PresentationClock,phase=motion.Phase,foot_error=motion.FootError,root=MotionPoint(((RectTransform)game.ArticulatedActor.transform.parent).anchoredPosition),near_foot=MotionPoint(motion.Feet[0]),far_foot=MotionPoint(motion.Feet[1]),near_contact=motion.Contacts[0],far_contact=motion.Contacts[1],joints=joints});
                        RenderTexture.active=rt;Canvas.ForceUpdateCanvases();camera.Render();texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();
                        File.WriteAllBytes(Path.Combine(folder,index.ToString("D4")+".jpg"),texture.EncodeToJPG(78));
                    }
                    Check("timed motion sequence includes actual actions and impacts "+hero,game.Session.AttackSerial-initialSerial>=6&&game.Session.HitStopEvents>0&&combos.Count==3&&actions.Contains(MoveKind.Basic)&&actions.Contains(MoveKind.Heavy)&&actions.Contains(MoveKind.Air)&&actions.Contains(MoveKind.Ultimate));
                }
                File.WriteAllText(Path.Combine(output,"motion-trace.json"),JsonUtility.ToJson(new MotionTrace{version=Application.version,guid=Application.buildGUID,frames=trace.ToArray()}));
                int cues=game.ExportReviewAudio(Path.Combine(output,"motion-review.wav"),index/30f);
                File.WriteAllText(Path.Combine(output,"motion-movie.json"),"{\"fps\":30,\"frames\":"+index+",\"heroes\":6,\"sound_cues\":"+cues+",\"scope\":\"Fixed-step native Unity locomotion, combo, heavy, jump-air, dodge and ultimate review; not an iPhone recording or FPS measurement\"}");
                Check("six complete motion sequences are rendered with audio",index==1350&&cues>=12);
            }
            finally {camera.targetTexture=previous;RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=plane;rt.Release();Destroy(rt);Destroy(texture);}
        }
    }
}
