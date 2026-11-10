using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private CanvasGroup controlsOpacity;
        private readonly Text[] skillFooters=new Text[TideLoadouts.SlotCount];
        private readonly Image[] skillCooldownFills=new Image[TideLoadouts.SlotCount];
        private Text classStateText,playerFeedback,enemyFeedback;
        private Image hpTrailFill,chargeFill,warningFill;
        private RectTransform playerShadow,enemyShadow;
        private float shownHp=150,previousHp=150,previousShield,playerFeedbackTime,enemyFeedbackTime;
        private float damageTrailAge;
        private int feedbackDamageSerial;
        private float appliedHudWidth=-1,appliedHudHeight=-1,appliedHudScale=-1;
        private static void Say(Text label,string value){if(label.text!=value)label.text=value;}
        private void MakeModalShade(GameObject root)
        {
            Image shade=root.AddComponent<Image>();shade.sprite=square;shade.color=new Color(0,.025f,.035f,.72f);shade.raycastTarget=true;
        }
        private void CreateHudDetails()
        {
            controlsOpacity=controlsRoot.AddComponent<CanvasGroup>();
            hpTrailFill=Picture("RecentDamage",hpFill.transform.parent,Center,Vector2.zero,hpFill.rectTransform.sizeDelta,new Color(.9f,.6f,.35f));
            hpTrailFill.type=Image.Type.Filled;hpTrailFill.fillMethod=Image.FillMethod.Horizontal;hpTrailFill.transform.SetAsFirstSibling();
            Image chargeTrack=Picture("ChargeTrack",Buttons["heavy"].transform,Center,Vector2.zero,new Vector2(92,92),
                new Color(.76f,.58f,.29f,.35f),touchRim);
            chargeFill=Picture("ChargeFill",chargeTrack.transform,Center,Vector2.zero,chargeTrack.rectTransform.sizeDelta,gold,touchRim);
            chargeFill.type=Image.Type.Filled;chargeFill.fillMethod=Image.FillMethod.Radial360;
            chargeFill.fillOrigin=(int)Image.Origin360.Bottom;chargeFill.fillClockwise=true;
            var warningPlate=Surface("AttackTell",safe,new Vector2(.5f,1),new Vector2(86,-168),new Vector2(428,56),surfaceDark).rectTransform;
            warningPlate.SetSiblingIndex(controlsRoot.transform.GetSiblingIndex());
            warningText=Label("",warningPlate,Center,new Vector2(0,4),new Vector2(402,37),20,gold,true);
            warningText.alignment=TextAnchor.MiddleCenter;
            Image warningTrack=Picture("TellClock",warningPlate,Center,new Vector2(0,-18),new Vector2(384,4),new Color(.26f,.28f,.27f));
            warningFill=Picture("TellClockFill",warningTrack.transform,Center,Vector2.zero,warningTrack.rectTransform.sizeDelta,gold);
            warningFill.type=Image.Type.Filled;warningFill.fillMethod=Image.FillMethod.Horizontal;
            playerShadow=Picture("PlayerShadow",world,Vector2.zero,Vector2.zero,new Vector2(75,16),new Color(0,0,0,.3f),circle).rectTransform;
            enemyShadow=Picture("EnemyShadow",world,Vector2.zero,Vector2.zero,new Vector2(180,24),new Color(0,0,0,.25f),circle).rectTransform;
            playerShadow.SetSiblingIndex(warning.GetSiblingIndex());enemyShadow.SetSiblingIndex(playerShadow.GetSiblingIndex());
            playerFeedback=Label("",world,Vector2.zero,Vector2.zero,new Vector2(220,40),28,gold,true);playerFeedback.alignment=TextAnchor.MiddleCenter;
            enemyFeedback=Label("",world,Vector2.zero,Vector2.zero,new Vector2(220,40),28,ink,true);enemyFeedback.alignment=TextAnchor.MiddleCenter;
            foreach(Text value in new[]{playerFeedback,enemyFeedback}){Outline edge=value.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.015f,.04f,.05f,.95f);edge.effectDistance=new Vector2(1,-1);}
            for(int i=0;i<TideLoadouts.SlotCount;i++)
            {
                Transform button=skillButtons[i].transform;
                RectTransform name=skillLabels[i].rectTransform;name.sizeDelta=new Vector2(80,22);name.anchoredPosition=new Vector2(0,-18);skillLabels[i].fontSize=17;
                BuildSkillIcon(i);
                name.SetAsLastSibling();
                Image cd=Picture("Cooldown",button,Center,SkillArtCenter,SkillArtSize,new Color(0,.025f,.035f,.85f),circle);
                cd.type=Image.Type.Filled;cd.fillMethod=Image.FillMethod.Radial360;cd.fillOrigin=2;cd.fillClockwise=true;skillCooldownFills[i]=cd;
                skillTimers[i]=Label("",button,Center,SkillArtCenter,new Vector2(48,34),24,Color.white,true);skillTimers[i].name="SkillCooldownNumber";skillTimers[i].alignment=TextAnchor.MiddleCenter;
                var timerOutline=skillTimers[i].gameObject.AddComponent<Outline>();timerOutline.effectColor=new Color(0,0,0,.95f);timerOutline.effectDistance=new Vector2(1,-1);
                Vector2 slotBadgeAt=new Vector2(-29,28),slotBadgeSize=new Vector2(20,20);
                Picture("SlotBadge",button,Center,slotBadgeAt,slotBadgeSize,surfaceDark,circle);
                Text key=Label("",button,Center,slotBadgeAt,slotBadgeSize,14,gold,true);key.alignment=TextAnchor.MiddleCenter;skillKeys[i]=key;
                skillFooters[i]=Label("",button,Center,new Vector2(0,-35),new Vector2(80,18),14,muted);skillFooters[i].alignment=TextAnchor.MiddleCenter;
            }
            ApplyHudPreferences();ResetFeedback();
        }
        private void ApplyHudPreferences()
        {
            float scale=Session.Profile.controlScale;
            if(Mathf.Approximately(appliedHudWidth,safe.rect.width)&&Mathf.Approximately(appliedHudHeight,safe.rect.height)&&Mathf.Approximately(appliedHudScale,scale))return;
            appliedHudWidth=safe.rect.width;appliedHudHeight=safe.rect.height;appliedHudScale=scale;
            for(int i=0;i<TideHudLayout.Ids.Length;i++)
            {
                RectTransform rect=(RectTransform)Buttons[TideHudLayout.Ids[i]].transform;
                rect.anchorMin=rect.anchorMax=new Vector2(1,0);rect.anchoredPosition=TideHudLayout.Positions[i]*scale;rect.sizeDelta=TideHudLayout.Sizes[i];rect.localScale=Vector3.one*scale;
            }
            foreach(Button button in new[]{fieldAction,gateAction})
            {
                var rect=(RectTransform)button.transform;
                rect.anchoredPosition=TideHudLayout.ContextCenter*scale;
                rect.localScale=Vector3.one*scale;
            }
            RectTransform joy=(RectTransform)Joystick.transform;joy.anchoredPosition=TideHudLayout.JoystickCenter*scale;joy.localScale=Vector3.one*scale;
            string[] npcIds={"urahara","tessai","kon","byakuya","kukaku"};
            float dockShift=(255*scale-202)*.5f;
            for(int i=0;i<npcIds.Length;i++)((RectTransform)Buttons["npc-"+npcIds[i]].transform).anchoredPosition=new Vector2((i-2)*160+dockShift,0);
            float left=(TideHudLayout.JoystickCenter.x+TideHudLayout.JoystickSize.x*.5f)*scale;
            float right=TideHudLayout.Bounds(4,safe.rect.width,scale).xMin;
            statusText.rectTransform.anchorMin=statusText.rectTransform.anchorMax=Vector2.zero;
            statusText.rectTransform.anchoredPosition=new Vector2((left+right)*.5f,43);statusText.rectTransform.sizeDelta=new Vector2(Mathf.Max(420,right-left-40),70);statusText.fontSize=20;
            LayoutFieldDialogue();
            actionText.rectTransform.anchorMin=actionText.rectTransform.anchorMax=Vector2.zero;
            actionText.rectTransform.anchoredPosition=new Vector2((left+right)*.5f,115);actionText.rectTransform.sizeDelta=new Vector2(Mathf.Max(420,right-left-40),36);
        }
        private void ResetFeedback(){ResetSkillEffects();feelAttackSerial=Session.AttackSerial;feelImpactSerial=Session.HitStopEvents;shakeTime=0;impactOffset=Vector2.zero;previousHp=Session.Hp;previousShield=Session.Shield;shownHp=Session.Hp;feedbackDamageSerial=Session.DamageEvents;playerFeedbackTime=enemyFeedbackTime=0;}
        private void UpdateFeedback(float dt)
        {
            controlsOpacity.alpha=AttackHold.Held||Joystick.Value.sqrMagnitude>.01f?Mathf.Max(.9f,Session.Profile.controlOpacity):Session.Profile.controlOpacity;
            bool touching=Joystick.Owner!=int.MinValue;
            float stickOpacity=touching?1:JoystickIdleOpacity;
            joystickOpacity.alpha=Mathf.MoveTowards(joystickOpacity.alpha,stickOpacity,dt*(touching?10:3.5f));
            UpdateAttackButton(dt);
            string classState=Session.IsFighting?TideUiInfo.ClassState(Session):"";
            classChip.gameObject.SetActive(classState.Length>0);
            Say(classStateText,classState);
            chargeFill.transform.parent.gameObject.SetActive(Session.IsCharging);chargeFill.fillAmount=Session.HeavyProgress;
            var warningPlate=(RectTransform)warningText.transform.parent;
            warningPlate.gameObject.SetActive(Session.IsFighting&&(Session.Telegraph||Session.BodyExposed));
            LayoutWarningPlate(warningPlate,Session.IsTraining);
            warningFill.fillAmount=Session.BodyExposed?Session.EnemyRecovery/1.3f:Session.EnemyTimer/Mathf.Max(.01f,Session.WarningDuration);
            warningFill.color=Session.BodyExposed?new Color(.5f,.9f,.75f):gold;
            if(Session.IsFighting)
            {
                if(Session.Hp<previousHp-.01f){damageTrailAge=0;shownHp=Mathf.Max(shownHp,previousHp);}
                damageTrailAge+=dt;
                if(damageTrailAge>.12f)shownHp=Mathf.Lerp(shownHp,Session.Hp,1-Mathf.Exp(-dt*10));
                if(damageTrailAge>.65f)shownHp=Session.Hp;
                shownHp=Mathf.Max(shownHp,Session.Hp);
                if(Mathf.Abs(previousHp-Session.Hp)>.01f)
                {
                    float delta=Session.Hp-previousHp;playerFeedback.text=(delta>0?"+":"−")+Mathf.CeilToInt(Mathf.Abs(delta));
                    playerFeedback.color=delta>0?new Color(.55f,.95f,.72f):new Color(1,.65f,.5f);playerFeedbackTime=.85f;
                }
                else if(Session.Shield<previousShield-.01f){playerFeedback.text="보호막 흡수";playerFeedback.color=new Color(.65f,.86f,1);playerFeedbackTime=.85f;}
                if(Session.DamageEvents!=feedbackDamageSerial){enemyFeedback.text=Mathf.CeilToInt(Session.LastDamage).ToString();enemyFeedbackTime=.65f;}
                playerFeedbackTime=Mathf.Max(0,playerFeedbackTime-dt);enemyFeedbackTime=Mathf.Max(0,enemyFeedbackTime-dt);
            }
            else if(!Session.InBattle){shownHp=Session.Hp;playerFeedbackTime=enemyFeedbackTime=0;}
            previousHp=Session.Hp;previousShield=Session.Shield;feedbackDamageSerial=Session.DamageEvents;
            hpTrailFill.fillAmount=shownHp/Session.MaxHp;
            playerFeedback.gameObject.SetActive(Session.IsFighting&&playerFeedbackTime>0);enemyFeedback.gameObject.SetActive(false);
            Vector2 playerValue=Project(Session.X,Session.Y)+new Vector2(-95,120+(.85f-playerFeedbackTime)*25);
            Vector2 enemyValue=Project(Session.EnemyX,Session.EnemyY)+new Vector2(75,120+(.65f-enemyFeedbackTime)*25);
            playerValue.y=Mathf.Min(playerValue.y,safe.rect.height-285);enemyValue.y=Mathf.Min(enemyValue.y,safe.rect.height-285);
            enemyValue.x=Mathf.Clamp(enemyValue.x,130,safe.rect.width-525*Session.Profile.controlScale-100);
            playerFeedback.rectTransform.anchoredPosition=playerValue;enemyFeedback.rectTransform.anchoredPosition=enemyValue;
            bool showBodies=Session.Mode==TideMode.Town||Session.InBattle;
            playerShadow.gameObject.SetActive(showBodies);enemyShadow.gameObject.SetActive(false);
            playerShadow.anchoredPosition=Project(Session.X,Session.Y);playerShadow.localScale=Vector3.one*(1-Mathf.Clamp01(Session.Z/2)*.25f);enemyShadow.anchoredPosition=Project(Session.EnemyX,Session.EnemyY);
        }

        private void LayoutWarningPlate(RectTransform plate,bool training)
        {
            Vector2 anchor=new Vector2(training?1:.5f,1);
            if(plate.anchorMin!=anchor)
            {
                float width=training?350:428;
                plate.anchorMin=plate.anchorMax=anchor;
                plate.sizeDelta=new Vector2(width,56);
                warningText.rectTransform.sizeDelta=new Vector2(width-(training?36:26),37);
                ((RectTransform)warningFill.transform.parent).sizeDelta=warningFill.rectTransform.sizeDelta=new Vector2(width-44,4);
            }
            plate.anchoredPosition=training?new Vector2(-184,-217):new Vector2(86,battleRoot.activeSelf?-168:-112);
        }
        private void BuildHelp()
        {
            MenuTitle("조작과 기술",TideStory.HeroName(Session.Profile.hero)+" · "+Session.Hero.role);
            Button("help-controls","조작법",panel,new Vector2(.5f,.5f),new Vector2(-240,85),new Vector2(440,84),()=>{helpTab=0;Refresh();},helpTab==0);
            Button("help-skills","기술 설명",panel,new Vector2(.5f,.5f),new Vector2(240,85),new Vector2(440,84),()=>{helpTab=1;Refresh();},helpTab==1);
            if(helpTab==0)
            {
                Label("왼손 패드로 이동 · 공격 버튼 세 번으로 기본 3연계\n공격 주변에 장착한 기술 4개 · 타격 직후 기술로 연계\n패드를 위로 곧게 밀고 첫 공격: 띄우기 · 점프 후 공격: 공중 연계\n강공격은 모아 놓기 · 정밀 회피 성공 후 공격: 반격\n기술 그림 가운데 숫자는 재사용까지 남은 시간입니다.\n오른쪽 상황 버튼으로 현장 조사와 출구를 이용합니다."+(ShowKeyboardHints?"\n방향키 · Z/X/C/Space · A/S/D/F · V 회복":""),panel,Center,new Vector2(0,-74),new Vector2(950,216),24);
            }
            else
            {
                for(int i=0;i<TideLoadouts.SlotCount;i++)
                {
                    int slot=i;
                    var button=Button("help-slot-"+i,"",panel,Center,new Vector2((i-1.5f)*236,-30),new Vector2(220,84),
                        ()=>{helpSlot=slot;pendingFocus="help-slot-"+slot;Refresh();},helpSlot==i);
                    AddHelpSkillIcon(button,i);
                }
                int index=Session.EquippedSkill(helpSlot);
                SkillDef skill=Session.Hero.Skill(index);
                string learn=Session.SkillAvailable(index)?"":" · "+TideProgress.SkillRequirement(index);
                Label(skill.name+learn+" · 영압 "+skill.spirit_cost+" · 재사용 "+skill.cooldown_seconds+"초\n"+TideUiInfo.Skill(Session.Profile.hero,index),panel,Center,new Vector2(0,-129),new Vector2(950,96),25);
            }
            bool loadoutLink=helpTab==1&&Session.CanEditSkills;
            Button("back","뒤로",panel,Center,new Vector2(loadoutLink?-180:0,-232),new Vector2(330,84),NavigateBack);
            if(loadoutLink)Button("help-loadout","기술 장착",panel,Center,new Vector2(180,-232),new Vector2(330,84),OpenSkillLoadout,true);
        }
    }
}
