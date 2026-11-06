using System;
using System.Collections.Generic;
using UnityEngine;
namespace SpiritTide
{
    public struct TideQuestPoint
    {
        public string title;public Vector2 position;public bool main;public TideSideQuest quest;
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
        public bool MainInvestigationPending=>IsCampaign&&!IsReplay&&Dungeon.number==4&&RoomIndex==0&&(Profile.mainInvestigations&(1<<(Dungeon.act-1)))==0;
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
            Profile.gold=Mathf.Min(1000000,Profile.gold+q.gold);Profile.crystals=Mathf.Min(99999,Profile.crystals+q.crystals);TideProgress.AddXp(Profile,q.xp);
            if(Profile.trackedSide==id)Profile.trackedSide="";TideCatalog.Sanitize(Profile);PersistentChange();Changed(q.title+" · 소식 전달 · 경험치 +"+q.xp+" · 골드 +"+q.gold);return true;
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
        private void QuestWardEvent()
        {
            if(!IsCampaign)return;
            foreach(var q in TideQuests.Data.side)
            {
                var state=TideQuests.State(Profile,q.id);if(q.act==Dungeon.act&&state!=null&&!state.claimed&&state.step<q.steps.Length&&q.steps[state.step].kind=="ward")CompleteQuestStep(q);
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
            if(MainInvestigationPending)
            {
                if(index==0){var main=TideQuests.Main(Dungeon.act);return new TideQuestPoint{main=true,title=main.inspect_title,position=new Vector2(main.x,main.y)};}index--;
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
            for(int i=0;i<QuestPointCount;i++){float d=Vector2.Distance(position,QuestPoint(i).position);if(d<distance){distance=d;target=i;}}
            return target;
        }
        public string FieldInteractionLabel
        {
            get {float distance;int target=NearestFieldTarget(out distance);return target>=0?QuestPoint(target).title+" · E":target==-1?"귀환 등불 안내 · E":"영압 장치 해제 · E";}
        }
        private bool InteractQuestPoint(int index)
        {
            var point=QuestPoint(index);
            if(point.main)
            {
                Profile.mainInvestigations|=1<<(Dungeon.act-1);Profile.roomCheckpoint=Mathf.Max(Profile.roomCheckpoint,RoomIndex+1);
                pendingQuestScenes.Enqueue(TideQuests.Main(Dungeon.act).inspect_scene);PersistentChange();Changed(point.title+" · 귀환 신호를 남겼습니다. 다음 통로로 갈 수 있습니다.");
            }
            else {CompleteQuestStep(point.quest);Changed(point.quest.title+" · "+TideQuests.Objective(Profile,point.quest));}
            return true;
        }
    }
}
