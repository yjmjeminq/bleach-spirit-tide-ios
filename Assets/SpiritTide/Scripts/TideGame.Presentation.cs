using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private CanvasGroup controlsOpacity;
        private readonly Text[] skillFooters=new Text[5];
        private readonly Image[] skillCooldownFills=new Image[5];
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
            hpTrailFill=Picture("RecentDamage",hpFill.transform.parent,new Vector2(.5f,.5f),Vector2.zero,new Vector2(330,11),new Color(.9f,.6f,.35f));
            hpTrailFill.type=Image.Type.Filled;hpTrailFill.fillMethod=Image.FillMethod.Horizontal;hpTrailFill.transform.SetAsFirstSibling();
            classStateText=Label("",safe,new Vector2(0,1),new Vector2(180,-160),new Vector2(330,54),21,muted);
            // The state is part of the HUD, behind modal shades.
            classStateText.transform.SetSiblingIndex(controlsRoot.transform.GetSiblingIndex());
            Image chargeTrack=Picture("ChargeTrack",Buttons["heavy"].transform,new Vector2(.5f,.5f),new Vector2(0,-40),new Vector2(82,6),new Color(.03f,.08f,.1f));
            chargeFill=Picture("ChargeFill",chargeTrack.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(82,6),gold);
            chargeFill.type=Image.Type.Filled;chargeFill.fillMethod=Image.FillMethod.Horizontal;
            Image warningTrack=Picture("TellClock",world,new Vector2(.5f,1),new Vector2(0,-243),new Vector2(720,5),new Color(.13f,.21f,.24f));
            warningFill=Picture("TellClockFill",warningTrack.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(720,5),gold);
            warningFill.type=Image.Type.Filled;warningFill.fillMethod=Image.FillMethod.Horizontal;
            playerShadow=Picture("PlayerShadow",world,Vector2.zero,Vector2.zero,new Vector2(75,16),new Color(0,0,0,.3f),circle).rectTransform;
            enemyShadow=Picture("EnemyShadow",world,Vector2.zero,Vector2.zero,new Vector2(180,24),new Color(0,0,0,.25f),circle).rectTransform;
            playerShadow.SetSiblingIndex(warning.GetSiblingIndex());enemyShadow.SetSiblingIndex(playerShadow.GetSiblingIndex());
            playerFeedback=Label("",world,Vector2.zero,Vector2.zero,new Vector2(220,40),28,gold,true);playerFeedback.alignment=TextAnchor.MiddleCenter;
            enemyFeedback=Label("",world,Vector2.zero,Vector2.zero,new Vector2(220,40),28,ink,true);enemyFeedback.alignment=TextAnchor.MiddleCenter;
            foreach(Text value in new[]{playerFeedback,enemyFeedback}){Outline edge=value.gameObject.AddComponent<Outline>();edge.effectColor=new Color(.015f,.04f,.05f,.95f);edge.effectDistance=new Vector2(1,-1);}
            for(int i=0;i<5;i++)
            {
                Transform button=skillButtons[i].transform;
                RectTransform name=skillLabels[i].rectTransform;name.sizeDelta=new Vector2(96,24);name.anchoredPosition=new Vector2(0,-21);skillLabels[i].fontSize=18;
                BuildSkillIcon(i);
                Image cd=Picture("Cooldown",button,new Vector2(.5f,.5f),new Vector2(0,22),new Vector2(66,60),new Color(0,.025f,.035f,.78f));
                cd.type=Image.Type.Filled;cd.fillMethod=Image.FillMethod.Radial360;cd.fillOrigin=2;cd.fillClockwise=true;skillCooldownFills[i]=cd;
                skillTimers[i]=Label("",button,new Vector2(.5f,.5f),new Vector2(0,22),new Vector2(64,44),28,Color.white,true);skillTimers[i].name="SkillCooldownNumber";skillTimers[i].alignment=TextAnchor.MiddleCenter;
                var timerOutline=skillTimers[i].gameObject.AddComponent<Outline>();timerOutline.effectColor=new Color(0,0,0,.95f);timerOutline.effectDistance=new Vector2(1,-1);
                Text key=Label(TideUiInfo.Keys[i],button,new Vector2(.5f,.5f),new Vector2(-40,43),new Vector2(18,18),15,gold,true);key.alignment=TextAnchor.MiddleCenter;skillKeys[i]=key;
                skillFooters[i]=Label("",button,new Vector2(.5f,.5f),new Vector2(0,-43),new Vector2(96,20),15,muted);skillFooters[i].alignment=TextAnchor.MiddleCenter;
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
            RectTransform joy=(RectTransform)Joystick.transform;joy.anchoredPosition=new Vector2(140,142)*scale;joy.localScale=Vector3.one*scale;
            string[] npcIds={"urahara","tessai","kon","byakuya","kukaku"};
            float dockShift=(255*scale-202)*.5f;
            for(int i=0;i<npcIds.Length;i++)((RectTransform)Buttons["npc-"+npcIds[i]].transform).anchoredPosition=new Vector2((i-2)*160+dockShift,0);
            float left=255*scale,right=safe.rect.width-525*scale;
            statusText.rectTransform.anchorMin=statusText.rectTransform.anchorMax=Vector2.zero;
            statusText.rectTransform.anchoredPosition=new Vector2((left+right)*.5f,43);statusText.rectTransform.sizeDelta=new Vector2(Mathf.Max(420,right-left-40),70);statusText.fontSize=20;
            actionText.rectTransform.anchorMin=actionText.rectTransform.anchorMax=Vector2.zero;
            actionText.rectTransform.anchoredPosition=new Vector2((left+right)*.5f,115);actionText.rectTransform.sizeDelta=new Vector2(Mathf.Max(420,right-left-40),36);
        }
        private void ResetFeedback(){ResetSkillEffects();feelAttackSerial=Session.AttackSerial;damageSerial=Session.DamageEvents;previousHp=Session.Hp;previousShield=Session.Shield;shownHp=Session.Hp;feedbackDamageSerial=Session.DamageEvents;playerFeedbackTime=enemyFeedbackTime=0;}
        private void UpdateFeedback(float dt)
        {
            controlsOpacity.alpha=AttackHold.Held||Joystick.Value.sqrMagnitude>.01f?Mathf.Max(.9f,Session.Profile.controlOpacity):Session.Profile.controlOpacity;
            classStateText.gameObject.SetActive(Session.InBattle);
            Say(classStateText,TideUiInfo.ClassState(Session));
            chargeFill.transform.parent.gameObject.SetActive(Session.IsCharging);chargeFill.fillAmount=Session.HeavyProgress;
            warningFill.transform.parent.gameObject.SetActive(Session.IsFighting&&(Session.Telegraph||Session.BodyExposed));
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
        private void BuildHelp()
        {
            MenuTitle("조작과 기술",TideStory.HeroName(Session.Profile.hero)+" · "+Session.Hero.role);
            Button("help-controls","조작법",panel,new Vector2(.5f,.5f),new Vector2(-240,85),new Vector2(440,84),()=>{helpTab=0;Refresh();},helpTab==0);
            Button("help-skills","기술 설명",panel,new Vector2(.5f,.5f),new Vector2(240,85),new Vector2(440,84),()=>{helpTab=1;Refresh();},helpTab==1);
            if(helpTab==0)
            {
                Label("왼손 패드로 이동 · 오른손 공격을 유지해 연계\n기본 타격 직후 기술로 연계 · 강공격·반격으로 보스 자세 붕괴\n패드 위 + 공격: 띄우기 · 점프 후 공격: 공중 연계\n강공격은 모아 놓기 · 정밀 회피 성공 후 공격: 반격\n기술 버튼 아래: 재사용 시간 · 필요한 영압"+(ShowKeyboardHints?"\n방향키 · Z/X/C/Space · A/S/D/F/R · V 회복":""),panel,new Vector2(.5f,.5f),new Vector2(0,-74),new Vector2(950,210),26);
            }
            else
            {
                for(int i=0;i<5;i++){int slot=i;var button=Button("help-slot-"+i,"",panel,new Vector2(.5f,.5f),new Vector2((i-2)*183,-30),new Vector2(167,84),()=>{helpSlot=slot;Refresh();},helpSlot==i);AddHelpSkillIcon(button,i);}
                SkillDef skill=Session.Hero.Skill(helpSlot);
                string learn=TideProgress.SkillLearned(Session.Profile,helpSlot)?"":" · "+TideProgress.SkillRequirement(helpSlot);
                Label(skill.name+learn+" · 영압 "+skill.spirit_cost+" · 재사용 "+skill.cooldown_seconds+"초\n"+TideUiInfo.Skill(Session.Profile.hero,helpSlot),panel,new Vector2(.5f,.5f),new Vector2(0,-130),new Vector2(950,100),26);
            }
            Button("back","뒤로",panel,new Vector2(.5f,.5f),new Vector2(0,-232),new Vector2(330,84),NavigateBack);
        }
    }
}
