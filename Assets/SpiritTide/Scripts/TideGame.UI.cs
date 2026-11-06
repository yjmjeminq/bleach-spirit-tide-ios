using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private Text warningText;
        private RectTransform[] cloudViews = new RectTransform[2];
        private RectTransform uiRoot;
        private RectTransform currentDialogueCard;
        private RectTransform lureView,beaconView;
        private Text lureLabel;
        private RectTransform Node(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
        {
            RectTransform r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = r.anchorMax = anchor; r.anchoredPosition = position; r.sizeDelta = size;
            return r;
        }
        private RectTransform Stretch(string name, Transform parent)
        {
            RectTransform r = Node(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; return r;
        }
        private Image Picture(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Color tint, Sprite sprite = null)
        {
            RectTransform r = Node(name, parent, anchor, position, size);
            Image image = r.gameObject.AddComponent<Image>(); image.sprite = sprite != null ? sprite : square;
            image.color = tint; image.raycastTarget = false; return image;
        }
        private Text Label(string text, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, int fontSize = 26, Color? tint = null, bool strong = false)
        {
            RectTransform r = Node("Text", parent, anchor, position, size);
            Text t = r.gameObject.AddComponent<Text>(); t.font = strong ? bold : font; t.fontSize = fontSize;
            t.text = text; t.color = tint ?? ink; t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Truncate; t.raycastTarget = false;
            return t;
        }
        private Button Button(string id, string label, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, Action action, bool primary = false)
        {
            Image image = Picture(id, parent, anchor, position, size, primary ? new Color(.18f,.4f,.43f) : card);
            image.raycastTarget = true;
            Button b = image.gameObject.AddComponent<Button>();
            b.gameObject.AddComponent<TideFocusRing>();
            Navigation navigation=b.navigation;navigation.mode=Session.IsMovingMode?Navigation.Mode.None:Navigation.Mode.Automatic;b.navigation=navigation;
            ColorBlock colors = b.colors; colors.highlightedColor = new Color(.7f,.9f,.9f);
            colors.pressedColor = new Color(.6f,.75f,.8f); colors.disabledColor = new Color(.4f,.45f,.45f); b.colors = colors;
            Text text = Label(label, image.transform, new Vector2(.5f,.5f), Vector2.zero, size - new Vector2(14,10), 26);
            text.alignment = TextAnchor.MiddleCenter;
            b.onClick.AddListener(() => {if(b==null||!b.isActiveAndEnabled||!b.interactable)return;if (audioSource != null) audioSource.PlayOneShot(tapClip); action?.Invoke(); if(Session.IsMovingMode&&EventSystem.current!=null)EventSystem.current.SetSelectedGameObject(null); });
            Buttons[id] = b; return b;
        }
        private Image Meter(Transform parent, Vector2 position, float width, Color tint)
        {
            Image track = Picture("Track", parent, new Vector2(0,1), position, new Vector2(width,11), new Color(.18f,.27f,.29f));
            Image fill = Picture("Fill", track.transform, new Vector2(.5f,.5f), Vector2.zero, new Vector2(width,11), tint);
            fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; return fill;
        }
        private void BuildUI()
        {
            GameObject canvasObject = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            Canvas canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1560,720); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight; scaler.matchWidthOrHeight = .5f;
            uiRoot = canvasObject.GetComponent<RectTransform>();
            Image background = Stretch("Background", uiRoot).gameObject.AddComponent<Image>(); background.color = bg; background.raycastTarget = false;
            safe = Stretch("SafeArea", uiRoot);
            if (FindFirstObjectByType<EventSystem>() == null) new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            EventSystem.current.sendNavigationEvents=false;
            world = Stretch("World", safe);
            world.gameObject.AddComponent<RectMask2D>();
            warningImage = Picture("AttackWarning", world, Vector2.zero, Vector2.zero, new Vector2(200,60), new Color(.85f,.34f,.2f,.3f));
            warning = warningImage.rectTransform;
            beaconView=Picture("ReturnBeacon",world,Vector2.zero,Vector2.zero,new Vector2(20,62),new Color(.55f,.95f,.85f),circle).rectTransform;
            Text beaconLabel=Label("귀환 등불",beaconView,new Vector2(.5f,.5f),new Vector2(0,-48),new Vector2(140,40),22,new Color(.65f,.95f,.87f));beaconLabel.alignment=TextAnchor.MiddleCenter;
            lureView=Picture("FisherLure",world,Vector2.zero,Vector2.zero,new Vector2(28,75),new Color(.95f,.68f,.35f),circle).rectTransform;
            lureLabel=Label("",lureView,new Vector2(.5f,.5f),new Vector2(0,62),new Vector2(240,45),24,gold,true);lureLabel.alignment=TextAnchor.MiddleCenter;
            for (int i = 0; i < 2; i++)
                cloudViews[i] = Picture("AshCloud"+i, world, Vector2.zero, Vector2.zero, new Vector2(210,95), new Color(.7f,.74f,.8f,.18f), circle).rectTransform;
            RectTransform actorsLayer=Stretch("Actors",world);
            enemyImage = Picture("Enemy", actorsLayer, Vector2.zero, Vector2.zero, new Vector2(320,220), Color.white, bossSprites[0]);
            enemyImage.preserveAspect = true; enemyActor = enemyImage.rectTransform; enemyActor.pivot = new Vector2(.5f,0);
            actorImage = Picture("Player", actorsLayer, Vector2.zero, Vector2.zero, new Vector2(128,192), Color.white, heroSprites[0]);
            actorImage.preserveAspect = true; actor = actorImage.rectTransform; actor.pivot = new Vector2(.5f,0);
            slashImage = Picture("AttackTrace", world, Vector2.zero, Vector2.zero, new Vector2(130,12), new Color(.65f,.88f,1,.7f));
            slash = slashImage.rectTransform;
            warningText = Label("", world, new Vector2(.5f,1),new Vector2(0,-216),new Vector2(860,40),24,gold,true);warningText.alignment=TextAnchor.MiddleCenter;
            lureView.SetAsLastSibling();
            townRoot = Stretch("TownServices", safe).gameObject;
            string[] npcIds = {"urahara","tessai","kon","byakuya","kukaku"};
            string[] npcLabels = {"우라하라\n의뢰·보급","텟사이\n정비·수련","콘\n구조·동료","뱌쿠야\n기록 확인","쿠카쿠\n출정"};
            for (int i = 0; i < 5; i++)
            {
                string npc = npcIds[i];
                Button("npc-"+npc, npcLabels[i], townRoot.transform, new Vector2(.5f,.18f), new Vector2((i-2)*160,0), new Vector2(150,94), () => Talk(npc));
            }
            Button("objective","",townRoot.transform,new Vector2(.5f,1),new Vector2(0,-150),new Vector2(560,88),ContinueObjective,true);
            Picture("Header", safe, new Vector2(.5f,1), new Vector2(0,-50), new Vector2(2200,100), new Color(.055f,.11f,.145f,.97f));
            titleText = Label("영해의 균열", safe, new Vector2(0,1), new Vector2(210,-25), new Vector2(390,40), 27, gold, true);
            hpText = Label("", safe, new Vector2(0,1), new Vector2(180,-58), new Vector2(330,30), 23);
            hpFill = Meter(safe, new Vector2(180,-79), 330, new Color(.4f,.8f,.58f));
            mpFill = Meter(safe, new Vector2(180,-94), 330, new Color(.42f,.7f,.9f));
            resourceText = Label("", safe, new Vector2(0,1), new Vector2(180,-114), new Vector2(330,32), 21, muted);
            goldText = Label("", safe, new Vector2(1,1), new Vector2(-260,-28), new Vector2(160,40), 25, gold);
            saveIndicator=Label("자동 저장",safe,new Vector2(1,1),new Vector2(-260,-66),new Vector2(160,26),16,muted);saveIndicator.name="SaveIndicator";
            for (int i = 0; i < 6; i++)
            {
                int index = i;
                heroButtons[i] = Button("hero-"+i, new[]{"루키아","오리히메","요루이치","소이폰","란기쿠","우노하나"}[i],
                    safe, new Vector2(.5f,1), new Vector2((i-2.5f)*103,-43), new Vector2(96,84), () => SelectHero(index));
                heroButtons[i].GetComponentInChildren<Text>().fontSize=20;
            }
            Button("pause","메뉴",safe,new Vector2(1,1),new Vector2(-72,-44),new Vector2(116,84),ToggleMainMenu);
            battleRoot = Stretch("BossHud", safe).gameObject;
            enemyText = Label("",battleRoot.transform,new Vector2(.5f,1),new Vector2(0,-119),new Vector2(800,34),25,gold,true);
            enemyText.alignment = TextAnchor.MiddleCenter;
            enemyFill = Meter(battleRoot.transform,new Vector2(safe.rect.width*.5f,-146),640,new Color(.67f,.38f,.38f));
            enemyFill.rectTransform.parent.GetComponent<RectTransform>().anchorMin = enemyFill.rectTransform.parent.GetComponent<RectTransform>().anchorMax = new Vector2(.5f,1);
            enemyFill.rectTransform.parent.GetComponent<RectTransform>().anchoredPosition = new Vector2(0,-146);
            questText = Label("",safe,new Vector2(.5f,1),new Vector2(0,-180),new Vector2(640,35),23,muted);
            questText.alignment=TextAnchor.MiddleCenter;
            actionText = Label("",safe,new Vector2(.5f,0),new Vector2(0,130),new Vector2(600,42),25,gold,true); actionText.alignment=TextAnchor.MiddleCenter;
            statusText = Label("",safe,new Vector2(.5f,0),new Vector2(0,32),new Vector2(940,40),21,muted); statusText.alignment=TextAnchor.MiddleCenter;
            controlsRoot=Stretch("TouchControls",safe).gameObject;
            Image joy = Picture("MoveJoystick",controlsRoot.transform,Vector2.zero,new Vector2(140,142),new Vector2(230,230),new Color(.2f,.35f,.38f,.8f),circle);
            joy.raycastTarget=true; Joystick=joy.gameObject.AddComponent<TideJoystick>();
            Joystick.Knob=Picture("Knob",joy.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(78,78),new Color(.55f,.76f,.75f,.9f),circle).rectTransform;
            Text move = Label("이동",joy.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(80,40),22);move.alignment=TextAnchor.MiddleCenter;
            Button basic=Button("attack","공격\n연계",controlsRoot.transform,new Vector2(1,0),new Vector2(-92,91),new Vector2(120,120),null,true);
            basic.GetComponent<Image>().sprite=circle; AttackHold=basic.gameObject.AddComponent<TideHold>(); AttackHold.Down=()=>{blockHeldAttackUntilRelease=false;Session.Request(Joystick.Value.y>.55f?MoveKind.Launcher:MoveKind.Basic);};
            Button heavy=Button("heavy","강공격\n모으기",controlsRoot.transform,new Vector2(1,0),new Vector2(-350,91),new Vector2(100,100),null);
            heavyHold=heavy.gameObject.AddComponent<TideHold>();heavyHold.Down=()=>Session.BeginHeavy();heavyHold.Up=()=>Session.ReleaseHeavy();
            Button("jump","점프",controlsRoot.transform,new Vector2(1,0),new Vector2(-92,221),new Vector2(100,100),()=>Session.Jump());
            Button("dodge","회피",controlsRoot.transform,new Vector2(1,0),new Vector2(-222,91),new Vector2(100,100),()=>Session.Dodge(Joystick.Value.sqrMagnitude>.01f?Joystick.Value:keyboardMove));
            Button potion=Button("potion","회복",controlsRoot.transform,new Vector2(1,0),new Vector2(-222,215),new Vector2(100,96),()=>{if(Session.UsePotion())Persist();});potion.GetComponentInChildren<Text>().fontSize=20;
            Vector2[] positions = {new Vector2(-470,215),new Vector2(-350,215),new Vector2(-470,335),new Vector2(-350,335),new Vector2(-222,335)};
            for(int i=0;i<5;i++)
            {
                int slot=i;
                skillButtons[i]=Button("skill-"+i,"",controlsRoot.transform,new Vector2(1,0),positions[i],new Vector2(110,108),
                    ()=>Session.Request((MoveKind)((int)MoveKind.Skill0+slot)),i==4);
                skillLabels[i]=skillButtons[i].GetComponentInChildren<Text>();skillLabels[i].fontSize=22;
            }
            menuRoot=Stretch("Menus",safe).gameObject;MakeModalShade(menuRoot);panel=Node("MenuCard",menuRoot.transform,new Vector2(.5f,.5f),new Vector2(0,-15),new Vector2(1060,560));
            Image panelBackground=panel.gameObject.AddComponent<Image>();panelBackground.color=new Color(.08f,.16f,.19f,.99f);panelBackground.raycastTarget=true;
            dialogueRoot=Stretch("Dialogue",safe).gameObject;
            MakeModalShade(dialogueRoot);
            Button("interact","E · 대화",safe,new Vector2(1,0),new Vector2(-110,105),new Vector2(184,96),()=>Talk(NearestNpc()),true);
            CreateHudDetails();BuildCampaignWorld();BuildExperienceWorld();BuildQuestWorld();BuildFeel();
        }
        private void Clear(Transform parent)
        {
            for(int i=parent.childCount-1;i>=0;i--){parent.GetChild(i).gameObject.SetActive(false);Destroy(parent.GetChild(i).gameObject);}
        }
        private void Refresh()
        {
            if(Session==null||safe==null)return;
            if(lastMode==Session.Mode&&pendingFocus==null&&EventSystem.current!=null&&EventSystem.current.currentSelectedGameObject!=null)pendingFocus=EventSystem.current.currentSelectedGameObject.name;
            ResetWhenModeChanges();
            actorImage.sprite=heroSprites[Session.Profile.hero];enemyImage.sprite=bossSprites[Session.BossIndex];
            bool fighting=Session.Mode==TideMode.Combat||Session.Mode==TideMode.Training;
            bool town=Session.Mode==TideMode.Town;
            townRoot.SetActive(town);battleRoot.SetActive(Session.InBattle&&Session.AliveCount>0);controlsRoot.SetActive(town||fighting);
            questText.gameObject.SetActive(Session.InBattle);
            Buttons["interact"].gameObject.SetActive(town);
            foreach(Button b in heroButtons)b.gameObject.SetActive(town||Session.Mode==TideMode.Training);
            for(int i=0;i<6;i++)heroButtons[i].GetComponent<Image>().color=Session.Profile.hero==i?new Color(.3f,.4f,.32f):card;
            for(int i=0;i<6;i++)heroButtons[i].interactable=!(Session.InBattle&&!Session.IsTraining);
            for(int i=0;i<5;i++)skillButtons[i].gameObject.SetActive(fighting);
            foreach(string id in new[]{"attack","heavy","jump","dodge"})Buttons[id].gameObject.SetActive(fighting);
            Buttons["potion"].gameObject.SetActive(fighting&&!Session.IsTraining);
            bool overlay=!town&&!fighting&&Session.Mode!=TideMode.Dialogue&&Session.Mode!=TideMode.Story;
            menuRoot.SetActive(overlay);dialogueRoot.SetActive(Session.Mode==TideMode.Dialogue||Session.Mode==TideMode.Story);
            Buttons["pause"].gameObject.SetActive(town||fighting);
            if(overlay)BuildMenu();
            if(Session.Mode==TideMode.Dialogue)BuildDialogue();
            if(Session.Mode==TideMode.Story)BuildStory();
            ApplyHudPreferences();UpdateHud();FocusMenu();
            viewRevision=Session.Revision;lastMode=Session.Mode;
            titleText.text="영해의 균열 · "+(Session.InBattle?Session.IsTraining?"수련장":"Act "+Session.BattleAct:"거점");
            if(Session.Mode==TideMode.Result||Session.Mode==TideMode.Defeat)Persist();
        }
        private void ResetWhenModeChanges(){if(lastMode!=Session.Mode)ResetInput();}
        private void UpdateHud()
        {
            hpText.text=TideStory.HeroName(Session.Profile.hero)+" · Lv."+Session.Profile.level+" · "+Mathf.CeilToInt(Session.Hp)+" / "+Session.MaxHp;
            hpFill.fillAmount=Session.Hp/Session.MaxHp;mpFill.fillAmount=Session.Mp/100;
            resourceText.text="영압 "+Mathf.FloorToInt(Session.Mp)+(Session.Shield>0?"  보호 "+Mathf.CeilToInt(Session.Shield):"");
            goldText.text=Session.Profile.gold+" 골드";
            questText.text=Session.InBattle?(Session.Mark>0?"첫 표식 · 봉익일섬 강화":Session.PerfectRemaining>0?"정밀 회피 · 공격하면 반격":Session.IsTraining?"수련 중 · 자원 소모 없음":"Act 1 · "+(Session.BossIndex==0?"사역체의 함정":"기억의 미끼")):
                Session.Quest==QuestState.Available?"우라하라에게 첫 의뢰를 확인하세요":Session.Quest==QuestState.Active?"진행 중 · 쿠카쿠에게 출정 위치를 확인하세요":
                Session.Quest==QuestState.Ready?"보고 가능 · 우라하라에게 기록을 전달하세요":"Act 1 보고 완료 · 수련과 전투 재현을 이용할 수 있습니다";
            enemyText.text=(Session.Target.Def?.id=="a3_boss_ulquiorra"?"["+Session.Target.Phase+" / 3단계] ":"")+(Session.IsTraining?"수련용 기록 · ":"")+Session.EnemyName+"  "+Mathf.CeilToInt(Session.EnemyHp)+" / "+Mathf.CeilToInt(Session.EnemyMaxHp)+(Session.Target.Def!=null&&Session.Target.Def.IsBoss?" · 자세 "+Mathf.CeilToInt(Session.Target.Poise):"");
            enemyFill.fillAmount=Session.EnemyHp/Mathf.Max(1,Session.EnemyMaxHp);
            actionText.text=Session.IsCharging?"강공격 준비 "+Mathf.RoundToInt(Session.HeavyProgress*100)+"%":Session.ActionRemaining>0?Session.ActionName:Session.PerfectRemaining>0?"반격 기회":"";
            statusText.text=Session.Status;
            Buttons["potion"].GetComponentInChildren<Text>().text="회복 +"+(45+Session.Profile.level*2)+"\n"+Session.Profile.potions+"개"+(ShowKeyboardHints?" · V":"");
            Buttons["potion"].interactable=Session.IsFighting&&!Session.IsTraining&&Session.Hp<Session.MaxHp&&Session.Profile.potions>0;
            if(Session.Mode==TideMode.Town)Buttons["interact"].GetComponentInChildren<Text>().text="대화 · "+new[]{"우라하라","텟사이","콘","뱌쿠야","쿠카쿠"}[NearestNpcIndex()];
            if(Session.Mode==TideMode.Town)Say(Buttons["objective"].GetComponentInChildren<Text>(),Session.CampaignFinished?"함께 돌아온 여섯 사람\n마을의 이야기와 지나온 길":TideQuests.Main(Session.Profile.campaignAct).title+"\n"+(Session.Quest==QuestState.Available?"우라하라에게 부탁 듣기":Session.Quest==QuestState.Ready?"우라하라에게 귀환 보고":TideCampaign.Dungeon(Session.Profile.campaignAct,Session.Profile.campaignStage).title));
            Say(Buttons["quest-log"].GetComponentInChildren<Text>(),"의뢰 수첩"+(ShowKeyboardHints?" · Q":""));
            for(int i=0;i<5;i++)
            {
                skillKeys[i].gameObject.SetActive(ShowKeyboardHints);
                SkillDef skill=Session.Hero.Skill(i);float remaining=Mathf.Max(0,Session.Cooldowns[i]-Session.Clock);
                Say(skillLabels[i],skill.name.Replace("·","\n"));
                Say(skillFooters[i],Session.IsTraining?"수련":remaining>0?Mathf.CeilToInt(remaining)+"초":Session.Mp<skill.spirit_cost?"영압 부족":"영압 "+skill.spirit_cost);
                skillFooters[i].color=Session.Mp<skill.spirit_cost&&!Session.IsTraining?gold:muted;
                skillCooldownFills[i].fillAmount=Session.IsTraining?0:remaining/Mathf.Max(.01f,skill.cooldown_seconds);
                skillButtons[i].interactable=Session.IsFighting&&(Session.IsTraining||(remaining<=0&&Session.Mp>=skill.spirit_cost));
            }
        }
        private Vector2 Project(float x,float y)
        {
            float camera=Session.InBattle?Session.X:0;
            return new Vector2(safe.rect.width*(Session.InBattle?.23f:.32f)+(x-camera)*(Session.InBattle?TideHudLayout.UnitX(safe.rect.width,Session.Profile.controlScale):62),safe.rect.height*.4f+y*35)+(Session.InBattle?impactOffset:Vector2.zero);
        }
        private void DrawWorld(){DrawCampaignWorld();DrawExperienceWorld();DrawQuestWorld();DrawFeel();DrawSkillEffects();}
        private void Talk(string npc)
        {
            if(Session.InBattle||Session.Mode==TideMode.Story)return;
            if(Session.QuestTalk(npc)){Persist();PlayQuestStory(()=>{ReturnTown();Talk(npc);});return;}
            PushContext();dialogueNpc=npc;ResetInput();Session.Mode=TideMode.Dialogue;Session.Revision++;Refresh();
        }
        private int NearestNpcIndex()
        {
            int nearest=0;float distance=float.MaxValue;
            for(int i=0;i<5;i++){float current=Mathf.Abs(Session.X-(-4+i*2));if(current<distance){distance=current;nearest=i;}}
            return nearest;
        }
        private string NearestNpc(){return new[]{"urahara","tessai","kon","byakuya","kukaku"}[NearestNpcIndex()];}
        private void Dialogue(string speaker,string text, string[] labels, Action[] actions)
        {
            Clear(dialogueRoot.transform);
            RectTransform r=Node("DialogueCard",dialogueRoot.transform,new Vector2(.5f,.5f),new Vector2(0,-20),new Vector2(1080,480));
            currentDialogueCard=r;
            Image img=r.gameObject.AddComponent<Image>();img.color=new Color(.08f,.16f,.19f,.99f);img.raycastTarget=true;
            Label(speaker,r,new Vector2(.5f,.5f),new Vector2(-140,195),new Vector2(660,55),31,gold,true);
            Button("dialogue-back","뒤로",r,new Vector2(.5f,.5f),new Vector2(470,188),new Vector2(84,84),NavigateBack);
            Text body=Label(text,r,new Vector2(.5f,.5f),new Vector2(0,25),new Vector2(980,260),Mathf.RoundToInt(28*Session.Profile.uiScale));
            body.alignment=TextAnchor.UpperLeft;
            for(int i=0;i<labels.Length;i++){int n=i;Button("choice-"+i,labels[i],r,new Vector2(.5f,.5f),new Vector2((i-(labels.Length-1)*.5f)*320,-177),new Vector2(304,88),()=>{actions[n]();Refresh();});}
        }
        private void BuildStory()
        {
            int page=storyPage,epoch=storyEpoch;StoryLine line=storyLines[page];bool last=page==storyLines.Length-1;
            string speaker=line.speaker;
            bool remote=Session.InBattle;
            if(storyRecordId!=null){string kind=TideDialogue.Scene(storyRecordId).kind;remote=remote||kind=="entry"||kind=="intro"||kind=="clear";}
            if(remote)for(int hero=0;hero<6;hero++)if(hero!=Session.Profile.hero&&speaker==TideStory.HeroName(hero))speaker+=" · 동료 통신";
            if(storyRecordId!=null){var scene=TideDialogue.Scene(storyRecordId);if(scene.kind.StartsWith("side_")&&(scene.act<Session.Profile.campaignAct||Session.Profile.claimed))speaker="지난 부탁 · "+speaker;}
            Action advance=()=>{if(epoch==storyEpoch)AdvanceStory(page);};Action skip=()=>{if(epoch==storyEpoch)FinishStory();};
            if(last)Dialogue(speaker+" · "+(page+1)+" / "+storyLines.Length,line.text,new[]{storyEndLabel,"마을로"},new Action[]{advance,ReturnTown});
            else Dialogue(speaker+" · "+(page+1)+" / "+storyLines.Length,line.text,new[]{"계속","장면 건너뛰기","마을로"},new Action[]{advance,skip,ReturnTown});
            Button("story-previous","이전 문장",currentDialogueCard,new Vector2(.5f,.5f),new Vector2(300,188),new Vector2(190,84),()=>{
                if(epoch!=storyEpoch||Session.Mode!=TideMode.Story||storyPage!=page||page<=0)return;
                storyPage--;Session.Revision++;Refresh();
            }).interactable=page>0;
        }
        private void BuildDialogue()
        {
            Action close=NavigateBack;
            string text=TideStory.NpcText(dialogueNpc,Session.Profile);
            if(dialogueNpc=="urahara")
            {
                if(Session.Quest==QuestState.Available)Dialogue("우라하라",text,
                    new[]{"의뢰를 받는다","보급품을 본다","나중에 이야기한다"},new Action[]{ReceiveMainQuest,()=>OpenMenu(TideMode.Shop),close});
                else if(Session.Quest==QuestState.Ready)Dialogue("우라하라",text,
                    new[]{"보고 · "+(250+(Session.Profile.campaignAct-1)*100)+" 골드","잠시 뒤 보고한다"},new Action[]{()=>{if(Session.ClaimReward()){Persist();ShowDialogueScene("a"+Session.LastActReported+"_report",ReturnTown);}},close});
                else if(Session.Quest==QuestState.Completed)Dialogue("우라하라",text,
                    new[]{"귀환 후 이야기","보급품을 본다","대화를 마친다"},new Action[]{()=>ShowDialogueScene("a5_epilogue",ReturnTown),()=>OpenMenu(TideMode.Shop),close});
                else Dialogue("우라하라",text,
                    new[]{"출정 기록 확인","보급품을 본다","대화를 마친다"},new Action[]{()=>OpenMenu(TideMode.Missions),()=>OpenMenu(TideMode.Shop),close});
            }
            else if(dialogueNpc=="tessai")Dialogue("텟사이",text,
                new[]{"장비를 비교한다","수련장에 들어간다","대화를 마친다"},new Action[]{()=>OpenMenu(TideMode.Inventory),()=>StartBattle(true),close});
            else if(dialogueNpc=="kukaku"&&Session.Quest==QuestState.Ready)Dialogue("쿠카쿠",text,
                new[]{"우라하라에게 보고","대화를 마친다"},new Action[]{()=>Talk("urahara"),close});
            else if(dialogueNpc=="kukaku")Dialogue("쿠카쿠",text,
                new[]{"출정 지역 확인","대화를 마친다"},new Action[]{()=>OpenMenu(TideMode.Missions),close});
            else if(dialogueNpc=="byakuya")Dialogue("뱌쿠야",text,
                new[]{"조사 기록 확인","대화를 마친다"},new Action[]{OpenJournal,close});
            else
            {
                Dialogue("콘",text,new[]{RescueReportAct()>0?"구조 소식 보고":"구조 목표 확인",Session.Quest==QuestState.Available?"동료 대화 · 의뢰 후":"동료와 이야기","대화를 마친다"},new Action[]{ReportTownRescue,()=>ShowDialogueScene("a"+Session.Profile.campaignAct+"_party_"+Session.Hero.id,ReturnTown),close});
                Buttons["choice-1"].interactable=Session.Quest!=QuestState.Available;
            }
            var card=currentDialogueCard;int notices=TideQuests.Notices(Session.Profile,dialogueNpc);
            Button("npc-requests",notices>0?"부탁 · "+notices:"의뢰 수첩",card,new Vector2(.5f,.5f),new Vector2(300,188),new Vector2(190,84),()=>OpenQuestBook(dialogueNpc),notices>0);
        }
        private void MenuTitle(string title,string subtitle="")
        {
            Clear(panel);Label(title,panel,new Vector2(.5f,.5f),new Vector2(-45,230),new Vector2(860,60),35,gold,true);
            Button("close","뒤로",panel,new Vector2(.5f,.5f),new Vector2(470,230),new Vector2(84,84),NavigateBack);
            if(subtitle.Length>0)Label(subtitle,panel,new Vector2(.5f,.5f),new Vector2(0,167),new Vector2(950,66),26,muted);
        }
        private void BuildMenu()
        {
            switch(Session.Mode)
            {
                case TideMode.Home:BuildHome();break;
                case TideMode.Inventory:BuildGear(false);break;
                case TideMode.Forge:BuildGear(true);break;
                case TideMode.Journal:BuildJournal();break;
                case TideMode.Quests:BuildQuestBook();break;
                case TideMode.Shop:
                    MenuTitle("우라하라 보급 상점","회복 영자 1개 · 80 골드 / 체력 "+(45+Session.Profile.level*2)+" 회복");
                    Label("소지 "+Session.Profile.potions+" / 5개 · 현재 "+Session.Profile.gold+" 골드",panel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(950,70),32);
                    Button buy=Button("buy",Session.Profile.potions>=5?"소지 한도 도달":Session.Profile.gold<80?"골드 부족":"1개 구매",panel,new Vector2(.5f,.5f),new Vector2(-190,-135),new Vector2(330,88),()=>{Session.BuyPotion();Persist();Refresh();},true);
                    buy.interactable=Session.Profile.gold>=80&&Session.Profile.potions<5;
                    Button("back","뒤로",panel,new Vector2(.5f,.5f),new Vector2(190,-135),new Vector2(330,88),NavigateBack);break;
                case TideMode.Missions:BuildMissions();break;
                case TideMode.Pause:
                case TideMode.Settings:
                    MenuTitle(Session.InBattle?"일시정지":"보기와 조작","보기·조작 환경을 조절합니다. 전투 시계와 기술 수치는 유지됩니다.");
                    Button("hints","키 안내 · "+new[]{"자동","표시","숨김"}[Session.Profile.keyboardHints],panel,new Vector2(.5f,.5f),new Vector2(300,230),new Vector2(190,84),()=>{Session.Profile.keyboardHints=(Session.Profile.keyboardHints+1)%3;PreferencesChanged();}).GetComponentInChildren<Text>().fontSize=22;
                    Button("effects",Session.Profile.reducedEffects?"효과 줄이기 · 켬":"효과 줄이기 · 끔",panel,new Vector2(.5f,.5f),new Vector2(-320,65),new Vector2(300,84),()=>{Session.Profile.reducedEffects=!Session.Profile.reducedEffects;PreferencesChanged();});
                    Button("hold",Session.Profile.holdAttack?"누르고 연계 · 켬":"누르고 연계 · 끔",panel,new Vector2(.5f,.5f),new Vector2(0,65),new Vector2(300,84),()=>{Session.Profile.holdAttack=!Session.Profile.holdAttack;PreferencesChanged();});
                    Button("font","대화 글자 "+(Session.Profile.uiScale>1?"크게":"보통"),panel,new Vector2(.5f,.5f),new Vector2(320,65),new Vector2(300,84),()=>{Session.Profile.uiScale=Session.Profile.uiScale>1?1:1.15f;PreferencesChanged();});
                    Button("hud-size","조작 버튼 "+(Session.Profile.controlScale>1?"크게":"기본"),panel,new Vector2(.5f,.5f),new Vector2(-320,-40),new Vector2(300,84),()=>{Session.Profile.controlScale=Session.Profile.controlScale>1?1:1.1f;PreferencesChanged();});
                    Button("hud-opacity","버튼 선명도 "+Mathf.RoundToInt(Session.Profile.controlOpacity*100)+"%",panel,new Vector2(.5f,.5f),new Vector2(0,-40),new Vector2(300,84),()=>{Session.Profile.controlOpacity=Session.Profile.controlOpacity>.95f?.65f:Session.Profile.controlOpacity>.75f?1:.85f;PreferencesChanged();});
                    Button("sound",Session.Profile.soundEnabled?"효과음 · 켬":"효과음 · 끔",panel,new Vector2(.5f,.5f),new Vector2(320,-40),new Vector2(300,84),()=>{Session.Profile.soundEnabled=!Session.Profile.soundEnabled;PreferencesChanged();});
                    Button("resume",Session.InBattle?"전투 계속":"뒤로",panel,new Vector2(.5f,.5f),new Vector2(Session.InBattle?-320:-190,-190),new Vector2(300,88),()=>{if(Session.InBattle)ResumeFromMenu();else NavigateBack();},true);
                    Button("help","조작·기술 설명",panel,new Vector2(.5f,.5f),new Vector2(Session.InBattle?0:190,-190),new Vector2(300,88),ShowHelp);
                    if(Session.InBattle)Button("retreat",Session.IsTraining?"수련 종료":"마을로 귀환",panel,new Vector2(.5f,.5f),new Vector2(320,-190),new Vector2(300,88),RequestRetreat);break;
                case TideMode.Help:BuildHelp();break;
                case TideMode.ConfirmRetreat:
                    MenuTitle("마을로 돌아갈까요?","정화한 방은 저장됩니다. 현재 방의 적 체력은 재입장 시 회복됩니다.");
                    Label(Session.IsCampaign?"저장 위치 · "+Session.Dungeon.title+" / "+(Session.Profile.roomCheckpoint+1)+"방":"보스 재현 · 진행 보상 없음",panel,new Vector2(.5f,.5f),Vector2.zero,new Vector2(950,80),30);
                    Button("cancel-retreat","취소 · 일시정지로",panel,new Vector2(.5f,.5f),new Vector2(-190,-190),new Vector2(330,88),NavigateBack,true);
                    Button("confirm-retreat","마을로 귀환",panel,new Vector2(.5f,.5f),new Vector2(190,-190),new Vector2(330,88),ReturnTown);break;
                case TideMode.Result:BuildCampaignResult();break;
                case TideMode.Defeat:
                    MenuTitle("다시 도전할 수 있습니다","이미 정화한 기록은 남습니다. 현재 전장의 시작부터 재개합니다.");
                    Button("retry","현재 기록 재도전",panel,new Vector2(.5f,.5f),new Vector2(-190,-135),new Vector2(340,88),()=>StartBattle(false),true);
                    Button("back","마을로",panel,new Vector2(.5f,.5f),new Vector2(190,-135),new Vector2(300,88),ReturnTown);break;
            }
        }
    }
}
