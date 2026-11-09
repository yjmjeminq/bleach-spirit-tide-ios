using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private void BuildTouchControls()
        {
            Sprite disc=MakeTouchSprite("Touch disc",true);
            Sprite rim=MakeTouchSprite("Touch rim",true,true);
            Sprite tile=MakeTouchSprite("Touch tile",false);
            BuildJoystick(disc,rim);

            var attack=CombatButton(0,"공격",null,disc,new Color(.90f,.73f,.43f),true);
            var attackName=attack.GetComponentInChildren<Text>();
            attackName.rectTransform.anchoredPosition=new Vector2(0,-16);
            attackName.rectTransform.sizeDelta=new Vector2(106,35);
            attackName.fontSize=27;
            var combo=Label("연계",attack.transform,Center,new Vector2(0,-39),new Vector2(72,23),16,gold);
            combo.alignment=TextAnchor.MiddleCenter;
            var slash=Picture("AttackSlash",attack.transform,Center,new Vector2(-4,24),new Vector2(7,37),gold,circle);
            slash.rectTransform.localRotation=Quaternion.Euler(0,0,-37);
            var second=Picture("AttackFollowThrough",attack.transform,Center,new Vector2(11,23),new Vector2(4,25),ink,circle);
            second.rectTransform.localRotation=slash.rectTransform.localRotation;
            AttackHold=attack.gameObject.AddComponent<TideHold>();
            AttackHold.Down=()=>{
                blockHeldAttackUntilRelease=false;
                Session.Request(Joystick.Value.y>.55f?MoveKind.Launcher:MoveKind.Basic);
            };

            var utility=new Color(.33f,.55f,.62f);
            var heavy=CombatButton(1,"강공격\n길게",null,tile,utility);
            heavyHold=heavy.gameObject.AddComponent<TideHold>();
            heavyHold.Down=()=>Session.BeginHeavy();
            heavyHold.Up=()=>Session.ReleaseHeavy();
            CombatButton(2,"점프",()=>Session.Jump(),tile,utility);
            CombatButton(3,"회피",()=>Session.Dodge(Joystick.Value.sqrMagnitude>.01f?Joystick.Value:keyboardMove),tile,utility);
            var potion=CombatButton(4,"회복",()=>{if(Session.UsePotion())Persist();},tile,new Color(.41f,.65f,.56f));
            potion.GetComponentInChildren<Text>().fontSize=19;

            for(int i=0;i<skillButtons.Length;i++)
            {
                int slot=i;
                Color color=i==4?new Color(.76f,.62f,.42f):new Color(.33f,.59f,.67f);
                skillButtons[i]=CombatButton(i+5,"",()=>Session.Request((MoveKind)((int)MoveKind.Skill0+slot)),tile,color,i==4);
                skillLabels[i]=skillButtons[i].GetComponentInChildren<Text>();
            }
        }

        private Button CombatButton(int index,string label,System.Action action,Sprite sprite,Color color,bool primary=false)
        {
            var button=Button(TideHudLayout.Ids[index],label,controlsRoot.transform,new Vector2(1,0),
                TideHudLayout.Positions[index],TideHudLayout.Sizes[index],action,primary);
            var image=button.GetComponent<Image>();
            image.sprite=sprite;
            image.color=color;
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
                TideHudLayout.JoystickSize,new Color(.20f,.36f,.43f,.86f),disc);
            baseImage.raycastTarget=true;
            Joystick=baseImage.gameObject.AddComponent<TideJoystick>();
            var root=baseImage.transform;
            Picture("DirectionGuide",root,Center,Vector2.zero,new Vector2(205,205),new Color(.43f,.69f,.74f,.57f),rim);
            Picture("ThumbTravelGuide",root,Center,Vector2.zero,new Vector2(155,155),new Color(.32f,.56f,.64f,.36f),rim);
            Joystick.DirectionRing=Picture("ActiveHeading",root,Center,Vector2.zero,new Vector2(216,216),new Color(.57f,.94f,1),rim);
            Joystick.DirectionRing.type=Image.Type.Filled;
            Joystick.DirectionRing.fillMethod=Image.FillMethod.Radial360;
            Joystick.DirectionRing.fillOrigin=(int)Image.Origin360.Right;
            Joystick.DirectionRing.fillClockwise=true;
            Joystick.DirectionRing.enabled=false;

            foreach(var direction in new[]{Vector2.up,Vector2.right,Vector2.down,Vector2.left})
            {
                var mark=Picture("DirectionMark",root,Center,direction*94,new Vector2(7,7),new Color(.65f,.83f,.86f,.8f));
                mark.rectTransform.localRotation=Quaternion.Euler(0,0,45);
            }
            Joystick.Knob=Picture("Knob",root,Center,Vector2.zero,new Vector2(82,82),new Color(.53f,.79f,.84f),disc).rectTransform;
            Picture("KnobEdge",Joystick.Knob,Center,Vector2.zero,new Vector2(66,66),new Color(.76f,.96f,.96f,.42f),rim);
            for(int i=-1;i<=1;i++)
                Picture("ThumbGrip",Joystick.Knob,Center,new Vector2(i*10,0),new Vector2(5,5),new Color(.89f,.98f,1,.85f),circle);
        }

        private static Sprite MakeTouchSprite(string name,bool round,bool ring=false)
        {
            const int size=128;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                var point=new Vector2(x-63.5f,y-63.5f);
                if(ring)
                {
                    float stroke=Mathf.Clamp01(1.8f-Mathf.Abs(point.magnitude-60));
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
            return Sprite.Create(texture,new Rect(0,0,size,size),new Vector2(.5f,.5f));
        }
    }
}
