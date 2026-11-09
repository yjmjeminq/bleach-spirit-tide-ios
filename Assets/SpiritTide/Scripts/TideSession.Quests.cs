using System;
using System.Collections.Generic;
using UnityEngine;
namespace SpiritTide
{
    public struct TideQuestPoint
    {
        public string title;public Vector2 position;public bool main;public TideSideQuest quest;public TideMainPoint evidence;
    }
    public sealed partial class TideSession
    {
        private readonly Queue<string> pendingQuestScenes=new Queue<string>();
        public bool TrainingAttacks {get;private set;}
        public void ToggleTrainingAttacks(){if(IsTraining){TrainingAttacks=!TrainingAttacks;Changed(TrainingAttacks?"예고 공격 연습 · 공격 직전 회피하면 반격할 수 있습니다.":"예고 공격 끔 · 기술을 자유롭게 맞춰 보세요.");}}
        private bool HasPerfectTrainingObjective()
        {
            foreach(var q in TideQuests.Data.side){var state=TideQuests.State(Profile,q.id);if(state!=null&&!state.claimed&&state.step<q.steps.Length&&q.steps[state.step].kind=="perfect")return true;}return false;
        }
        public string PendingQuestScene=>pendingQuestScenes.Count==0?null:pendingQuestScenes.Peek();
        public void ConsumeQuestScene(){if(pendingQuestScenes.Count>0)pendingQuestScenes.Dequeue();}
        public void ClearQuestNotifications(){pendingQuestScenes.Clear();}
        public TideMainPoint MainPoint=>IsCampaign&&!IsReplay?TideQuests.PendingPoint(Profile,Dungeon.act,Dungeon.number,RoomIndex):null;
        public bool MainInvestigationPending=>MainPoint!=null;
        public bool GateReady=>RoomCleared&&!MainInvestigationPending;
        public bool AcceptSideQuest(string id)
        {
            var q=TideQuests.Find(id);if(InBattle||!TideQuests.CanOffer(Profile,q))return false;
            var list=new List<TideSideQuestState>(Profile.sideQuests);list.Add(new TideSideQuestState{id=q.id});Profile.sideQuests=list.ToArray();
            if(string.IsNullOrEmpty(Profile.trackedSide))Profile.trackedSide=id;
            PersistentChange();Changed(q.title+" · "+q.steps[0].goal);return true;
        }
        public bool TrackSideQuest(string id)
        {
            var state=TideQuests.State(Profile,id);if(state==null||state.claimed)return false;
            Profile.trackedSide=id;PersistentChange();return true;
        }
        public bool ClaimSideQuest(string id,int choice)
        {
            var q=TideQuests.Find(id);if(InBattle||q==null||!TideQuests.Ready(Profile,q)||choice<0||choice>=q.choices.Length)return false;
            var state=TideQuests.State(Profile,id);state.choice=choice;state.claimed=true;int order=0;foreach(var item in Profile.sideQuests)order=Mathf.Max(order,item.order);state.order=order+1;
            int before=Profile.level;
            Profile.gold=Mathf.Min(1000000,Profile.gold+q.gold);Profile.crystals=Mathf.Min(99999,Profile.crystals+q.crystals);TideProgress.AddXp(Profile,q.xp);
            string learned=TideProgress.LearnedByLevel(Profile,before);
            if(Profile.trackedSide==id)Profile.trackedSide="";TideCatalog.Sanitize(Profile);PersistentChange();Changed(learned.Length>0?learned:q.title+" · 소식 전달 · 경험치 +"+q.xp+" · 골드 +"+q.gold);return true;
        }
        private void CompleteQuestStep(TideSideQuest q)
        {
            var state=TideQuests.State(Profile,q.id);if(state==null||state.claimed||state.step>=q.steps.Length)return;
            pendingQuestScenes.Enqueue(q.steps[state.step].scene);state.step++;PersistentChange();
        }
        private void QuestCombatEvent(string kind)
        {
            if(!IsTraining)return;
            foreach(var q in TideQuests.Data.side)
            {
                var state=TideQuests.State(Profile,q.id);if(state!=null&&!state.claimed&&state.step<q.steps.Length&&q.steps[state.step].kind==kind)CompleteQuestStep(q);
            }
        }
        private void QuestFieldEvent(string kind)
        {
            if(!IsCampaign)return;
            foreach(var q in TideQuests.Data.side)
            {
                var state=TideQuests.State(Profile,q.id);
                if(q.act!=Dungeon.act||state==null||state.claimed||state.step>=q.steps.Length)continue;
                var step=q.steps[state.step];
                if(step.kind==kind&&(step.stage==0||step.stage==Dungeon.number&&step.room==RoomIndex))CompleteQuestStep(q);
            }
        }
        public bool QuestTalk(string npc)
        {
            if(InBattle)return false;bool changed=false;
            foreach(var q in TideQuests.Data.side)
            {
                var state=TideQuests.State(Profile,q.id);if(state==null||state.claimed||state.step>=q.steps.Length)continue;
                var step=q.steps[state.step];if(step.kind=="talk"&&step.npc==npc){CompleteQuestStep(q);changed=true;}
            }
            return changed;
        }
        public int QuestPointCount
        {
            get {if(!IsCampaign||!RoomCleared)return 0;int n=MainInvestigationPending?1:0;foreach(var q in TideQuests.Data.side)if(QuestSiteAvailable(q))n++;return n;}
        }
        private bool QuestSiteAvailable(TideSideQuest q)
        {
            if(!IsCampaign||!RoomCleared||q.act!=Dungeon.act)return false;
            var state=TideQuests.State(Profile,q.id);if(state==null||state.claimed||state.step>=q.steps.Length)return false;
            var step=q.steps[state.step];return step.kind=="site"&&step.stage==Dungeon.number&&step.room==RoomIndex;
        }
        public TideQuestPoint QuestPoint(int index)
        {
            var main=MainPoint;
            if(main!=null)
            {
                if(index==0)return new TideQuestPoint{main=true,title=main.title,position=new Vector2(main.x,main.y),evidence=main};
                index--;
            }
            foreach(var q in TideQuests.Data.side)if(QuestSiteAvailable(q))
            {
                if(index--==0){var step=q.steps[TideQuests.State(Profile,q.id).step];return new TideQuestPoint{quest=q,title=step.title,position=new Vector2(step.x,step.y)};}
            }
            throw new InvalidOperationException("Quest point out of range.");
        }
        private int NearestFieldTarget(out float distance)
        {
            distance=float.MaxValue;int target=-3;var position=new Vector2(X,Y);
            if(EchoAvailable){distance=Vector2.Distance(position,new Vector2(EchoX,0));target=-1;}
            if(WardActive&&!RoomCleared){float d=Vector2.Distance(position,WardPosition);if(d<distance){distance=d;target=-2;}}
            if(RoomSignalPending){float d=Vector2.Distance(position,SignalPosition);if(d<distance){distance=d;target=-4;}}
            for(int i=0;i<QuestPointCount;i++){float d=Vector2.Distance(position,QuestPoint(i).position);if(d<distance){distance=d;target=i;}}
            return target;
        }
        public string FieldInteractionLabel
        {
            get {float distance;int target=NearestFieldTarget(out distance);return target>=0?(QuestPoint(target).main?"현장 조사 · E":"부탁 조사 · E"):target==-4?"신호 보내기 · E":target==-1?"귀환 등불 안내 · E":"영압 장치 해제 · E";}
        }
        private bool InteractQuestPoint(int index)
        {
            var point=QuestPoint(index);
            if(point.main)
            {
                var evidence=point.evidence;
                if(evidence.bit<0)Profile.mainInvestigations|=1<<(Dungeon.act-1);
                else Profile.mainEvidence|=1<<evidence.bit;
                int newHero=evidence.grant&~Profile.recruitedHeroes;
                Profile.recruitedHeroes|=evidence.grant;
                if(!MainInvestigationPending)Profile.roomCheckpoint=Mathf.Max(Profile.roomCheckpoint,RoomIndex+1);
                pendingQuestScenes.Enqueue(evidence.scene);
                PersistentChange();
                string result=point.title+" · 확인 완료. 다음 통로로 갈 수 있습니다.";
                for(int hero=1;hero<6;hero++)
                    if(newHero==(1<<hero))result=TideStory.HeroName(hero)+" 합류 · 거점에서 선두로 선택할 수 있습니다.";
                Changed(result);
            }
            else {CompleteQuestStep(point.quest);Changed(point.quest.title+" · "+TideQuests.Objective(Profile,point.quest));}
            return true;
        }
    }
}
