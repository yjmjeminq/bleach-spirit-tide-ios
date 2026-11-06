using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritTide
{
    [Serializable] public class StoryLine { public string speaker, text; }
    [Serializable] public class StoryAct
    {
        public int act, next_act;
        public string title, midboss_id, boss_id, entry_clue, mid_clue, boss_clue, after_mid, after_boss;
        public StoryLine[] departure, before_boss, report, bridge;
    }
    [Serializable] public class StoryHero { public string id, departure, after_mid, after_boss; }
    [Serializable] public class StoryTown
    {
        public string stage, urahara, tessai, kon, byakuya, kukaku;
        public string Text(string npc)
        {
            switch(npc){case "urahara":return urahara;case "tessai":return tessai;case "kon":return kon;case "byakuya":return byakuya;case "kukaku":return kukaku;default:throw new ArgumentException("Unknown NPC: "+npc);}
        }
    }
    [Serializable] public class StoryCatalog { public int schema; public string note; public StoryAct[] acts; public StoryHero[] heroes; public StoryTown[] town; }
    public static class TideStory
    {
        public static StoryCatalog Data {get;private set;}
        public static void Load()
        {
            TextAsset source=Resources.Load<TextAsset>("SpiritTide/Data/story_flow");
            if(source==null)throw new InvalidOperationException("Story flow data is missing.");
            Data=JsonUtility.FromJson<StoryCatalog>(source.text);TideExperience.Load();TideDialogue.Load();
            if(Data==null||Data.schema!=1||Data.acts==null||Data.acts.Length!=5||Data.heroes==null||Data.heroes.Length!=6||Data.town==null||Data.town.Length!=5)
                throw new InvalidOperationException("Story flow data is incomplete.");
        }
        public static string HeroName(int hero){return new[]{"루키아","오리히메","요루이치","소이폰","란기쿠","우노하나"}[Mathf.Clamp(hero,0,5)];}
        public static string Stage(TideProfile profile)
        {
            if(profile.claimed||(QuestState)profile.quest==QuestState.Completed)return "complete";
            if((QuestState)profile.quest==QuestState.Ready)return "report";
            if((QuestState)profile.quest==QuestState.Available)return "offer";
            return profile.campaignStage>3?"fisher":"shrieker";
        }
        public static string NpcText(string npc,TideProfile profile)
        {
            return TideQuests.NpcReply(profile,npc,TideDialogue.Town(npc,profile));
        }
        public static StoryLine[] BeforeDungeon(TideDungeonDef dungeon,TideProfile profile,bool replay)
        {
            if(replay)return new[]{new StoryLine{speaker="우라하라",text="정화한 전장의 재현입니다. 경험치와 골드는 줄어들며, 최초 전리품과 조사 진행은 반복되지 않습니다."}};
            if(profile.roomCheckpoint>0)return new[]{new StoryLine{speaker="쿠카쿠",text="정화한 길은 남아 있어. "+(profile.roomCheckpoint+1)+"번째 방부터 이어 가면 된다. 돌아올 표지도 그대로야."}};
            return TideDialogue.Lines(TideDialogue.DungeonId(dungeon,"entry"),profile.hero);
        }
        public static string DungeonOutcome(TideDungeonDef dungeon,TideProfile profile=null)
        {
            return dungeon.resolution;
        }
        public static StoryLine[] Report(int act=1)
        {
            var data=Data.acts[Mathf.Clamp(act,1,5)-1];var lines=new List<StoryLine>(data.report);lines.AddRange(data.bridge);return lines.ToArray();
        }
    }
}
