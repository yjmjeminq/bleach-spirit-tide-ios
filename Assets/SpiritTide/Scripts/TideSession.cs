using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritTide
{
    public sealed partial class TideSession
    {
        public readonly TideProfile Profile;
        public TideMode Mode=TideMode.Town;
        public bool InBattle,IsTraining,IsReplay,IsPaused,CounterStance,IsCampaign,RoomCleared;
        public int BossIndex,Facing=1,Combo,Revision,AttackSerial,DamageEvents,SaveRevision,RoomIndex;
        public float PresentationClock;
        public int PresentationEpoch;
        public float Clock,Hp=150,Mp=100,Shield,X=-3,Y,Z,JumpSpeed,DodgeRemaining,DodgeElapsed,PerfectRemaining,ReflectRemaining,LastDamage,HitFlash,TotalDamage;
        public float ActionRemaining,ActionDuration,ActionElapsed;
        public MoveKind Action;
        public string Status="",ActionName="",LastReward="";
        public int LastXp,LastGold,LastGearUid,LastActReported;
        public readonly float[] Cooldowns=new float[5];
        public readonly Vector2[] Clouds=new Vector2[2];
        public readonly float[] CloudTime=new float[2];
        public readonly List<TideEnemy> Enemies=new List<TideEnemy>();
        public TideDungeonDef Dungeon {get;private set;}
        private TideEnemy target,burstTarget;
        private readonly TideEnemy emptyTarget=new TideEnemy();
        private float cloudTick,hitAt,lastComboAt=-9,heavyCharge,counterBuff,burstTick,bufferUntil,hurtInvulnerability;
        private bool hitApplied,charging;
        private int burstHits,nextEnemyUid=1;
        private MoveKind? buffered;
        private TideMode pausedFrom;
        private Vector2 dodgeDirection;
        public TideSession(TideProfile profile)
        {
            Profile=profile??throw new ArgumentNullException(nameof(profile));TideCatalog.Sanitize(Profile);Hp=MaxHp;
            Status=Quest==QuestState.Completed?"다섯 균열의 보고 완료 · 후일담과 전투 재현을 이용하세요.":Quest==QuestState.Ready?"이번 액트의 기록을 우라하라에게 보고하세요.":Quest==QuestState.Active?"진행 중 · "+TideCampaign.Dungeon(Profile.campaignAct,Profile.campaignStage).title:"우라하라에게 Act "+Profile.campaignAct+" 조사를 확인하세요.";
        }
        public CharacterDef Hero=>TideCatalog.Characters[Profile.hero];
        public QuestState Quest=>(QuestState)Profile.quest;
        public TideEnemy Target=>target??emptyTarget;
        public int AliveCount {get{int n=0;foreach(var e in Enemies)if(e.Alive)n++;return n;}}
        public int MaxHp=>150+(Profile.level-1)*8+(TideProgress.Gear(Profile,Profile.guardGear)?.Health??0);
        public int AttackBonus=>(Profile.level-1)*2+(TideProgress.Gear(Profile,Profile.powerGear)?.Attack??0);
        public bool CampaignFinished=>(Profile.reportedActs&31)==31;
        public int BattleAct=>IsCampaign?Dungeon.act:Mathf.Clamp(BossIndex/2+1,1,5);
        public bool IsMovingMode=>Mode==TideMode.Town||Mode==TideMode.Combat||Mode==TideMode.Training;
        public bool IsFighting=>!IsPaused&&(Mode==TideMode.Combat||Mode==TideMode.Training);
        public bool IsCharging=>charging;
        public float HeavyProgress=>charging?Mathf.Clamp01(heavyCharge/.8f):0;
        public string LastDamageFoe {get;private set;}
        public string LastDamageCue {get;private set;}
        public string EnemyId=>Target.Def?.id??"a1_mid_shrieker";
        public string EnemyName=>Target.Def?.name??"기록";
        public float EnemyX {get=>Target.X;set=>Target.X=value;}
        public float EnemyY {get=>Target.Y;set=>Target.Y=value;}
        public float EnemyHp {get=>Target.Hp;set=>Target.Hp=value;}
        public float EnemyMaxHp {get=>Target.MaxHp;set=>Target.MaxHp=value;}
        public float EnemyStun {get=>Target.Stun;set=>Target.Stun=value;}
        public float EnemySlow {get=>Target.Slow;set=>Target.Slow=value;}
        public float EnemyTimer=>Target.Timer;
        public float EnemyRecovery=>Target.Recovery;
        public float WarningDuration=>Target.TellDuration;
        public int EnemyPattern=>Target.Pattern;
        public int EnemyAttacks=>Target.Attacks;
        public int Mark {get=>Target.Mark;set=>Target.Mark=value;}
        public bool Telegraph=>Target.Telegraph;
        public Vector2 WarningPosition=>Target.Warning;
        public bool LureActive=>IsFighting&&Target.Def?.id=="a1_boss_grand_fisher"&&Target.Telegraph&&Target.Pattern==1;
        public bool BodyExposed=>IsFighting&&Target.Exposed>0;
        public float GateX=>8.2f;
        public bool AtGate=>RoomCleared&&Mathf.Abs(X-GateX)<1.7f;
        public void FocusEnemy(int uid){foreach(var e in Enemies)if(e.Uid==uid&&e.Alive){if(target!=e){target=e;Revision++;}return;}}
        public void CycleTarget(){int at=Enemies.IndexOf(target);for(int i=1;i<=Enemies.Count;i++){var e=Enemies[(at+i+Enemies.Count)%Enemies.Count];if(e.Alive){FocusEnemy(e.Uid);return;}}}
        private void Changed(string message){Status=message;Revision++;}
        private void PersistentChange(){SaveRevision++;Revision++;}
        public bool SelectHero(int index)
        {
            if(!TideProgress.HeroJoined(Profile,index)||InBattle&&(!IsTraining||Mode!=TideMode.Training))return false;
            Profile.hero=Mathf.Clamp(index,0,5);ClearAction();Shield=0;Mp=100;ReflectRemaining=0;CounterStance=false;
            foreach(var e in Enemies)e.Mark=0;Array.Clear(CloudTime,0,2);Array.Clear(Cooldowns,0,5);Revision++;return true;
        }
        public bool BuyPotion(){if(InBattle||Profile.potions>=5||Profile.gold<80)return false;Profile.gold-=80;Profile.potions++;PersistentChange();Changed("회복 영자 1개 구매 · 80 골드");return true;}
        public bool UsePotion(){if(!IsFighting||IsTraining||Hp>=MaxHp||IsCampaign&&Profile.potions<=0)return false;if(IsCampaign){Profile.potions--;PersistentChange();}Hp=Mathf.Min(MaxHp,Hp+45+Profile.level*2);Changed(IsCampaign?"회복 영자로 체력을 회복했습니다.":"연습용 회복 · 소모품은 유지됩니다.");return true;}
        public void ReturnTown()
        {
            PresentationEpoch++;
            InBattle=IsTraining=IsPaused=IsCampaign=IsFieldVisit=false;RoomCleared=false;Mode=TideMode.Town;Hp=MaxHp;Mp=100;Shield=0;X=-3;Y=Z=JumpSpeed=DodgeRemaining=0;
            Enemies.Clear();target=null;ClearAction();Changed(Quest==QuestState.Completed?"다섯 기록 봉인 · 모두의 이름과 귀환을 확인했습니다. 동료들과 쉬어 가세요.":Quest==QuestState.Ready?"우라하라에게 액트 기록을 보고하세요.":"마을로 귀환 · 정화한 방과 던전 기록은 유지됩니다.");
        }
        public bool OpenMenu(TideMode mode)
        {
            if(InBattle)
            {
                if(mode==TideMode.Journey&&(Mode==TideMode.Defeat||Mode==TideMode.Result)){SuspendInput();Mode=mode;Revision++;return true;}
                if(mode!=TideMode.Pause&&mode!=TideMode.Settings&&mode!=TideMode.Help&&mode!=TideMode.ConfirmRetreat)return false;
                if(!IsFighting&&!IsPaused)return false;if(!IsPaused)pausedFrom=Mode;IsPaused=true;
            }
            SuspendInput();Mode=mode;Revision++;return true;
        }
        public void Pause(){if(!InBattle||IsPaused||!IsFighting)return;pausedFrom=Mode;IsPaused=true;Mode=TideMode.Pause;SuspendInput();Revision++;}
        public void Resume(){if(!InBattle||!IsPaused)return;IsPaused=false;Mode=pausedFrom;SuspendInput();Revision++;}
        public void SuspendInput(){buffered=null;charging=false;heavyCharge=0;}
        public void ClearAction(){ActionRemaining=ActionElapsed=0;SuspendInput();burstHits=0;hitApplied=true;}
        public void Jump(){if(!IsFighting||Z>.02f||DodgeRemaining>0)return;JumpSpeed=6.1f;Changed("도약 · 공중에서 공격하면 공중 연계");}
        public void Dodge(Vector2 direction)
        {
            if(!IsFighting||DodgeRemaining>0||(ActionRemaining>.16f&&(!hitApplied||Action==MoveKind.Ultimate)))return;
            ClearAction();DodgeRemaining=.32f;DodgeElapsed=0;if(direction.sqrMagnitude<.04f)direction=new Vector2(Facing,0);dodgeDirection=direction.normalized;Changed("회피");
        }
        public void BeginHeavy(){if(!IsFighting||ActionRemaining>0||DodgeRemaining>0||RoomCleared)return;charging=true;heavyCharge=0;ActionName="강공격 준비";Revision++;}
        public void ReleaseHeavy(){if(!charging)return;if(!IsFighting){charging=false;heavyCharge=0;return;}float charge=heavyCharge;charging=false;if(StartAction(MoveKind.Heavy))counterBuff=Mathf.Clamp01(charge/.8f)*12;}
        public bool Request(MoveKind kind)
        {
            if(!IsFighting||charging||RoomCleared)return false;
            int slot=(int)kind-(int)MoveKind.Skill0;
            if(slot>=0&&!SkillAvailable(slot))
            {
                if(slot<=4)Changed(Hero.Skill(slot).name+" · "+TideProgress.SkillRequirement(slot));
                return false;
            }
            if(kind==MoveKind.Basic&&PerfectRemaining>0)kind=MoveKind.Counter;else if(kind==MoveKind.Basic&&Z>.15f)kind=MoveKind.Air;
            if(ActionRemaining>0&&kind>=MoveKind.Skill0&&Action<=MoveKind.Air&&hitApplied&&DamageEvents>actionDamageStart)
            {
                if(!IsTraining&&(Cooldowns[slot]>Clock||Mp<Hero.Skill(slot).spirit_cost))return false;
                ClearAction();if(!StartAction(kind))return false;CancelEvents++;QuestCombatEvent("cancel");return true;
            }
            if(ActionRemaining>0||DodgeRemaining>.06f){buffered=kind;bufferUntil=Clock+.2f;return false;}return StartAction(kind);
        }
        private bool StartAction(MoveKind kind)
        {
            if(!IsFighting||RoomCleared||AliveCount==0)return false;
            int slot=(int)kind-(int)MoveKind.Skill0;
            if(slot>=0)
            {
                if(!SkillAvailable(slot))return false;
                SkillDef skill=Hero.Skill(slot);if(!IsTraining&&(Cooldowns[slot]>Clock||Mp<skill.spirit_cost)){Changed(Cooldowns[slot]>Clock?"재사용 시간 중입니다.":"영압이 부족합니다.");return false;}
                if(!IsTraining){Mp-=skill.spirit_cost;Cooldowns[slot]=Clock+skill.cooldown_seconds;}
            }
            ChooseTarget();if(Profile.autoFace&&target!=null)Facing=target.X>=X?1:-1;
            Action=kind;ActionElapsed=0;hitApplied=false;counterBuff=0;actionDamageStart=DamageEvents;
            float speed=Profile.hero==2||Profile.hero==3?.28f:Profile.hero==4?.43f:.35f;
            ActionDuration=kind==MoveKind.Heavy?.58f:kind==MoveKind.Ultimate?1.05f:slot>=0?.48f:speed;ActionRemaining=ActionDuration;hitAt=ActionDuration*.4f;
            if(kind==MoveKind.Basic){Combo=Clock-lastComboAt<.8f?(Combo+1)%3:0;lastComboAt=Clock;}
            ActionName=slot>=0?Hero.Skill(slot).name:kind==MoveKind.Heavy?"집중 강공격":kind==MoveKind.Launcher?"띄우기":kind==MoveKind.Air?"공중 연계":kind==MoveKind.Counter?"정밀 회피 반격":"기본 공격 "+(Combo+1);
            if(kind==MoveKind.Counter)PerfectRemaining=0;AttackSerial++;Revision++;return true;
        }
        public bool SkillAvailable(int slot)=>slot>=0&&slot<=4&&(IsTraining||TideProgress.SkillLearned(Profile,slot));
        public void Step(float dt,Vector2 movement,bool heldAttack)
        {
            dt=Mathf.Clamp(dt,0,.05f);if(Mode==TideMode.Town){PresentationClock+=dt;Move(dt,movement,3.4f);return;}if(!IsFighting)return;
            if(HitStopRemaining>0){HitStopRemaining=Mathf.Max(0,HitStopRemaining-dt);return;}
            PresentationClock+=dt;StepFeel(dt);Clock+=dt;Mp=Mathf.Min(100,Mp+dt*6);HitFlash=Mathf.Max(0,HitFlash-dt);PerfectRemaining=Mathf.Max(0,PerfectRemaining-dt);ReflectRemaining=Mathf.Max(0,ReflectRemaining-dt);hurtInvulnerability=Mathf.Max(0,hurtInvulnerability-dt);
            foreach(var e in Enemies){e.Stun=Mathf.Max(0,e.Stun-dt);e.Slow=Mathf.Max(0,e.Slow-dt);e.Flash=Mathf.Max(0,e.Flash-dt);e.Exposed=Mathf.Max(0,e.Exposed-dt);}
            for(int i=0;i<2;i++)CloudTime[i]=Mathf.Max(0,CloudTime[i]-dt);
            cloudTick-=dt;if(cloudTick<=0){cloudTick+=1;for(int i=0;i<2;i++)if(CloudTime[i]>0)foreach(var e in Enemies)if(e.Alive&&Vector2.Distance(Clouds[i],e.Position)<2)Damage(e,8,true,true);}
            if(!IsFighting)return;
            if(Z>0||JumpSpeed>0){Z=Mathf.Max(0,Z+JumpSpeed*dt);JumpSpeed-=16*dt;if(Z<=0)JumpSpeed=0;}
            if(DodgeRemaining>0){DodgeRemaining=Mathf.Max(0,DodgeRemaining-dt);DodgeElapsed+=dt;Move(dt,dodgeDirection,9);}
            else if(!charging){float pace=Profile.hero==2?4.5f:3.6f;if(ActionRemaining<=0)Move(dt,movement,pace);else if((Action==MoveKind.Basic||Action==MoveKind.Air||Action==MoveKind.Launcher)&&ActionElapsed>=hitAt)Move(dt,movement,pace*.45f);}
            if(charging){heavyCharge+=dt;if(heavyCharge>=.8f)ReleaseHeavy();}
            if(ActionRemaining>0){ActionElapsed+=dt;ActionRemaining=Mathf.Max(0,ActionRemaining-dt);if(!hitApplied&&ActionElapsed>=hitAt){hitApplied=true;ResolveAction();}}
            if(!IsFighting)return;
            if(burstHits>0){burstTick-=dt;if(burstTick<=0){burstTick=.09f;burstHits--;if(burstTarget!=null&&burstTarget.Alive)Damage(burstTarget,8,true);}}
            if(!IsFighting)return;
            if(ActionRemaining<=0&&DodgeRemaining<=0&&!charging)
            {
                if(buffered.HasValue&&bufferUntil>=Clock){MoveKind value=buffered.Value;buffered=null;StartAction(value);}
                else {buffered=null;if(heldAttack&&Profile.holdAttack)Request(MoveKind.Basic);}
            }
            if((!IsTraining||TrainingAttacks)&&!RoomCleared)foreach(var e in Enemies)if(e.Alive)StepEnemy(e,dt);
            StepRoomWorld(dt);ChooseTarget();
        }
        private void Move(float dt,Vector2 movement,float speed)
        {
            movement=Vector2.ClampMagnitude(movement,1);X=Mathf.Clamp(X+movement.x*speed*dt,-6,IsCampaign?10.5f:6);Y=Mathf.Clamp(Y+movement.y*speed*dt*.65f,-1.5f,1.5f);if(Mathf.Abs(movement.x)>.1f)Facing=movement.x>0?1:-1;
        }
        private bool InRange(TideEnemy e,float reach,float depth=.9f,bool allDirections=false){return e.Alive&&(allDirections||(e.X-X)*Facing>=-.3f)&&Mathf.Abs(e.X-X)<=reach&&Mathf.Abs(e.Y-Y)<=depth;}
        private void Hit(float amount,float reach,float depth,bool skill=false,Action<TideEnemy> extra=null,bool allDirections=false)
        {foreach(var e in Enemies)if(InRange(e,reach,depth,allDirections)){Damage(e,amount,skill);extra?.Invoke(e);}}
        private void ResolveAction()
        {
            int slot=(int)Action-(int)MoveKind.Skill0;if(slot>=0){ResolveSkill(slot);return;}
            float reach=Profile.hero==1?7:Profile.hero==4?3.8f:2.6f;
            float damage=Action==MoveKind.Heavy?32+counterBuff:Action==MoveKind.Counter?29:Action==MoveKind.Air?18:Action==MoveKind.Launcher?16:12+Combo*3;
            Hit(damage,reach,.9f,false,e=>{if(Action==MoveKind.Launcher)e.Stun=Mathf.Max(e.Stun,.45f);if(Profile.hero==0&&Combo==2)e.Slow=1.1f;});
        }
        private void ResolveSkill(int slot)
        {
            if(slot==4){Hit((Profile.hero==2?100:85)*((Profile.reportedActs&4)!=0?1.2f:1),8,3,true,e=>e.Stun=Mathf.Max(e.Stun,1),true);if(Profile.hero==1)Shield=Mathf.Max(Shield,45);if(Profile.hero==5)Hp=Mathf.Min(MaxHp,Hp+22+Profile.level);return;}
            switch(Profile.hero)
            {
                case 0:if(slot==2)X=Mathf.Clamp(X+Facing*2.3f,-6,IsCampaign?10.5f:6);Hit(slot==3?43:29,slot==1?6:3.8f,1.2f,true,e=>{e.Slow=slot==1?3:1.2f;e.Stun=.35f;});break;
                case 1:if(slot==0)Shield=Mathf.Min(90,Shield+35+Profile.level);if(slot==1)Hp=Mathf.Min(MaxHp,Hp+26+Profile.level);if(slot==2&&Target.Alive)Damage(Target,34,true);if(slot==3){ReflectRemaining=1.7f;Shield=Mathf.Max(Shield,15);}break;
                case 2:if(slot==0||slot==2)X=Mathf.Clamp(EnemyX-Facing*1.1f,-6,IsCampaign?10.5f:6);if(slot==3){DodgeRemaining=.32f;DodgeElapsed=.08f;dodgeDirection=new Vector2(-Facing,0);PerfectRemaining=.5f;}else{Hit(slot==2?35:30,4,1.3f,true);if(slot==2){burstTarget=Target;burstHits=2;burstTick=.09f;}}break;
                case 3:if(slot==1)X=Mathf.Clamp(EnemyX-Facing*1.2f,-6,IsCampaign?10.5f:6);if(slot==0)Hit(22,3.8f,1.2f,true,e=>e.Mark=1);if(slot==1)Hit(15,3.8f,1.2f,true);if(slot==2&&InRange(Target,3.8f,1.2f)){burstTarget=Target;burstHits=5;burstTick=.04f;}if(slot==3)foreach(var e in Enemies)if(InRange(e,3.8f,1.2f)){Damage(e,e.Mark>0?70:27,true);e.Mark=0;}break;
                case 4:if(slot==0)Hit(28,5,1.2f,true);if(slot==1){int i=CloudTime[0]<CloudTime[1]?0:1;Clouds[i]=new Vector2(X+Facing*2,Y);CloudTime[i]=5;}if(slot==2){foreach(var e in Enemies)if(e.Alive){e.X=Mathf.MoveTowards(e.X,X+Facing*1.8f,1);e.Stun=.4f;}Hit(14,5,2,true,null,true);}if(slot==3){bool cloud=CloudTime[0]>0||CloudTime[1]>0;Hit(cloud?61:22,7,3,true,null,true);Array.Clear(CloudTime,0,2);}break;
                case 5:if(slot==0)Hp=Mathf.Min(MaxHp,Hp+22+Profile.level);if(slot==1)Hit(CounterStance?39:29,3.8f,1.1f,true);if(slot==2)ReflectRemaining=1.25f;if(slot==3)CounterStance=!CounterStance;break;
            }
        }
        private void Damage(TideEnemy e,float amount,bool skill,bool periodic=false)
        {
            if(!IsFighting||e==null||!e.Alive)return;
            amount+=AttackBonus;if(e.Lift>.2f&&(Action==MoveKind.Air||skill))amount*=1.15f;if(e.Exposed>0)amount*=1.25f;
            if(e.Def.brain=="guard"&&e.Guard>0&&Action!=MoveKind.Heavy&&Action!=MoveKind.Ultimate)amount*=.45f;
            if(Action==MoveKind.Heavy||Action==MoveKind.Ultimate)e.Guard=Mathf.Max(0,e.Guard-1);
            LastDamage=amount;HitFlash=.12f;DamageEvents++;TotalDamage+=amount;e.Flash=.12f;e.LastDamage=amount;e.DamageSerial++;
            if(!periodic)RegisterImpact(e);
            if(IsTraining)return;
            e.Hp=Mathf.Max(0,e.Hp-amount);
            if(e.Def.id=="a5_mid_haschwalth"&&e.Hp>0&&!e.Telegraph&&e.Recovery<=0){e.ReturnHits++;if(e.ReturnHits>=3){e.ReturnHits=0;StartTell(e,1,.95f);}}
            if(e.Hp>0)return;
            e.Telegraph=false;Revision++;
            if(AliveCount==0)FinishRoom();
        }
        private void TakeDamage(TideEnemy source,float amount)
        {
            if(!IsFighting||hurtInvulnerability>0)return;
            if(DodgeRemaining>0&&DodgeElapsed>=.05f&&DodgeElapsed<=.26f){PerfectRemaining=.4f;PerfectEvents++;QuestCombatEvent("perfect");Changed("정밀 회피 · 지금 공격하면 반격");return;}
            if(ReflectRemaining>0&&source!=null){ReflectRemaining=0;Damage(source,Profile.hero==5?40:32,true);if(IsFighting)Changed("반격 성공");return;}
            float absorbed=Mathf.Min(Shield,amount);Shield-=absorbed;amount-=absorbed;Hp=Mathf.Max(IsTraining?1:0,Hp-amount);hurtInvulnerability=.35f;
            if(amount>0){LastDamageFoe=source?.Def?.id??"";LastDamageCue=source?.Cue??"바닥의 환경 예고를 벗어나거나 영압 장치를 해제하세요.";DungeonDamageTaken+=amount;HurtSerial++;ChainHits=0;ComboRemaining=0;}
            if(amount>0)Changed("피격 · 겹친 예고를 피해 빈틈을 노리세요.");
            if(Hp<=0){Mode=TideMode.Defeat;foreach(var e in Enemies)e.Telegraph=false;ClearAction();Changed("재도전은 마지막으로 정화한 방 뒤에서 이어집니다.");}
        }
    }
}
