using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private Sprite surfaceSkin;
        private readonly Color surfaceDark = new Color(.13f,.23f,.30f,.99f);
        private readonly Color surfaceMenu = new Color(.17f,.29f,.37f,.99f);
        private readonly Color surfaceButton = new Color(.22f,.36f,.44f,.98f);
        private readonly Color surfaceAction = new Color(.36f,.61f,.66f);
        private RectTransform playerVitals, wallet, enemyVitals, classChip;
        private Text vitalityValue;
        private Image poiseFill;
        private bool compactEnemyVitals;

        private Image Surface(string name,Transform parent,Vector2 anchor,Vector2 position,Vector2 size,Color color)
        {
            var image=Picture(name,parent,anchor,position,size,color,surfaceSkin);
            image.type=Image.Type.Sliced;
            return image;
        }

        private void Stroke(string name,Transform parent,Vector2 from,Vector2 to,float width,Color color)
        {
            Vector2 span=to-from;
            var stroke=Picture(name,parent,Center,(from+to)*.5f,new Vector2(span.magnitude,width),color);
            stroke.rectTransform.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(span.y,span.x)*Mathf.Rad2Deg);
        }

        private void BuildPlayerVitals()
        {
            Vector2 upperLeft=new Vector2(0,1),upperRight=new Vector2(1,1);
            playerVitals=Surface("PlayerVitals",safe,upperLeft,new Vector2(170,-82),new Vector2(324,140),surfaceDark).rectTransform;
            Picture("VitalsAccent",playerVitals,Center,new Vector2(-159,24),new Vector2(3,38),new Color(.51f,.85f,.82f));
            titleText=Label("",playerVitals,Center,new Vector2(0,46),new Vector2(286,26),20,gold,true);
            hpText=Label("",playerVitals,Center,new Vector2(-47,16),new Vector2(192,32),24,ink,true);
            vitalityValue=Label("",playerVitals,Center,new Vector2(98,16),new Vector2(90,29),20,ink);
            vitalityValue.alignment=TextAnchor.MiddleRight;
            hpFill=Meter(playerVitals,new Vector2(162,-76),286,new Color(.44f,.82f,.63f));
            mpFill=Meter(playerVitals,new Vector2(162,-96),286,new Color(.48f,.72f,.94f));
            resourceText=Label("",playerVitals,Center,new Vector2(0,-49),new Vector2(286,26),19,muted);

            classChip=Surface("ActiveClassState",safe,upperLeft,new Vector2(170,-175),new Vector2(324,34),surfaceDark).rectTransform;
            classStateText=Label("",classChip,Center,Vector2.zero,new Vector2(296,30),18,ink);
            classStateText.alignment=TextAnchor.MiddleCenter;

            wallet=Surface("TownWallet",safe,upperRight,new Vector2(-247,-49),new Vector2(190,72),surfaceDark).rectTransform;
            goldText=Label("",wallet,Center,new Vector2(0,13),new Vector2(154,32),22,gold,true);
            saveIndicator=Label("자동 저장",wallet,Center,new Vector2(0,-13),new Vector2(154,25),17,muted);
            saveIndicator.name="SaveIndicator";
        }

        private void BuildEnemyVitals()
        {
            battleRoot=Stretch("BossHud",safe).gameObject;
            enemyVitals=Surface("EnemyVitals",battleRoot.transform,new Vector2(.5f,1),new Vector2(0,-49),new Vector2(620,74),surfaceDark).rectTransform;
            enemyText=Label("",enemyVitals,Center,new Vector2(0,12),new Vector2(578,32),23,ink,true);
            enemyText.alignment=TextAnchor.MiddleCenter;
            enemyFill=Meter(enemyVitals,new Vector2(310,-54),574,new Color(.83f,.43f,.37f));
            poiseFill=Meter(enemyVitals,new Vector2(310,-61),574,new Color(.49f,.75f,.92f));
            var poiseTrack=(RectTransform)poiseFill.transform.parent;
            poiseTrack.sizeDelta=new Vector2(574,5);
            poiseFill.rectTransform.sizeDelta=poiseTrack.sizeDelta;
        }

        private void LayoutEnemyVitals(bool training)
        {
            if(compactEnemyVitals==training)return;
            compactEnemyVitals=training;
            float width=training?350:620;
            enemyVitals.anchorMin=enemyVitals.anchorMax=new Vector2(training?1:.5f,1);
            enemyVitals.anchoredPosition=training?new Vector2(-184,-144):new Vector2(0,-49);
            enemyVitals.sizeDelta=new Vector2(width,74);
            enemyText.rectTransform.sizeDelta=new Vector2(width-36,32);
            enemyText.fontSize=training?20:23;
            foreach(Image fill in new[]{enemyFill,poiseFill})
            {
                var track=(RectTransform)fill.transform.parent;
                Vector2 size=new Vector2(width-46,track.sizeDelta.y);
                track.sizeDelta=fill.rectTransform.sizeDelta=size;
                track.anchoredPosition=new Vector2(width*.5f,track.anchoredPosition.y);
            }
        }

        private void UpdateVitals()
        {
            Say(hpText,TideStory.HeroName(Session.Profile.hero)+" · Lv."+Session.Profile.level);
            Say(vitalityValue,Mathf.CeilToInt(Session.Hp)+" / "+Session.MaxHp);
            hpFill.fillAmount=Session.Hp/Session.MaxHp;
            mpFill.fillAmount=Session.Mp/100;
            Say(resourceText,"영압 "+Mathf.FloorToInt(Session.Mp)+(Session.Shield>0?"    보호막 "+Mathf.CeilToInt(Session.Shield):""));
            if(wallet.gameObject.activeSelf)Say(goldText,Session.Profile.gold+" 골드");
            if(battleRoot.activeSelf)
            {
                var target=Session.Target;
                string phase=target.Def?.id=="a3_boss_ulquiorra"?"  ·  "+target.Phase+"단계":"";
                bool boss=target.Def!=null&&target.Def.IsBoss;
                string poise=boss?"  ·  자세 "+Mathf.CeilToInt(target.Poise):"";
                Say(enemyText,Session.EnemyName+phase+"    "+Mathf.CeilToInt(Session.EnemyHp)+" / "+Mathf.CeilToInt(Session.EnemyMaxHp)+poise);
                enemyFill.fillAmount=Session.EnemyHp/Mathf.Max(1,Session.EnemyMaxHp);
                ((RectTransform)enemyFill.transform.parent).anchoredPosition=new Vector2(enemyVitals.sizeDelta.x*.5f,boss?-47:-54);
                poiseFill.transform.parent.gameObject.SetActive(boss);
                poiseFill.fillAmount=target.Poise/100;
            }
        }
    }
}
