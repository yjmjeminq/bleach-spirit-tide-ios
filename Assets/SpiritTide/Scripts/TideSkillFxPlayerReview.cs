using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        private IEnumerator StepSkillSeconds(float duration)
        {
            float until=game.Session.Clock+duration;int guard=0;
            while(game.Session.Clock<until&&guard++<600){for(int i=0;i<4&&game.Session.Clock<until;i++)game.Session.Step(1f/60,Vector2.zero,false);yield return null;}
            Check("effect review simulation advances without a loop",game.Session.Clock>=until);
        }
        private IEnumerator ReviewSkillEffects()
        {
            string saved=JsonUtility.ToJson(game.Session.Profile);int casts=0,peak=0;game.ReviewStepping=true;
            try
            {
                for(int hero=0;hero<6;hero++)
                {
                    game.ReturnTown();yield return TapUi("hero-"+hero);game.StartBattle(true);yield return null;
                    int nodes=game.GetComponentsInChildren<TideSkillFxGraphic>(true).Length;
                    for(int slot=0;slot<5;slot++)
                    {
                        game.Session.X=0;game.Session.Y=0;game.Session.EnemyX=2.3f;game.Session.EnemyY=0;
                        yield return TapUi("skill-"+slot);float duration=game.Session.ActionDuration;
                        yield return StepSkillSeconds(duration*.48f);Canvas.ForceUpdateCanvases();
                        string id=game.Session.Hero.Skill(slot).id;var definition=TideSkillFx.Get(hero,slot);
                        Check("real skill cast selects its own effect "+id,game.LastSkillFxId==id&&game.SkillFxActiveCount>0);
                        int vertices=0;bool bounded=true,nonBlocking=true;foreach(var mesh in game.GetComponentsInChildren<TideSkillFxGraphic>())
                        {vertices+=mesh.VertexCount;bounded&=mesh.VertexCount<=TideSkillFx.MaxVerticesPerLayer;nonBlocking&=!mesh.raycastTarget;}
                        peak=Mathf.Max(peak,vertices);Check("skill effect has bounded visible nonblocking geometry "+id,vertices>30&&bounded&&nonBlocking);
                        CheckHit("attack");yield return Capture("90-skill-"+id);casts++;
                        if(hero==4&&slot==1)Check("ash garden follows an actually installed cloud",game.Session.CloudTime[0]>0||game.Session.CloudTime[1]>0);
                        if(hero==4&&slot==3)Check("detonation removes actual cloud state",game.Session.CloudTime[0]==0&&game.Session.CloudTime[1]==0);
                        yield return StepSkillSeconds(duration+definition.tail+.1f);
                        Check("completed cast releases its pooled visual "+id,game.SkillFxActiveCount==0);
                        Check("repeated skill casts reuse the mesh pool "+id,game.GetComponentsInChildren<TideSkillFxGraphic>(true).Length==nodes);
                    }
                    game.Session.Profile.reducedEffects=true;game.Session.Revision++;yield return null;
                    yield return TapUi("skill-4");yield return StepSkillSeconds(.5f);yield return Capture("91-reduced-"+hero);
                    Check("reduced mode retains the hero ultimate motif "+hero,game.LastSkillFxId==game.Session.Hero.ultimate.id&&game.SkillFxActiveCount>0);
                    yield return TapUi("pause");float clock=game.Session.Clock;string motif=game.LastSkillFxId;yield return null;
                    Check("pause preserves active effect time and identity "+hero,game.Session.Clock==clock&&game.LastSkillFxId==motif);yield return TapUi("resume");
                    game.Session.Profile.reducedEffects=false;game.Session.Revision++;game.ReturnTown();yield return null;
                    Check("return to town clears all battle skill meshes "+hero,game.SkillFxActiveCount==0&&game.GetComponentsInChildren<TideSkillFxGraphic>().Length==0);
                }
                Check("all thirty distinct playable skills rendered",casts==30);
                File.WriteAllText(Path.Combine(output,"skill-effects-review.json"),"{\"passed\":true,\"version\":\""+Application.version+"\",\"guid\":\""+Application.buildGUID+"\",\"cast_skills\":30,\"hero_motifs\":6,\"peak_vertices\":"+peak+",\"pool_capacity\":"+game.SkillFxPoolCapacity+",\"scope\":\"Windows actual Unity mesh rendering and input events; not iPhone GPU performance\"}");
                if(Screen.width==2556&&Screen.height==1179)yield return SkillEffectsMovie();
            }
            finally {game.ReviewStepping=false;JsonUtility.FromJsonOverwrite(saved,game.Session.Profile);game.Session.ClearQuestNotifications();game.ReturnTown();}
        }
        private IEnumerator SkillEffectsMovie()
        {
            string folder=Path.Combine(output,"SkillFrames");Directory.CreateDirectory(folder);game.BeginReviewAudio();const int framesPerHero=72;int index=0;
            Canvas canvas=game.GetComponentInChildren<Canvas>();Camera camera=Camera.main;var mode=canvas.renderMode;Camera oldCamera=canvas.worldCamera;float plane=canvas.planeDistance;
            var previous=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;camera.targetTexture=rt;
                for(int hero=0;hero<6;hero++)
                {
                    game.ReturnTown();Click("hero-"+hero);game.StartBattle(true);game.Session.X=0;game.Session.EnemyX=2.3f;game.ReviewTime=index/30f;Click("skill-4");
                    for(int frame=0;frame<framesPerHero;frame++,index++)
                    {
                        game.ReviewTime=index/30f;game.Session.Step(1f/30,Vector2.zero,false);yield return null;
                        if(frame==3)Check("timed ultimate showcase uses the current hero effect "+hero,game.LastSkillFxId==game.Session.Hero.ultimate.id&&game.SkillFxActiveCount>0);
                        RenderTexture.active=rt;Canvas.ForceUpdateCanvases();camera.Render();texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();
                        File.WriteAllBytes(Path.Combine(folder,index.ToString("D4")+".jpg"),texture.EncodeToJPG(65));
                    }
                }
                int cues=game.ExportReviewAudio(Path.Combine(output,"skill-effects-review.wav"),index/30f);
                File.WriteAllText(Path.Combine(output,"skill-effects-movie.json"),"{\"fps\":30,\"frames\":"+index+",\"sound_cues\":"+cues+",\"scope\":\"Fixed-step Unity skill showcase with synthesized game cue audio, not an iPhone recording or FPS measurement\"}");
                Check("six ultimates have a rendered timed showcase with audio",index==432&&cues>=6);
            }
            finally {camera.targetTexture=previous;RenderTexture.active=active;canvas.renderMode=mode;canvas.worldCamera=oldCamera;canvas.planeDistance=plane;rt.Release();Destroy(rt);Destroy(texture);}
        }
    }
}
