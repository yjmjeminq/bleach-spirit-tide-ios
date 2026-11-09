using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private int mapAct=1,gearPage,gearSelection,lastPersistRevision;
        private Image scenery;
        private RectTransform actorLayer,gateView;
        private Text gateLabel,progressText;
        private RectTransform statusPlate;
        private sealed class FoeView
        {
            public RectTransform root,shadow;public TideFoeMotionGraphic body;public Image hp;public Text label,damage;
            public Image[] areas=new Image[4];public int uid,damageSerial;public float flash;
        }
        private readonly List<FoeView> foeViews=new List<FoeView>();
        private void BuildCampaignWorld()
        {
            scenery=Stretch("Scenery",world).gameObject.AddComponent<Image>();scenery.raycastTarget=false;scenery.color=Color.white;scenery.transform.SetAsFirstSibling();
            Image dim=Stretch("SceneryShade",world).gameObject.AddComponent<Image>();dim.color=new Color(.025f,.06f,.09f,.23f);dim.raycastTarget=false;dim.transform.SetSiblingIndex(1);
            var statePlate=Picture("PlayerStateBackdrop",safe,new Vector2(0,1),new Vector2(180,-146),new Vector2(360,92),new Color(.025f,.06f,.09f,.78f));statePlate.transform.SetSiblingIndex(resourceText.transform.GetSiblingIndex());
            statusPlate=Picture("StatusBackdrop",safe,Vector2.zero,Vector2.zero,new Vector2(600,70),new Color(.025f,.06f,.09f,.88f)).rectTransform;statusPlate.SetSiblingIndex(statusText.transform.GetSiblingIndex());
            actorLayer=(RectTransform)actor.parent;
            gateView=Picture("ExitGate",world,Vector2.zero,Vector2.zero,new Vector2(30,135),new Color(.4f,.95f,.85f,.6f),circle).rectTransform;
            gateLabel=Label("",gateView,new Vector2(.5f,.5f),new Vector2(0,96),new Vector2(220,52),22,gold,true);gateLabel.alignment=TextAnchor.MiddleCenter;
            progressText=Label("",safe,new Vector2(.5f,1),new Vector2(0,-180),new Vector2(720,36),22,muted);progressText.alignment=TextAnchor.MiddleCenter;progressText.transform.SetSiblingIndex(controlsRoot.transform.GetSiblingIndex());
            Button("gate","다음 방 · E",safe,new Vector2(.5f,0),new Vector2(0,190),new Vector2(280,84),AdvanceRoom,true);
            Buttons["gate"].transform.SetSiblingIndex(menuRoot.transform.GetSiblingIndex());
            Button("target-next","대상 변경",safe,Vector2.zero,new Vector2(140,350),new Vector2(200,84),()=>Session.CycleTarget());Buttons["target-next"].transform.SetSiblingIndex(menuRoot.transform.GetSiblingIndex());
            for(int i=0;i<4;i++)
            {
                var v=new FoeView();
                for(int j=0;j<4;j++){v.areas[j]=Picture("FoeArea-"+i+"-"+j,world,Vector2.zero,Vector2.zero,new Vector2(100,30),new Color(1,.35f,.15f,.3f));v.areas[j].transform.SetSiblingIndex(actorLayer.GetSiblingIndex());}
                v.shadow=Picture("FoeShadow-"+i,world,Vector2.zero,Vector2.zero,new Vector2(85,17),new Color(0,0,0,.3f),circle).rectTransform;v.shadow.SetSiblingIndex(actorLayer.GetSiblingIndex());
                int slot=i;var b=Button("target-"+i,"",actorLayer,Vector2.zero,Vector2.zero,new Vector2(150,205),()=>{if(slot<Session.Enemies.Count)Session.FocusEnemy(Session.Enemies[slot].Uid);});
                b.GetComponent<Image>().color=Color.clear;b.GetComponent<Image>().raycastTarget=false;v.body=Node("FoeSprite",b.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(150,205)).gameObject.AddComponent<TideFoeMotionGraphic>();v.body.color=Color.white;b.targetGraphic=v.body;v.body.preserveAspect=true;v.root=(RectTransform)b.transform;v.root.pivot=new Vector2(.5f,0);
                v.label=Label("",v.root,new Vector2(.5f,1),new Vector2(0,-15),new Vector2(260,35),19,ink,true);v.label.alignment=TextAnchor.MiddleCenter;
                v.hp=Meter(v.root,new Vector2(75,-5),100,new Color(.85f,.35f,.3f));var track=(RectTransform)v.hp.transform.parent;track.anchorMin=track.anchorMax=new Vector2(.5f,1);track.anchoredPosition=new Vector2(0,-34);
                v.damage=Label("",world,Vector2.zero,Vector2.zero,new Vector2(100,40),27,gold,true);v.damage.alignment=TextAnchor.MiddleCenter;
                foeViews.Add(v);
            }
        }
        private void DrawCampaignWorld()
        {
            bool battle=Session.InBattle;scenery.sprite=TideArt.Background(battle?Session.IsCampaign?Session.Dungeon.background:"battle_act_"+Session.BattleAct:"town");
            statusPlate.anchoredPosition=statusText.rectTransform.anchoredPosition;statusPlate.sizeDelta=statusText.rectTransform.sizeDelta+new Vector2(18,0);
            scenery.color=scenery.sprite==null?bg:Color.white;
            float pan=Session.InBattle?-(naturalCameraX-cameraOriginX)*TideHudLayout.UnitX(safe.rect.width,Session.Profile.controlScale):0;
            float margin=Session.InBattle?470:70,zoom=(safe.rect.width+margin*2)/(safe.rect.width+140),extra=(zoom-1)*safe.rect.height;
            scenery.rectTransform.offsetMin=new Vector2(-margin+pan,-16-extra*.4f)+impactOffset;scenery.rectTransform.offsetMax=new Vector2(margin+pan,16+extra*.6f)+impactOffset;
            enemyActor.gameObject.SetActive(false);warning.gameObject.SetActive(false);enemyShadow.gameObject.SetActive(false);enemyFeedback.gameObject.SetActive(false);
            actor.gameObject.SetActive(!Session.InBattle||battle);
            Vector2 pos=Project(Session.X,Session.Y)+new Vector2(0,Session.Z*54);actor.anchoredPosition=pos;
            float wave=Session.ActionRemaining>0?Mathf.Sin(Session.ActionElapsed/Mathf.Max(.01f,Session.ActionDuration)*Mathf.PI):0;

            var sorting=new List<KeyValuePair<float,Transform>>{new KeyValuePair<float,Transform>(Session.Y-.04f,actor)};
            for(int i=0;i<foeViews.Count;i++)
            {
                var v=foeViews[i];var e=i<Session.Enemies.Count?Session.Enemies[i]:null;bool show=battle&&e!=null&&e.Alive;
                v.root.gameObject.SetActive(show);v.shadow.gameObject.SetActive(show);v.damage.gameObject.SetActive(false);foreach(var area in v.areas)area.gameObject.SetActive(false);if(!show)continue;
                if(v.uid!=e.Uid){v.uid=e.Uid;v.damageSerial=e.DamageSerial;v.body.sprite=TideArt.Enemy(e.Def.id);v.flash=0;}
                v.root.anchoredPosition=Project(e.X,e.Y)+new Vector2(0,e.Lift*32);v.root.sizeDelta=e.Def.IsBoss?new Vector2(250,180):new Vector2(190,190);
                v.body.rectTransform.sizeDelta=v.root.sizeDelta;v.body.rectTransform.localScale=new Vector3(e.X>=Session.X?1:-1,1,1);
                v.body.color=e.Flash>0&&!Session.Profile.reducedEffects?new Color(.7f,.85f,1):Color.white;
                v.body.Tick(e,Session.PresentationEpoch,Session.PresentationClock,TideHudLayout.UnitX(safe.rect.width,Session.Profile.controlScale));
                v.shadow.anchoredPosition=Project(e.X,e.Y);v.hp.fillAmount=e.Hp/e.MaxHp;
                v.label.gameObject.SetActive(e.Mark>0||e.Guard>0);v.hp.transform.parent.gameObject.SetActive(!e.Def.IsBoss);
                Say(v.label,(e.Mark>0?"표식 ":"")+(e.Guard>0?"방어":""));
                if(v.damageSerial!=e.DamageSerial){v.damageSerial=e.DamageSerial;v.flash=.7f;v.damage.text=Mathf.CeilToInt(e.LastDamage).ToString();}
                v.flash=Mathf.Max(0,v.flash-Time.unscaledDeltaTime);v.damage.gameObject.SetActive(Session.IsFighting&&v.flash>0);v.damage.rectTransform.anchoredPosition=Project(e.X,e.Y)+new Vector2(20,220+(.7f-v.flash)*30);
                sorting.Add(new KeyValuePair<float,Transform>(e.Y,v.root));DrawAreas(v,e);
            }
            sorting.Sort((a,b)=>b.Key.CompareTo(a.Key));foreach(var pair in sorting)pair.Value.SetAsLastSibling();
            gateView.gameObject.SetActive(Session.IsFighting&&Session.IsCampaign&&Session.RoomCleared&&!Session.IsFieldVisit);
            gateView.anchoredPosition=Project(Session.GateX,0)+new Vector2(0,55);gateLabel.text="통로 정화 →\n출구로 이동";
            Buttons["gate"].gameObject.SetActive(Session.IsFighting&&Session.IsCampaign&&Session.RoomCleared);
            Buttons["gate"].interactable=Session.IsFieldVisit||Session.AtGate&&Session.GateReady;
            Buttons["gate"].GetComponentInChildren<Text>().text=Session.IsFieldVisit?"거점으로 귀환":
                Session.MainInvestigationPending?"남은 조사 살피기":Session.AtGate?"다음 방"+(ShowKeyboardHints?" · E":""):"오른쪽 출구로 이동 →";
            Buttons["target-next"].gameObject.SetActive(Session.IsFighting&&Session.AliveCount>1);
            progressText.gameObject.SetActive(battle);
            string waveLabel=Session.IsCampaign&&Session.Room.WaveCount>1?" · 증원 "+Session.RoomWave+"/"+Session.Room.WaveCount:"";
            Say(progressText,OpeningHint()??(Session.IsCampaign?"Act "+Session.Dungeon.act+" · "+Session.Dungeon.number+"구역 · "+(Session.RoomIndex+1)+"/"+Session.Dungeon.rooms.Length+"방"+waveLabel+" · 적 "+Session.AliveCount:Session.IsTraining?"수련 · 조작과 기술을 자유롭게 확인":"보스 기록 재현 · 진행 보상 없음"));
            questText.gameObject.SetActive(false);
            beaconView.gameObject.SetActive(false);lureView.gameObject.SetActive(Session.IsFighting&&Session.LureActive);lureView.anchoredPosition=Project(Session.WarningPosition.x,Session.WarningPosition.y)+new Vector2(0,90);
            warningText.gameObject.SetActive(Session.IsFighting&&(Session.Telegraph||Session.BodyExposed));
            warningText.text=Session.BodyExposed?"본체 노출 · 피해 +25%":Session.Target.Cue+"  "+Session.EnemyTimer.ToString("0.0")+"초";
            for(int i=0;i<2;i++){cloudViews[i].gameObject.SetActive(battle&&Session.CloudTime[i]>0);cloudViews[i].anchoredPosition=Project(Session.Clouds[i].x,Session.Clouds[i].y);}
            slash.gameObject.SetActive(Session.IsFighting&&Session.ActionRemaining>0&&Session.ActionElapsed>Session.ActionDuration*.3f&&Session.ActionElapsed<Session.ActionDuration*.7f);
            slash.anchoredPosition=pos+new Vector2(Session.Facing*85,75);slash.localRotation=Quaternion.Euler(0,0,-Session.Facing*wave*35);slash.sizeDelta=new Vector2(Session.Profile.hero==4?220:140,Session.Profile.hero==3?6:14);
        }
        private void DrawAreas(FoeView view,TideEnemy e)
        {
            if(!Session.IsFighting||!e.Telegraph)return;
            float unit=TideHudLayout.UnitX(safe.rect.width,Session.Profile.controlScale);int p=e.Pattern;
            for(int i=0;i<4;i++)
            {
                bool visible=i==0||i==1&&(e.SecondArea||p==4)||e.Decoys&&i<3||p==6&&!e.FutureLocked&&i<3;
                var area=view.areas[i];area.gameObject.SetActive(visible);if(!visible)continue;
                Vector2 at=i==1&&e.SecondArea?e.Warning2:e.Warning;
                if(e.Decoys&&i>0)at+=new Vector2(i==1?-2:2,i==1?.9f:-.9f);
                if(p==6&&i>0)at.y+=i==1?-1.1f:1.1f;
                area.rectTransform.anchoredPosition=Project(at.x,at.y);area.sprite=p==1?circle:square;
                area.rectTransform.localRotation=Quaternion.Euler(0,0,p==4?(i==0?1:-1)*Mathf.Atan(7/unit)*Mathf.Rad2Deg:0);
                area.rectTransform.sizeDelta=p==1?new Vector2(unit*2.3f,80.5f):p==0?new Vector2(unit*6,59.5f):new Vector2(safe.rect.width*2,p==2?45.5f:p==4?24.5f:p==6?31.5f:35);
                area.color=e.Decoys&&i>0?new Color(.45f,.65f,.9f,.12f):p==6&&!e.FutureLocked?new Color(.9f,.7f,.25f,.13f):new Color(1,.3f,.12f,Session.Profile.reducedEffects?.32f:.28f+Mathf.PingPong(Session.Clock*.5f,.15f));
            }
        }
        private void BuildMissions()
        {
            mapAct=Mathf.Clamp(mapAct,1,5);MenuTitle("균열 지도 · Act "+mapAct,TideStory.Data.acts[mapAct-1].title);
            Button("mission-guide","출정 준비",panel,new Vector2(.5f,.5f),new Vector2(300,230),new Vector2(240,84),()=>OpenJourney(1)).GetComponentInChildren<Text>().fontSize=24;
            for(int i=1;i<=5;i++){int a=i;Button("map-act-"+a,"Act "+a,panel,new Vector2(.5f,.5f),new Vector2((i-3)*185,94),new Vector2(168,84),()=>{mapAct=a;Refresh();},a==mapAct);}
            for(int n=1;n<=6;n++)
            {
                int number=n;var d=TideCampaign.Dungeon(mapAct,n);bool destination=mapAct==routeAct&&n==routeStage;bool clear=(Session.Profile.clearedDungeons&(1<<d.index))!=0,current=Session.Profile.campaignAct==mapAct&&Session.Profile.campaignStage==n&&Session.Quest==QuestState.Active;
                var button=Button("dungeon-"+n,(destination?"목적지 · ":clear?"정화 · ":current?"현재 · ":"잠김 · ")+n+"\n"+d.title,panel,new Vector2(.5f,.5f),new Vector2(((n-1)%3-1)*310,0-(n-1)/3*100),new Vector2(294,88),()=>{if(destination)DepartRoute();else DepartDungeon(mapAct,number,clear&&!current);},destination||current);
                var thumb=Picture("MapThumbnail",button.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(294,88),new Color(1,1,1,.27f),TideArt.Background(d.background));thumb.transform.SetAsFirstSibling();
                var text=button.GetComponentInChildren<Text>();text.fontSize=21;var outline=text.gameObject.AddComponent<Outline>();outline.effectColor=new Color(0,.02f,.03f,.95f);outline.effectDistance=new Vector2(1,-1);button.interactable=destination?RouteReady():clear||current;
            }
            if(routeAct>0)
            {
                string note=routeStage+"구역 · "+(routeRoom+1)+"방 · "+routeTitle;
                Label(note,panel,new Vector2(.5f,.5f),new Vector2(0,-165),new Vector2(950,32),19,gold);
                string action=mapAct!=routeAct?"목적지 Act "+routeAct+" 보기":RouteReady()
                    ?routeVisit&&TideCampaign.CanVisitRoom(Session.Profile,routeAct,routeStage,routeRoom)?"지나온 방 바로 방문":"표시한 목적지로 출정"
                    :"앞 구역의 조사를 먼저 진행";
                Button("depart",action,panel,new Vector2(.5f,.5f),new Vector2(-260,-226),new Vector2(440,84),()=>{if(mapAct!=routeAct){mapAct=routeAct;Refresh();}else DepartRoute();},true).interactable=mapAct!=routeAct||RouteReady();
            }
            else Button("depart",Session.Quest==QuestState.Available?"이번 액트 의뢰 받기":Session.Quest==QuestState.Ready?"액트 기록 보고":"현재 조사 계속",panel,new Vector2(.5f,.5f),new Vector2(-260,-226),new Vector2(440,84),()=>{if(Session.Quest==QuestState.Available||Session.Quest==QuestState.Ready){Session.ReturnTown();Talk("urahara");}else if(!Session.CampaignFinished)DepartDungeon(Session.Profile.campaignAct,Session.Profile.campaignStage,false);},true).interactable=!Session.CampaignFinished;
            Button("back","뒤로",panel,new Vector2(.5f,.5f),new Vector2(260,-226),new Vector2(440,84),NavigateBack);
        }
        private TideGear SelectedGear()
        {
            var item=TideProgress.Gear(Session.Profile,gearSelection);if(item==null){item=Session.Profile.gear[0];gearSelection=item.uid;}return item;
        }
        private string GearStats(TideGear g){return g==null?"미착용":(g.slot=="guard"?"최대 체력 +"+g.Health:"타격 피해 +"+g.Attack);}
        private void BuildGear(bool forge)
        {
            var items=VisibleGear();gearPage=Mathf.Clamp(gearPage,0,Mathf.Max(0,(items.Count-1)/3));
            if(items.Count>0&&!items.Exists(g=>g.uid==gearSelection))gearSelection=items[gearPage*3].uid;
            var selected=SelectedGear();MenuTitle(forge?"텟사이 · 영장 강화":"전리품과 장비");
            string[] filters={"전체","공격","수호"};
            for(int i=0;i<3;i++){int filter=i;Button("gear-filter-"+i,filters[i],panel,new Vector2(.5f,.5f),new Vector2(-415+i*140,145),new Vector2(130,84),()=>{gearFilter=filter;gearPage=0;Refresh();},i==gearFilter).GetComponentInChildren<Text>().fontSize=22;}
            Button("gear-sort",gearSort==0?"정렬 · 최근 획득":"정렬 · 성능순",panel,new Vector2(.5f,.5f),new Vector2(210,145),new Vector2(470,84),()=>{gearSort=1-gearSort;var sorted=VisibleGear();gearPage=Mathf.Max(0,sorted.FindIndex(g=>g.uid==gearSelection))/3;Refresh();});
            for(int i=0;i<3;i++)
            {
                int index=gearPage*3+i;if(index>=items.Count)break;var item=items[index];int uid=item.uid;
                Button("gear-"+i,(item.favorite?"★ ":"")+item.name+" +"+item.upgrade+"\n"+GearStats(item)+(item.uid==Session.Profile.powerGear||item.uid==Session.Profile.guardGear?" · 착용":""),panel,new Vector2(.5f,.5f),new Vector2(-275,53-i*92),new Vector2(410,84),()=>{gearSelection=uid;Refresh();},item.uid==selected.uid).GetComponentInChildren<Text>().fontSize=21;
            }
            if(items.Count==0)Label("이 슬롯의 장비가 없습니다.",panel,new Vector2(.5f,.5f),new Vector2(-275,-30),new Vector2(410,130),25,muted);
            var worn=TideProgress.Gear(Session.Profile,selected.slot=="guard"?Session.Profile.guardGear:Session.Profile.powerGear);
            int delta=selected.slot=="guard"?selected.Health-(worn==null?0:worn.Health):selected.Attack-(worn==null?0:worn.Attack);
            bool equipped=worn!=null&&worn.uid==selected.uid;
            string comparison=equipped?"현재 착용 중":delta==0?"착용 장비와 성능 동일":(selected.slot=="guard"?"최대 체력 ":"타격 피해 ")+(delta>0?"+":"")+delta+" 변화";
            string detail=(selected.slot=="guard"?"수호 슬롯":"공격 슬롯")+" · "+new[]{"일반","희귀","영웅"}[selected.rarity]+"\n현재: "+GearStats(worn)+"\n선택: "+GearStats(selected)+"\n"+comparison;
            bool affordable=Session.Profile.gold>=Session.UpgradeGold(selected)&&Session.Profile.crystals>=Session.UpgradeCrystals(selected);
            if(forge)detail+="\n"+(selected.upgrade>=5?"최대 강화 +5":"다음: "+(selected.slot=="guard"?"체력 +8":"피해 +3")+"\n비용 "+Session.UpgradeGold(selected)+" 골드 · 결정 "+Session.UpgradeCrystals(selected)+(affordable?"":" · 부족"));
            else detail+="\n파티 Lv."+Session.Profile.level+" · 경험치 "+Session.Profile.xp+" / "+TideProgress.NeedXp(Session.Profile.level);
            if(items.Count==0)detail="이 슬롯의 장비가 없습니다.\n던전에서 얻은 전리품을 확인해 주세요.";
            Label(detail,panel,new Vector2(.5f,.5f),new Vector2(210,-20),new Vector2(470,240),23);
            Label(Session.Profile.gold+" 골드 · 결정 "+Session.Profile.crystals+" · "+(gearPage+1)+" / "+Mathf.Max(1,(items.Count+2)/3)+"쪽",panel,new Vector2(.5f,.5f),new Vector2(210,-157),new Vector2(470,30),20,muted);
            Button("gear-prev","‹",panel,new Vector2(.5f,.5f),new Vector2(-435,-226),new Vector2(88,84),()=>ChangeGearPage(-1)).interactable=gearPage>0;
            Button("gear-next","›",panel,new Vector2(.5f,.5f),new Vector2(-325,-226),new Vector2(88,84),()=>ChangeGearPage(1)).interactable=(gearPage+1)*3<items.Count;
            if(forge)
            {
                var up=Button("upgrade",items.Count==0?"선택한 장비 없음":selected.upgrade>=5?"최대 강화":!affordable?"강화 재료 부족":"선택 장비 강화",panel,new Vector2(.5f,.5f),new Vector2(245,-226),new Vector2(450,84),()=>{Session.UpgradeGear(selected.uid);Persist();Refresh();},true);
                up.interactable=items.Count>0&&selected.upgrade<5&&affordable;
            }
            else
            {
                Button("equip",equipped?"착용 중":"착용",panel,new Vector2(.5f,.5f),new Vector2(-60,-226),new Vector2(150,84),()=>{Session.EquipGear(selected.uid);Persist();Refresh();},true).interactable=items.Count>0&&!equipped;
                Button("lock",selected.favorite?"★ 해제":"★ 즐겨찾기",panel,new Vector2(.5f,.5f),new Vector2(135,-226),new Vector2(220,84),()=>{Session.FavoriteGear(selected.uid);Persist();Refresh();}).interactable=items.Count>0;
                Button("forge","강화",panel,new Vector2(.5f,.5f),new Vector2(370,-226),new Vector2(200,84),()=>OpenMenu(TideMode.Forge)).interactable=items.Count>0;
            }
        }
        private void BuildJournal(){BuildRecordJournal();}
        private void BuildCampaignResult()
        {
            MenuTitle(Session.IsCampaign?"던전 정화 · "+Session.Dungeon.title:"보스 전투 재현 완료");
            Label(Session.IsCampaign?"등급 "+Session.ClearRank+" · 최고 "+Session.ChainPeak+" HIT · 받은 피해 "+Mathf.CeilToInt(Session.DungeonDamageTaken)+"\n"+Session.LastReward:"수련 기록 · 조사와 보상에 영향을 주지 않습니다.",panel,new Vector2(.5f,.5f),new Vector2(0,125),new Vector2(950,126),21,gold);
            bool midpoint=Session.IsCampaign&&!Session.IsReplay&&Session.Dungeon.number==3;
            bool fieldResult=Session.IsCampaign&&!Session.IsReplay&&Session.Dungeon.number!=3&&Session.Dungeon.number!=6;
            Label(Session.IsCampaign?midpoint?TideJourney.Data.chapters[Session.Dungeon.act-1].midpoint:TideStory.DungeonOutcome(Session.Dungeon,Session.Profile):"공격 예고와 기술 연계를 확인했습니다. 다음에는 다른 동료로 도전해 보세요.",panel,new Vector2(.5f,.5f),new Vector2(0,midpoint||fieldResult?-7:-45),new Vector2(950,midpoint||fieldResult?120:180),24);
            AddResultBreather();
            if(fieldResult)
                Button("result-story","현장 대화",panel,new Vector2(.5f,.5f),new Vector2(0,-121),new Vector2(270,84),ReadResultStory);
            int notices=Session.IsCampaign&&!Session.IsReplay?TideQuests.ChapterNotices(Session.Profile,Session.Dungeon.act):0;
            if(notices>0)
                Button("result-requests","마을의 부탁 "+notices,panel,Center,new Vector2(350,-121),new Vector2(244,84),
                    ()=>{ReturnTown();OpenQuestBook(startWithSide:true);});
            bool next=Session.IsCampaign&&!Session.IsReplay&&Session.Quest==QuestState.Active;
            bool report=Session.IsCampaign&&!Session.IsReplay&&Session.Quest==QuestState.Ready;
            Button("next-dungeon",next?"다음 · "+TideJourney.Current(Session.Profile).title:report?"귀환 보고":"마을로",panel,new Vector2(.5f,.5f),new Vector2(-240,-214),new Vector2(420,84),()=>{
                if(next){Session.ReturnTown();StartBattle(false);}
                else if(report){Session.ReturnTown();BeginActReport();}
                else ReturnTown();
            },true).GetComponentInChildren<Text>().fontSize=22;
            Button("result-gear","전리품 확인",panel,new Vector2(.5f,.5f),new Vector2(240,-214),new Vector2(420,84),()=>{ReturnTown();SelectLoot(Session.LastGearUid);OpenMenu(TideMode.Inventory);});
        }
    }
}
