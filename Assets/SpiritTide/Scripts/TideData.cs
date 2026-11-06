using System;
using UnityEngine;

namespace SpiritTide
{
    [Serializable] public class SkillDef { public string id, name, effect; public float cooldown_seconds, spirit_cost; }
    [Serializable] public class CharacterDef
    {
        public string id, name, role, feel, basic, combo, appearance;
        public SkillDef[] skills;
        public SkillDef ultimate;
        public SkillDef Skill(int index) { return index == 4 ? ultimate : skills[index]; }
    }
    [Serializable] public class CharacterCatalog { public CharacterDef[] characters; }
    [Serializable] public class PatternDef { public string name, tell, action, counter; }
    [Serializable] public class PhaseDef { public string trigger; public PatternDef[] patterns; }
    [Serializable] public class BossDef { public string id, name, rank, canon_basis; public int act; public PhaseDef[] phases; }
    [Serializable] public class BossCatalog { public BossDef[] bosses; }
    [Serializable] public class EnemyDef { public string id, name, rank, canon_type; public int act; }
    [Serializable] public class EnemyCatalog { public EnemyDef[] enemies; }
    [Serializable] public class DungeonDef { public string title, objective, encounter_id; public int number; public string[] enemy_ids, wave_enemy_ids; }
    [Serializable] public class ActDef { public int act; public string title, region, midboss_id, boss_id; public DungeonDef[] dungeons; }
    [Serializable] public class ActCatalog { public ActDef[] acts; }

    public enum TideMode { Town, Combat, Training, Dialogue, Inventory, Shop, Missions, Settings, Pause, Result, Defeat, Story, Help, ConfirmRetreat, Forge, Journal, Quests, Home, Journey }
    public enum QuestState { Available, Active, Ready, Completed }
    public enum MoveKind { Basic, Heavy, Launcher, Air, Counter, Skill0, Skill1, Skill2, Skill3, Ultimate }

    [Serializable] public class TideProfile
    {
        public int version = 1;
        public int hero, gold = 800, potions = 3, quest, checkpoint;
        public bool claimed, precision, locked, reducedEffects = false, holdAttack = true, autoFace = true;
        public float uiScale = 1;
        public int controlSchema = 1;
        public int keyboardHints; // 0 auto, 1 show, 2 hide; absent legacy fields safely default to auto.
        public float controlScale = 1, controlOpacity = .85f;
        public bool soundEnabled = true;
        public int campaignSchema,campaignAct,campaignStage,roomCheckpoint,clearedDungeons,reportedActs,level,xp,crystals,powerGear,guardGear;
        public TideGear[] gear;
        public int rescuedDungeons,rescueReports,seenIntros,seenOutros;
        public TideDialogueSeen[] dialogueHistory=Array.Empty<TideDialogueSeen>();
        public int questSchema,mainInvestigations;
        public string trackedSide="";
        public TideSideQuestState[] sideQuests=Array.Empty<TideSideQuestState>();
    }

    public static class TideCatalog
    {
        public static CharacterDef[] Characters;
        public static BossDef[] Bosses;
        public static EnemyDef[] Enemies;
        public static ActDef[] Acts;
        public static void Load()
        {
            Characters = Read<CharacterCatalog>("characters").characters;
            Bosses = Read<BossCatalog>("bosses").bosses;
            Enemies = Read<EnemyCatalog>("enemies").enemies;
            Acts = Read<ActCatalog>("acts").acts;
            if (Characters == null || Characters.Length != 6 || Bosses == null || Bosses.Length != 10 ||
                Enemies == null || Enemies.Length != 60 || Acts == null || Acts.Length != 5)
                throw new InvalidOperationException("Design catalog is incomplete.");
            foreach (CharacterDef c in Characters)
                if (c.skills == null || c.skills.Length != 4 || c.ultimate == null)
                    throw new InvalidOperationException("Missing skills for " + c.id);
            TideCampaign.Load();TideQuests.Load();
        }
        private static T Read<T>(string name)
        {
            TextAsset asset = Resources.Load<TextAsset>("SpiritTide/Data/" + name);
            if (asset == null) throw new InvalidOperationException("Missing design data: " + name);
            T value = JsonUtility.FromJson<T>(asset.text);
            if (value == null) throw new InvalidOperationException("Invalid design data: " + name);
            return value;
        }
        public static void Sanitize(TideProfile p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            p.version = 1; p.hero = Mathf.Clamp(p.hero, 0, 5); p.gold = Mathf.Clamp(p.gold, 0, 1000000);
            p.potions = Mathf.Clamp(p.potions, 0, 5);
            p.uiScale = float.IsNaN(p.uiScale) || float.IsInfinity(p.uiScale) ? 1 : Mathf.Clamp(p.uiScale, .85f, 1.25f);
            if(p.controlSchema<1){p.controlScale=1;p.controlOpacity=.85f;p.soundEnabled=true;}
            p.controlSchema=1;
            p.keyboardHints=Mathf.Clamp(p.keyboardHints,0,2);
            p.controlScale=float.IsNaN(p.controlScale)||float.IsInfinity(p.controlScale)?1:Mathf.Clamp(p.controlScale,1,1.1f);
            p.controlOpacity=float.IsNaN(p.controlOpacity)||float.IsInfinity(p.controlOpacity)?.85f:Mathf.Clamp(p.controlOpacity,.65f,1);
            TideProgress.Sanitize(p);
            TideQuests.Sanitize(p);
            TideDialogue.Sanitize(p);
        }
    }
}
