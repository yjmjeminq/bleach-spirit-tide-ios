using System;
using UnityEngine;

namespace SpiritTide
{
    [Serializable] public class SkillDef { public string id, name, effect, label; public float cooldown_seconds, spirit_cost; }
    [Serializable] public class CharacterDef
    {
        public string id, name, role, feel, basic, combo, appearance;
        public const int Awakening = 4, FirstExtraSkill = 5, SkillCount = 9;
        public SkillDef[] skills, extra_skills;
        public SkillDef ultimate;
        public SkillDef Skill(int index)
        {
            if (index == Awakening) return ultimate;
            return index < FirstExtraSkill ? skills[index] : extra_skills[index - FirstExtraSkill];
        }
        public int SkillIndex(string id)
        {
            for (int i = 0; i < SkillCount; i++) if (Skill(i).id == id) return i;
            return -1;
        }
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

    public enum TideMode { Town, Combat, Training, Dialogue, Inventory, Shop, Missions, Settings, Pause, Result, Defeat, Story, Help, ConfirmRetreat, Forge, Journal, Quests, Home, Journey, Title, Loadout }
    public enum QuestState { Available, Active, Ready, Completed }
    public enum MoveKind { Basic, Heavy, Launcher, Air, Counter, Skill0, Skill1, Skill2, Skill3, Ultimate, Skill5, Skill6, Skill7, Skill8 }

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
        public int mainEvidence,recruitedHeroes;
        public bool classicSkills;
        public string trackedSide="";
        public TideSideQuestState[] sideQuests=Array.Empty<TideSideQuestState>();
        public TideEquippedSkills[] skillLoadouts;
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
            var expandedSkills = Read<CharacterCatalog>("extra_skills").characters;
            Bosses = Read<BossCatalog>("bosses").bosses;
            Enemies = Read<EnemyCatalog>("enemies").enemies;
            Acts = Read<ActCatalog>("acts").acts;
            if (Characters == null || Characters.Length != 6 || expandedSkills == null || expandedSkills.Length != Characters.Length ||
                Bosses == null || Bosses.Length != 10 ||
                Enemies == null || Enemies.Length != 60 || Acts == null || Acts.Length != 5)
                throw new InvalidOperationException("Design catalog is incomplete.");
            for (int hero = 0; hero < Characters.Length; hero++)
            {
                CharacterDef c = Characters[hero];
                if (expandedSkills[hero]?.id != c.id) throw new InvalidOperationException("Mismatched skills for " + c.id);
                c.extra_skills = expandedSkills[hero].extra_skills;
                if (c.skills == null || c.skills.Length != CharacterDef.Awakening || c.ultimate == null ||
                    c.extra_skills == null || c.extra_skills.Length != CharacterDef.SkillCount - CharacterDef.FirstExtraSkill)
                    throw new InvalidOperationException("Missing skills for " + c.id);
                for (int i = 0; i < CharacterDef.SkillCount; i++)
                {
                    var skill = c.Skill(i);
                    if (skill == null || string.IsNullOrEmpty(skill.id) || string.IsNullOrEmpty(skill.name) ||
                        skill.cooldown_seconds <= 0 || skill.spirit_cost < 0 || c.SkillIndex(skill.id) != i ||
                        i >= CharacterDef.FirstExtraSkill && (string.IsNullOrEmpty(skill.label) || string.IsNullOrEmpty(skill.effect)))
                        throw new InvalidOperationException("Invalid skill for " + c.id + " / " + i);
                }
            }
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
            TideLoadouts.Sanitize(p);
        }
    }
}
