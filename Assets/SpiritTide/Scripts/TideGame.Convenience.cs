using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private int gearFilter,gearSort,archivePage,archiveFilter;
        private bool archiveIndex;
        private int routeAct,routeStage,routeRoom;
        private string routeTitle;
        private bool routeVisit;
        private Text saveIndicator;
        private readonly Text[] skillKeys=new Text[5];
        private bool ShowKeyboardHints=>Session.Profile.keyboardHints==1||Session.Profile.keyboardHints==0&&!Application.isMobilePlatform&&!smokeSafeArea.HasValue;

        private void BuildHome()
        {
            MenuTitle("빠른 메뉴");
            Button("help","도움말",panel,new Vector2(.5f,.5f),new Vector2(370,230),new Vector2(84,84),ShowHelp).GetComponentInChildren<Text>().fontSize=20;
            string next=Session.CampaignFinished?"완료한 이야기 다시 읽기":Session.Quest==QuestState.Available?"우라하라에게 이번 의뢰 듣기":Session.Quest==QuestState.Ready?"우라하라에게 귀환 보고":"조사 계속 · "+TideCampaign.Dungeon(Session.Profile.campaignAct,Session.Profile.campaignStage).title;
            Button("home-primary",next,panel,new Vector2(.5f,.5f),new Vector2(-165,112),new Vector2(620,84),ContinueObjective,true);
            Button("home-guide","이야기 · 전투 준비",panel,new Vector2(.5f,.5f),new Vector2(325,112),new Vector2(310,84),()=>OpenJourney()).GetComponentInChildren<Text>().fontSize=24;
            string[] ids={"home-quests","home-gear","home-map","home-journal","home-shop","home-settings"};
            string[] names={"의뢰 수첩","장비 비교 · 강화","출정 지도","조사 기록 · 대화록","회복 영자 보급","보기 · 조작 설정"};
            System.Action[] actions={()=>OpenQuestBook(),()=>OpenMenu(TideMode.Inventory),()=>OpenMenu(TideMode.Missions),OpenJournal,()=>OpenMenu(TideMode.Shop),()=>OpenMenu(TideMode.Settings)};
            for(int i=0;i<ids.Length;i++)Button(ids[i],names[i],panel,new Vector2(.5f,.5f),new Vector2((i%3-1)*310,8-i/3*100),new Vector2(294,84),actions[i]).GetComponentInChildren<Text>().fontSize=24;
            Label("Lv."+Session.Profile.level+" · 회복 영자 "+Session.Profile.potions+" / 5 · 장비 "+Session.Profile.gear.Length+" / 48",panel,new Vector2(.5f,.5f),new Vector2(0,-167),new Vector2(950,40),22,muted);
            Button("back","게임으로",panel,new Vector2(.5f,.5f),new Vector2(0,-236),new Vector2(950,84),NavigateBack);
        }
        private void ContinueObjective()
        {
            if(Session.CampaignFinished){OpenJournal();return;}
            if(Session.Quest==QuestState.Available||Session.Quest==QuestState.Ready){Talk("urahara");return;}
            OpenRoute(Session.Profile.campaignAct,Session.Profile.campaignStage,Session.Profile.roomCheckpoint,"진행 중인 본편 조사");
        }
        private List<TideGear> VisibleGear()
        {
            var items=new List<TideGear>();
            foreach(var item in Session.Profile.gear)if(gearFilter==0||item.slot==(gearFilter==1?"power":"guard"))items.Add(item);
            items.Sort((a,b)=>{if(gearSort==1){int slot=a.slot.CompareTo(b.slot);if(slot!=0)return slot;int delta=(b.slot=="guard"?b.Health:b.Attack).CompareTo(a.slot=="guard"?a.Health:a.Attack);if(delta!=0)return delta;}return b.uid.CompareTo(a.uid);});
            return items;
        }
        private void SelectLoot(int uid)
        {
            gearFilter=0;gearSelection=uid;var items=VisibleGear();gearPage=Mathf.Max(0,items.FindIndex(g=>g.uid==uid))/3;
        }
        private void ChangeGearPage(int delta)
        {
            var items=VisibleGear();gearPage=Mathf.Clamp(gearPage+delta,0,Mathf.Max(0,(items.Count-1)/3));
            if(items.Count>0)gearSelection=items[gearPage*3].uid;Refresh();
        }
        private void OpenRoute(int act,int stage,int room,string title,bool visit=false)
        {
            OpenMenu(TideMode.Missions);mapAct=routeAct=act;routeStage=stage;routeRoom=room;routeTitle=title;routeVisit=visit;Refresh();
        }
        private void ClearRoute(){routeAct=routeStage=routeRoom=0;routeTitle=null;routeVisit=false;}
        private bool RouteReady()
        {
            if(routeAct<1||routeStage<1)return false;
            if(routeVisit&&TideCampaign.CanVisitRoom(Session.Profile,routeAct,routeStage,routeRoom))return true;
            var d=TideCampaign.Dungeon(routeAct,routeStage);
            bool cleared=(Session.Profile.clearedDungeons&(1<<d.index))!=0;
            bool current=routeAct==Session.Profile.campaignAct&&routeStage==Session.Profile.campaignStage&&Session.Quest==QuestState.Active;
            return cleared||current&&Session.Profile.roomCheckpoint<=routeRoom;
        }
        private void DepartRoute()
        {
            if(!RouteReady())return;
            if(routeVisit&&TideCampaign.CanVisitRoom(Session.Profile,routeAct,routeStage,routeRoom))
            {
                BeginDungeonNow(routeAct,routeStage,true,routeRoom);
                return;
            }
            bool current=routeAct==Session.Profile.campaignAct&&routeStage==Session.Profile.campaignStage&&Session.Quest==QuestState.Active;
            // A cleared current dungeon may still need replay to revisit an earlier room.
            bool replay=!current||Session.Profile.roomCheckpoint>routeRoom;
            DepartDungeon(routeAct,routeStage,replay);
        }
        private void RouteToWard(TideSideQuest q)
        {
            // Prefer a ward ahead of the checkpoint, then a previously visited room.
            for(int pass=0;pass<2;pass++)for(int stage=1;stage<=6;stage++)
            {
                var d=TideCampaign.Dungeon(q.act,stage);
                bool current=q.act==Session.Profile.campaignAct&&stage==Session.Profile.campaignStage&&Session.Quest==QuestState.Active;
                for(int room=0;room<d.rooms.Length;room++)if(d.rooms[room].ward&&((pass==0&&current&&room>=Session.Profile.roomCheckpoint)||(pass==1&&TideCampaign.CanVisitRoom(Session.Profile,q.act,stage,room))))
                {OpenRoute(q.act,stage,room,q.title+" · 장치 해제",true);return;}
            }
            for(int stage=1;stage<=6;stage++){var d=TideCampaign.Dungeon(q.act,stage);for(int room=0;room<d.rooms.Length;room++)if(d.rooms[room].ward){OpenRoute(q.act,stage,room,q.title+" · 장치 해제",true);return;}}
        }
        private List<TideDialogueScene> ArchiveScenes()
        {
            var result=new List<TideDialogueScene>();
            foreach(var scene in TideDialogue.Available(Session.Profile,journalAct))
            {
                bool side=scene.kind.StartsWith("side_"),party=scene.kind=="party";
                if(archiveFilter==0||archiveFilter==1&&!side&&!party||archiveFilter==2&&party||archiveFilter==3&&side)result.Add(scene);
            }
            return result;
        }
        private void BuildArchiveIndex()
        {
            journalAct=Mathf.Clamp(journalAct,1,5);var scenes=ArchiveScenes();archivePage=Mathf.Clamp(archivePage,0,Mathf.Max(0,(scenes.Count-1)/6));
            MenuTitle("대화 장면 목록 · "+(archivePage+1)+" / "+Mathf.Max(1,(scenes.Count+5)/6));
            for(int a=1;a<=5;a++){int act=a;Button("archive-act-"+a,"Act "+a,panel,new Vector2(.5f,.5f),new Vector2((a-3)*185,132),new Vector2(168,84),()=>{journalAct=act;archivePage=0;Refresh();},a==journalAct);}
            string[] categories={"전체","본편","동료","부탁"};
            for(int i=0;i<4;i++){int filter=i;Button("archive-filter-"+i,categories[i],panel,new Vector2(.5f,.5f),new Vector2((i-1.5f)*240,40),new Vector2(224,84),()=>{archiveFilter=filter;archivePage=0;Refresh();},i==archiveFilter);}
            for(int i=0;i<6&&archivePage*6+i<scenes.Count;i++)
            {
                var scene=scenes[archivePage*6+i];
                Button("archive-scene-"+i,scene.title,panel,new Vector2(.5f,.5f),new Vector2((i%3-1)*310,-52-i/3*92),new Vector2(294,84),()=>{
                    dialogueScene=TideDialogue.Available(Session.Profile,journalAct).FindIndex(s=>s.id==scene.id);dialogueLine=0;archiveIndex=false;Refresh();
                }).GetComponentInChildren<Text>().fontSize=20;
            }
            if(scenes.Count==0)Label("아직 열린 장면이 없습니다. 이야기를 진행하면 이곳에 기록됩니다.",panel,new Vector2(.5f,.5f),new Vector2(0,-98),new Vector2(950,110),26,muted);
            Button("archive-prev","‹",panel,new Vector2(.5f,.5f),new Vector2(-435,-236),new Vector2(88,84),()=>{archivePage--;Refresh();}).interactable=archivePage>0;
            Button("archive-next","›",panel,new Vector2(.5f,.5f),new Vector2(-325,-236),new Vector2(88,84),()=>{archivePage++;Refresh();}).interactable=(archivePage+1)*6<scenes.Count;
            Button("dialogue-reader","문장으로 돌아가기",panel,new Vector2(.5f,.5f),new Vector2(-40,-236),new Vector2(380,84),()=>{archiveIndex=false;Refresh();},true);
            Button("back","뒤로",panel,new Vector2(.5f,.5f),new Vector2(330,-236),new Vector2(280,84),NavigateBack);
        }
    }
}
