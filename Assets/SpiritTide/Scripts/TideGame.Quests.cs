using System;
using UnityEngine;
using UnityEngine.UI;
namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private int questAct=1,mainStep=1;
        private bool questSide;
        private string questOwner,questSelected;
        private Text questTracker,townMemory;
        private readonly Text[] questBadges=new Text[5],questPointLabels=new Text[4];
        private readonly RectTransform[] questPointViews=new RectTransform[4];
        private readonly Image[] questPointPapers=new Image[4];

        private void ReceiveMainQuest()
        {
            if(Session.Quest!=QuestState.Available||(Session.Mode!=TideMode.Dialogue&&Session.Mode!=TideMode.Quests))return;
            ShowDialogueScene(TideQuests.Main(Session.Profile.campaignAct).accept_scene,()=>{Session.AcceptQuest();Persist();ReturnTown();},"의뢰를 맡는다");
        }
        private bool MaybeQuestStory()
        {
            if(Session.PendingQuestScene==null)return false;
            if(Session.Mode!=TideMode.Town&&Session.Mode!=TideMode.Result&&!(Session.IsFighting&&Session.RoomCleared))return false;
            TideMode restore=Session.Mode;string status=Session.Status;
            return PlayQuestStory(()=>{Session.Mode=restore;Session.Status=status;Session.Revision++;Persist();Refresh();});
        }
        private bool PlayQuestStory(Action finish)
        {
            while(Session.PendingQuestScene!=null&&TideDialogue.Seen(Session.Profile,Session.PendingQuestScene)!=null)Session.ConsumeQuestScene();
            string id=Session.PendingQuestScene;if(id==null){finish();return false;}
            Session.ConsumeQuestScene();ShowDialogueScene(id,finish,Session.InBattle?"조사 계속":"이야기 마침");return true;
        }
        private void OpenQuestBook(string owner=null)
        {
            questOwner=owner;questSelected=null;questAct=Session.Profile.campaignAct;mainStep=Session.Profile.campaignStage;questSide=owner!=null||Session.CampaignFinished;
            if(owner!=null)foreach(var q in TideQuests.Data.side)if(q.giver==owner&&(TideQuests.CanOffer(Session.Profile,q)||TideQuests.State(Session.Profile,q.id)!=null&&!TideQuests.State(Session.Profile,q.id).claimed)){questAct=q.act;break;}
            if(Session.Mode==TideMode.Quests){Refresh();return;}OpenMenu(TideMode.Quests);
        }
        private void BuildQuestBook()
        {
            questAct=Mathf.Clamp(questAct,1,5);
            if(questSelected!=null){BuildSideQuestDetail(TideQuests.Find(questSelected));return;}
            MenuTitle("의뢰 수첩"+(questOwner==null?"":" · "+TideQuests.Giver(questOwner)));
            for(int a=1;a<=5;a++){int target=a;Button("quest-act-"+a,"Act "+a,panel,new Vector2(.5f,.5f),new Vector2((a-3)*185,132),new Vector2(168,84),()=>{questAct=target;mainStep=1;Refresh();},a==questAct);}
            Button("quest-main","주요 의뢰",panel,new Vector2(.5f,.5f),new Vector2(-240,38),new Vector2(440,84),()=>{questSide=false;Refresh();},!questSide);
            Button("quest-side","마을의 부탁",panel,new Vector2(.5f,.5f),new Vector2(240,38),new Vector2(440,84),()=>{questSide=true;Refresh();},questSide);
            if(!questSide){BuildMainQuestPage();return;}
            int count=0;
            foreach(var q in TideQuests.Data.side)if(q.act==questAct&&(questOwner==null||q.giver==questOwner)&&(TideQuests.CanOffer(Session.Profile,q)||TideQuests.State(Session.Profile,q.id)!=null))
            {
                var selected=q;int column=count++;
                Button("side-select-"+q.id,q.title+"\n"+TideQuests.Giver(q.giver)+"\n"+TideQuests.Status(Session.Profile,q),panel,new Vector2(.5f,.5f),new Vector2((column-1)*330,-95),new Vector2(310,150),()=>{questSelected=selected.id;Refresh();},TideQuests.Ready(Session.Profile,q)).GetComponentInChildren<Text>().fontSize=23;
            }
            if(count==0)Label(questAct>Session.Profile.campaignAct?"아직 이 지역의 부탁을 듣지 못했습니다.\n앞선 지역의 이야기를 먼저 이어 가세요.":"아직 새 부탁이 없습니다.\n주요 조사를 이어 가거나 다른 인물에게 이야기를 들어 보세요.",panel,new Vector2(.5f,.5f),new Vector2(0,-95),new Vector2(950,130),26);
            Button("quest-all","모두 보기",panel,new Vector2(.5f,.5f),new Vector2(-240,-226),new Vector2(440,84),()=>{questOwner=null;Refresh();});
            Button("back","뒤로",panel,new Vector2(.5f,.5f),new Vector2(240,-226),new Vector2(440,84),NavigateBack);
        }
        private void BuildMainQuestPage()
        {
            bool known=questAct<=Session.Profile.campaignAct;
            var p=Session.Profile;var main=TideQuests.Main(questAct);mainStep=Mathf.Clamp(mainStep,1,6);var dungeon=TideCampaign.Dungeon(questAct,mainStep);
            bool reported=(p.reportedActs&(1<<(questAct-1)))!=0,clear=(p.clearedDungeons&(1<<dungeon.index))!=0;
            string title=known?main.title:"아직 도착하지 않은 지역";
            Label(title,panel,new Vector2(.5f,.5f),new Vector2(0,-29),new Vector2(950,35),26,gold,true);
            Label(known?mainStep+" / 6 · "+dungeon.title+(reported?" · 보고 완료":clear?" · 정화 완료":""):"앞선 지역의 보고 뒤 확인합니다",panel,new Vector2(.5f,.5f),new Vector2(0,-70),new Vector2(740,34),22,muted);
            string body=!known?"아직 이 지역의 의뢰를 받지 않았습니다.":clear?dungeon.resolution:questAct==p.campaignAct&&mainStep==p.campaignStage?dungeon.reason:"앞 구역에서 얻은 단서를 따라가면 이 이야기가 이어집니다.";
            if(known&&mainStep==4&&(p.mainInvestigations&(1<<(questAct-1)))==0)body="첫 통로에서 ‘"+main.inspect_title+"’ 살펴보기\n"+body;
            Label(body,panel,new Vector2(.5f,.5f),new Vector2(0,-130),new Vector2(740,100),20);
            Button("quest-step-prev","‹",panel,new Vector2(.5f,.5f),new Vector2(-435,-100),new Vector2(88,84),()=>{mainStep--;Refresh();}).interactable=known&&mainStep>1;
            Button("quest-step-next","›",panel,new Vector2(.5f,.5f),new Vector2(435,-100),new Vector2(88,84),()=>{mainStep++;Refresh();}).interactable=known&&mainStep<6;
            var primary=Button("quest-main-action",!known?"미확인 의뢰":reported?"지나온 지도 보기":Session.Quest==QuestState.Available?"부탁 듣기":Session.Quest==QuestState.Ready?"귀환 보고":"목적지 보기",panel,new Vector2(.5f,.5f),new Vector2(-240,-226),new Vector2(440,84),()=>{
                if(questAct==p.campaignAct&&Session.Quest==QuestState.Available)ReceiveMainQuest();
                else if(questAct==p.campaignAct&&Session.Quest==QuestState.Ready){ReturnTown();Talk("urahara");}
                else OpenRoute(questAct,mainStep,questAct==p.campaignAct&&mainStep==p.campaignStage?p.roomCheckpoint:0,"본편 의뢰 · "+main.title);
            },true);primary.interactable=known;
            Button("back","뒤로",panel,new Vector2(.5f,.5f),new Vector2(240,-226),new Vector2(440,84),NavigateBack);
        }
        private void BuildSideQuestDetail(TideSideQuest q)
        {
            if(q==null){questSelected=null;BuildQuestBook();return;}
            var state=TideQuests.State(Session.Profile,q.id);bool ready=TideQuests.Ready(Session.Profile,q),done=state!=null&&state.claimed;
            MenuTitle(q.title,"Act "+q.act+" · "+TideQuests.Giver(q.giver)+" · "+TideQuests.Status(Session.Profile,q));
            Label(done?"이야기 기록 · "+q.choice_keepsakes[state.choice]:q.summary,panel,new Vector2(.5f,.5f),new Vector2(0,80),new Vector2(950,94),Mathf.RoundToInt(24*Session.Profile.uiScale),gold,true);
            if(done)Label(q.choice_notes[state.choice],panel,new Vector2(.5f,.5f),new Vector2(0,-35),new Vector2(950,170),Mathf.RoundToInt(26*Session.Profile.uiScale));
            else for(int i=0;i<q.steps.Length;i++)Label((state!=null&&state.step>i?"✓ ":"○ ")+q.steps[i].goal,panel,new Vector2(.5f,.5f),new Vector2(0,-25-i*73),new Vector2(950,70),Mathf.RoundToInt(22*Session.Profile.uiScale));
            Label(done?"소식 전달 완료 · 보상은 한 번 지급합니다":"보상 · 경험치 "+q.xp+" · 골드 "+q.gold+" · 영자 결정 "+q.crystals,panel,new Vector2(.5f,.5f),new Vector2(0,-165),new Vector2(950,32),20,muted);
            Action back=()=>{questSelected=null;Refresh();};
            if(state==null)
            {
                Button("side-accept","부탁 듣기",panel,new Vector2(.5f,.5f),new Vector2(-240,-226),new Vector2(440,84),()=>{
                    if(!TideQuests.CanOffer(Session.Profile,q)||Session.Mode!=TideMode.Quests)return;
                    ShowDialogueScene(q.offer_scene,()=>{Session.AcceptSideQuest(q.id);Persist();ReturnTown();OpenQuestBook(q.giver);questAct=q.act;questSelected=q.id;Refresh();},"부탁을 맡는다");
                },true).interactable=TideQuests.CanOffer(Session.Profile,q);
                Button("side-list","목록으로",panel,new Vector2(.5f,.5f),new Vector2(240,-226),new Vector2(440,84),back);return;
            }
            if(ready)
            {
                for(int i=0;i<q.choices.Length;i++)
                {
                    int choice=i;float x=q.choices.Length==1?-240:(i-1)*320;
                    Button("side-claim-"+i,q.choices[i],panel,new Vector2(.5f,.5f),new Vector2(x,-226),new Vector2(q.choices.Length==1?440:304,84),()=>{
                        if(Session.Mode!=TideMode.Quests||!TideQuests.Ready(Session.Profile,q))return;
                        ShowDialogueScene(q.finish_scenes[choice],()=>{Session.ClaimSideQuest(q.id,choice);Persist();ReturnTown();OpenQuestBook(q.giver);questAct=q.act;questSelected=q.id;Refresh();},"소식 전달");
                    },true);
                }
                Button("side-list","목록으로",panel,new Vector2(.5f,.5f),new Vector2(q.choices.Length==1?240:320,-226),new Vector2(q.choices.Length==1?440:304,84),back);return;
            }
            if(done)
            {
                Button("side-after","이후 이야기",panel,new Vector2(.5f,.5f),new Vector2(-240,-226),new Vector2(440,84),()=>{ReturnTown();Talk(q.giver);},true);
                Button("side-list","목록으로",panel,new Vector2(.5f,.5f),new Vector2(240,-226),new Vector2(440,84),back);return;
            }
            Button("side-go","목적지 보기",panel,new Vector2(.5f,.5f),new Vector2(-320,-226),new Vector2(304,84),()=>GoToSideObjective(q),true);
            Button("side-track",Session.Profile.trackedSide==q.id?"추적 중":"화면에 추적",panel,new Vector2(.5f,.5f),new Vector2(0,-226),new Vector2(304,84),()=>{Session.TrackSideQuest(q.id);Persist();Refresh();});
            Button("side-list","목록으로",panel,new Vector2(.5f,.5f),new Vector2(320,-226),new Vector2(304,84),back);
        }
        private void GoToSideObjective(TideSideQuest q)
        {
            var state=TideQuests.State(Session.Profile,q.id);if(state==null||state.claimed||state.step>=q.steps.Length)return;
            var step=q.steps[state.step];
            if(step.kind=="talk"){ReturnTown();Talk(step.npc);}
            else if(step.kind=="cancel"||step.kind=="perfect")StartBattle(true);
            else if(step.kind=="ward")RouteToWard(q);
            else OpenRoute(q.act,step.stage,step.room,q.title+" · "+step.title);
        }
        private void BuildQuestWorld()
        {
            Button("quest-log","의뢰 수첩 · Q",townRoot.transform,new Vector2(1,0),new Vector2(-120,260),new Vector2(220,84),()=>OpenQuestBook(),true);
            Button("training-tells","공격 예고",safe,Vector2.zero,new Vector2(140,445),new Vector2(220,84),()=>Session.ToggleTrainingAttacks(),true);
            questTracker=Label("",safe,new Vector2(0,1),new Vector2(205,-225),new Vector2(380,68),18,muted,true);questTracker.name="QuestTracker";
            var board=Picture("TownMemoryBoard",world,new Vector2(1,.5f),new Vector2(-180,60),new Vector2(330,148),new Color(.07f,.14f,.16f,.82f));
            townMemory=Label("",board.transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(306,136),18,gold);townMemory.name="TownMemoryText";
            for(int i=0;i<5;i++){questBadges[i]=Label("",world,Vector2.zero,Vector2.zero,new Vector2(70,30),23,gold,true);questBadges[i].alignment=TextAnchor.MiddleCenter;}
            for(int i=0;i<4;i++)
            {
                var root=Node("QuestPoint-"+i,world,Vector2.zero,Vector2.zero,new Vector2(56,50));root.SetSiblingIndex(actorLayer.GetSiblingIndex());questPointViews[i]=root;
                Picture("PointGlow",root,new Vector2(.5f,.5f),Vector2.zero,new Vector2(62,17),new Color(.5f,1,.75f,.3f),circle);
                questPointPapers[i]=Picture("Paper",root,new Vector2(.5f,.5f),new Vector2(0,18),new Vector2(32,38),new Color(.93f,.85f,.64f));
                var mark=Label("≡",questPointPapers[i].transform,new Vector2(.5f,.5f),Vector2.zero,new Vector2(30,34),25,bg,true);mark.alignment=TextAnchor.MiddleCenter;
                questPointLabels[i]=Label("",root,new Vector2(.5f,.5f),new Vector2(0,63),new Vector2(150,32),20,gold,true);questPointLabels[i].alignment=TextAnchor.MiddleCenter;
            }
        }
        private void DrawQuestWorld()
        {
            bool town=!Session.InBattle;townMemory.transform.parent.gameObject.SetActive(town);townMemory.text=TideQuests.TownMemory(Session.Profile);
            string[] npc={"urahara","tessai","kon","byakuya","kukaku"};
            for(int i=0;i<5;i++){int notices=TideQuests.Notices(Session.Profile,npc[i]);questBadges[i].gameObject.SetActive(Session.Mode==TideMode.Town&&notices>0);questBadges[i].text="부탁 "+notices;questBadges[i].rectTransform.anchoredPosition=Project(-4+i*2,.45f)+new Vector2(62,110);}
            questTracker.gameObject.SetActive(Session.IsFighting&&!Session.IsTraining&&!Session.WardActive&&!Session.EchoAvailable&&Session.QuestPointCount==0);
            var tracked=TideQuests.Find(Session.Profile.trackedSide);
            bool practiceDone=tracked!=null&&TideQuests.State(Session.Profile,tracked.id).step>0&&(tracked.steps[0].kind=="cancel"||tracked.steps[0].kind=="perfect");
            Buttons["training-tells"].gameObject.SetActive(Session.IsFighting&&Session.IsTraining);Say(Buttons["training-tells"].GetComponentInChildren<Text>(),(practiceDone?"수련 목표 달성\n":"")+(Session.TrainingAttacks?"공격 예고 · 켬":"공격 예고 · 끔"));
            questTracker.text=tracked==null?Session.IsCampaign?"주요 · "+TideQuests.Main(Session.Dungeon.act).title+"\n"+(Session.MainInvestigationPending?TideQuests.Main(Session.Dungeon.act).inspect_title+" 살펴보기":Session.Dungeon.title):"수련 · 실제 동작을 자유롭게 맞춰 보세요":"부탁 · "+tracked.title+"\n"+TideQuests.Objective(Session.Profile,tracked);
            for(int i=0;i<4;i++)
            {
                bool active=Session.IsFighting&&i<Session.QuestPointCount;questPointViews[i].gameObject.SetActive(active);if(!active)continue;
                var point=Session.QuestPoint(i);questPointViews[i].anchoredPosition=Project(point.position.x,point.position.y);
                questPointLabels[i].text=point.main?"주요 조사":"부탁";questPointPapers[i].color=point.main?new Color(.65f,.87f,1):new Color(.93f,.85f,.64f);
            }
            if(Session.MainInvestigationPending)gateLabel.text="남은 조사\n표지를 살펴보세요";
        }
    }
}
