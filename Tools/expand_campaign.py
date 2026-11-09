"""Lay out the expedition rooms after the original, stable field coordinates."""
import json
from pathlib import Path

DATA = Path(__file__).resolve().parents[1] / "Assets/SpiritTide/Resources/SpiritTide/Data"

# Short approaches and prepared enemy positions alternate within each expedition.
# The fifth room of the final approaches sets up the encounter beyond it.
ROOM_TITLES = [
    [
        ("둑 아래 피난로", "수문 뒤 대피 계단", "가면이 막은 제방", "강변 인계 지점"),
        ("잠긴 현관들", "되울리는 골목", "빨랫줄 사이 사잇길", "문패가 떨어진 마당", "집으로 향하는 뒷길"),
        ("사역체의 둥지 입구", "발소리가 하나인 길", "울음이 겹친 지하도", "부서진 확성 표지", "폭발 고리 바깥"),
        ("씨앗이 파고든 바닥", "비어 있는 매대", "반쯤 내려온 셔터", "상가의 비상 계단", "등불이 보이는 모퉁이"),
        ("빗물이 고인 선착장", "부두 크레인 아래", "밧줄을 삼킨 물결", "등대의 측면 다리", "흔들리는 불빛의 뒤편"),
        ("등대 기슭", "돌계단의 그림자", "먼저 부르는 소리", "부러진 난간 너머", "제자리에 남은 불빛", "미끼가 닿은 바닥"),
    ],
    [
        ("지붕 끝의 낮은 창", "반쯤 찢긴 순찰표", "두 거리 사이 옥상", "순보 흔적이 끊긴 굴뚝"),
        ("가로등의 흔들림", "나킴이 남긴 발판", "디 로이의 좁은 골목", "짧은 자국이 겹친 지붕", "교차로로 통하는 계단"),
        ("교차선 바깥 지붕", "낡은 정찰 매듭", "무전이 돌아온 거리", "샤우롱의 앞쪽 횡단로", "가르간타를 향하는 선"),
        ("관문의 바깥 지지대", "경계가 밀려오는 틈", "두 사람의 그림자", "돌아올 좌표의 빈 칸", "마주 보는 문지방"),
        ("아이스링거의 견제선", "데모우라 뒤의 경첩", "문을 받친 영압 돌", "들어올 때의 시점", "왕복 신호가 만나는 자리"),
        ("문이 울리는 통로", "허공에 남은 발톱", "돌진이 꺾이는 발판", "가르간타의 바깥 매듭", "흔들리지 않는 좌표", "그림죠의 착지선"),
    ],
    [
        ("발자국이 되감기는 둔덕", "도르도니의 바람목", "치루치가 긁은 돌기둥", "바람이 닿지 않는 문"),
        ("무너진 회랑의 안쪽", "붕대가 걸린 난간", "잔해 밑의 좁은 길", "부상자 대기소", "회랑 밖 들것 인계점"),
        ("낫에 패인 모래", "테슬러가 지나간 통로", "회랑 아래의 끊긴 등불", "초승달 아래 빈터", "낫자루 밑 표식"),
        ("먼지가 눌린 서가", "덮여 쓴 장부", "아로니로의 빈 이름칸", "조마리의 관측 틈", "분석실로 가는 책등"),
        ("분석실의 측면 관찰대", "핀돌의 끊긴 파동", "지오가 뛰어든 낮은 담", "겹쳐 뜬 영압 측정판", "천개로 가는 발판"),
        ("천개 아래 그림자", "유리 없는 창", "빛의 창이 지난 자리", "사본을 비춘 돌벽", "원본을 들 수 있는 빈터", "천개 끝 귀환선"),
    ],
    [
        ("네 번째 기둥의 그림자", "사본 밖 골목", "엇갈린 신호등", "같은 곳을 가리키는 창"),
        ("멎은 수로의 난간", "할리벨의 파문", "야미가 디딘 교각", "혼자 울리는 목소리", "수로 밖 인계 지점"),
        ("기울어진 다리", "신창이 긁은 난간", "돌아서면 보이는 검흔", "다시 걸을 자리", "사선 끝에 남은 길"),
        ("원더와이스의 소리 아래", "아파치 그림자의 맞은편", "미라 로즈가 딛던 광장", "서로 다른 두 방향", "광장을 벗어나는 모서리"),
        ("출구가 돌던 간판", "아욘이 무너뜨린 입구", "선순의 짧은 사각", "샤를로테가 가린 표지", "세 자국이 이어진 곳"),
        ("거울 도시 첫 반사면", "서로 다른 걸음 소리", "옅은 영압이 가린 거리", "사본과 겹치는 횡단로", "갈라진 퀸시 문장", "진한 범위의 바깥"),
    ],
    [
        ("뒤에서 돌아오는 그림자", "킬게의 갇힌 통로", "에스 노트의 흔들린 표지", "왕문으로 꺾이는 발자국"),
        ("화염 뒤의 흰 발판", "밤비에타의 낙하 지점", "버즈비의 잔불 사이", "저울이 비친 바닥", "그림자가 돌아오는 쪽"),
        ("거꾸로 이어진 회랑", "발걸음이 쌓인 바닥", "저울 아래 숨 고르는 곳", "고리를 잇는 사선", "지워지지 않는 첫 자국"),
        ("창 두가 막은 회랑", "BG9의 관측선", "마스크가 내려온 곳", "왕성 바깥의 출구 표지", "정원이 내려다보이는 문"),
        ("발소리가 있는 정원", "가짜 귀환 갈림길", "같은 발의 다른 자국", "다섯 줄을 잇는 자리", "마지막 문 바깥 좌표"),
        ("왕문의 바깥 계단", "미리 보이는 사선", "두 이름이 엇갈린 곳", "끊어야 할 그림자의 끝", "여섯 번째 대답의 자리", "유하바하 앞의 문턱"),
    ],
]

