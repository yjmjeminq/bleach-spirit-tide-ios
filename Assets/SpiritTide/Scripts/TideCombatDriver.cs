// Deterministic input-only driver for model and native player regression checks.
// It uses the same movement, resource, cooldown, damage and gate rules as a player.
using UnityEngine;
namespace SpiritTide
{
    public static class TideCombatDriver
    {
        public static void Tick(TideSession s,float dt=1f/60,bool advanceGate=true,bool attackOpportunity=true,bool visitSites=false)
        {
            if(!s.IsFighting)return;
            if(s.RoomSignalPending)
            {
                var delta=s.SignalPosition-new Vector2(s.X,s.Y);
                s.Step(dt,delta.magnitude>.35f?delta.normalized:Vector2.zero,false);
                if(delta.magnitude<1.2f&&s.NearInteraction)s.InteractWorld();
                return;
            }
            if(s.RoomCleared)
            {
                if(s.MainInvestigationPending){var point=s.QuestPoint(0);var delta=point.position-new Vector2(s.X,s.Y);s.Step(dt,delta.magnitude>.5f?delta.normalized:Vector2.zero,false);if(delta.magnitude<1.2f&&s.NearInteraction)s.InteractWorld();return;}
                if(s.EchoAvailable){s.Step(dt,new Vector2(Mathf.Abs(s.X-s.EchoX)>.5f?Mathf.Sign(s.EchoX-s.X):0,Mathf.Abs(s.Y)>.1f?-Mathf.Sign(s.Y):0),false);if(s.NearInteraction)s.InteractWorld();return;}
                if(visitSites&&s.QuestPointCount>0)
                {
                    var delta=s.QuestPoint(0).position-new Vector2(s.X,s.Y);
                    s.Step(dt,delta.magnitude>.4f?delta.normalized:Vector2.zero,false);
                    if(delta.magnitude<.7f)s.InteractWorld();
                    return;
                }
                s.Step(dt,new Vector2(Mathf.Sign(s.GateX-s.X),0),false);if(s.AtGate&&advanceGate)s.EnterGate();return;
            }
            TideEnemy nearest=null;float best=float.MaxValue;
            foreach(var e in s.Enemies)if(e.Alive){float distance=Mathf.Abs(e.X-s.X)+Mathf.Abs(e.Y-s.Y);if(distance<best){best=distance;nearest=e;}}
            if(nearest==null){s.Step(dt,Vector2.zero,false);return;}s.FocusEnemy(nearest.Uid);
            float dx=nearest.X-s.X,dy=nearest.Y-s.Y;Vector2 move=new Vector2(Mathf.Abs(dx)>1.6f?Mathf.Sign(dx):0,Mathf.Abs(dy)>.25f?Mathf.Sign(dy):0);
            bool danger=false,urgent=false;TideEnemy threat=null;
            foreach(var e in s.Enemies)if(e.Timer<.72f&&e.Timer>0&&s.InTelegraph(e))
            {danger=true;if(threat==null||e.Timer<threat.Timer)threat=e;if(e.Timer<.16f)urgent=true;}
            if(danger)
            {
                float escape=threat.Warning.y>=0?-1:1;move.y=escape;
                if(urgent){if(threat.Pattern==2)s.Jump();s.Dodge(new Vector2(0,escape));}
            }
            if(s.EnvironmentWarning){move.y=s.EnvironmentY>=0?-1:1;if(s.Room.hazard=="tide"&&s.EnvironmentTimer<.2f)s.Jump();}
            if(s.WardActive&&s.NearInteraction)s.InteractWorld();
            if(s.Hp<s.MaxHp*.4f)s.UsePotion();
            if(attackOpportunity&&!danger&&s.ActionRemaining<=0&&s.DodgeRemaining<=0)
            {
                int heal=s.Profile.hero==1?1:s.Profile.hero==5?0:-1;
                int healSlot=heal>=0?s.EquippedSlot(heal):-1;
                if(healSlot>=0&&s.SkillAvailable(heal)&&s.Hp<s.MaxHp*.75f&&s.Cooldowns[heal]<=s.Clock&&s.Mp>=s.Hero.Skill(heal).spirit_cost)s.RequestEquippedSkill(healSlot);
                else if(Mathf.Abs(dx)<6&&Mathf.Abs(dy)<1.1f)
                {
                    int slot=-1;
                    for(int i=4;i>=0;i--)if(s.SkillAvailable(i)&&s.Cooldowns[i]<=s.Clock&&s.Mp>=s.Hero.Skill(i).spirit_cost)
                    {
                        int equipped=s.EquippedSlot(i);if(equipped<0)continue;
                        if(s.Profile.hero==1&&i!=2&&i!=4)continue;
                        if(s.Profile.hero==5&&i!=1&&i!=4)continue;
                        if(s.Profile.hero==4&&i==3&&s.CloudTime[0]<=0&&s.CloudTime[1]<=0)continue;
                        if(s.Profile.hero==3&&i==3&&s.Mark<=0)continue;
                        slot=equipped;break;
                    }
                    if(slot>=0)s.RequestEquippedSkill(slot);
                    else if(Mathf.Abs(dx)<2.5f)s.Request(nearest.Guard>0?MoveKind.Heavy:MoveKind.Basic);
                }
            }
            s.Step(dt,move,false);
        }
        public static void Prepare(TideSession s)
        {
            s.ReturnTown();TideGear power=null,guard=null;
            foreach(var g in s.Profile.gear)if(g.slot=="power"&&(power==null||g.Attack>power.Attack))power=g;else if(g.slot=="guard"&&(guard==null||g.Health>guard.Health))guard=g;
            if(power!=null){s.EquipGear(power.uid);s.UpgradeGear(power.uid);}if(guard!=null)s.EquipGear(guard.uid);
            while(s.Profile.potions<3&&s.BuyPotion()){}
            if(s.SkillAvailable(CharacterDef.Awakening)&&s.EquippedSlot(CharacterDef.Awakening)<0)
            {
                int replace=s.Profile.hero==1?0:s.Profile.hero==2?3:s.Profile.hero==3?1:2;
                s.EquipSkill(replace,CharacterDef.Awakening);
            }
        }
    }
}
