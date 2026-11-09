using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame : MonoBehaviour
    {
        public TideSession Session { get; private set; }
        public TideJoystick Joystick { get; private set; }
        public readonly Dictionary<string, Button> Buttons = new Dictionary<string, Button>();
        public TideHold AttackHold { get; private set; }
        private TideHold heavyHold;
        private RectTransform safe, world, panel, actor, enemyActor, warning, slash;
        private GameObject menuRoot, dialogueRoot, controlsRoot, townRoot, battleRoot;
        private Image actorImage, enemyImage, warningImage, slashImage;
        private Text titleText, statusText, hpText, enemyText, actionText, resourceText, goldText, questText;
        private Image hpFill, mpFill, enemyFill;
        private Button[] heroButtons = new Button[6], skillButtons = new Button[5];
        private Text[] skillLabels = new Text[5];
        private Sprite circle, square;
        private Font font, bold;
        private readonly Sprite[] heroSprites = new Sprite[6];
        private Sprite[] bossSprites = new Sprite[10];
        private string savePath, lastSaveNotice;
        private int viewRevision = -1, damageSerial, lastWidth, lastHeight;
        private Rect lastSafe;
        private Rect? smokeSafeArea;
        private TideMode lastMode = (TideMode)(-1);
        private string dialogueNpc = "urahara";
        private Vector2 keyboardMove;
        private bool blockHeldAttackUntilRelease;
        private StoryLine[] storyLines;
        private int storyPage;
        public int StoryPage => storyPage;
        private Action storyFinish, storyCancel;
        private string storyEndLabel, storyCancelLabel;
        private string storyRecordId;
        private int storyHero;
        private int storyEpoch;
        private readonly Color ink = new Color(.91f, .95f, .93f), muted = new Color(.67f, .79f, .78f);
        private readonly Color bg = new Color(.055f, .115f, .145f), card = new Color(.09f, .18f, .21f), gold = new Color(.9f, .72f, .4f);
        private AudioSource audioSource;
        private AudioClip tapClip;
        public bool SmokeMode { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (FindFirstObjectByType<TideGame>() == null) new GameObject("SpiritTide").AddComponent<TideGame>();
        }
        private void Awake()
        {
            Application.targetFrameRate = 60; Application.runInBackground = true;
            Input.multiTouchEnabled = true;
            TideCatalog.Load();TideStory.Load();
            string[] args = Environment.GetCommandLineArgs();
            string smokeOutput = null;
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "-spirit-smoke") smokeOutput = args[i + 1];
            SmokeMode = !string.IsNullOrEmpty(smokeOutput);
            if(SmokeMode&&Array.IndexOf(args,"-spirit-phone-layout")>=0)
                smokeSafeArea=new Rect(Screen.width*(177f/2556),Screen.height*(63f/1179),Screen.width*(2202f/2556),Screen.height*(1116f/1179));
            savePath = SmokeMode ? Path.Combine(smokeOutput, "qa-profile.json") : Path.Combine(Application.persistentDataPath, "SpiritTide", "profile-v1.json");
            TideProfile profile = SmokeMode ? new TideProfile() : TideStorage.Load(savePath, out lastSaveNotice);
            Session = new TideSession(profile);
            Session.Mode = TideMode.Title;
            font = Resources.Load<Font>("SpiritTide/UI/Pretendard-Regular");
            bold = Resources.Load<Font>("SpiritTide/UI/Pretendard-SemiBold");
            if (font == null || bold == null) throw new InvalidOperationException("Bundled Korean fonts are missing.");
            for (int i = 0; i < 6; i++) heroSprites[i] = Resources.Load<Sprite>("SpiritTide/Art/" + TideCatalog.Characters[i].id + "_bikini_v2");
            for(int i=0;i<10;i++)bossSprites[i]=Resources.Load<Sprite>("SpiritTide/Art/"+TideCatalog.Bosses[i].id);
            TideArt.Load();mapAct=Session.Profile.campaignAct;
            foreach (Sprite image in heroSprites) if (image == null) throw new InvalidOperationException("A playable character image is missing.");
            foreach (Sprite image in bossSprites) if (image == null) throw new InvalidOperationException("A boss image is missing.");
            MakeShapes(); MakeAudio(); BuildUI(); ApplySafeArea(); Refresh();
            if (!string.IsNullOrEmpty(lastSaveNotice)) Session.Status = lastSaveNotice;
            if (SmokeMode) gameObject.AddComponent<TidePlayerSmoke>().StartChecks(this, smokeOutput);
        }
        private void MakeShapes()
        {
            square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[64 * 64];
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                pixels[y * 64 + x] = new Color(1, 1, 1, Mathf.Clamp01((31 - Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f))) * .8f));
            texture.SetPixels(pixels); texture.Apply(false, true);
            circle = Sprite.Create(texture, new Rect(0, 0, 64, 64), new Vector2(.5f, .5f), 64);
        }
        private void MakeAudio()
        {
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.volume = .13f;
            tapClip = Tone(580, .055f);
        }
        private AudioClip Tone(float frequency, float seconds)
        {
            int n = Mathf.RoundToInt(22050 * seconds); float[] samples = new float[n];
            for (int i = 0; i < n; i++) samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * i / 22050) * (1 - i / (float)n) * .35f;
            AudioClip clip = AudioClip.Create("SpiritTideSynth", n, 1, 22050, false); clip.SetData(samples, 0); return clip;
        }
        private void Update()
        {
            if (Session == null) return;
            ApplySafeArea();
            ApplyHudPreferences();
            MaybeEncounterStory();MaybeQuestStory();ReadKeyboard();
            Vector2 movement = Joystick.Value.sqrMagnitude > .001f ? Joystick.Value : keyboardMove;
            if(!AttackHold.Held&&!Input.GetKey(KeyCode.Z))blockHeldAttackUntilRelease=false;
            if(!SmokeMode||!ReviewStepping)Session.Step(Time.unscaledDeltaTime, movement, !blockHeldAttackUntilRelease&&(AttackHold.Held || Input.GetKey(KeyCode.Z)));
            if (lastMode != Session.Mode || viewRevision != Session.Revision) Refresh();
            MaybeEncounterStory();MaybeQuestStory();DrawWorld();UpdateHud();
            if(lastPersistRevision!=Session.SaveRevision){Persist();lastPersistRevision=Session.SaveRevision;}
            UpdateFeedback(SmokeMode&&ReviewStepping?1f/30:Time.unscaledDeltaTime);
            audioSource.volume=Session.Profile.soundEnabled?.13f:0;
            if (damageSerial != Session.DamageEvents)
            { bool landed=Session.DamageEvents>damageSerial;damageSerial = Session.DamageEvents; if (landed&&audioSource != null) PlayImpactSound(); }
        }
        private void ReadKeyboard()
        {
            keyboardMove = Vector2.zero;
            if(HandleMenuKeyboard())return;
            if(Input.GetKeyDown(KeyCode.H)){ShowHelp();return;}
            if (Session.Mode == TideMode.Town || Session.IsFighting)
                keyboardMove = new Vector2((Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                    (Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.DownArrow) ? 1 : 0));
            if (Input.GetKeyDown(KeyCode.Escape))
            {ToggleMainMenu();return;}
            if (Session.Mode == TideMode.Town)
            {
                if (Input.GetKeyDown(KeyCode.E)) Talk(NearestNpc());
                if (Input.GetKeyDown(KeyCode.I)) OpenMenu(TideMode.Inventory);
                if (Input.GetKeyDown(KeyCode.M)) OpenMenu(TideMode.Missions);
                if (Input.GetKeyDown(KeyCode.Q)) OpenQuestBook();
                return;
            }
            if (!Session.IsFighting) return;
            if(Input.GetKeyDown(KeyCode.E)){if(Session.NearInteraction)InteractField();else if(Session.RoomCleared)AdvanceRoom();return;}
            if (Input.GetKeyDown(KeyCode.Z)){blockHeldAttackUntilRelease=false;Session.Request(Input.GetKey(KeyCode.UpArrow) ? MoveKind.Launcher : MoveKind.Basic);}
            if (Input.GetKeyDown(KeyCode.X)) Session.BeginHeavy();
            if (Input.GetKeyUp(KeyCode.X)) Session.ReleaseHeavy();
            if (Input.GetKeyDown(KeyCode.C)) Session.Jump();
            if (Input.GetKeyDown(KeyCode.T)) Session.ToggleTrainingAttacks();
            if (Input.GetKeyDown(KeyCode.Space)) Session.Dodge(keyboardMove);
            if (Input.GetKeyDown(KeyCode.V) && Session.UsePotion()) Persist();
            KeyCode[] keys = { KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.F, KeyCode.R };
            for (int i = 0; i < keys.Length; i++) if (Input.GetKeyDown(keys[i])) Session.Request((MoveKind)((int)MoveKind.Skill0 + i));
        }
        public void ResetInput()
        {
            Joystick?.ResetInput(); AttackHold?.ResetInput(); heavyHold?.ResetInput(); keyboardMove = Vector2.zero;
            blockHeldAttackUntilRelease=true;
        }
        private void ApplySafeArea()
        {
            if (safe == null) return;
            Rect currentSafe=smokeSafeArea??Screen.safeArea;
            if (Screen.width == lastWidth && Screen.height == lastHeight && currentSafe == lastSafe) return;
            bool geometryChanged=lastWidth>0&&lastHeight>0;
            lastWidth = Screen.width; lastHeight = Screen.height; lastSafe = currentSafe;
            Rect area = TideLayout.NormalizedSafeArea(lastSafe, lastWidth, lastHeight);
            safe.anchorMin = area.min; safe.anchorMax = area.max; safe.offsetMin = safe.offsetMax = Vector2.zero;
            if(geometryChanged&&Session.IsFighting)Session.Pause();
            ResetInput();
        }
        private void OnApplicationPause(bool paused)
        {
            if (!paused || Session == null) return;
            Session.Pause(); ResetInput(); Persist();
        }
        private void OnApplicationFocus(bool focus)
        {
            if (!focus && Session != null && !SmokeMode) { Session.Pause(); ResetInput(); Persist(); }
        }
        private void OnApplicationQuit() { if (Session != null) Persist(); }
        public void Persist()
        {
            string error;
            bool saved=TideStorage.Save(savePath, Session.Profile, out error);
            if(!saved)Session.Status=error;
            if(saveIndicator!=null){saveIndicator.text=saved?"자동 저장됨":"저장 확인 필요";saveIndicator.color=saved?muted:gold;}
        }
        public void StartBattle(bool training,int index=-1)
        {
            if(Session.Mode==TideMode.Story)return;
            if(training){BeginPractice(true,0);return;}
            if(Session.Mode==TideMode.Defeat)
            {
                if(Session.IsCampaign)BeginDungeonNow(Session.Dungeon.act,Session.Dungeon.number,Session.IsReplay);
                else BeginPractice(false,Session.BossIndex);return;
            }
            DepartDungeon(Session.Profile.campaignAct,Session.Profile.campaignStage,false);
        }
        private void DepartDungeon(int act,int number,bool replay)
        {
            if(!replay&&Session.Quest!=QuestState.Active){Session.Status="이번 액트 의뢰를 먼저 확인하세요.";return;}
            var dungeon=TideCampaign.Dungeon(act,number);
            string id=TideDialogue.DungeonId(dungeon,"entry");
            bool newEntry=!replay&&Session.Profile.roomCheckpoint==0&&TideDialogue.Seen(Session.Profile,id)==null;
            if(newEntry&&(number==1||number==3||number==6))
                ShowDialogueScene(id,()=>BeginDungeonNow(act,number,replay),"현장으로",cancelLabel:"출정 취소");
            else if(BeginDungeonNow(act,number,replay)&&newEntry)
                StartFieldScene(id);
        }
        private bool BeginDungeonNow(int act,int number,bool replay)
        {
            navigation.Clear();ResetInput();
            if(!Session.BeginDungeon(act,number,replay)){ReturnTown();return false;}
            PrepareFieldStory();
            ResetFeedback();Persist();Refresh();return true;
        }
        private void BeginPractice(bool training,int boss)
        {
            navigation.Clear();ResetInput();Session.BeginBattle(training,boss);ResetFeedback();Refresh();
        }
        private void ShowDialogueScene(string id,Action finish,string finalLabel="마을로",Action cancel=null,string cancelLabel="거점으로")
        {
            ResetInput();Session.ClearAction();
            storyLines=TideDialogue.Lines(id,Session.Profile.hero);
            storyPage=0;storyFinish=finish;storyEndLabel=finalLabel;storyCancel=cancel??ReturnTown;storyCancelLabel=cancelLabel;
            storyRecordId=id;storyHero=Session.Profile.hero;
            storyEpoch++;navigation.Clear();
            Session.Mode=TideMode.Story;Session.Revision++;Refresh();
        }
        private void AdvanceStory(int expectedPage)
        {
            if(Session.Mode!=TideMode.Story||storyFinish==null||storyPage!=expectedPage)return;
            storyPage++;if(storyPage<storyLines.Length){Session.Revision++;Refresh();return;}FinishStory();
        }
        private void FinishStory()
        {
            if(Session.Mode!=TideMode.Story||storyFinish==null)return;
            Action finish=storyFinish;
            TideDialogue.Remember(Session.Profile,storyRecordId,storyHero);
            ClearStory();
            finish();
        }
        private void CancelStory()
        {
            if(Session.Mode!=TideMode.Story||storyCancel==null)return;
            Action cancel=storyCancel;
            ClearStory();
            cancel();
        }
        private void ClearStory()
        {
            storyFinish=null;storyCancel=null;storyLines=null;storyRecordId=null;
        }
        public void ReturnTown()
        {
            ClearStory();
            navigation.Clear();
            ResetInput(); Session.ReturnTown(); Persist(); Refresh();
        }
        private void SelectHero(int i) { if (Session.SelectHero(i)) { ResetInput(); Persist(); Refresh(); } }
    }
}