FORMATIONS = (
    ((3, 3, 3), ("ranged", "flank", "open")),
    ((4, 3), ("guarded", "pincer")),
    ((3, 4, 3), ("open", "ranged", "guarded")),
    ((2, 3, 4), ("flank", "staggered", "pincer")),
    ((3, 3), ("pincer", "ranged")),
    ((4, 3, 3), ("guarded", "flank", "open")),
)
HAZARDS = ("tide", "rubble", "wind", "barrier", "shadow")
SIGNALS = (
    ("피난로 종 울리기", "종소리에 가면들이 반응합니다. 피난 계단을 지키세요."),
    ("왕복 표지 고정", "반대편 표지를 확인했습니다. 관문을 붙잡으려는 적이 옵니다."),
    ("회랑 불빛 잇기", "빛이 이어집니다. 등불 쪽으로 모이는 적을 막아 주세요."),
    ("서로 본 길 맞추기", "진짜 발소리를 따라 환영이 몰립니다. 남은 길을 지키세요."),
    ("귀환 발자국 남기기", "새 자국이 남았습니다. 왕문 쪽에서 되돌아오는 병력을 막으세요."),
)


def expand(campaign):
    for dungeon in campaign["stages"]:
        act, stage = dungeon["act"], dungeon["number"]
        base = [room for room in dungeon["rooms"] if not room.get("extension")]
        dungeon["legacy_rooms"] = len(base)
        boss = base[-1:] if base[-1]["kind"] == "boss" else []
        body = base[:-1] if boss else base
        troops = list(dict.fromkeys(foe for room in body if room["kind"] == "troops" for foe in room["foes"]))
        named = [room["foes"][0] for room in body if room["kind"] == "named"]
        additions = []
        for index, title in enumerate(ROOM_TITLES[act - 1][stage - 1]):
            sizes, layouts = FORMATIONS[(index + (stage - 1) * 2 + act - 1) % len(FORMATIONS)]
            signal = index == 1 or stage == 6 and index == 4
            if signal and len(sizes) == 2:
                sizes, layouts = sizes + (3,), layouts + ("flank",)
            foes = [troops[(index + wave + slot) % len(troops)] for wave, size in enumerate(sizes) for slot in range(size)]
            # Keep the original solo encounter, and put one supporting memory in the longer approach.
            mixed = bool(named) and index == 3
            if mixed:
                foes[sum(sizes[:-1])] = named[(act + stage) % len(named)]
            ward = index == 2 or stage in (3, 6) and index == 0
            room = {
                "title": title, "kind": "mixed" if mixed else "troops", "layout": layouts[0],
                "hazard": HAZARDS[act - 1] if ward else "", "echo": False, "ward": ward,
                "reinforcement": False, "extension": True, "foes": foes,
                "wave_sizes": sizes, "wave_layouts": layouts,
                "pressure": round(2.05 + .12 * (act - 1), 2),
            }
            if signal:
                room["signal_wave"] = 2
                room["signal_title"], room["signal_reply"] = SIGNALS[act - 1]
            additions.append(room)
        dungeon["rooms"] = body + additions + boss
        for index, room in enumerate(body):
            room["pressure"] = 1.0 if (act, stage, index) == (1, 1, 0) else 1.50 if room["kind"] == "named" else 1.35
        if boss:
            boss[0]["pressure"] = 2.25 if stage == 3 else 2.65
    return campaign


if __name__ == "__main__":
    path = DATA / "campaign.json"
    data = expand(json.loads(path.read_text(encoding="utf-8")))
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(sum(len(d["rooms"]) for d in data["stages"]), "rooms in 30 expeditions")
