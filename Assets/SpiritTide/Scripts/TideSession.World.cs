using System;
using UnityEngine;
namespace SpiritTide
{
    public sealed partial class TideSession
    {
        public TideRoomDef Room=>IsCampaign?Dungeon.rooms[RoomIndex]:null;
        public bool WardActive,EnvironmentWarning;
        public float EnvironmentTimer,EnvironmentDuration,EnvironmentY,EnvironmentX,EnvironmentPush;
        private int reinforcementOffset;
        private float reinforcementDelay;
        public bool ReinforcementsPending=>IsCampaign&&reinforcementOffset>0;
        public Vector2 WardPosition=>new Vector2(1.1f,RoomIndex%2==0?-.65f:.65f);
        public float EchoX=>2.2f;
        public bool EchoAvailable=>IsCampaign&&RoomCleared&&Room.echo&&(Profile.rescuedDungeons&(1<<Dungeon.index))==0;
        public bool NearInteraction{get{float distance;int target=NearestFieldTarget(out distance);return target!=-3&&distance<(target==-1?1.45f:1.35f);}}
        public string EnvironmentCue=>!WardActive?"":Room.hazard=="tide"?"해안 파도 · 점프 또는 위아래로":Room.hazard=="rubble"?"무너지는 발판 · 낙하 원 밖으로":Room.hazard=="wind"?"백사막 돌풍 · 바람을 가로질러 이동":Room.hazard=="barrier"?"불안정 결계 · 사선 밖으로":"그림자 통로 · 진한 사선 밖으로";
        private void SetupRoomWorld()
        {
            WardActive=Room.ward;EnvironmentWarning=false;EnvironmentTimer=3.6f;EnvironmentPush=0;reinforcementOffset=Room.reinforcement&&Room.foes.Length>2?2:0;reinforcementDelay=.6f;
        }
        private void SpawnRoomFoes()
        {
            int count=reinforcementOffset>0?2:Room.foes.Length;
            for(int i=0;i<count;i++)Spawn(Room.foes[i],i,count,Dungeon.index);
        }
        private void ApplyFormation(TideEnemy e,int ordinal,int total)
        {
            if(!IsCampaign||total<=1)return;
            switch(Room.layout)
            {
                case "flank":e.X=ordinal%2==0?-5:3.8f+ordinal*.3f;e.Y=ordinal%2==0?.9f:-.9f;break;
                case "pincer":e.X=ordinal%2==0?-4.8f:5.3f;e.Y=ordinal<2?-1.1f:1.1f;break;
                case "ranged":e.X=3.3f+ordinal*.8f;e.Y=ordinal%2==0?-1.15f:1.15f;break;
                case "guarded":e.X=ordinal==0?1.2f:4.3f+ordinal*.3f;e.Y=(ordinal-(total-1)*.5f)*.7f;break;
                case "open":e.X=2.2f+ordinal*.95f;e.Y=ordinal%2==0?-1.2f:1.2f;break;
            }
        }
        private void StepRoomWorld(float dt)
        {
            if(!IsCampaign||RoomCleared)return;
            if(AliveCount==0&&reinforcementOffset>0)
            {
                reinforcementDelay-=dt;if(reinforcementDelay>0)return;
                int from=reinforcementOffset;reinforcementOffset=0;Enemies.Clear();target=null;
                for(int i=from;i<Room.foes.Length;i++)Spawn(Room.foes[i],i-from,Room.foes.Length-from,Dungeon.index);
                ChooseTarget();Changed("기습 증원 · 양쪽 진입로와 원거리 적부터 확인하세요.");
            }
            EnvironmentPush=Mathf.MoveTowards(EnvironmentPush,0,dt*3);
            if(!WardActive||AliveCount==0)return;
            EnvironmentTimer-=dt;
            if(!EnvironmentWarning)
            {
                if(EnvironmentTimer<=0){EnvironmentWarning=true;EnvironmentDuration=Room.hazard=="shadow"?1.65f:1.35f;EnvironmentTimer=EnvironmentDuration;EnvironmentX=X;EnvironmentY=Y;Changed(EnvironmentCue+" · 영압 장치를 끄면 멈춥니다.");}return;
            }
            if(EnvironmentTimer>0)return;
            bool hit=Room.hazard=="rubble"?Vector2.Distance(new Vector2(X,Y),new Vector2(EnvironmentX,EnvironmentY))<1.15f:Room.hazard=="barrier"?Mathf.Abs((Y-EnvironmentY)-(X-EnvironmentX)*.2f)<.36f:Mathf.Abs(Y-EnvironmentY)<.48f;
            if(Room.hazard=="tide"&&Z>.45f)hit=false;
            if(hit&&Room.hazard=="wind"){EnvironmentPush=1.6f;X=Mathf.Clamp(X+1.1f,-6,10.5f);Changed("돌풍에 밀렸습니다 · 장치를 끄거나 예고를 벗어나세요.");}
            else if(hit)TakeDamage(null,7+Dungeon.act*2);
            EnvironmentWarning=false;EnvironmentTimer=4.2f;
        }
        public bool InteractWorld()
        {
            if(!IsFighting||!IsCampaign||!NearInteraction)return false;
            float distance;int target=NearestFieldTarget(out distance);
            if(target>=0)return InteractQuestPoint(target);
            if(target==-1)
            {
                Profile.rescuedDungeons|=1<<Dungeon.index;Hp=Mathf.Min(MaxHp,Hp+Mathf.CeilToInt(MaxHp*.15f));Mp=Mathf.Min(100,Mp+15);PersistentChange();Changed("정령에게 귀환 등불을 안내했습니다 · 체력·영압 회복. 마을의 콘에게 소식을 전하세요.");return true;
            }
            if(target==-2&&WardActive){WardActive=EnvironmentWarning=false;Mp=Mathf.Min(100,Mp+15);QuestWardEvent();Changed("영압 장치 해제 · 이 방의 환경 예고가 멈췄습니다. 영압 +15");return true;}
            return false;
        }
        public int RescuesInAct(int act){int mask=(Profile.rescuedDungeons>>((Mathf.Clamp(act,1,5)-1)*6))&63,n=0;while(mask!=0){n+=mask&1;mask>>=1;}return n;}
        public bool CanReportRescue(int act){int a=Mathf.Clamp(act,1,5);return !InBattle&&(Profile.rescueReports&(1<<(a-1)))==0&&RescuesInAct(a)>=2;}
        public bool ReportRescue(int act)
        {
            int a=Mathf.Clamp(act,1,5);if(!CanReportRescue(a))return false;
            Profile.rescueReports|=1<<(a-1);Profile.gold+=100;Profile.crystals+=2;if(Profile.potions<5)Profile.potions++;else Profile.gold+=40;TideCatalog.Sanitize(Profile);PersistentChange();Changed("콘의 구조 소식 · 100 골드·결정 2·회복 영자 1개 (소지 한도면 40 골드)");return true;
        }
    }
}
