using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private Text potionName,potionDetail,attackChain;
        private Button fieldAction,gateAction;
        private Text fieldActionLabel,gateActionLabel;
        private const float JoystickIdleOpacity=.48f;
        private static readonly string[] BasicChainLabels={"1 / 3","2 / 3","3 / 3"};
        private CanvasGroup joystickOpacity;
        private RectTransform attackGlyph;
        private TideArc attackGlint;
        private float attackPress;
        private Sprite touchDisc,touchRim;

        private void BuildTouchControls()
        {
            touchDisc=MakeTouchSprite("Touch disc",true);
            touchRim=MakeTouchSprite("Touch rim",true,true);
            BuildJoystick(touchDisc,touchRim);
            BuildAttackButton();
            BuildUtilityControls();

            for(int i=0;i<skillButtons.Length;i++)
            {
                int slot=i;
                skillButtons[i]=CombatButton(i+5,"",()=>Session.RequestEquippedSkill(slot),Color.clear);
                skillLabels[i]=skillButtons[i].GetComponentInChildren<Text>();
            }

            Vector2 right=new Vector2(1,0);
            fieldAction=Button("field-interact","",controlsRoot.transform,right,TideHudLayout.ContextCenter,TideHudLayout.ContextSize,InteractField,true);
            fieldActionLabel=fieldAction.GetComponentInChildren<Text>();
            gateAction=Button("gate","",controlsRoot.transform,right,TideHudLayout.ContextCenter,TideHudLayout.ContextSize,AdvanceRoom,true);
            gateActionLabel=gateAction.GetComponentInChildren<Text>();
            foreach(Text label in new[]{fieldActionLabel,gateActionLabel})label.fontSize=23;
        }

        private void BuildAttackButton()
        {
            var attack=CombatButton(0,"공격",null,new Color(.22f,.24f,.25f,.96f),true);
            Transform root=attack.transform;
            var name=attack.GetComponentInChildren<Text>();
            name.rectTransform.anchoredPosition=new Vector2(0,-17);
            name.rectTransform.sizeDelta=new Vector2(104,35);
            name.fontSize=26;
            name.font=bold;
            attackChain=Label("연계",root,Center,new Vector2(0,-39),new Vector2(72,22),15,gold);
            attackChain.alignment=TextAnchor.MiddleCenter;

            Picture("AttackRing",root,Center,Vector2.zero,new Vector2(128,128),new Color(.88f,.70f,.44f,.58f),touchRim);
            Picture("AttackInnerRing",root,Center,Vector2.zero,new Vector2(116,116),new Color(.88f,.74f,.54f,.23f),touchRim);
            attackGlint=ControlArc("AttackGlint",root,Vector2.zero,new Vector2(124,124),58,109,.047f,
                new Color(1,.85f,.58f,.38f));
            ControlArc("AttackLowerGlint",root,Vector2.zero,new Vector2(124,124),225,63,.037f,new Color(.84f,.67f,.40f,.40f));

            attackGlyph=Node("AttackGlyph",root,Center,new Vector2(0,24),new Vector2(70,48));
            ControlArc("BladeTrail",attackGlyph,new Vector2(1,-1),new Vector2(66,41),-53,172,.16f,
                new Color(.95f,.73f,.40f,.82f));
            ControlArc("BladeShine",attackGlyph,new Vector2(1,0),new Vector2(62,36),-47,135,.071f,
                new Color(1,.92f,.72f,.87f));
            Stroke("Blade",attackGlyph,new Vector2(-11,-9),new Vector2(21,14),5,new Color(1,.92f,.75f));
            Stroke("BladeTip",attackGlyph,new Vector2(17,12),new Vector2(25,18),2.4f,ink);
            Stroke("SwordGrip",attackGlyph,new Vector2(-20,-16),new Vector2(-10,-9),4,gold);
            Stroke("SwordGuard",attackGlyph,new Vector2(-15,-4),new Vector2(-7,-15),2.6f,gold);
            Picture("BladeSpark",attackGlyph,Center,new Vector2(26,18),new Vector2(5,5),new Color(1,.87f,.60f,.8f),circle);

            AttackHold=attack.gameObject.AddComponent<TideHold>();
            AttackHold.Down=()=>{
                blockHeldAttackUntilRelease=false;
                Vector2 stick=Joystick.Value;
                RequestAttack(stick.y>.82f&&Mathf.Abs(stick.x)<.22f);
            };
        }

        private void RequestAttack(bool launch)=>Session.Request(launch&&!Session.BasicChainActive?MoveKind.Launcher:MoveKind.Basic);

        private TideArc ControlArc(string name,Transform root,Vector2 center,Vector2 size,float heading,float sweep,float thickness,Color tint)
        {
            var arc=Node(name,root,Center,center,size).gameObject.AddComponent<TideArc>();
            arc.Shape(sweep,thickness,30);
            arc.rectTransform.localRotation=Quaternion.Euler(0,0,heading);
            arc.color=tint;arc.raycastTarget=false;
            return arc;
        }

        private void UpdateAttackButton(float dt)
        {
            Say(attackChain,Session.BasicChainActive||Session.HasActionPose&&Session.Action==MoveKind.Basic?BasicChainLabels[Session.Combo]:"연계");
            float target=AttackHold.Held?1:0;
            bool reduced=Session.Profile.reducedEffects;
            float next=reduced?target:Mathf.MoveTowards(attackPress,target,dt*(target>attackPress?18:8));
            if(Mathf.Approximately(next,attackPress))return;
            attackPress=next;
            attackGlint.color=new Color(1,.85f,.58f,Mathf.Lerp(.38f,.93f,next));
            attackGlyph.localScale=Vector3.one*(reduced?1:1-.075f*next);
        }

        private void BuildUtilityControls()
        {
            Color amber=new Color(.98f,.82f,.55f), sky=new Color(.73f,.84f,1);
            Color teal=new Color(.61f,.90f,.98f), mint=new Color(.61f,.92f,.76f);
            var heavy=UtilityButton(1,"강공격",null,amber,true);
            UtilityCaption(heavy,"길게 누르기",amber);
            var strike=UtilityGlyph(heavy,18);
            Stroke("HeavyCut",strike,new Vector2(-10,11),new Vector2(6,-6),4,amber);
            Stroke("HeavyPoint",strike,new Vector2(6,-6),new Vector2(9,-10),2,ink);
            Stroke("HeavyGuard",strike,new Vector2(-11,5),new Vector2(-5,11),2,ink);
            ControlArc("HeavyImpact",strike,new Vector2(4,-8),new Vector2(27,12),-90,162,.17f,amber);
            heavyHold=heavy.gameObject.AddComponent<TideHold>();
            heavyHold.Down=Session.BeginHeavy;
            heavyHold.Up=Session.ReleaseHeavy;

            var jump=UtilityButton(2,"점프",()=>Session.Jump(),sky);
            var lift=UtilityGlyph(jump,13);
            Stroke("JumpStem",lift,new Vector2(0,-5),new Vector2(0,12),3,sky);
            Stroke("JumpTip",lift,new Vector2(-8,4),new Vector2(0,12),3,sky);
            Stroke("JumpTip",lift,new Vector2(0,12),new Vector2(8,4),3,sky);
            ControlArc("JumpLift",lift,new Vector2(0,-6),new Vector2(28,11),-90,146,.19f,sky);

            var dodge=UtilityButton(3,"회피",()=>Session.Dodge(Joystick.Value.sqrMagnitude>.01f?Joystick.Value:keyboardMove),teal);
            var step=UtilityGlyph(dodge,13);
            Stroke("DodgeTrail",step,new Vector2(-16,0),new Vector2(-7,0),1.6f,new Color(teal.r,teal.g,teal.b,.55f));
            for(int i=0;i<2;i++)
            {
                float x=-6+i*10;
                var shade=new Color(teal.r,teal.g,teal.b,i==0?.62f:1);
                Stroke("DodgeChevron",step,new Vector2(x-2,7),new Vector2(x+5,0),2.6f,shade);
                Stroke("DodgeChevron",step,new Vector2(x+5,0),new Vector2(x-2,-7),2.6f,shade);
            }

            var potion=UtilityButton(4,"회복",()=>{if(Session.UsePotion())Persist();},mint,true);
            potionName=potion.GetComponentInChildren<Text>();
            potionName.fontSize=17;
            potionDetail=UtilityCaption(potion,"",mint);
            var heal=UtilityGlyph(potion,18);
            ControlArc("HealPulse",heal,Vector2.zero,new Vector2(27,27),35,254,.10f,new Color(mint.r,mint.g,mint.b,.52f));
            Stroke("HealCross",heal,new Vector2(-7,0),new Vector2(7,0),3,mint);
            Stroke("HealCross",heal,new Vector2(0,-7),new Vector2(0,7),3,mint);
        }

        private Button UtilityButton(int index,string title,System.Action action,Color accent,bool hasCaption=false)
        {
            var button=CombatButton(index,title,action,Color.clear);
            Transform root=button.transform;
            var plate=Picture("UtilityPlate",root,Center,Vector2.zero,new Vector2(86,86),
                Color.Lerp(new Color(.12f,.20f,.24f,.95f),accent,.16f),touchDisc);
            plate.transform.SetAsFirstSibling();
            button.targetGraphic=plate;
            Picture("UtilityRing",root,Center,Vector2.zero,new Vector2(84,84),
                new Color(accent.r,accent.g,accent.b,.54f),touchRim);
            ControlArc("UtilitySheen",root,Vector2.zero,new Vector2(83,83),76,97,.035f,
                new Color(accent.r,accent.g,accent.b,.35f));
            var label=button.GetComponentInChildren<Text>();
            label.rectTransform.anchoredPosition=new Vector2(0,hasCaption?-10:-17);
            label.rectTransform.sizeDelta=new Vector2(80,25);
            label.font=bold;
            label.fontSize=20;
            return button;
        }

        private RectTransform UtilityGlyph(Button button,float y)
        {
            return Node("UtilityGlyph",button.transform,Center,new Vector2(0,y),new Vector2(42,36));
        }

        private Text UtilityCaption(Button button,string caption,Color accent)
        {
            var label=Label(caption,button.transform,Center,new Vector2(0,-28),new Vector2(78,18),13,accent);
            label.alignment=TextAnchor.MiddleCenter;
            return label;
        }

        private void DrawContextAction()
        {
            bool campaign=Session.IsFighting&&Session.IsCampaign;
            bool near=campaign&&Session.NearInteraction;
            bool ward=campaign&&Session.WardActive&&!Session.RoomCleared;
            bool investigation=campaign&&Session.RoomCleared&&Session.MainInvestigationPending;
            bool signal=campaign&&Session.RoomSignalPending;
            bool showField=near||ward||investigation||signal;
            bool showGate=campaign&&Session.RoomCleared&&!showField;
            fieldAction.gameObject.SetActive(showField);
            gateAction.gameObject.SetActive(showGate);
            if(showField)
            {
                fieldAction.interactable=near;
                if(near)
                {
                    string label=Session.FieldInteractionLabel;
                    Say(fieldActionLabel,ShowKeyboardHints?label:label.Replace(" · E",""));
                }
                else
                    Say(fieldActionLabel,signal?"신호 표지로 이동":ward?"영압 장치로 이동":"조사 표지로 이동");
            }
            if(showGate)
            {
                bool ready=Session.IsFieldVisit||Session.AtGate&&Session.GateReady;
                gateAction.interactable=ready;
                Say(gateActionLabel,Session.IsFieldVisit?"거점으로 귀환"
                    :ready?"다음 방"+(ShowKeyboardHints?" · E":"")+"  ›":"오른쪽 출구로 이동  ›");
            }
        }

        private Button CombatButton(int index,string label,System.Action action,Color color,bool primary=false)
        {
            var button=Button(TideHudLayout.Ids[index],label,controlsRoot.transform,new Vector2(1,0),
                TideHudLayout.Positions[index],TideHudLayout.Sizes[index],action,primary);
            var image=button.GetComponent<Image>();
            image.sprite=touchDisc;
            image.type=Image.Type.Simple;
            image.color=color;
            if(color.a==0)image.canvasRenderer.cullTransparentMesh=false;
            button.GetComponentInChildren<Text>().fontSize=22;
            var colors=button.colors;
            colors.highlightedColor=new Color(.94f,.97f,1);
            colors.pressedColor=new Color(.72f,.88f,.94f);
            colors.disabledColor=new Color(.57f,.63f,.68f);
            colors.fadeDuration=.075f;
            button.colors=colors;
            return button;
        }

        private void BuildJoystick(Sprite disc,Sprite rim)
        {
            var baseImage=Picture("MoveJoystick",controlsRoot.transform,Vector2.zero,TideHudLayout.JoystickCenter,
                TideHudLayout.JoystickSize,new Color(.14f,.24f,.31f,.16f),disc);
            baseImage.raycastTarget=true;
            Joystick=baseImage.gameObject.AddComponent<TideJoystick>();
            var root=baseImage.transform;
            joystickOpacity=root.gameObject.AddComponent<CanvasGroup>();
            joystickOpacity.alpha=JoystickIdleOpacity;
            Picture("GuideShadow",root,Center,Vector2.zero,new Vector2(208,208),new Color(.03f,.08f,.11f,.67f),rim);
            Picture("DirectionGuide",root,Center,Vector2.zero,new Vector2(204,204),new Color(.72f,.86f,.92f,.66f),rim);
            Joystick.DirectionRing=Picture("ActiveHeading",root,Center,Vector2.zero,new Vector2(204,204),new Color(.65f,.95f,1,.98f),rim);
            Joystick.DirectionRing.type=Image.Type.Filled;
            Joystick.DirectionRing.fillMethod=Image.FillMethod.Radial360;
            Joystick.DirectionRing.fillOrigin=(int)Image.Origin360.Right;
            Joystick.DirectionRing.fillClockwise=true;
            Joystick.DirectionRing.enabled=false;

            foreach(var direction in new[]{Vector2.up,Vector2.right,Vector2.down,Vector2.left})
            {
                Vector2 tickSize=direction.x!=0?new Vector2(2,8):new Vector2(8,2);
                Picture("DirectionMark",root,Center,direction*89,tickSize,new Color(.77f,.90f,.95f,.60f));
            }
            Joystick.Knob=Picture("Knob",root,Center,Vector2.zero,new Vector2(64,64),new Color(.43f,.64f,.72f,.52f),disc).rectTransform;
            Picture("KnobEdge",Joystick.Knob,Center,Vector2.zero,new Vector2(60,60),new Color(.79f,.90f,.95f,.79f),rim);
            Picture("ThumbGrip",Joystick.Knob,Center,Vector2.zero,new Vector2(7,7),new Color(.88f,.96f,1,.9f),circle);
        }

        private static Sprite MakeTouchSprite(string name,bool round,bool ring=false,bool sliced=false)
        {
            const int size=128;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var point=new Vector2(x-63.5f,y-63.5f);
                if(ring)
                {
                    float stroke=Mathf.Clamp01(1.35f-Mathf.Abs(point.magnitude-60));
                    pixels[y*size+x]=new Color(1,1,1,stroke);
                    continue;
                }
                var corner=new Vector2(Mathf.Max(0,Mathf.Abs(point.x)-46),Mathf.Max(0,Mathf.Abs(point.y)-46));
                float edge=round?point.magnitude-62:corner.magnitude-16;
                float alpha=Mathf.Clamp01(.6f-edge);
                float border=Mathf.Clamp01((edge+3.8f)*.34f);
                float surface=Mathf.Lerp(.37f,.59f,y/(size-1f));
                float shade=Mathf.Lerp(surface,1,border);
                pixels[y*size+x]=new Color(shade,shade,shade,alpha);
            }
            texture.SetPixels32(pixels);
            texture.Apply(false,true);
            Vector4 sliceBorder=sliced?new Vector4(20,20,20,20):Vector4.zero;
            return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,sliceBorder);
        }
    }
}
