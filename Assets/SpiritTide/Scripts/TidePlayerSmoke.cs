using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke : MonoBehaviour
    {
        [Serializable] private class Row { public string name; public bool passed; }
        [Serializable] private class Report { public bool passed; public string device, scope,version,guid,runMode; public Row[] checks; public string[] errors; }
        private readonly List<Row> rows=new List<Row>();
        private readonly List<string> errors=new List<string>();
        private readonly HashSet<string> reviewedSettings=new HashSet<string>();
        private TideGame game; private string output;
        public void StartChecks(TideGame target,string folder)
        {game=target;output=Path.GetFullPath(folder);Directory.CreateDirectory(output);Application.logMessageReceived+=Log;StartCoroutine(RunGuarded());}
        private void Log(string message,string stack,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(message);}
        private void Check(string name,bool value){rows.Add(new Row{name=name,passed=value});if(!value)throw new InvalidOperationException(name);}
        private void Click(string id)
        {
            if(!game.Buttons.ContainsKey(id)||game.Buttons[id]==null)throw new InvalidOperationException("Missing UI button "+id);
            Button b=game.Buttons[id];if(b.gameObject.activeInHierarchy&&b.interactable&&b.navigation.mode!=Navigation.Mode.None)EventSystem.current.SetSelectedGameObject(b.gameObject);b.onClick.Invoke();
        }
        private void CheckHit(string id)
        {
            Canvas.ForceUpdateCanvases();Button button;
            if(!game.Buttons.TryGetValue(id,out button)||button==null)throw new InvalidOperationException("Missing live button "+id+" while "+game.Session.Mode);
            var pointer=new PointerEventData(EventSystem.current){position=HitPoint(button)};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
            Check("visible button receives raycast "+id+" at "+pointer.position+" hits="+(hits.Count>0?hits[0].gameObject.name:"none"),button.gameObject.activeInHierarchy&&button.interactable&&hits.Count>0&&hits[0].gameObject.GetComponentInParent<Button>()==button);
        }
        private static Vector2 HitPoint(Button button){var r=(RectTransform)button.transform;return RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.center));}
        private static Rect Bounds(RectTransform rect)
        {
            var corners=new Vector3[4];rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
        }
        private void CheckTouchLayout(string scene)
        {
            Button[] buttons=Array.FindAll(game.GetComponentsInChildren<Button>(),b=>(!b.name.StartsWith("target-")||b.name=="target-next")&&!b.name.StartsWith("town-figure-"));bool separate=true,clearOfJoystick=true,inside=true,largeEnough=true;
            Rect area=Bounds((RectTransform)game.transform.Find("GameCanvas/SafeArea"));
            for(int i=0;i<buttons.Length;i++)
            {
                Rect a=Bounds((RectTransform)buttons[i].transform);
                inside&=a.xMin>=area.xMin-.02f&&a.yMin>=area.yMin-.02f&&a.xMax<=area.xMax+.02f&&a.yMax<=area.yMax+.02f;
                Rect size=((RectTransform)buttons[i].transform).rect;largeEnough&=size.width>=84&&size.height>=84;
                for(int j=i+1;j<buttons.Length;j++)if(a.Overlaps(Bounds((RectTransform)buttons[j].transform))){separate=false;Debug.Log("Overlapping controls "+scene+": "+buttons[i].name+" / "+buttons[j].name);}
                if(game.Joystick.gameObject.activeInHierarchy&&a.Overlaps(Bounds((RectTransform)game.Joystick.transform)))clearOfJoystick=false;
            }
            Check("buttons do not overlap "+scene,separate);Check("joystick does not cover buttons "+scene,clearOfJoystick);
            Check("all controls remain in safe area "+scene,inside);Check("all touch targets meet minimum size "+scene,largeEnough);
        }
        private IEnumerator ReadStory()
        {
            int count=0;
            while(game.Session.Mode==TideMode.Story&&count++<8){Click("choice-0");yield return null;}
            Check("story sequence finishes without looping",game.Session.Mode!=TideMode.Story);
            Check("finished scene dismisses its continue prompt",!game.Session.Status.StartsWith("기록 대화"));
        }
        private IEnumerator RunGuarded()
        {
            var stack=new Stack<IEnumerator>();stack.Push(Checks());bool failed=false;
            while(stack.Count>0)
            {
                object current=null;bool moved=false;
                try{moved=stack.Peek().MoveNext();if(moved)current=stack.Peek().Current;}
                catch(Exception ex){errors.Add(ex.ToString());failed=true;}
                if(failed)break;
                if(!moved){stack.Pop();continue;}
                if(current is IEnumerator)stack.Push((IEnumerator)current);else yield return current;
            }
            bool passed=!failed&&errors.Count==0;
            File.WriteAllText(Path.Combine(output,"report.json"),JsonUtility.ToJson(new Report{passed=passed,runMode=Array.IndexOf(Environment.GetCommandLineArgs(),"-spirit-fx-review")>=0?"skills-only":Array.IndexOf(Environment.GetCommandLineArgs(),"-spirit-icons-review")>=0?"icons-only":Array.IndexOf(Environment.GetCommandLineArgs(),"-spirit-journey-review")>=0?"journey-only":Array.IndexOf(Environment.GetCommandLineArgs(),"-spirit-motion-review")>=0?"motion-only":"full",version=Application.version,guid=Application.buildGUID,device=SystemInfo.graphicsDeviceType+" / "+Screen.width+"x"+Screen.height,scope="Windows live UI rendering, pointer-event simulation, menu focus, campaign loot and input-only multi-enemy victories. Not iPhone OS touch, Metal or performance validation.",checks=rows.ToArray(),errors=errors.ToArray()},true));
            Application.logMessageReceived-=Log;Application.Quit(passed?0:1);
        }
        private IEnumerator Capture(string name)
        {
            // Hidden Windows players can skip swap-chain presentation. Render the actual
            // live canvas through the scene camera so QA never accepts a black capture.
            yield return null;
            Canvas canvas=game.GetComponentInChildren<Canvas>();Camera camera=Camera.main;
            RenderMode previousMode=canvas.renderMode;Camera previousCamera=canvas.worldCamera;
            float previousDistance=canvas.planeDistance;
            RenderTexture previousTarget=camera.targetTexture,previousActive=RenderTexture.active;
            RenderTexture target=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);
            Texture2D capture=new Texture2D(Screen.width,Screen.height,TextureFormat.RGB24,false);
            try
            {
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
                camera.targetTexture=target;Canvas.ForceUpdateCanvases();camera.Render();RenderTexture.active=target;
                CheckTouchLayout(name);
                foreach(Text label in game.GetComponentsInChildren<Text>())
                    Check("text fits "+name+" / "+label.text.Replace("\n"," "),label.preferredHeight<=label.rectTransform.rect.height+2);
                capture.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);capture.Apply();
                Color32[] pixels=capture.GetPixels32();int colored=0;
                for(int i=0;i<pixels.Length;i+=31)if(pixels[i].r>20||pixels[i].g>40||pixels[i].b>45)colored++;
                Check("rendered canvas contains visible content "+name,colored>pixels.Length/310);
                File.WriteAllBytes(Path.Combine(output,name+".png"),capture.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture=previousTarget;RenderTexture.active=previousActive;
                canvas.renderMode=previousMode;canvas.worldCamera=previousCamera;canvas.planeDistance=previousDistance;
                target.Release();Destroy(target);Destroy(capture);Canvas.ForceUpdateCanvases();
            }
        }
        private IEnumerator TapUi(string id){yield return null;Tap(id);yield return null;}
        private void DriveNativeTick(){TideCombatDriver.Tick(game.Session,1f/60,false);if(game.Session.AtGate)game.AdvanceRoom();}
        private IEnumerator FightDungeon()
        {
            int frame=0;
            while((game.Session.IsFighting||game.Session.Mode==TideMode.Story)&&frame<60000)
            {
                if(game.Session.Mode==TideMode.Story){yield return ReadStory();continue;}
                for(int i=0;i<180&&game.Session.IsFighting;i++){DriveNativeTick();frame++;}
                yield return null;
            }
            Check("native input-only dungeon clear "+game.Session.Dungeon.index,game.Session.Mode==TideMode.Result&&game.Session.Hp>0);
            yield return null;
        }
        private IEnumerator Checks()
        {
            yield return new WaitForSecondsRealtime(.4f);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-spirit-fx-review")>=0){yield return ReviewSkillEffects();yield break;}
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-spirit-icons-review")>=0){yield return ReviewSkillIcons();yield break;}
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-spirit-journey-review")>=0){yield return ReviewJourney();yield break;}
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-spirit-motion-review")>=0){yield return ReviewNaturalMotion();yield break;}
            yield return ReviewOpening();
            yield return ReviewUi();
            Check("town boot and one listener",game.Session.Mode==TideMode.Town&&FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length==1);
            yield return Capture("01-town");yield return TapUi("npc-urahara");yield return Capture("05-dialogue");yield return TapUi("choice-0");yield return ReadStory();
            Check("quest accepted from NPC",game.Session.Quest==QuestState.Active&&game.Session.Mode==TideMode.Town);
            yield return TapUi("npc-tessai");yield return TapUi("choice-0");yield return TapUi("gear-1");yield return TapUi("equip");Check("selected gear actually equips",game.Session.Profile.powerGear==2);yield return TapUi("lock");Check("selected gear favorite",TideProgress.Gear(game.Session.Profile,2).favorite);yield return Capture("06-equipment");LeaveToTown();
            yield return TapUi("npc-kukaku");yield return TapUi("choice-0");yield return Capture("30-map-locked");Check("future dungeon remains locked",!game.Buttons["dungeon-6"].interactable);yield return TapUi("dungeon-1");yield return Capture("07-departure");
            Check("departure story prevents combat",game.Session.Mode==TideMode.Story&&!game.Session.IsFighting);
            Button first=game.Buttons["choice-0"];first.onClick.Invoke();first.onClick.Invoke();Check("duplicate story click advances once",game.StoryPage==1);yield return ReadStory();
            Check("multiple troop foes spawn",game.Session.Enemies.Count>=2);yield return Capture("03-combat");reviewedSettings.Add(game.Session.Dungeon.background);yield return Capture("50-scene-"+game.Session.Dungeon.background);CheckHit("target-0");yield return TapUi("target-next");Check("tap selects a different live foe",game.Session.Target==game.Session.Enemies[1]);
            game.Session.Pause();yield return null;float clock=game.Session.Clock;Click("retreat");yield return Capture("22-confirm-retreat");yield return TapUi("cancel-retreat");Check("cancel retreat preserves room and clock",game.Session.Mode==TideMode.Pause&&game.Session.Clock==clock&&game.Session.Profile.roomCheckpoint==0);yield return TapUi("resume");
            game.Session.Hp=80;yield return null;yield return TapUi("potion");Check("potion label matches actual level-scaled healing",game.Session.Hp==127&&game.Session.Profile.potions==2);
            int frame=0;while(!game.Session.RoomCleared&&game.Session.IsFighting&&frame++<12000){for(int i=0;i<60&&!game.Session.RoomCleared;i++)DriveNativeTick();yield return null;}
            Check("room exit opens with saved checkpoint",game.Session.RoomCleared&&game.Session.Profile.roomCheckpoint==1);yield return Capture("31-room-clear");
            while(!game.Session.NearInteraction){game.Session.Step(.05f,new Vector2(Mathf.Sign(game.Session.EchoX-game.Session.X),0),false);yield return null;}yield return TapUi("field-interact");Check("field rescue is a real one-time interaction",(game.Session.Profile.rescuedDungeons&1)!=0&&!game.Session.EchoAvailable);yield return Capture("51-rescue");
            Check("rescued spirit answers on the field without blocking combat",
                game.Session.IsFighting&&VisibleText(TideDialogue.Lines("a1_d1_answer",game.Session.Profile.hero)[0].text)
                &&TideDialogue.Seen(game.Session.Profile,"a1_d1_answer")!=null);
            while(!game.Session.AtGate){game.Session.Step(.05f,Vector2.right,false);yield return null;}yield return null;yield return TapUi("gate");Check("touch gate enters next room",game.Session.RoomIndex==1);yield return Capture("32-next-room");
            yield return FightDungeon();yield return Capture("33-first-loot");yield return ReviewResultConversation();yield return TapUi("result-gear");yield return Capture("34-loot-inventory");Check("new loot is selected on its visible page",game.Buttons["gear-0"].GetComponentInChildren<Text>().text.Contains(TideProgress.Gear(game.Session.Profile,game.Session.LastGearUid).name));yield return TapUi("equip");yield return TapUi("forge");yield return Capture("35-forge");int gold=game.Session.Profile.gold;yield return TapUi("upgrade");Check("native forge spends real reward resources",game.Session.Profile.gold<gold);yield return Capture("36-forged");LeaveToTown();
            for(int act=1;act<=5;act++)
            {
                if(act>1){yield return TapUi("npc-urahara");yield return TapUi("choice-0");yield return ReadStory();}
                int start=act==1?2:1;
                for(int n=start;n<=6;n++)
                {
                    TideCombatDriver.Prepare(game.Session);yield return TapUi("npc-kukaku");yield return TapUi("choice-0");Check("town departure map follows current act "+act+" / "+n,VisibleText("균열 지도 · Act "+act));yield return TapUi("depart");yield return ReadStory();
                    if(reviewedSettings.Add(game.Session.Dungeon.background))yield return Capture("50-scene-"+game.Session.Dungeon.background);
                    if(n==1)yield return ReviewFirstLantern(act);
                    if(n==3||n==6)
                    {
                        // Reach the final encounter with normal combat, then capture the actual boss.
                        int limit=0;while(game.Session.IsFighting&&game.Session.RoomIndex<game.Session.Dungeon.rooms.Length-1&&limit++<2000){for(int i=0;i<120&&game.Session.RoomIndex<game.Session.Dungeon.rooms.Length-1;i++)DriveNativeTick();yield return null;}
                        if(game.Session.Mode==TideMode.Story)yield return ReadStory();game.Session.Step(.05f,Vector2.zero,false);yield return Capture("40-act"+act+"-boss"+n);
                        if(act==5&&n==6)
                        {
                            int wait=0;while(!game.Session.Telegraph&&wait++<180)game.Session.Step(1f/60,Vector2.zero,false);
                            yield return null;Check("future preview is visible before lock",!game.Session.Target.FutureLocked&&VisibleText("미래"));yield return Capture("40-future-preview");
                            wait=0;while(!game.Session.Target.FutureLocked&&wait++<120)game.Session.Step(1f/60,Vector2.zero,false);
                            yield return null;Check("future lock changes the visible cue",game.Session.Target.FutureLocked&&VisibleText("확정된 미래"));yield return Capture("40-future-locked");
                        }
                    }
                    yield return FightDungeon();
                    if(n==6){yield return Capture("41-act"+act+"-loot");break;}
                    if(n!=3)yield return ReviewResultConversation();
                    yield return TapUi("next-dungeon");
                    if(n+1==2||n+1==4||n+1==5)
                        Check("routine departure keeps live controls "+act+" / "+(n+1),game.Session.IsFighting&&game.Session.Dungeon.number==n+1);
                    yield return ReadStory();game.ReturnTown();
                }
                int before=game.Session.Profile.gold;
                yield return TapUi("next-dungeon");
                Check("boss result opens report directly without awarding it early "+act,
                    game.Session.Mode==TideMode.Story&&game.Session.Quest==QuestState.Ready&&game.Session.Profile.gold==before);
                yield return Capture("42-act"+act+"-report");
                if(act==1)
                {
                    yield return TapUi("choice-2");
                    var resumed=TideStorage.Load(Path.Combine(output,"qa-profile.json"),out _);
                    Check("canceling report keeps the saved act ready",game.Session.Mode==TideMode.Town&&resumed.campaignAct==1
                        &&resumed.quest==(int)QuestState.Ready&&resumed.gold==before&&TideDialogue.Seen(resumed,"a1_report")==null);
                    yield return TapUi("npc-urahara");yield return TapUi("choice-0");
                }
                var reportLines=TideDialogue.Lines("a"+act+"_report",game.Session.Profile.hero);
                for(int page=1;page<reportLines.Length;page++)yield return TapUi("choice-0");
                Button report=game.Buttons["choice-0"];report.onClick.Invoke();report.onClick.Invoke();yield return null;
                Check("report once unlocks next act "+act,game.Session.Profile.gold==before+250+(act-1)*100&&game.Session.Profile.reportedActs==((1<<act)-1));
                if(act==5)
                {
                    Check("final report naturally leads to the original caller's epilogue",
                        game.Session.Mode==TideMode.Story&&VisibleText(TideDialogue.Scene("a5_epilogue").title));
                    yield return TapUi("choice-2");
                    game.Session.Mode=TideMode.Title;game.Session.Revision++;yield return null;
                    Check("unfinished epilogue is available on continue",VisibleText("후일담 이어보기"));
                    yield return TapUi("title-start");yield return Capture("42-ending-epilogue");yield return ReadStory();
                    Check("epilogue is remembered without repeating the final reward",
                        game.Session.Profile.gold==before+650&&TideDialogue.Seen(game.Session.Profile,"a5_epilogue")!=null);
                }
            }
            Check("native five-act campaign complete",game.Session.CampaignFinished&&game.Session.Profile.clearedDungeons==TideProgress.AllDungeons);yield return Capture("43-ending-town");
            yield return TapUi("npc-byakuya");yield return TapUi("choice-0");yield return Capture("44-journal");LeaveToTown();yield return TapUi("npc-kukaku");yield return TapUi("choice-0");yield return TapUi("map-act-1");yield return Capture("45-replay-map");yield return TapUi("dungeon-1");yield return ReadStory();Check("replay distinct from new progress",game.Session.IsReplay);yield return FightDungeon();Check("replay cannot duplicate first loot",game.Session.LastGearUid==0);yield return Capture("46-replay-result");game.ReturnTown();
            yield return ReviewExperience();
            yield return ReviewDialogueLibrary();
            yield return ReviewQuestGameplay();
            yield return ReviewConvenience();
            yield return ReviewTouchInput();
            yield return ReviewSkillEffects();
            yield return ReviewSkillIcons();
            yield return ReviewJourney();
            yield return ReviewNaturalMotion();
            Check("player log has no errors",errors.Count==0);
        }
    }
}
