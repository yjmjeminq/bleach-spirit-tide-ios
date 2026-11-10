using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritTide
{
    [Serializable] public class TideRoomDef
    {
        public string title,kind,layout,hazard,signal_title,signal_reply;
        public bool echo,ward,reinforcement;
        public string[] foes,wave_layouts;
        public int[] wave_sizes;
        public int signal_wave;
        public float pressure;
        public int WaveCount => wave_sizes != null && wave_sizes.Length > 0 ? wave_sizes.Length : reinforcement && foes.Length > 2 ? 2 : 1;
        public int WaveSize(int wave) => wave_sizes != null && wave_sizes.Length > 0
            ? wave_sizes[wave] : WaveCount == 1 ? foes.Length : wave == 0 ? 2 : foes.Length - 2;
        public string Formation(int wave) => wave_layouts != null && wave_layouts.Length > wave ? wave_layouts[wave] : layout;
    }
    [Serializable] public class TideDungeonDef {public int index,act,number,xp,gold,legacy_rooms;public string title,objective,background,reason,resolution,landmark;public TideRoomDef[] rooms;}
    [Serializable] public class TideFoeDef {public string id,name,rank,brain,atlas,tell,counter,attack;public int act,cell,base_hp;public bool IsBoss=>rank=="boss"||rank=="midboss";public bool IsNamed=>rank=="named";}
    [Serializable] public class TideCampaignCatalog {public int schema;public TideDungeonDef[] stages;public TideFoeDef[] foes;}
    public static class TideCampaign
    {
        public const int MaxRooms=14;
        public static TideCampaignCatalog Data {get;private set;}
        private static readonly Dictionary<string,TideFoeDef> Foes=new Dictionary<string,TideFoeDef>();
        public static void Load()
        {
            TextAsset source=Resources.Load<TextAsset>("SpiritTide/Data/campaign");
            if(source==null)throw new InvalidOperationException("Campaign data missing.");
            Data=JsonUtility.FromJson<TideCampaignCatalog>(source.text);
            if(Data==null||Data.schema!=1||Data.stages.Length!=30||Data.foes.Length!=70)throw new InvalidOperationException("Campaign data incomplete.");
            Foes.Clear();foreach(var f in Data.foes)Foes.Add(f.id,f);
            for(int i=0;i<30;i++)
            {
                var d=Data.stages[i];
                if(d.index!=i||d.act!=i/6+1||d.number!=i%6+1||d.rooms.Length<2||d.rooms.Length>MaxRooms
                    ||d.legacy_rooms<2||d.legacy_rooms>d.rooms.Length)
                    throw new InvalidOperationException("Invalid dungeon progression.");
                foreach(var room in d.rooms)
                {
                    int count=0;
                    for(int wave=0;wave<room.WaveCount;wave++)
                    {
                        int size=room.WaveSize(wave);
                        if(size<1||size>4)throw new InvalidOperationException("Invalid wave size: "+room.title);
                        count+=size;
                    }
                    if(count!=room.foes.Length||room.signal_wave!=0&&(room.signal_wave<2||room.signal_wave>room.WaveCount
                        ||string.IsNullOrEmpty(room.signal_title)||string.IsNullOrEmpty(room.signal_reply)))
                        throw new InvalidOperationException("Invalid expedition room: "+room.title);
                    foreach(string id in room.foes)if(!Foes.ContainsKey(id))throw new InvalidOperationException("Unknown room enemy: "+id);
                }
            }
        }
        public static TideFoeDef Foe(string id){return Foes[id];}
        public static TideDungeonDef Dungeon(int act,int number){return Data.stages[(Mathf.Clamp(act,1,5)-1)*6+Mathf.Clamp(number,1,6)-1];}
        public static bool CanVisitRoom(TideProfile profile,int act,int number,int room)
        {
            if(act<1||act>5||number<1||number>6||room<0)return false;
            var dungeon=Dungeon(act,number);
            return room<dungeon.rooms.Length&&((profile.clearedDungeons&(1<<dungeon.index))!=0
                ||profile.campaignAct==act&&profile.campaignStage==number&&profile.quest==(int)QuestState.Active&&room<profile.roomCheckpoint);
        }
    }
    [Serializable] public class TideGear
    {
        public int uid,tier,rarity,upgrade;public string name,slot;public bool favorite;
        public int Attack=>slot=="power"?Mathf.Max(0,tier*3+rarity*2+upgrade*3):0;
        public int Health=>slot=="guard"?Mathf.Max(0,tier*12+rarity*8+upgrade*8):0;
    }
    public static class TideProgress
    {
        public const int MaxLevel=40,AllDungeons=0x3fffffff;
        public const int AllHeroes=63;
        private static readonly int[] LearningLevels={1,3,5,8,0,4,7,12,18};
        public static int NeedXp(int level){return 80+Mathf.Clamp(level,1,MaxLevel)*20;}
        public static bool HeroJoined(TideProfile p,int hero)=>hero>=0&&hero<6&&(p.recruitedHeroes&(1<<hero))!=0;
        public static bool SkillLearned(TideProfile p,int slot)
        {
            if(slot<0||slot>=CharacterDef.SkillCount)return false;
            return slot<CharacterDef.FirstExtraSkill&&p.classicSkills
                ||(slot==CharacterDef.Awakening?(p.reportedActs&4)!=0:p.level>=LearningLevels[slot]);
        }
        public static string SkillRequirement(int slot)=>slot==CharacterDef.Awakening?"Act 3 보고 후 습득":"Lv."+LearningLevels[slot]+" 습득";
        public static string LearningBadge(int slot)=>slot==CharacterDef.Awakening?"Act 3":"Lv."+LearningLevels[slot];
        public static string LearnedByLevel(TideProfile p,int before)
        {
            if(before==p.level)return "";
            var names=new List<string>();
            for(int slot=1;slot<LearningLevels.Length;slot++)
                if(slot!=CharacterDef.Awakening&&before<LearningLevels[slot]&&SkillLearned(p,slot)
                    &&(!p.classicSkills||slot>=CharacterDef.FirstExtraSkill))
                    names.Add(TideCatalog.Characters[p.hero].Skill(slot).name);
            if(names.Count==0)return "";
            return names.Count==1?"새 기술 · "+names[0]:"새 기술 · "+names[0]+" 외 "+(names.Count-1)+"개 · 기술 장착에서 확인";
        }
        public static TideGear Gear(TideProfile p,int uid){if(p.gear!=null)foreach(var g in p.gear)if(g!=null&&g.uid==uid)return g;return null;}
        public static void Sanitize(TideProfile p)
        {
            if(p.campaignSchema<1)
            {
                int oldQuest=Mathf.Clamp(p.quest,0,3),oldCheckpoint=Mathf.Clamp(p.checkpoint,0,2);bool reported=p.claimed||oldQuest==3;
                p.campaignSchema=1;p.campaignAct=reported?2:1;p.campaignStage=reported?1:oldQuest==2?6:oldCheckpoint>0?4:1;p.roomCheckpoint=0;
                p.reportedActs=reported?1:0;p.clearedDungeons=reported||oldQuest==2?63:oldCheckpoint>0?7:0;
                p.level=reported||oldQuest==2?7:oldCheckpoint>0?4:1;p.xp=0;p.crystals=0;
                p.quest=reported?0:oldQuest;p.claimed=false;
                p.gear=new[]{new TideGear{uid=1,name="정찰의 영장",slot="power"},new TideGear{uid=2,name="정밀의 영장",slot="power",rarity=1},new TideGear{uid=3,name="수호의 영장",slot="guard"}};
                p.powerGear=p.precision?2:1;p.guardGear=3;
            }
            p.campaignAct=Mathf.Clamp(p.campaignAct,1,5);p.campaignStage=Mathf.Clamp(p.campaignStage,1,6);
            var dungeon=TideCampaign.Dungeon(p.campaignAct,p.campaignStage);
            if(p.campaignSchema<2)
            {
                bool played=p.hero>0||p.quest!=0||p.campaignAct>1||p.campaignStage>1||p.roomCheckpoint>0
                    ||p.level>1||p.clearedDungeons!=0||p.sideQuests!=null&&p.sideQuests.Length>0;
                p.recruitedHeroes=played?AllHeroes:1;
                p.classicSkills=played;
                // Original non-boss room indices are stable. Only the boss moves past the new approach.
                if(dungeon.rooms[dungeon.rooms.Length-1].kind=="boss"&&p.roomCheckpoint==dungeon.legacy_rooms-1)
                    p.roomCheckpoint=dungeon.rooms.Length-1;
            }
            p.campaignSchema=2;
            p.recruitedHeroes=(p.recruitedHeroes&AllHeroes)|1;
            if(!HeroJoined(p,p.hero))p.hero=0;
            p.roomCheckpoint=Mathf.Clamp(p.roomCheckpoint,0,dungeon.rooms.Length-1);
            p.reportedActs&=31;p.clearedDungeons&=AllDungeons;p.level=Mathf.Clamp(p.level,1,MaxLevel);p.xp=Mathf.Clamp(p.xp,0,NeedXp(p.level)-1);p.crystals=Mathf.Clamp(p.crystals,0,99999);
            p.rescuedDungeons&=AllDungeons;p.rescueReports&=31;p.seenIntros&=1023;p.seenOutros&=1023;
            if(p.gear==null||p.gear.Length==0)p.gear=new[]{new TideGear{uid=1,name="정찰의 영장",slot="power"},new TideGear{uid=3,name="수호의 영장",slot="guard"}};
            var clean=new List<TideGear>();var ids=new HashSet<int>();
            foreach(var g in p.gear)if(g!=null&&g.uid>0&&ids.Add(g.uid)&&clean.Count<48)
            {g.tier=Mathf.Clamp(g.tier,0,5);g.rarity=Mathf.Clamp(g.rarity,0,2);g.upgrade=Mathf.Clamp(g.upgrade,0,5);g.slot=g.slot=="guard"?"guard":"power";if(string.IsNullOrEmpty(g.name))g.name=g.slot=="power"?"힘의 영장":"수호의 영장";clean.Add(g);}
            if(clean.Count==0)clean.Add(new TideGear{uid=1,name="정찰의 영장",slot="power"});
            p.gear=clean.ToArray();
            if(Gear(p,p.powerGear)==null||Gear(p,p.powerGear).slot!="power"){p.powerGear=0;foreach(var g in p.gear)if(g.slot=="power"){p.powerGear=g.uid;break;}}
            if(Gear(p,p.guardGear)==null||Gear(p,p.guardGear).slot!="guard"){p.guardGear=0;foreach(var g in p.gear)if(g.slot=="guard"){p.guardGear=g.uid;break;}}
            if((p.reportedActs&31)==31){p.campaignAct=5;p.campaignStage=6;p.quest=3;p.claimed=true;}
            else {p.quest=Mathf.Clamp(p.quest,0,2);p.claimed=false;}
            p.checkpoint=p.campaignStage>3?1:0;if(p.quest==2)p.checkpoint=2;
        }
        public static void AddXp(TideProfile p,int amount)
        {
            p.xp+=Mathf.Max(0,amount);while(p.level<MaxLevel&&p.xp>=NeedXp(p.level)){p.xp-=NeedXp(p.level);p.level++;}if(p.level>=MaxLevel)p.xp=0;
        }
    }
}
