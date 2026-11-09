using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private readonly Image[] townFigures=new Image[5],memorials=new Image[5],roomMarkers=new Image[TideCampaign.MaxRooms];
        private readonly Text[] townSigns=new Text[5];private Text townProgress;
        private Image wardFigure,echoFigure,signalFigure,environmentArea,environmentSecond;private Text wardLabel,echoLabel,signalLabel;
        public void AdvanceRoom()
        {
            if(MaybeQuestStory())return;
            if(Session.IsFieldVisit&&Session.IsFighting&&Session.RoomCleared){ReturnTown();return;}
            if(Session.EnterGate()){MaybeEncounterStory();Persist();Refresh();}
        }
        private bool MaybeEncounterStory()
        {
            if(!Session.IsCampaign||Session.IsReplay||Session.IsPaused)return false;
            TideMode restore=Session.Mode;bool intro=restore==TideMode.Combat,outro=restore==TideMode.Result;
            if(!intro&&!outro)return false;
            int bit=Session.Room.kind=="boss"?TideExperience.EncounterBit(Session.Room.foes[0]):0;
            if(bit==0)return false;
            string sceneId=TideDialogue.DungeonId(Session.Dungeon,intro?"intro":"clear");
            if(intro&&(Session.Profile.seenIntros&bit)!=0||outro&&TideDialogue.Seen(Session.Profile,sceneId)!=null)return false;
            string previousStatus=Session.Status;
            ShowDialogueScene(sceneId,()=>{
                if(intro)Session.Profile.seenIntros|=bit;else if(bit!=0)Session.Profile.seenOutros|=bit;
                Session.Mode=restore;Session.Status=previousStatus;Session.Revision++;Persist();Refresh();
            },intro?"전투 시작":"결과 확인");return true;
        }
        private void BuildExperienceWorld()
        {
            string[] ids={"urahara","tessai","kon","byakuya","kukaku"};string[] names={"우라하라\n의뢰·보급","텟사이\n정비·수련","콘\n구조 소식","뱌쿠야\n조사 기록","쿠카쿠\n출정"};
            for(int i=0;i<5;i++)
            {
                int index=i;var b=Button("town-figure-"+i,"",actorLayer,Vector2.zero,Vector2.zero,new Vector2(i==2?100:156,i==2?100:218),()=>Talk(ids[index]));
                townFigures[i]=b.GetComponent<Image>();townFigures[i].sprite=TideArt.Grid("town_npcs_v1",i,3,2);townFigures[i].color=Color.white;townFigures[i].preserveAspect=true;((RectTransform)b.transform).pivot=new Vector2(.5f,0);
                townSigns[i]=Label(names[i],world,Vector2.zero,Vector2.zero,new Vector2(142,44),17,gold,true);townSigns[i].alignment=TextAnchor.MiddleCenter;
                memorials[i]=Picture("Memorial-"+i,world,Vector2.zero,Vector2.zero,new Vector2(56,90),Color.white,TideArt.Grid("town_npcs_v1",5,3,2));memorials[i].preserveAspect=true;
            }
            townProgress=Label("",safe,new Vector2(1,1),new Vector2(-160,-155),new Vector2(280,76),22,gold,true);townProgress.alignment=TextAnchor.MiddleCenter;townProgress.transform.SetSiblingIndex(menuRoot.transform.GetSiblingIndex());
            wardFigure=Picture("FieldWard",world,Vector2.zero,Vector2.zero,new Vector2(48,92),new Color(.65f,.9f,1),TideArt.Grid("town_npcs_v1",5,3,2));wardFigure.preserveAspect=true;
            wardLabel=Label("영압 장치 · E",world,Vector2.zero,Vector2.zero,new Vector2(240,36),21,gold,true);wardLabel.alignment=TextAnchor.MiddleCenter;
            echoFigure=Picture("RescueLantern",world,Vector2.zero,Vector2.zero,new Vector2(55,95),new Color(.6f,1,.85f),TideArt.Grid("town_npcs_v1",5,3,2));echoFigure.preserveAspect=true;
            echoLabel=Label("정령의 귀환 등불 · E",world,Vector2.zero,Vector2.zero,new Vector2(280,36),21,gold,true);echoLabel.alignment=TextAnchor.MiddleCenter;
            signalFigure=Picture("ExpeditionSignal",world,Vector2.zero,Vector2.zero,new Vector2(52,96),new Color(1,.8f,.44f),TideArt.Grid("town_npcs_v1",5,3,2));signalFigure.preserveAspect=true;
            signalLabel=Label("",world,Vector2.zero,Vector2.zero,new Vector2(266,40),21,gold,true);signalLabel.alignment=TextAnchor.MiddleCenter;
            foreach(var value in new[]{wardFigure,echoFigure,signalFigure})value.transform.SetSiblingIndex(actorLayer.GetSiblingIndex());
            environmentArea=Picture("EnvironmentTell",world,Vector2.zero,Vector2.zero,new Vector2(100,30),new Color(.25f,.8f,1,.3f));environmentArea.transform.SetSiblingIndex(actorLayer.GetSiblingIndex());
            environmentSecond=Picture("EnvironmentPreview",world,Vector2.zero,Vector2.zero,new Vector2(100,30),new Color(.35f,.65f,1,.14f));environmentSecond.transform.SetSiblingIndex(actorLayer.GetSiblingIndex());
            Button("field-interact","",safe,Vector2.zero,new Vector2(140,445),new Vector2(220,84),InteractField,true);Buttons["field-interact"].transform.SetSiblingIndex(menuRoot.transform.GetSiblingIndex());
            for(int i=0;i<roomMarkers.Length;i++){roomMarkers[i]=Picture("RoomMarker-"+i,safe,new Vector2(.5f,1),new Vector2((i-3.5f)*14,-156),new Vector2(10,4),muted);roomMarkers[i].transform.SetSiblingIndex(menuRoot.transform.GetSiblingIndex());}
        }
        private void DrawExperienceWorld()
        {
            bool town=!Session.InBattle;int restored=0,rescues=0;
            for(int i=0;i<5;i++)
            {
                bool reported=(Session.Profile.reportedActs&(1<<i))!=0;if(reported)restored++;
                rescues+=Session.RescuesInAct(i+1);
                townFigures[i].gameObject.SetActive(town);townSigns[i].gameObject.SetActive(town);memorials[i].gameObject.SetActive(town);
                ((RectTransform)townFigures[i].transform).anchoredPosition=Project(-4+i*2,.45f)+(i==2?new Vector2(0,6):Vector2.zero);townFigures[i].GetComponent<Button>().interactable=Session.Mode==TideMode.Town;
                townSigns[i].rectTransform.anchoredPosition=Project(-4+i*2,.45f)+new Vector2(0,-20);
                townSigns[i].color=Session.Mode==TideMode.Town&&NearestNpcIndex()==i?new Color(.65f,1,.85f):gold;
                memorials[i].rectTransform.anchoredPosition=new Vector2(safe.rect.width*.82f+(i-2)*45,safe.rect.height*.42f);memorials[i].color=reported?new Color(.85f,1,.8f):new Color(.3f,.38f,.42f,.6f);
            }
            townProgress.gameObject.SetActive(town);Say(townProgress,"귀환 기록 · 봉인 "+restored+" / 5\n정령 안내 "+rescues+" / 30");
            Say(wardLabel,"영압 장치"+(ShowKeyboardHints?" · E":""));Say(echoLabel,"정령의 귀환 등불"+(ShowKeyboardHints?" · E":""));
            bool ward=Session.IsFighting&&Session.IsCampaign&&Session.WardActive&&!Session.RoomCleared,echo=Session.IsFighting&&Session.EchoAvailable;
            wardFigure.gameObject.SetActive(ward);wardLabel.gameObject.SetActive(ward);
            echoFigure.gameObject.SetActive(echo||FirstLanternInSight);echoLabel.gameObject.SetActive(echo);
            echoFigure.color=echo?new Color(.6f,1,.85f):new Color(.6f,1,.85f,.62f);
            wardFigure.rectTransform.anchoredPosition=Project(Session.WardPosition.x,Session.WardPosition.y)+new Vector2(0,45);wardLabel.rectTransform.anchoredPosition=wardFigure.rectTransform.anchoredPosition+new Vector2(0,67);
            echoFigure.rectTransform.anchoredPosition=Project(Session.EchoX,0)+new Vector2(0,48);echoLabel.rectTransform.anchoredPosition=echoFigure.rectTransform.anchoredPosition+new Vector2(0,72);
            bool signal=Session.IsFighting&&Session.RoomSignalPending;
            signalFigure.gameObject.SetActive(signal);signalLabel.gameObject.SetActive(signal);
            if(signal)
            {
                signalFigure.rectTransform.anchoredPosition=Project(Session.SignalPosition.x,Session.SignalPosition.y)+new Vector2(0,48);
                signalLabel.rectTransform.anchoredPosition=signalFigure.rectTransform.anchoredPosition+new Vector2(0,68);
                Say(signalLabel,Session.Room.signal_title);
            }
            Buttons["field-interact"].gameObject.SetActive(ward||echo||signal||Session.IsFighting&&Session.QuestPointCount>0);Buttons["field-interact"].interactable=Session.NearInteraction;
            Say(Buttons["field-interact"].GetComponentInChildren<Text>(),ShowKeyboardHints?Session.FieldInteractionLabel:Session.FieldInteractionLabel.Replace(" · E",""));
            bool hazard=ward&&Session.EnvironmentWarning;environmentArea.gameObject.SetActive(hazard);environmentSecond.gameObject.SetActive(hazard&&Session.Room.hazard=="shadow"&&Session.EnvironmentTimer>Session.EnvironmentDuration*.45f);
            if(hazard)
            {
                string kind=Session.Room.hazard;float unit=TideHudLayout.UnitX(safe.rect.width,Session.Profile.controlScale);environmentArea.rectTransform.anchoredPosition=Project(Session.EnvironmentX,Session.EnvironmentY);
                environmentArea.sprite=kind=="rubble"?circle:square;environmentArea.rectTransform.sizeDelta=kind=="rubble"?new Vector2(unit*2.3f,80.5f):new Vector2(safe.rect.width*2,kind=="barrier"?25:34);
                environmentArea.rectTransform.localRotation=Quaternion.Euler(0,0,kind=="barrier"?Mathf.Atan(7/unit)*Mathf.Rad2Deg:0);environmentArea.color=kind=="shadow"&&Session.EnvironmentTimer>Session.EnvironmentDuration*.45f?new Color(.3f,.65f,1,.16f):new Color(.4f,.75f,1,.36f);
                environmentSecond.rectTransform.anchoredPosition=Project(Session.EnvironmentX,-Session.EnvironmentY);environmentSecond.rectTransform.sizeDelta=new Vector2(safe.rect.width*2,34);
            }
            int rooms=Session.IsCampaign?Session.Dungeon.rooms.Length:0;
            for(int i=0;i<roomMarkers.Length;i++)
            {
                bool active=Session.InBattle&&i<rooms;
                roomMarkers[i].gameObject.SetActive(active);
                if(!active)continue;
                roomMarkers[i].rectTransform.anchoredPosition=new Vector2((i-(rooms-1)*.5f)*14,-162);
                roomMarkers[i].color=i<Session.RoomIndex?new Color(.5f,.85f,.7f):i==Session.RoomIndex?gold:muted;
            }
        }
        private int RescueReportAct(){for(int a=1;a<=5;a++)if(Session.CanReportRescue(a))return a;return 0;}
        private void ReportTownRescue()
        {
            int act=RescueReportAct();if(act==0){Session.Status="각 액트에서 귀환 등불 두 곳을 안내한 뒤 콘에게 소식을 전하세요.";return;}
            if(Session.ReportRescue(act)){Persist();ShowDialogueScene("a"+act+"_rescue",ReturnTown);}
        }
    }
}
