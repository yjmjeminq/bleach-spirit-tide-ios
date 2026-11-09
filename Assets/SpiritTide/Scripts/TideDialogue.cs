using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritTide
{
    [Serializable] public class TideDialogueSeen { public string id; public int hero; }
    [Serializable] public class TideDialogueScene
    {
        public string id,kind,title,place,quest_id;
        public int act,stage,quest_step,branch;
        public StoryLine[] lines;
        public string[] responses;
    }
    [Serializable] public class TideTownReply { public string npc,phase,text; public int act; }
    [Serializable] public class TideDialogueData { public int schema; public TideDialogueScene[] scenes; public TideTownReply[] town; }

    public static class TideDialogue
    {
        public static TideDialogueData Data { get; private set; }
        private static readonly Dictionary<string,TideDialogueScene> scenes=new Dictionary<string,TideDialogueScene>();
        public static void Load()
        {
            var asset=Resources.Load<TextAsset>("SpiritTide/Data/dialogue");
            if(asset==null)throw new InvalidOperationException("Dialogue data missing.");
            Data=JsonUtility.FromJson<TideDialogueData>(asset.text);
            if(Data==null||Data.schema!=1||Data.scenes==null||Data.scenes.Length<189||Data.town==null||Data.town.Length!=105)
                throw new InvalidOperationException("Incomplete dialogue data.");
            scenes.Clear();
            foreach(var scene in Data.scenes)
            {
                if(scene==null||string.IsNullOrEmpty(scene.id)||scenes.ContainsKey(scene.id)||scene.lines==null||scene.lines.Length==0)
                    throw new InvalidOperationException("Invalid dialogue scene.");
                scenes.Add(scene.id,scene);
            }
        }
        public static TideDialogueScene Scene(string id)
        {
            TideDialogueScene scene;
            if(id==null||!scenes.TryGetValue(id,out scene))throw new InvalidOperationException("Missing scene: "+id);
            return scene;
        }
        public static string DungeonId(TideDungeonDef dungeon,string kind)=>"a"+dungeon.act+"_d"+dungeon.number+"_"+kind;
        public static StoryLine[] Lines(string id,int hero)
        {
            var scene=Scene(id);var result=new StoryLine[scene.lines.Length];hero=Mathf.Clamp(hero,0,5);
            for(int i=0;i<result.Length;i++)
            {
                var source=scene.lines[i];bool lead=source.speaker=="$lead";
                if(lead&&(scene.responses==null||scene.responses.Length!=6))throw new InvalidOperationException("Missing lead response: "+id);
                result[i]=new StoryLine{speaker=lead?TideStory.HeroName(hero):source.speaker,text=lead?scene.responses[hero]:source.text};
            }
            return result;
        }
        public static TideDialogueSeen Seen(TideProfile p,string id)
        {
            if(p.dialogueHistory!=null)foreach(var record in p.dialogueHistory)if(record!=null&&record.id==id)return record;
            return null;
        }
        public static void Remember(TideProfile p,string id,int hero)
        {
            Scene(id);if(Seen(p,id)!=null)return;
            var records=new List<TideDialogueSeen>(p.dialogueHistory??Array.Empty<TideDialogueSeen>());
            records.Add(new TideDialogueSeen{id=id,hero=Mathf.Clamp(hero,0,5)});p.dialogueHistory=records.ToArray();
        }
        public static void Sanitize(TideProfile p)
        {
            var clean=new List<TideDialogueSeen>();var seen=new HashSet<string>();
            if(p.dialogueHistory!=null)foreach(var record in p.dialogueHistory)
                if(record!=null&&record.id!=null&&record.id.Length<=64&&scenes.ContainsKey(record.id)&&seen.Add(record.id)&&clean.Count<scenes.Count)
                    clean.Add(new TideDialogueSeen{id=record.id,hero=Mathf.Clamp(record.hero,0,5)});
            p.dialogueHistory=clean.ToArray();
        }
        public static bool CanRead(TideProfile p,TideDialogueScene scene)
        {
            if(scene.act>p.campaignAct)return false;
            int dungeonBit=1<<((scene.act-1)*6+scene.stage-1),actBit=1<<(scene.act-1);
            bool cleared=(p.clearedDungeons&dungeonBit)!=0,reported=(p.reportedActs&actBit)!=0;
            switch(scene.kind)
            {
                case "main_accept":return reported||scene.act<p.campaignAct||(QuestState)p.quest!=QuestState.Available;
                case "main_step":return (p.mainInvestigations&actBit)!=0;
                case "main_checkpoint":return (p.mainEvidence&(1<<scene.quest_step))!=0;
                case "side_offer":return TideQuests.State(p,scene.quest_id)!=null;
                case "side_step":var stepState=TideQuests.State(p,scene.quest_id);return stepState!=null&&stepState.step>scene.quest_step;
                case "side_finish":var finishState=TideQuests.State(p,scene.quest_id);return finishState!=null&&finishState.claimed&&finishState.choice==scene.branch;
                case "report":return reported;
                case "epilogue":return p.claimed;
                case "rescue":return (p.rescueReports&actBit)!=0;
                case "field_echo":return (p.rescuedDungeons&dungeonBit)!=0;
                case "clear":return cleared;
                case "intro":
                    var dungeon=TideCampaign.Dungeon(scene.act,scene.stage);int bit=TideExperience.EncounterBit(dungeon.rooms[dungeon.rooms.Length-1].foes[0]);
                    return cleared||reported||Seen(p,scene.id)!=null||(p.seenIntros&bit)!=0;
                case "party":return reported||scene.act<p.campaignAct||(QuestState)p.quest!=QuestState.Available;
                default:return cleared||reported||Seen(p,scene.id)!=null||scene.act==p.campaignAct&&(QuestState)p.quest!=QuestState.Available&&scene.stage<=p.campaignStage;
            }
        }
        private static int Order(TideDialogueScene s)
        {
            if(s.kind=="main_accept")return 0;
            if(s.kind=="field_echo")return s.stage*100+1;
            if(s.kind=="main_step")return s.stage*100+3;
            if(s.kind=="main_checkpoint")return s.stage*100+4+s.quest_step%5;
            if(s.kind.StartsWith("side_")){var q=TideQuests.Find(s.quest_id);return 1100+(q==null?0:q.index*10)+(s.kind=="side_offer"?0:s.kind=="side_step"?1+s.quest_step:5+s.branch);}
            if(s.kind=="epilogue")return 1000;
            if(s.kind=="party")return 900;
            if(s.kind=="rescue")return 950;
            return s.stage*100+(s.kind=="entry"?0:s.kind=="intro"?10:s.kind=="clear"?20:80);
        }
        public static List<TideDialogueScene> Available(TideProfile p,int act)
        {
            var result=new List<TideDialogueScene>();foreach(var s in Data.scenes)if(s.act==act&&CanRead(p,s))result.Add(s);
            result.Sort((a,b)=>{int order=Order(a).CompareTo(Order(b));return order==0?string.CompareOrdinal(a.id,b.id):order;});return result;
        }
        public static int ArchiveHero(TideProfile p,string id){var seen=Seen(p,id);return seen==null?p.hero:seen.hero;}
        public static string Town(string npc,TideProfile p)
        {
            string phase=p.claimed?"complete":(QuestState)p.quest==QuestState.Ready?"report":(QuestState)p.quest==QuestState.Available?"offer":p.campaignStage>3?"late":"early";
            foreach(var reply in Data.town)if(reply.npc==npc&&reply.act==p.campaignAct&&reply.phase==phase)
                return reply.text.Replace("{next}",TideCampaign.Dungeon(p.campaignAct,p.campaignStage).title);
            throw new InvalidOperationException("Missing town reply: "+npc+" / "+p.campaignAct+" / "+phase);
        }
    }
}
