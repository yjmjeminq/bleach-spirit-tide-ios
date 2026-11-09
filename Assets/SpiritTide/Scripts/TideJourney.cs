using System;
using UnityEngine;

namespace SpiritTide
{
    [Serializable] public sealed class TideJourneyChapter { public int act; public string stakes,midpoint; }
    [Serializable] public sealed class TideJourneyData { public int schema; public TideJourneyChapter[] chapters; }
    public static class TideJourney
    {
        private static TideJourneyData data;
        public static TideJourneyData Data { get { if(data==null) {var asset=Resources.Load<TextAsset>("SpiritTide/Data/journey");if(asset==null)throw new InvalidOperationException("Missing journey context");data=JsonUtility.FromJson<TideJourneyData>(asset.text);if(data.schema!=1||data.chapters.Length!=5)throw new InvalidOperationException("Incomplete journey context");}return data;} }
        public static bool Finished(TideProfile p)=>(p.reportedActs&31)==31;
        public static int BossStage(int index)=>index%2==0?3:6;
        public static bool BossKnown(TideProfile p,int index)
        {
            if(index<0||index>=10)return false;
            int act=index/2+1;if(act>p.campaignAct)return false;
            int clear=1<<((act-1)*6+BossStage(index)-1);
            return (p.clearedDungeons&clear)!=0||(p.seenIntros&TideExperience.EncounterBit(TideCatalog.Bosses[index].id))!=0;
        }
        public static TideDungeonDef Current(TideProfile p)=>TideCampaign.Dungeon(p.campaignAct,p.campaignStage);
        public static string Proof(TideProfile p)
        {
            if(Finished(p))return "다섯 균열의 연결을 봉인하고 되찾은 이름을 전했습니다. 여섯 동료가 모두 귀환했습니다.";
            for(int n=6;n>=1;n--){var d=TideCampaign.Dungeon(p.campaignAct,n);if((p.clearedDungeons&(1<<d.index))!=0)return d.resolution;}
            if(p.campaignAct>1&&(p.reportedActs&(1<<(p.campaignAct-2)))!=0)return TideCampaign.Dungeon(p.campaignAct-1,6).resolution;
            return "강변에서 이름을 잊은 사람의 구조 신호가 왔습니다. 아직 현장에서 확인한 단서는 없습니다.";
        }
        public static string Brief(string text){int end=text.IndexOf('.');return end<0?text:text.Substring(0,end+1);}
        public static string Narrative(TideProfile p)=>Finished(p)?Proof(p):Data.chapters[p.campaignAct-1].stakes+"\n최근 확인 · "+Brief(Proof(p));
        public static string Next(TideProfile p)
        {
            if(Finished(p))return "후일담과 남은 부탁을 읽거나 보스 기록을 연습할 수 있습니다. 본편 보고 보상은 반복되지 않습니다.";
            if((QuestState)p.quest==QuestState.Available)return "우라하라에게 ‘"+TideQuests.Main(p.campaignAct).title+"’의 부탁을 듣고 출정합니다.";
            if((QuestState)p.quest==QuestState.Ready)return "우라하라에게 이번 기록을 보고하세요. 보고를 마쳐야 다음 좌표가 확정됩니다.";
            var d=Current(p);var point=TideQuests.NextPoint(p,p.campaignAct,d.number);
            string lead=point==null?"정화한 방 다음부터 이어 갑니다. 오른쪽 출구에서 다음 방을 누르세요."
                :(point.room+1)+"방 정화 뒤 · "+point.goal;
            return d.title+" · "+(p.roomCheckpoint+1)+" / "+d.rooms.Length+"방\n"+lead;
        }
        public static int BetterGear(TideProfile p)
        {
            int uid=0,best=0;foreach(var item in p.gear){var worn=TideProgress.Gear(p,item.slot=="guard"?p.guardGear:p.powerGear);int delta=item.slot=="guard"?item.Health-(worn?.Health??0):item.Attack-(worn?.Attack??0);if(delta>best){best=delta;uid=item.uid;}}return uid;
        }
        public static string Preparation(TideProfile p)
        {
            var d=Current(p);var worn=TideProgress.Gear(p,p.powerGear);var guard=TideProgress.Gear(p,p.guardGear);int better=BetterGear(p);
            string upgrade=better==0?"보유 장비 중 더 높은 수치의 영장 없음":"더 높은 수치의 전리품 있음 · 직접 비교";
            return "파티 Lv."+p.level+" · 회복 영자 "+p.potions+" / 5\n착용 영장 · 피해 +"+(worn?.Attack??0)+" / 체력 +"+(guard?.Health??0)+"\n"+upgrade+"\n"+(p.potions==0?"보급 1개 80골드 · 출정의 필수 조건 아님":"회복은 체력이 줄었을 때 사용");
        }
        public static string Lesson(int index)
        {
            switch(index)
            {
                case 0:return "사역체가 만드는 두 원을 모두 벗어나세요. 낮은 음파는 점프로, 정면 강타는 위아래 이동으로 피합니다.";
                case 1:return "미끼의 원 밖으로 이동한 뒤 본체 노출 때 공격하세요. 낮게 휘두르는 공격은 점프로 넘습니다.";
                case 2:return "교차 사선 사이의 빈 공간으로 이동하세요. 다음 정면 강타까지 보고 짧게 공격합니다.";
                case 3:return "소니도 직선에서는 위아래로, 도약의 원에서는 바깥으로 이동하세요. 멈춘 뒤 접근합니다.";
                case 4:return "짧은 간격으로 이어지는 연격을 끝까지 확인하세요. 체력이 절반 이하가 되면 두 번째 예고도 살핍니다.";
                case 5:return "체력에 따라 세 단계로 변합니다. 직선·교차 사선과 착지의 원을 구분하고 착지 뒤 노출을 노리세요.";
                case 6:return "길게 뻗는 직선에서 위아래로 벗어나세요. 멀리 떨어져도 같은 사선 위라면 안전하지 않습니다.";
                case 7:return "옅은 환영 표식은 실제 공격이 아닙니다. 진한 원을 피해 본체가 노출되는 순간에 공격하세요.";
                case 8:return "정면 강타와 세 번째 공격의 되돌림 고리를 구분하세요. 큰 공격 뒤 쉬는 동안 접근합니다.";
                default:return "옅은 미래 후보를 보고 서두르지 마세요. 진하게 확정된 사선 밖으로 이동하고 정면 검격도 살핍니다.";
            }
        }
        public static string DefeatAdvice(TideSession s)
        {
            int retryRoom=s.IsFieldVisit?s.RoomIndex:s.IsReplay?0:s.Profile.roomCheckpoint;
            string at=s.IsCampaign?s.Dungeon.title+" · "+(retryRoom+1)+"방부터 재도전":"보스 패턴 연습 · 진행 보상 없음";
            string cause=string.IsNullOrEmpty(s.LastDamageCue)?"바닥 예고를 확인하고 짧게 공격한 뒤 이동해 보세요.":"마지막 피격 · "+(string.IsNullOrEmpty(s.LastDamageFoe)?"환경 공격":TideCampaign.Foe(s.LastDamageFoe).name)+"\n"+s.LastDamageCue;
            return at+"\n"+cause;
        }
    }
}
