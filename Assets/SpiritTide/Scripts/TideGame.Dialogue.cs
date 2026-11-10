using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        public static readonly Vector2 SceneSpeechSize=new Vector2(976,104);
        private static readonly Vector2 TownSpeechSize=new Vector2(976,136);
        private static readonly Color DialogueInk=new Color(.075f,.14f,.19f,.94f);
        private static readonly Color DialogueChoice=new Color(.14f,.26f,.32f,.74f);
        private static readonly Color DialogueFocus=new Color(.25f,.44f,.49f,.96f);
        private readonly Dictionary<Sprite,Sprite> dialogueCrops=new Dictionary<Sprite,Sprite>();
        private Sprite dialogueShade,dialogueRibbon;
        private Image enteringPortrait;
        private Vector2 portraitRest;
        private float portraitTime;
        private string shownSpeaker;
        private RectTransform fieldDialogue;
        private Image fieldPortrait,fieldTime;
        private Text fieldSpeaker,fieldSpeech;
        private StoryLine displayedFieldLine;

        private RectTransform OpenDialogueStage(string name,string setting)
        {
            Clear(dialogueRoot.transform);
            enteringPortrait=null;
            SceneBackground(name+"Setting",dialogueRoot.transform,setting);
            if(dialogueShade==null)dialogueShade=MakeDialogueShade();
            var shade=Stretch("DialogueVignette",dialogueRoot.transform).gameObject.AddComponent<Image>();
            shade.sprite=dialogueShade;shade.color=new Color(.016f,.035f,.055f);shade.raycastTarget=false;
            return Node(name,dialogueRoot.transform,Center,Vector2.zero,new Vector2(1080,610));
        }

        private void DialogueHeading(RectTransform stage,string location,string title)
        {
            Label(location,stage,Center,new Vector2(-193,278),new Vector2(638,30),19,gold,true);
            if(!string.IsNullOrEmpty(title))
                Label(title,stage,Center,new Vector2(-193,242),new Vector2(638,44),31,ink,true);
            Picture("LocationRule",stage,Center,new Vector2(-469,258),new Vector2(84,2),new Color(.83f,.69f,.46f,.78f));
        }

        private static Sprite MakeDialogueShade()
        {
            const int height=128;
            var texture=new Texture2D(2,height,TextureFormat.RGBA32,false){name="Dialogue depth",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[2*height];
            for(int y=0;y<height;y++)
            {
                float t=y/(height-1f);
                Color color=new Color(1,1,1,.12f+.66f*Mathf.Pow(1-t,2.35f)+.34f*Mathf.Pow(t,5));
                pixels[y*2]=pixels[y*2+1]=color;
            }
            texture.SetPixels32(pixels);texture.Apply(false,true);
            return Sprite.Create(texture,new Rect(0,0,2,height),Center);
        }

        private static Sprite MakeDialogueRibbon()
        {
            const int size=128,cut=22;
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name="Dialogue ribbon",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++)
            {
                float bevel=Mathf.Min(x+size-1-y,size-1-x+y)-cut;
                float alpha=Mathf.Clamp01((bevel+.75f)*.72f);
                float shade=Mathf.Lerp(.66f,1,y/(size-1f));
                pixels[y*size+x]=new Color(shade,shade,shade,alpha);
            }
            texture.SetPixels32(pixels);texture.Apply(false,true);
            return Sprite.Create(texture,new Rect(0,0,size,size),Center,100,0,SpriteMeshType.FullRect,new Vector4(26,26,26,26));
        }

        private Sprite DialogueRibbon
        {
            get { if(dialogueRibbon==null)dialogueRibbon=MakeDialogueRibbon();return dialogueRibbon; }
        }

        private Image Ribbon(string name,Transform parent,Vector2 at,Vector2 size,Color tint)
        {
            var image=Picture(name,parent,Center,at,size,tint,DialogueRibbon);
            image.type=Image.Type.Sliced;
            return image;
        }

        private static Color SpeakerColor(string speaker)
        {
            switch(speaker)
            {
                case "루키아":return new Color(.58f,.87f,1);
                case "오리히메":return new Color(1,.80f,.54f);
                case "요루이치":return new Color(.82f,.71f,1);
                case "소이폰":return new Color(1,.84f,.52f);
                case "란기쿠":return new Color(1,.70f,.60f);
                case "우노하나":return new Color(.65f,.91f,.83f);
                default:return new Color(.67f,.89f,.89f);
            }
        }

        private Sprite PortraitCrop(string speaker,TideDungeonDef dungeon=null,bool encounter=false)
        {
            Sprite source=StoryPortrait(speaker,dungeon,encounter);
            if(source==null)return null;
            if(dialogueCrops.TryGetValue(source,out var crop))return crop;
            Rect bounds=source.rect;
            bool foe=encounter&&speaker.EndsWith("의 기록",StringComparison.Ordinal);
            float bottom=speaker=="콘"?.35f:foe?.18f:.49f;
            float top=speaker=="콘"?.86f:1;
            float sides=speaker=="콘"?.18f:foe?0:.10f;
            var rect=new Rect(bounds.x+bounds.width*sides,bounds.y+bounds.height*bottom,
                bounds.width*(1-2*sides),bounds.height*(top-bottom));
            crop=Sprite.Create(source.texture,rect,Center,100,0,SpriteMeshType.FullRect);
            crop.name="Dialogue-"+source.name;
            dialogueCrops.Add(source,crop);
            return crop;
        }

        private void PlaceDialoguePortrait(RectTransform stage,Sprite portrait,string speaker,bool active,float y=45)
        {
            if(portrait==null)return;
            Vector2 at=new Vector2(active?-296:300,y);
            var image=Picture(active?"SpeakerPortrait":"ListeningPortrait",stage,Center,at,
                active?new Vector2(424,360):new Vector2(330,316),
                active?Color.white:new Color(.67f,.76f,.82f,.60f),portrait);
            image.preserveAspect=true;
            if(!active)return;
            if(!Session.Profile.reducedEffects&&(lastMode!=Session.Mode||shownSpeaker!=speaker))
            {
                enteringPortrait=image;portraitTime=0;portraitRest=at;
                image.rectTransform.anchoredPosition=at+new Vector2(-13,-4);
                image.color=new Color(1,1,1,.64f);
            }
            shownSpeaker=speaker;
        }

        private void TickDialoguePresentation(float dt)
        {
            if(enteringPortrait==null||!dialogueRoot.activeSelf)return;
            portraitTime=Mathf.Min(1,portraitTime+dt/.18f);
            float ease=1-(1-portraitTime)*(1-portraitTime);
            enteringPortrait.rectTransform.anchoredPosition=portraitRest+new Vector2(-13,-4)*(1-ease);
            enteringPortrait.color=new Color(1,1,1,Mathf.Lerp(.64f,1,ease));
            if(portraitTime>=1)enteringPortrait=null;
        }

        private float DrawDialogueSpeech(RectTransform stage,string speaker,string text,float y,string channel="")
        {
            Color hue=SpeakerColor(speaker);
            bool story=Session.Mode==TideMode.Story;
            Vector2 textArea=story?SceneSpeechSize:TownSpeechSize;
            float height=textArea.y+42,heading=height*.5f-4;
            var strip=Ribbon("DialogueSpeech",stage,new Vector2(0,y),new Vector2(1044,height),DialogueInk).rectTransform;
            Picture("SpeechRule",strip,Center,new Vector2(0,heading),new Vector2(1012,1),new Color(hue.r,hue.g,hue.b,.54f));
            var body=Label(text,strip,Center,new Vector2(0,-12),textArea,Mathf.RoundToInt((story?32:28)*Session.Profile.uiScale));
            body.name=story?"StorySpeech":"TownSpeech";
            body.alignment=TextAnchor.UpperLeft;
            var title=Label(speaker,strip,Center,new Vector2(-249,heading),new Vector2(470,46),28,hue,true);
            title.name="DialogueSpeaker";
            float nameWidth=Mathf.Clamp(title.preferredWidth+48,152,486);
            var nameplate=Ribbon("SpeakerNameplate",strip,new Vector2(-495+nameWidth*.5f,heading),new Vector2(nameWidth,46),
                new Color(.09f,.19f,.24f,.99f));
            nameplate.transform.SetSiblingIndex(title.transform.GetSiblingIndex());
            title.rectTransform.sizeDelta=new Vector2(nameWidth-39,46);
            title.rectTransform.anchoredPosition=new Vector2(-475+(nameWidth-39)*.5f,heading);
            Picture("SpeakerMark",strip,Center,new Vector2(-486,heading),new Vector2(3,24),hue);
            if(channel.Length>0)
                Label(channel,strip,Center,new Vector2(-473+nameWidth+150,heading),new Vector2(300,28),18,muted);
            return y+heading;
        }

        private Button DialogueButton(string id,string text,Transform stage,Vector2 at,Vector2 size,Action action,bool primary=false)
        {
            var button=Button(id,text,stage,Center,at,size,action,primary);
            var image=button.GetComponent<Image>();
            image.sprite=DialogueRibbon;
            image.color=primary?DialogueFocus:DialogueChoice;
            if(!primary&&Session.Mode==TideMode.Story)image.color=new Color(.10f,.21f,.27f,.46f);
            var label=button.GetComponentInChildren<Text>();
            label.fontSize=24;
            if(primary)
            {
                label.rectTransform.anchoredPosition=new Vector2(-8,0);
                label.rectTransform.sizeDelta=new Vector2(size.x-46,46);
                float edge=size.x*.5f-23;
                Stroke("ReplyChevron",button.transform,new Vector2(edge-4,6),new Vector2(edge+3,0),2,ink);
                Stroke("ReplyChevron",button.transform,new Vector2(edge+3,0),new Vector2(edge-4,-6),2,ink);
            }
            return button;
        }

        private void BuildFieldDialogue()
        {
            var panel=Picture("FieldDialogue",safe,Vector2.zero,Vector2.zero,new Vector2(480,94),DialogueInk,DialogueRibbon);
            panel.type=Image.Type.Sliced;fieldDialogue=panel.rectTransform;
            fieldDialogue.SetSiblingIndex(statusText.transform.GetSiblingIndex()+1);
            fieldPortrait=Picture("FieldSpeakerPortrait",fieldDialogue,Center,Vector2.zero,new Vector2(64,68),Color.white);
            fieldPortrait.preserveAspect=true;
            fieldSpeaker=Label("",fieldDialogue,Center,new Vector2(28,28),new Vector2(360,26),18,ink,true);
            fieldSpeaker.name="FieldSpeaker";
            fieldSpeech=Label("",fieldDialogue,Center,new Vector2(28,-11),new Vector2(360,56),18,ink);
            fieldSpeech.name="FieldSpeech";
            fieldSpeech.alignment=TextAnchor.UpperLeft;
            fieldTime=Picture("FieldSpeechTime",fieldDialogue,Center,new Vector2(28,-40),new Vector2(360,2),muted);
            fieldTime.type=Image.Type.Filled;fieldTime.fillMethod=Image.FillMethod.Horizontal;
            LayoutFieldDialogue();
            fieldDialogue.gameObject.SetActive(false);
        }

        private void LayoutFieldDialogue()
        {
            if(fieldDialogue==null)return;
            var status=statusText.rectTransform;
            float width=status.sizeDelta.x+12;
            fieldDialogue.anchoredPosition=new Vector2(status.anchoredPosition.x,54);
            fieldDialogue.sizeDelta=new Vector2(width,94);
            fieldPortrait.rectTransform.anchoredPosition=new Vector2(-width*.5f+46,1);
            fieldSpeaker.rectTransform.sizeDelta=new Vector2(width-108,26);
            fieldSpeech.rectTransform.sizeDelta=new Vector2(width-108,56);
            fieldTime.rectTransform.sizeDelta=new Vector2(width-108,2);
        }

        private void DrawFieldDialogue(StoryLine line)
        {
            bool show=line!=null;
            if(fieldDialogue.gameObject.activeSelf!=show)fieldDialogue.gameObject.SetActive(show);
            if(!show)return;
            if(line!=displayedFieldLine)
            {
                displayedFieldLine=line;
                Say(fieldSpeaker,line.speaker);
                Say(fieldSpeech,line.text);
                fieldPortrait.sprite=PortraitCrop(line.speaker);
                fieldPortrait.enabled=fieldPortrait.sprite!=null;
                fieldSpeaker.color=SpeakerColor(line.speaker);
                fieldTime.color=fieldSpeaker.color;
            }
            fieldTime.fillAmount=Mathf.Clamp01((fieldNext-Session.PresentationClock)/FieldLineTime(line));
        }

        private void ReleaseDialogueArt()
        {
            foreach(var portrait in dialogueCrops.Values)Destroy(portrait);
            if(dialogueRibbon!=null){Destroy(dialogueRibbon.texture);Destroy(dialogueRibbon);}
            if(dialogueShade!=null){Destroy(dialogueShade.texture);Destroy(dialogueShade);}
        }
    }
}
