using UnityEngine;

namespace SpiritTide
{
    public static class TideUiInfo
    {
        public static readonly string[] Keys={"A","S","D","F","R"};
        private static readonly string[][] Descriptions={
            new[]{"앞쪽 검격으로 타격하고 냉기로 적의 이동을 늦춥니다.","넓은 범위에 냉기를 남깁니다. 다른 냉기 기술보다 오래 적의 이동을 늦춥니다.","바라보는 방향으로 전진한 뒤 검격을 연결합니다.","앞쪽에 강한 냉기 검격을 가하고 적의 행동을 잠시 멈춥니다.","큰 냉기 타격으로 적의 행동을 잠시 멈춥니다. 큰 공격 뒤의 빈틈에 사용하세요."},
            new[]{"보호막을 세워 다음 피해를 흡수합니다. 체력 회복과 별도로 유지됩니다.","자신의 체력을 회복합니다. 체력이 부족할 때 결계와 함께 사용하세요.","멀리 있는 적에게 영탄을 날려 피해를 줍니다.","짧은 시간 반격 결계를 준비합니다. 다음 공격을 막고 되돌려줍니다.","큰 영탄 타격과 함께 보호막을 만듭니다."},
            new[]{"적의 가까이로 파고들어 타격합니다. 진입 뒤에는 다음 공격 예고를 확인하세요.","앞쪽에 장타를 가합니다. 기본 공격 사이에 연결하기 좋습니다.","적에게 접근해 여러 번 타격합니다. 후속 타격이 끝날 때까지 이어집니다.","뒤로 빠지며 회피와 반격을 준비합니다. 정밀 회피 뒤 기본 공격으로 반격하세요.","강한 순홍 타격으로 큰 피해를 주고 적의 행동을 잠시 멈춥니다."},
            new[]{"적에게 첫 표식을 남깁니다. 봉익일섬으로 표식을 소모하면 더 강하게 타격합니다.","적에게 접근해 빠르게 찌릅니다. 표식을 남길 거리로 들어갈 때 활용하세요.","짧은 간격으로 여러 번 찌릅니다. 적이 빈틈을 보일 때 사용하세요.","표식이 있으면 소모하며 강하게 타격합니다. 표식 표시를 확인한 뒤 사용하세요.","큰 포격으로 적의 행동을 잠시 멈춥니다. 빈틈이 긴 순간을 노리세요."},
            new[]{"앞쪽에 재를 흩뿌려 타격합니다. 기본 공격보다 멀리 닿습니다.","바라보는 방향 앞에 재구름을 설치합니다. 가까운 적에게 지속 피해를 줍니다.","적을 가까운 위치로 끌어당기며 타격합니다. 설치한 재구름으로 유도하세요.","남아 있는 재구름을 소모해 강한 피해를 줍니다. 재구름 표시를 먼저 확인하세요.","큰 재 폭풍으로 타격하고 적의 행동을 잠시 멈춥니다."},
            new[]{"자신의 체력을 회복합니다. 적의 예고를 피한 뒤 안전하게 사용하세요.","앞쪽을 베어 타격합니다. 검격 태세에서는 더 강하게 타격합니다.","짧은 시간 받아치기를 준비합니다. 다음 공격을 막고 반격합니다.","회복 태세와 검격 태세를 전환합니다. 체력 아래의 태세 표시로 확인할 수 있습니다.","강한 검격과 함께 자신의 체력을 회복하고 적의 행동을 잠시 멈춥니다."}
        };
        public static string Skill(int hero,int slot){return Descriptions[Mathf.Clamp(hero,0,5)][Mathf.Clamp(slot,0,4)];}
        public static string ClassState(TideSession s)
        {
            switch(s.Profile.hero)
            {
                case 0:return s.EnemySlow>0?"냉기 적용 · "+s.EnemySlow.ToString("0.0")+"초":"냉기 제어 · 검격과 연계";
                case 1:return s.ReflectRemaining>0?"반격 결계 · "+s.ReflectRemaining.ToString("0.0")+"초":s.Shield>0?"결계 보호 중":"결계 · 회복과 반격";
                case 2:return s.PerfectRemaining>0?"반격 기회 · 지금 기본 공격":"순홍 · 기동과 연속 타격";
                case 3:return s.Mark>0?"첫 표식 · 봉익일섬 강화":"작봉 · 첫 표식을 노리세요";
                case 4:int count=(s.CloudTime[0]>0?1:0)+(s.CloudTime[1]>0?1:0);return "재구름 "+count+" / 2 · 설치 후 폭발";
                default:return s.CounterStance?"검격 태세 · 정적일섬 강화":"회복 태세 · 유상전환으로 전환";
            }
        }
    }
    public static class TideHudLayout
    {
        public static readonly string[] Ids={"attack","heavy","jump","dodge","potion","skill-0","skill-1","skill-2","skill-3","skill-4"};
        public static readonly Vector2[] Positions={new Vector2(-92,91),new Vector2(-350,91),new Vector2(-92,221),new Vector2(-222,91),new Vector2(-222,215),new Vector2(-470,215),new Vector2(-350,215),new Vector2(-470,335),new Vector2(-350,335),new Vector2(-222,335)};
        public static readonly Vector2[] Sizes={new Vector2(120,120),new Vector2(100,100),new Vector2(100,100),new Vector2(100,100),new Vector2(100,96),new Vector2(110,108),new Vector2(110,108),new Vector2(110,108),new Vector2(110,108),new Vector2(110,108)};
        public static Rect Bounds(int index,float width,float scale){Vector2 center=new Vector2(width,0)+Positions[index]*scale;return new Rect(center-Sizes[index]*scale*.5f,Sizes[index]*scale);}
        public static float UnitX(float width,float scale){return Mathf.Clamp((width-525*scale-width*.23f-184)/6,35,62);}
    }
}
