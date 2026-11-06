using System;
using System.Collections.Generic;
using UnityEngine;
namespace SpiritTide
{
    [Serializable] public class TideMainQuest
    {
        public int act,stage,room;public float x,y;
        public string title,premise,giver,accept_scene,inspect_scene,inspect_title;
    }
    [Serializable] public class TideQuestStep
    {
        public string kind,title,goal,npc,scene;public int stage,room;public float x,y;
    }
    [Serializable] public class TideSideQuest
    {
        public int index,act,unlock_stage,gold,xp,crystals;
        public string id,giver,title,summary,keepsake,afterglow,offer_scene,active_line,ready_line;
        public string[] choices,finish_scenes,choice_notes,choice_keepsakes;
        public TideQuestStep[] steps;
    }
    [Serializable] public class TideSideQuestState {public string id;public int step,choice,order;public bool claimed;}
    [Serializable] public class TideQuestData {public int schema;public TideMainQuest[] main;public TideSideQuest[] side;}
    public static class TideQuests
    {
        public static TideQuestData Data {get;private set;}
        public static void Load()
        {
            var text=Resources.Load<TextAsset>("SpiritTide/Data/quests");if(text==null)throw new InvalidOperationException("Quest data missing.");
            Data=JsonUtility.FromJson<TideQuestData>(text.text);
            if(Data==null||Data.schema!=1||Data.main.Length!=5||Data.side.Length!=15)throw new InvalidOperationException("Quest data incomplete.");
        }
        public static TideMainQuest Main(int act)=>Data.main[Mathf.Clamp(act,1,5)-1];
        public static TideSideQuest Find(string id){if(Data!=null)foreach(var q in Data.side)if(q.id==id)return q;return null;}
        public static TideSideQuestState State(TideProfile p,string id){if(p.sideQuests!=null)foreach(var s in p.sideQuests)if(s!=null&&s.id==id)return s;return null;}
        public static bool CanOffer(TideProfile p,TideSideQuest q)
        {
            if(q==null||q.act>p.campaignAct||State(p,q.id)!=null)return false;
            return q.act<p.campaignAct||(QuestState)p.quest!=QuestState.Available&&p.campaignStage>=q.unlock_stage;
        }
        public static bool Ready(TideProfile p,TideSideQuest q){var s=State(p,q.id);return s!=null&&!s.claimed&&s.step>=q.steps.Length;}
        public static string Giver(string npc)=>npc=="urahara"?"우라하라":npc=="tessai"?"텟사이":npc=="kon"?"콘":npc=="byakuya"?"뱌쿠야":"쿠카쿠";
        public static string Objective(TideProfile p,TideSideQuest q)
        {
            var s=State(p,q.id);return s==null?q.summary:s.claimed?"이야기 기록 · "+q.choice_keepsakes[s.choice]:s.step>=q.steps.Length?Giver(q.giver)+"에게 소식 전하기":q.steps[s.step].goal;
        }
        public static string Status(TideProfile p,TideSideQuest q)
        {
            var s=State(p,q.id);return s==null?CanOffer(p,q)?"부탁 가능":"아직 만나지 않은 부탁":s.claimed?"소식 전달 완료":s.step>=q.steps.Length?"소식 전달 가능":"진행 중 · "+(s.step+1)+" / "+q.steps.Length;
        }
        public static int Notices(TideProfile p,string npc)
        {
            int count=0;foreach(var q in Data.side)if(q.giver==npc&&(CanOffer(p,q)||Ready(p,q)))count++;return count;
        }
        public static string NpcReply(TideProfile p,string npc,string normal)
        {
            if(npc=="urahara"&&((QuestState)p.quest==QuestState.Available||(QuestState)p.quest==QuestState.Ready))return normal;
            TideSideQuest latest=null;int order=-1;
            foreach(var q in Data.side)if(q.giver==npc)
            {
                var state=State(p,q.id);if(state==null)continue;
                if(Ready(p,q))return q.ready_line;
                if(!state.claimed&&q.act==p.campaignAct)return q.active_line;
                if(state.claimed&&state.order>order){order=state.order;latest=q;}
            }
            return latest==null?normal:latest.choice_notes[State(p,latest.id).choice];
        }
        public static string TownMemory(TideProfile p)
        {
            TideSideQuest latest=null;int order=-1;
            foreach(var s in p.sideQuests)if(s.claimed&&s.order>order){latest=Find(s.id);order=s.order;}
            if(latest==null)return "상점 앞 작은 게시판\n돌아오는 사람들의 소식을 기다립니다.";
            var state=State(p,latest.id);string note=latest.choice_notes[state.choice];int end=note.IndexOf('.');if(end>=0)note=note.Substring(0,end+1);
            return "남겨진 이야기\n"+latest.choice_keepsakes[state.choice]+"\n"+note;
        }
        public static void Sanitize(TideProfile p)
        {
            if(Data==null)return;
            if(p.questSchema<1)
            {
                p.mainInvestigations=0;
                for(int act=1;act<=5;act++)if((p.reportedActs&(1<<(act-1)))!=0||(p.clearedDungeons&(1<<((act-1)*6+3)))!=0||act<p.campaignAct||act==p.campaignAct&&(p.campaignStage>4||p.campaignStage==4&&p.roomCheckpoint>0))p.mainInvestigations|=1<<(act-1);
            }
            p.questSchema=1;p.mainInvestigations&=31;
            var clean=new List<TideSideQuestState>();var ids=new HashSet<string>();
            if(p.sideQuests!=null)foreach(var state in p.sideQuests)
            {
                var q=state==null?null:Find(state.id);if(q==null||q.act>p.campaignAct||!ids.Add(q.id)||clean.Count>=15)continue;
                state.step=Mathf.Clamp(state.step,0,q.steps.Length);state.choice=Mathf.Clamp(state.choice,0,q.choices.Length-1);
                state.claimed&=state.step==q.steps.Length;state.order=state.claimed?Mathf.Clamp(state.order,1,15):0;clean.Add(state);
            }
            p.sideQuests=clean.ToArray();var tracked=State(p,p.trackedSide);
            if(tracked==null||tracked.claimed)p.trackedSide="";
        }
    }
}
