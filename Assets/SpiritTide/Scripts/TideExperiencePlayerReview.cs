using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        private bool DialogueFits()
        {
            Canvas.ForceUpdateCanvases();Transform root=game.transform.Find("GameCanvas/SafeArea/Dialogue");
            foreach(Text t in root.GetComponentsInChildren<Text>())
            {
                if(t.preferredHeight>t.rectTransform.rect.height+2)return false;
                foreach(var clue in TideExperience.Data.clues)if(t.text.Contains(clue.id))return false;
            }
            return true;
        }
        private IEnumerator ReviewExperience()
        {
            Check("all ten encounter introductions and resolutions were played",game.Session.Profile.seenIntros==1023&&game.Session.Profile.seenOutros==1023);
            Check("all fifteen settings were rendered during the campaign",reviewedSettings.Count==15);
            game.ReturnTown();yield return null;yield return TapUi("town-figure-0");Check("visible NPC opens its own service",game.Session.Mode==TideMode.Dialogue&&game.transform.Find("GameCanvas/SafeArea/Dialogue").GetComponentInChildren<Text>().text.StartsWith("우라하라"));game.NavigateBack();yield return null;
            int gold=game.Session.Profile.gold;yield return TapUi("npc-kon");yield return TapUi("choice-0");yield return ReadStory();Check("Kon accepts an actual rescue follow-up",(game.Session.Profile.rescueReports&1)!=0&&game.Session.Profile.gold>=gold+100);
            string saved=JsonUtility.ToJson(game.Session.Profile);
            for(int act=1;act<=5;act++)for(int hero=0;hero<6;hero++)for(int phase=0;phase<4;phase++)
            {
                JsonUtility.FromJsonOverwrite(saved,game.Session.Profile);var p=game.Session.Profile;p.campaignAct=act;p.campaignStage=phase==0?1:phase==1?2:phase==2?4:6;p.quest=phase==0?0:phase==3?2:1;p.claimed=false;p.reportedActs=(1<<(act-1))-1;p.clearedDungeons=phase==3?(1<<(act*6))-1:(1<<((act-1)*6+Mathf.Max(0,p.campaignStage-1)))-1;p.hero=hero;p.uiScale=1.25f;game.ReturnTown();
                foreach(string npc in new[]{"urahara","tessai","kon","byakuya","kukaku"})
                {
                    Click("npc-"+npc);yield return null;Check("maximum dialogue size fits without internal IDs "+act+" / "+hero+" / "+phase+" / "+npc,DialogueFits());
                    if(hero==4&&phase==2&&npc=="kon")yield return Capture("52-act"+act+"-companion");game.NavigateBack();yield return null;
                }
            }
            JsonUtility.FromJsonOverwrite(saved,game.Session.Profile);game.ReturnTown();yield return null;yield return TapUi("npc-byakuya");yield return TapUi("choice-0");
            Text targetName=game.transform.Find("GameCanvas/SafeArea/BossHud").GetComponentInChildren<Text>(true);foreach(var foe in TideCampaign.Data.foes){targetName.text=foe.name+"  99999 / 99999";Check("full canonical target name fits "+foe.id,targetName.preferredHeight<=targetName.rectTransform.rect.height);}
            Check("final journal records returned names",VisibleText(TideExperience.Clue("returned_names")));
            for(int act=1;act<=5;act++){yield return TapUi("journal-act-"+act);while(game.Buttons["journal-prev"].interactable)yield return TapUi("journal-prev");yield return Capture("53-journal-act"+act);for(int page=1;page<6;page++){yield return TapUi("journal-next");Check("journal page remains readable "+act+" / "+page,VisibleText(TideCampaign.Dungeon(act,page+1).title));}}
            LeaveToTown();
            var checkpoint=new TideProfile{campaignSchema=1,campaignAct=1,campaignStage=2,roomCheckpoint=1,quest=1};TideCatalog.Sanitize(checkpoint);JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(checkpoint),game.Session.Profile);game.ReturnTown();game.StartBattle(false);yield return ReadStory();
            int guard=0;while(!game.Session.EnvironmentWarning&&guard++<600)game.Session.Step(1f/60,Vector2.zero,false);yield return Capture("54-environment-warning");Check("environment hazard has a visible warning",game.Session.EnvironmentWarning&&game.Session.WardActive);
            guard=0;while(!game.Session.NearInteraction&&guard++<300){Vector2 delta=game.Session.WardPosition-new Vector2(game.Session.X,game.Session.Y);game.Session.Step(.05f,delta.normalized,false);yield return null;}yield return TapUi("field-interact");Check("touch disables the actual environment device",!game.Session.WardActive&&!game.Session.EnvironmentWarning);yield return Capture("55-ward-disabled");
            JsonUtility.FromJsonOverwrite(saved,game.Session.Profile);game.ReturnTown();yield return ClipReview();JsonUtility.FromJsonOverwrite(saved,game.Session.Profile);game.ReturnTown();
        }
        private IEnumerator ClipReview()
        {
            var p=new TideProfile{quest=1};TideCatalog.Sanitize(p);JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(p),game.Session.Profile);game.ReturnTown();game.StartBattle(false);yield return ReadStory();
            string folder=Path.Combine(output,"ReviewFrames");Directory.CreateDirectory(folder);game.ReviewStepping=true;game.BeginReviewAudio();int action=0;int frames=360;
            Canvas canvas=game.GetComponentInChildren<Canvas>();Camera camera=Camera.main;var mode=canvas.renderMode;Camera previousCamera=canvas.worldCamera;float distance=canvas.planeDistance;var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
            var target=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);var texture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;camera.targetTexture=target;
                for(int frame=0;frame<frames;frame++)
                {
                    var s=game.Session;game.ReviewTime=frame/30f;
                    if(s.RoomCleared){TideCombatDriver.Tick(s,1f/30,false);if(s.AtGate)game.AdvanceRoom();}
                    else if(s.IsFighting)
                    {
                        var e=s.Target;float dx=e.X-s.X,dy=e.Y-s.Y;Vector2 move=new Vector2(Mathf.Abs(dx)>1.65f?Mathf.Sign(dx):0,Mathf.Abs(dy)>.2f?Mathf.Sign(dy):0);
                        if(e.Telegraph&&e.Timer<.2f)s.Dodge(new Vector2(0,e.Warning.y>=0?-1:1));
                        if(action%7==1&&s.Action==MoveKind.Basic&&s.ActionElapsed>.19f&&s.Request(MoveKind.Skill0))action++;
                        if(s.ActionRemaining<=0&&s.DodgeRemaining<=0&&Mathf.Abs(dx)<2.5f&&Mathf.Abs(dy)<.8f)
                        {
                            int step=action%7;
                            if(step==2){s.Request(MoveKind.Launcher);action++;}
                            else if(step==3){if(s.Z<=.1f)s.Jump();else if(s.Request(MoveKind.Basic))action++;}
                            else if(step==4){s.BeginHeavy();action++;}
                            else if(step==5){if(s.Request(MoveKind.Skill2))action++;else if(s.Cooldowns[2]>s.Clock)action++;}
                            else if(step==6){if(s.Request(MoveKind.Ultimate))action++;else if(s.Cooldowns[4]>s.Clock)action++;}
                            else {s.Request(MoveKind.Basic);action++;}
                        }
                        s.Step(1f/30,move,false);
                    }
                    yield return null;RenderTexture.active=target;Canvas.ForceUpdateCanvases();camera.Render();texture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);texture.Apply();File.WriteAllBytes(Path.Combine(folder,frame.ToString("D4")+".jpg"),texture.EncodeToJPG(65));
                }
                Check("input replay includes hit stop and linked actions",game.Session.HitStopEvents>0&&game.Session.ChainPeak>=2&&game.Session.CancelEvents>0);Check("input replay survives at normal starting health",game.Session.Hp>0);
                int cues=game.ExportReviewAudio(Path.Combine(output,"combat-review.wav"),frames/30f);Check("action and impact audio are recorded",cues>5);
                File.WriteAllText(Path.Combine(output,"combat-review.json"),"{\"fps\":30,\"frames\":"+frames+",\"sound_cues\":"+cues+",\"scope\":\"Input-only Unity replay with fixed simulation steps; not a performance recording\"}");
            }
            finally{game.ReviewStepping=false;camera.targetTexture=previousTarget;RenderTexture.active=previousActive;canvas.renderMode=mode;canvas.worldCamera=previousCamera;canvas.planeDistance=distance;target.Release();Destroy(target);Destroy(texture);}
            game.ReturnTown();yield return null;
        }
    }
}
