using System;

namespace SpiritTide
{
    [Serializable] public sealed class TideEquippedSkills
    {
        public string[] skills;
    }

    public static class TideLoadouts
    {
        public const int SlotCount = 4;

        public static void Sanitize(TideProfile profile)
        {
            var heroes = TideCatalog.Characters;
            if (profile.skillLoadouts == null) profile.skillLoadouts = new TideEquippedSkills[heroes.Length];
            else if (profile.skillLoadouts.Length != heroes.Length) Array.Resize(ref profile.skillLoadouts, heroes.Length);

            for (int hero = 0; hero < heroes.Length; hero++)
            {
                var row = profile.skillLoadouts[hero] ?? (profile.skillLoadouts[hero] = new TideEquippedSkills());
                if (row.skills == null) row.skills = new string[SlotCount];
                else if (row.skills.Length != SlotCount) Array.Resize(ref row.skills, SlotCount);

                // Keep valid selections in their original slots before filling any gaps.
                for (int slot = 0; slot < SlotCount; slot++)
                {
                    string id = row.skills[slot];
                    int index = string.IsNullOrEmpty(id) ? -1 : heroes[hero].SkillIndex(id);
                    if (index < 0 || index >= SlotCount && !TideProgress.SkillLearned(profile, index)
                        || Array.IndexOf(row.skills, id, 0, slot) >= 0)
                        row.skills[slot] = null;
                }
                for (int slot = 0; slot < SlotCount; slot++)
                {
                    if (row.skills[slot] != null) continue;
                    string preferred = heroes[hero].Skill(slot).id;
                    if (Array.IndexOf(row.skills, preferred) < 0) row.skills[slot] = preferred;
                    else
                    {
                        for (int index = 0; index < SlotCount; index++)
                        {
                            string fallback = heroes[hero].Skill(index).id;
                            if (Array.IndexOf(row.skills, fallback) >= 0) continue;
                            row.skills[slot] = fallback;
                            break;
                        }
                    }
                }
            }
        }
    }

    public sealed partial class TideSession
    {
        private string[][] trainingLoadouts;
        public bool CanEditSkills => !InBattle || IsTraining && IsPaused;

        private string[] EquippedSkills => (IsTraining ? trainingLoadouts?[Profile.hero] : null)
            ?? Profile.skillLoadouts[Profile.hero].skills;

        public int EquippedSkill(int slot) => Hero.SkillIndex(EquippedSkills[slot]);
        public int EquippedSlot(int skill) => Array.IndexOf(EquippedSkills, Hero.Skill(skill).id);

        public bool EquipSkill(int slot, int skill)
        {
            if (!CanEditSkills || slot < 0 || slot >= TideLoadouts.SlotCount || !SkillAvailable(skill)) return false;
            if (IsTraining)
            {
                trainingLoadouts ??= new string[TideCatalog.Characters.Length][];
                trainingLoadouts[Profile.hero] ??= (string[])EquippedSkills.Clone();
            }
            string[] skills = EquippedSkills;
            string id = Hero.Skill(skill).id;
            int previousSlot = Array.IndexOf(skills, id);
            if (previousSlot == slot) return false;
            if (previousSlot >= 0) skills[previousSlot] = skills[slot];
            skills[slot] = id;
            if (!IsTraining) PersistentChange();
            Changed(Hero.Skill(skill).name + " · " + (slot + 1) + "번 슬롯 장착");
            return true;
        }

        public bool RequestEquippedSkill(int slot)
        {
            if (slot < 0 || slot >= TideLoadouts.SlotCount) return false;
            return Request((MoveKind)((int)MoveKind.Skill0 + EquippedSkill(slot)));
        }
    }
}
