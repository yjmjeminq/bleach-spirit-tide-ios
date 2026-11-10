using System;
using System.IO;
using UnityEngine;

namespace SpiritTide.EditorTools
{
    public static class TideLoadoutValidation
    {
        public static void Run(Action<string,bool> check)
        {
            string path=Path.GetFullPath("BuildLogs/LoadoutChecks-"+Guid.NewGuid().ToString("N")+"/profile.json");
            var profile=new TideProfile{campaignSchema=2,level=4,recruitedHeroes=TideProgress.AllHeroes};
            var session=new TideSession(profile);
            check("new and legacy defaults populate four distinct skill ids",HasDefaultSlots(session)&&
                HasDefaultSlots(new TideSession(TideStorage.Load("Assets/SpiritTide/Editor/Fixtures/profile-v1-0.1.0.json",out _))));
            check("a learned technique equips and duplicate selection swaps slots",!session.EquipSkill(0,6)&&
                session.EquipSkill(1,5)&&session.EquipSkill(0,5)&&session.EquippedSkill(0)==5&&session.EquippedSkill(1)==0&&
                !session.EquipSkill(0,5)&&!session.EquipSkill(4,5));
            check("an earlier save keeps classic unlocks but observes expansion requirements",
                TideProgress.SkillLearned(new TideProfile{classicSkills=true,level=1},4)&&
                !TideProgress.SkillLearned(new TideProfile{classicSkills=true,level=1},5));
            int[] levels={4,7,12,18};
            for(int i=0;i<levels.Length;i++)
                check("expansion unlocks at the stated level "+levels[i],
                    !TideProgress.SkillLearned(new TideProfile{level=levels[i]-1},CharacterDef.FirstExtraSkill+i)&&
                    TideProgress.SkillLearned(new TideProfile{level=levels[i]},CharacterDef.FirstExtraSkill+i));
            check("each hero has a separate four-slot loadout",session.SelectHero(1)&&HasDefaultSlots(session)&&session.EquipSkill(2,5)&&
                session.SelectHero(0)&&session.EquippedSkill(0)==5&&session.EquippedSkill(2)==2);
            bool saved=TideStorage.Save(path,profile,out _);
            var loaded=new TideSession(TideStorage.Load(path,out _));
            check("skill identities survive checksum save reload for separate heroes",saved&&loaded.EquippedSkill(0)==5&&
                loaded.SelectHero(1)&&loaded.EquippedSkill(2)==5&&loaded.EquippedSkill(0)==0);

            profile.skillLoadouts[0].skills=new[]{"unknown","rk_s5","rk_s5","rk_ult","rk_s8"};
            TideCatalog.Sanitize(profile);
            check("invalid and duplicate skill ids repair without moving valid slots",session.EquippedSkill(1)==5&&
                session.EquippedSlot(5)==1&&session.EquippedSlot(4)==-1&&session.EquippedSlot(8)==-1&&profile.skillLoadouts[0].skills.Length==4);
            session.EquipSkill(0,5);
            session.BeginBattle(false);
            session.X=0;session.EnemyX=1;
            check("equipped cast pays cost and sets cooldown by skill identity",session.RequestEquippedSkill(0)&&
                session.Action==MoveKind.Skill5&&session.Mp==100-session.Hero.Skill(5).spirit_cost&&
                session.Cooldowns[5]>session.Clock&&session.Cooldowns[0]==0);
            Advance(session,.65f);
            check("the equipped skill applies its real effect and respects cooldown",session.EnemySlow>1&&
                !session.RequestEquippedSkill(0)&&session.DamageEvents==1);
            session.Pause();
            check("campaign pause cannot change the active loadout",!session.OpenMenu(TideMode.Loadout)&&!session.EquipSkill(0,0));
            session.ReturnTown();session.BeginBattle(true);int revision=session.SaveRevision;
            session.Pause();
            check("training can preview an unlearned awakening without changing the save",session.OpenMenu(TideMode.Loadout)&&
                session.EquipSkill(0,4)&&session.EquippedSkill(0)==4&&session.SaveRevision==revision&&
                profile.skillLoadouts[0].skills[0]=="rk_s5");
            session.Resume();
            check("the preview slot casts through the same four inputs",session.RequestEquippedSkill(0)&&session.Action==MoveKind.Ultimate);
            check("switching training partners keeps each preview in that session",session.SelectHero(1)&&
                session.EquippedSkill(2)==5&&session.SelectHero(0)&&session.EquippedSkill(0)==4);
            session.ReturnTown();
            check("leaving training restores the saved loadout",session.EquippedSkill(0)==5);

            var frost=Practice(0);
            Cast(frost,MoveKind.Skill5);Cast(frost,MoveKind.Skill8);
            check("ice finisher consumes real slow and boosts damage",frost.EnemySlow==0&&frost.LastDamage==66+frost.AttackBonus);
            var skating=Practice(0);skating.EnemyX=4;
            Cast(skating,MoveKind.Skill6);
            check("ice skate crosses into striking range",skating.X>1.7f&&skating.EnemySlow>0&&skating.DamageEvents==1);
            Cast(frost,MoveKind.Skill7);
            check("ice wall applies its shield and short-range damage",frost.Shield>26&&frost.DamageEvents==3);
            var shield=Practice(1);
            Cast(shield,MoveKind.Skill5);
            check("nested barrier absorbs a larger hit",shield.Shield==48+shield.Profile.level&&shield.DamageEvents==1);
            shield.Shield=0;
            shield.Hp=40;Cast(shield,MoveKind.Skill8);
            check("Orihime gains the low-health healing bonus and a shield",shield.Hp==90+shield.Profile.level&&shield.Shield==12);
            shield.Mp=30;Cast(shield,MoveKind.Skill7);
            TriggerCounter(shield);
            check("Orihime earns spirit on a successful ward counter",shield.Mp>54&&shield.ReflectRemaining==0&&shield.DamageEvents==2);
            var petals=Practice(1);petals.EnemyX=5;
            petals.Enemies.Add(new TideEnemy{Def=TideCampaign.Foe("a1_e07"),X=6,Y=1.35f,Hp=52});
            Cast(petals,MoveKind.Skill6);
            check("petal fan reaches multiple targets across lanes",petals.DamageEvents==2);
            var thunder=Practice(2);
            Cast(thunder,MoveKind.Skill6);
            check("Yoruichi's lightning chain resolves four follow-up hits",thunder.DamageEvents==5);
            var kick=Practice(2);kick.EnemyX=4.5f;
            Cast(kick,MoveKind.Skill5);
            check("lightning kick moves into range before it hits",kick.X>2.8f&&kick.DamageEvents==1);
            kick.X=kick.EnemyX-1;
            Cast(kick,MoveKind.Skill7,.42f);
            check("cat counter crosses behind a nearby foe",kick.X>kick.EnemyX&&kick.ReflectRemaining>0);
            Cast(kick,MoveKind.Skill8);
            check("thunderfall hits enemies behind the fighter",kick.DamageEvents==2&&kick.EnemyStun>0);
            var mark=Practice(3);
            Cast(mark,MoveKind.Skill5);Cast(mark,MoveKind.Skill8);
            check("Soi Fon's mark fuels four hits and survives the flurry",mark.Mark==1&&mark.DamageEvents==6);
            var pierce=Practice(3);pierce.Target.Guard=2;
            Cast(pierce,MoveKind.Skill6);
            check("guard-breaking thrust removes one guard layer",pierce.Target.Guard==1&&pierce.DamageEvents==1);
            Cast(pierce,MoveKind.Skill7);
            check("retreating thrust hits and then retreats",pierce.X<-1.8f&&pierce.DamageEvents==2);
            var ashField=Practice(4);ashField.EnemyX=2;
            Cast(ashField,MoveKind.Skill5,.6f);
            check("ranged ash deploys near the selected target",ashField.CloudTime[1]>0&&ashField.Clouds[1].x==2);
            ashField.EnemyX=4;
            Cast(ashField,MoveKind.Skill6);
            check("ash pull directs foes to an active cloud",ashField.EnemyX<3.5f&&ashField.EnemySlow>0);
            Cast(ashField,MoveKind.Skill7);
            check("ash ward gains stronger shielding near a cloud",ashField.Shield==39);
            var ash=Practice(4);
            ash.Clouds[0]=ash.Clouds[1]=ash.Target.Position;
            ash.CloudTime[0]=ash.CloudTime[1]=5;
            Cast(ash,MoveKind.Skill8,.8f);
            check("overlapping ash clouds each deal a separate hit and expire",ash.DamageEvents==2&&ash.CloudTime[0]==0&&ash.CloudTime[1]==0);
            var stance=Practice(5);
            stance.Hp=40;Cast(stance,MoveKind.Skill5);
            check("healing pulse also shields in recovery stance",stance.Hp==55+stance.Profile.level&&stance.Shield==19);
            Cast(stance,MoveKind.Skill8);
            check("lotus slash heals while in recovery stance",stance.Hp==85+2*stance.Profile.level&&stance.DamageEvents==1);
            Cast(stance,MoveKind.Skill7);
            TriggerCounter(stance);
            check("Unohana's successful counter changes the active stance",stance.CounterStance&&stance.DamageEvents==2);
            stance.Target.Guard=2;
            Cast(stance,MoveKind.Skill6);
            check("Unohana's blade stance improves damage and removes guard",stance.Target.Guard==1&&stance.LastDamage==47+stance.AttackBonus);
        }

        private static bool HasDefaultSlots(TideSession session)
        {
            for(int slot=0;slot<TideLoadouts.SlotCount;slot++)if(session.EquippedSkill(slot)!=slot)return false;
            return true;
        }

        private static TideSession Practice(int hero)
        {
            var session=new TideSession(new TideProfile{campaignSchema=2,recruitedHeroes=TideProgress.AllHeroes,hero=hero});
            session.BeginBattle(true);session.X=0;session.EnemyX=1;
            return session;
        }

        private static void Cast(TideSession session,MoveKind move,float seconds=.9f)
        {
            if(!session.Request(move))throw new InvalidOperationException("Practice cast did not start: "+move);
            Advance(session,seconds);
        }

        private static void TriggerCounter(TideSession session)
        {
            session.ToggleTrainingAttacks();
            session.Target.Pattern=1;session.Target.Telegraph=true;
            session.Target.Warning=new Vector2(session.X,session.Y);session.Target.Timer=.01f;
            Advance(session,.05f);
        }

        private static void Advance(TideSession session,float time)
        {
            for(int frame=0;frame<Mathf.CeilToInt(time*60);frame++)session.Step(1f/60,Vector2.zero,false);
        }
    }
}
