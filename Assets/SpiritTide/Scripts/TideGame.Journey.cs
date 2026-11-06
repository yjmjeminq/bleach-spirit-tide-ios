using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private int journeyTab,journeyBoss=-1;
        private void OpenJourney(int tab=0)
        {
            journeyTab=tab;journeyBoss=-1;OpenMenu(TideMode.Journey);
        }
        private Text JourneyText(string name,string text,Vector2 at,Vector2 size,int fontSize=22,Color? tint=null)
        {
            var label=Label(text,panel,new Vector2(.5f,.5f),at,size,Mathf.RoundToInt(fontSize*Session.Profile.uiScale),tint??ink);label.name=name;label.alignment=TextAnchor.UpperLeft;return label;
        }
        private void BuildJourney()
        {
            MenuTitle("이야기와 전투 준비");
            string[] tabs={"지난 이야기","출정 준비","보스 기록"};
            for(int i=0;i<3;i++){int tab=i;Button("journey-tab-"+i,tabs[i],panel,new Vector2(.5f,.5f),new Vector2((i-1)*320,132),new Vector2(304,84),()=>{journeyTab=tab;journeyBoss=-1;Refresh();},journeyTab==i);}
            var p=Session.Profile;
            if(journeyTab==2){BuildBossRecords();return;}
            Label("Act "+p.campaignAct+" · "+TideQuests.Main(p.campaignAct).title,panel,new Vector2(.5f,.5f),new Vector2(0,65),new Vector2(950,38),24,gold,true);
            if(journeyTab==0)
            {
                JourneyText("JourneyNarrativeText",TideJourney.Narrative(p),new Vector2(0,-18),new Vector2(950,126),21);
                JourneyText("JourneyNextText","지금 할 일 · "+TideJourney.Next(p),new Vector2(0,-139),new Vector2(950,100),22,muted);
                Button("journey-continue",Session.InBattle?"마을에서 조사 이어가기":"이어서 진행",panel,new Vector2(.5f,.5f),new Vector2(-165,-236),new Vector2(620,84),()=>{if(Session.InBattle)ReturnTown();ContinueObjective();},true);
            }
            else
            {
                JourneyText("JourneyPreparationText",TideJourney.Preparation(p),new Vector2(0,-20),new Vector2(950,136));
                string note=Session.Quest==QuestState.Active?"현재 목적 · "+TideJourney.Next(p):TideJourney.Next(p);
                JourneyText("JourneyPreparationGoal",note,new Vector2(0,-139),new Vector2(950,100),22,muted);
                Button("journey-gear",Session.InBattle?"마을에서 장비 정비":"장비 비교 · 직접 선택",panel,new Vector2(.5f,.5f),new Vector2(-165,-236),new Vector2(620,84),()=>{int uid=TideJourney.BetterGear(p);if(Session.InBattle)ReturnTown();if(uid>0)SelectLoot(uid);OpenMenu(TideMode.Inventory);},true);
            }
            Button("back","뒤로",panel,new Vector2(.5f,.5f),new Vector2(325,-236),new Vector2(310,84),NavigateBack);
        }
        private void BuildBossRecords()
        {
            if(journeyBoss<0||!TideJourney.BossKnown(Session.Profile,journeyBoss))
            {
                for(int i=0;i<10;i++)
                {
                    int index=i;bool known=TideJourney.BossKnown(Session.Profile,i);var boss=TideCatalog.Bosses[i];
                    Button("journey-boss-"+i,"Act "+boss.act+"\n"+(known?boss.name:"미확인 기록"),panel,new Vector2(.5f,.5f),new Vector2((i%5-2)*190,32-i/5*96),new Vector2(174,84),()=>{journeyBoss=index;Refresh();},known).interactable=known;
                    Buttons["journey-boss-"+i].GetComponentInChildren<Text>().fontSize=21;
                }
                JourneyText("JourneyBossLockText","본편에서 실제로 만난 보스만 열립니다.\n기본 체력 연습 · 보상·소모품·본편 진행 유지",new Vector2(0,-160),new Vector2(950,64),20,muted);
                Button("journey-training","자유 수련장",panel,new Vector2(.5f,.5f),new Vector2(-165,-236),new Vector2(620,84),()=>StartBattle(true),true);
                Button("back","뒤로",panel,new Vector2(.5f,.5f),new Vector2(325,-236),new Vector2(310,84),NavigateBack);return;
            }
            var selected=TideCatalog.Bosses[journeyBoss];
            var portrait=Picture("JourneyBossPortrait",panel,new Vector2(.5f,.5f),new Vector2(-325,-25),new Vector2(250,225),Color.white,TideArt.Enemy(selected.id));portrait.preserveAspect=true;
            Label(selected.name,panel,new Vector2(.5f,.5f),new Vector2(-325,-168),new Vector2(280,38),24,gold,true);
            JourneyText("JourneyBossLesson",TideJourney.Lesson(journeyBoss)+"\n\n기본 체력 · 본편 보정 없음\n보상 없음 · 회복 무료\n소모품·본편 기록 유지",new Vector2(170,-43),new Vector2(600,248),21);
            Button("journey-practice","이 보스 연습",panel,new Vector2(.5f,.5f),new Vector2(-320,-236),new Vector2(294,84),()=>{
                if(!Session.BeginBossPractice(journeyBoss))return;navigation.Clear();ResetInput();ResetFeedback();Refresh();
            },true);
            Button("journey-boss-list","다른 기록",panel,new Vector2(.5f,.5f),new Vector2(0,-236),new Vector2(294,84),()=>{journeyBoss=-1;Refresh();});
            Button("back","뒤로",panel,new Vector2(.5f,.5f),new Vector2(320,-236),new Vector2(294,84),NavigateBack);
        }
        private void BuildDefeat()
        {
            MenuTitle("다시 도전할 수 있습니다","정화한 방과 단서는 저장돼 있습니다.");
            JourneyText("JourneyDefeatAdvice",TideJourney.DefeatAdvice(Session),new Vector2(0,10),new Vector2(950,210),25);
            Button("retry","현재 기록 재도전",panel,new Vector2(.5f,.5f),new Vector2(-320,-214),new Vector2(294,84),()=>StartBattle(false),true);
            Button("defeat-guide","전투 준비 확인",panel,new Vector2(.5f,.5f),new Vector2(0,-214),new Vector2(294,84),()=>OpenJourney(1));
            Button("back","마을로",panel,new Vector2(.5f,.5f),new Vector2(320,-214),new Vector2(294,84),ReturnTown);
        }
        private void AddResultBreather()
        {
            if(!Session.IsCampaign||Session.IsReplay||Session.Dungeon.number!=3)return;
            Button("result-breather","동료와 잠시 정비",panel,new Vector2(.5f,.5f),new Vector2(0,-121),new Vector2(420,84),()=>{
                string id="a"+Session.Dungeon.act+"_party_"+Session.Hero.id;
                ReturnTown();ShowDialogueScene(id,()=>{ReturnTown();OpenMenu(TideMode.Home);},"정비하며 다음 길 확인");
            });
        }
    }
}
