using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritTide
{
    public sealed partial class TideSession
    {
        public bool AcceptQuest()
        {
            if(Quest!=QuestState.Available||InBattle||CampaignFinished)return false;
            Profile.quest=(int)QuestState.Active;Profile.campaignStage=Mathf.Clamp(Profile.campaignStage,1,6);PersistentChange();Changed("Act "+Profile.campaignAct+" 조사 수락 · 첫 통로를 확인하세요.");return true;
        }
        public bool ClaimReward()
        {
            int bit=1<<(Profile.campaignAct-1);
            if(Quest!=QuestState.Ready||InBattle||(Profile.reportedActs&bit)!=0||!TideQuests.ChapterEvidenceReady(Profile,Profile.campaignAct))return false;
            LastActReported=Profile.campaignAct;int reward=250+(Profile.campaignAct-1)*100;
            Profile.gold=Mathf.Min(1000000,Profile.gold+reward);Profile.reportedActs|=bit;
            if(Profile.campaignAct==5){Profile.quest=(int)QuestState.Completed;Profile.claimed=true;}
            else {Profile.campaignAct++;Profile.campaignStage=1;Profile.roomCheckpoint=0;Profile.quest=(int)QuestState.Available;Profile.claimed=false;}
            TideCatalog.Sanitize(Profile);PersistentChange();Changed(LastActReported==3&&!Profile.classicSkills
                ?"각성기 습득 · "+Hero.ultimate.name+" · 메뉴에서 기술 장착":"Act "+LastActReported+" 보고 완료 · "+reward+" 골드");return true;
        }
        public bool BeginBossPractice(int index)
        {
            if(!TideJourney.BossKnown(Profile,index)||InBattle&&Mode!=TideMode.Defeat&&Mode!=TideMode.Result&&Mode!=TideMode.Journey)return false;
            return BeginBattle(false,index);
        }
        public bool BeginBattle(bool training,int bossOverride=0)
        {
            IsCampaign=IsFieldVisit=false;Dungeon=null;BossIndex=Mathf.Clamp(bossOverride<0?0:bossOverride,0,9);
            StartBattleState(training);string id=TideCatalog.Bosses[BossIndex].id;target=Spawn(id,0,1,0);
            Changed(training?"수련 · 자원과 이야기 진행에 영향을 주지 않습니다.":"보스 기록 재현 · 최초 조사 보상은 바뀌지 않습니다.");return true;
        }
        public bool IsFieldVisit {get;private set;}
        public bool BeginDungeon(int act=-1,int number=-1,bool replay=false,int visitRoom=-1)
        {
            if(InBattle&&Mode!=TideMode.Result&&Mode!=TideMode.Defeat)return false;
            if(visitRoom>=0&&(!replay||!TideCampaign.CanVisitRoom(Profile,act,number,visitRoom)))return false;
            int a=act<0?Profile.campaignAct:Mathf.Clamp(act,1,5),n=number<0?Profile.campaignStage:Mathf.Clamp(number,1,6);
            if(replay&&visitRoom<0)
            {
                int idx=(a-1)*6+n-1;if((Profile.clearedDungeons&(1<<idx))==0){Changed("아직 정화하지 않은 던전입니다.");return false;}
            }
            if(!replay&&(Quest!=QuestState.Active||a!=Profile.campaignAct||n!=Profile.campaignStage)){Changed("현재 조사 목표를 먼저 확인해 주세요.");return false;}
            IsCampaign=true;IsFieldVisit=visitRoom>=0;Dungeon=TideCampaign.Dungeon(a,n);
            BossIndex=(a-1)*2+(n>=4?1:0);StartBattleState(false);IsReplay=replay;
            RoomIndex=IsFieldVisit?visitRoom:replay?0:Mathf.Clamp(Profile.roomCheckpoint,0,Dungeon.rooms.Length-1);
            LoadRoom();return true;
        }
        private void StartBattleState(bool training)
        {
            PresentationEpoch++;LastDamageFoe=LastDamageCue=null;
            trainingLoadouts=null;
            InBattle=true;IsTraining=training;IsReplay=!IsCampaign&&!training;IsPaused=false;RoomCleared=false;Mode=training?TideMode.Training:TideMode.Combat;
            TrainingAttacks=training&&HasPerfectTrainingObjective();
            Hp=MaxHp;Mp=100;Shield=0;ResetFightFeel();WardActive=EnvironmentWarning=false;X=-3;Y=Z=JumpSpeed=0;Facing=1;hurtInvulnerability=0;PerfectRemaining=ReflectRemaining=DodgeRemaining=0;CounterStance=false;
            Enemies.Clear();target=null;cloudTick=1;Array.Clear(CloudTime,0,2);Array.Clear(Cooldowns,0,Cooldowns.Length);ClearAction();LastXp=LastGold=LastGearUid=0;LastReward="";
        }
        private void LoadRoom()
        {
            PresentationEpoch++;
            Enemies.Clear();target=null;RoomCleared=false;X=-3;Y=Z=JumpSpeed=0;Facing=1;ClearAction();Array.Clear(CloudTime,0,2);
            SetupRoomWorld();SpawnRoomFoes();
            ChooseTarget();Changed(Dungeon.title+" · "+(RoomIndex+1)+" / "+Dungeon.rooms.Length+"방 · "+Room.title);
        }
        public bool EnterGate()
        {
            if(!IsFighting||!IsCampaign||IsFieldVisit||!GateReady||!AtGate||RoomIndex>=Dungeon.rooms.Length-1)return false;
            RoomIndex++;LoadRoom();return true;
        }
        private void FinishRoom()
        {
            if(ReinforcementsPending)
            {
                EnvironmentWarning=false;
                Changed(RoomSignalPending?"길을 막은 적을 몰아냈습니다. 표지에 다가가 ‘"+Room.signal_title+"’를 선택하세요.":"증원 신호 · 다음 진입로를 확인하세요.");
                return;
            }
            EnvironmentWarning=false;
            foreach(var e in Enemies)e.Telegraph=false;
            bool followThrough=IsCampaign&&(IsFieldVisit||RoomIndex<Dungeon.rooms.Length-1)
                &&ActionRemaining>0&&contactSerial==AttackSerial&&contactClock==PresentationClock;
            int followUps=followThrough&&Action==MoveKind.Basic?queuedBasics:0;
            ClearAction();
            if(followThrough)
            {
                clearStrikeUntil=PresentationClock+ClearStrikeDuration;
                queuedBasics=followUps;
                if(followUps>0)basicContinueUntil=Clock+ClearStrikeDuration+BasicLinkGrace;
            }
            if(!IsCampaign){Mode=TideMode.Result;Changed("보스 전투 재현 완료 · 조사 진행과 보상은 유지됩니다.");return;}
            RoomCleared=true;
            QuestFieldEvent("clear");
            if(IsFieldVisit){Changed("재방문한 방을 정리했습니다. 남은 부탁을 확인하고 거점으로 돌아갈 수 있습니다.");return;}
            if(RoomIndex<Dungeon.rooms.Length-1)
            {
                if(!IsReplay&&!MainInvestigationPending){Profile.roomCheckpoint=Mathf.Max(Profile.roomCheckpoint,RoomIndex+1);PersistentChange();}
                Hp=Mathf.Min(MaxHp,Hp+Mathf.CeilToInt(MaxHp*.12f));Changed(MainPoint?.goal??"통로 정화 · 체력을 조금 회복했습니다. 오른쪽 출구로 이동하세요.");return;
            }
            AwardDungeon();Mode=TideMode.Result;Changed(IsReplay?"던전 재현 완료":"던전 정화 완료 · 전리품과 경험치를 확인하세요.");
        }
        private void AwardDungeon()
        {
            int bit=1<<Dungeon.index;bool first=(Profile.clearedDungeons&bit)==0;
            LastXp=first?Dungeon.xp:Mathf.Max(20,Dungeon.xp/5);LastGold=first?Dungeon.gold:Mathf.Max(15,Dungeon.gold/4);int before=Profile.level;
            TideProgress.AddXp(Profile,LastXp);Profile.gold=Mathf.Min(1000000,Profile.gold+LastGold);
            if(first)
            {
                Profile.clearedDungeons|=bit;Profile.crystals=Mathf.Min(99999,Profile.crystals+3+Dungeon.act);
                int uid=1000+Dungeon.index;
                if(TideProgress.Gear(Profile,uid)==null)
                {
                    var list=new List<TideGear>(Profile.gear);var item=new TideGear{uid=uid,tier=Dungeon.act,rarity=Dungeon.number%3==0?2:Dungeon.number%2,slot=Dungeon.number%2==0?"guard":"power",name=Dungeon.title+"의 "+(Dungeon.number%2==0?"수호 영장":"힘 영장")};
                    if(list.Count<48){list.Add(item);Profile.gear=list.ToArray();LastGearUid=uid;}else Profile.crystals+=5;
                }
            }
            if(!IsReplay)
            {
                Profile.roomCheckpoint=0;
                if(Dungeon.number==6)Profile.quest=(int)QuestState.Ready;
                else Profile.campaignStage=Mathf.Max(Profile.campaignStage,Dungeon.number+1);
            }
            LastReward="경험치 +"+LastXp+" · 골드 +"+LastGold+(first?" · 영자 결정 +"+(3+Dungeon.act):" · 재현 보상");
            if(Profile.level>before)LastReward+="\n파티 레벨 "+before+" → "+Profile.level;
            if(LastGearUid>0)LastReward+="\n새 장비 · "+TideProgress.Gear(Profile,LastGearUid).name;
            string learned=TideProgress.LearnedByLevel(Profile,before);
            if(learned.Length>0)LastReward+="\n"+learned;
            TideCatalog.Sanitize(Profile);PersistentChange();
        }
        public bool Equip(){int next=Profile.powerGear==1?2:1;return EquipGear(next);}
        public bool EquipGear(int uid)
        {
            if(InBattle)return false;TideGear item=TideProgress.Gear(Profile,uid);if(item==null)return false;
            if(item.slot=="guard")Profile.guardGear=uid;else Profile.powerGear=uid;Profile.precision=Profile.powerGear==2;Hp=MaxHp;PersistentChange();Changed(item.name+" 착용");return true;
        }
        public int UpgradeGold(TideGear item){return item==null?0:120*(item.upgrade+1)+item.tier*60;}
        public int UpgradeCrystals(TideGear item){return item==null?0:3+item.upgrade;}
        public bool UpgradeGear(int uid)
        {
            if(InBattle)return false;TideGear item=TideProgress.Gear(Profile,uid);if(item==null||item.upgrade>=5)return false;
            int gold=UpgradeGold(item),crystals=UpgradeCrystals(item);if(Profile.gold<gold||Profile.crystals<crystals)return false;
            Profile.gold-=gold;Profile.crystals-=crystals;item.upgrade++;Hp=MaxHp;PersistentChange();Changed(item.name+" +"+item.upgrade+" 강화 완료");return true;
        }
        public bool FavoriteGear(int uid){TideGear item=TideProgress.Gear(Profile,uid);if(item==null||InBattle)return false;item.favorite=!item.favorite;Profile.locked=item.favorite;PersistentChange();return true;}
    }
}
