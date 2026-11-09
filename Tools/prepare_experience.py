"""Author causal stage beats and varied arenas without rewriting the selected artwork."""
import json
from pathlib import Path
ROOT=Path(__file__).resolve().parent.parent
DATA=ROOT/'Assets/SpiritTide/Resources/SpiritTide/Data'
def read(p):return json.loads(p.read_text(encoding='utf-8'))
def write(p,d):p.write_text(json.dumps(d,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')

def main():
    c=read(DATA/'campaign.json');story=read(DATA/'story_flow.json')
    clues=[('unnamed_signal','이름을 잊은 생존자의 구조 신호'),('lighthouse_signal','등대 아래로 모이는 사역체 신호'),('garganta_trace','미끼에 남은 가르간타의 흔적'),('sonido_route','샤우롱이 남긴 소니도의 이동 경로'),('las_noches_coordinates','가르간타 너머 라스 노체스의 좌표'),('record_anchor','왜곡되지 않은 기록을 붙드는 표식'),('unaltered_record','서로 대조할 수 있는 원본 기록'),('mirror_discrepancy','긴의 검흔과 환영 표지가 어긋나는 지점'),('quincy_connection','균열에 닿은 퀸시의 그림자 통로'),('balance_lock','되돌림을 지탱하는 왕문의 연결 고리'),('returned_names','원래 주인에게 돌아간 모든 이름')]
    beats=[
      [
       ('생존자가 마지막으로 본 것은 강변의 흰 가면입니다. 먼저 피난로를 열어, 가족의 목소리가 사라진 지점을 찾습니다.','피시본의 기억을 걷어 내자 같은 구조 신호가 주택가에서 들려왔습니다. 살아 있는 사람의 신호부터 확인합니다.'),
       ('강변에서 찾은 신호가 주택가를 되풀이합니다. 애시드와이어의 기억과 현재 구조자를 혼동하지 않도록 기록을 따로 남깁니다.','구조자의 이름은 지켰습니다. 집집마다 퍼진 소리의 출처가 슈리커의 사역체임을 확인했습니다.'),
       ('사역체가 진짜 구조 신호 위에 가짜 목소리를 겹칩니다. 퍼뜨리는 쪽을 먼저 끊어 미끼의 본체를 찾습니다.','슈리커를 정화하자 신호가 등대 아래로 모였습니다. 상가에 남은 씨앗을 제거해야 안전하게 접근할 수 있습니다.'),
       ('닫힌 상가의 씨앗이 등대로 가는 피난로를 막습니다. 넘 샹들리에의 재현을 봉인하고 귀환 표지를 남깁니다.','씨앗이 사라진 상가에서 등대 방향의 큰 발자국을 찾았습니다. 구조 등불은 여전히 일정하게 빛납니다.'),
       ('발자국은 비 오는 부두로 이어집니다. 벌보스와 메타스타시아의 기억을 분리해, 미끼가 숨은 등대 진입로를 찾습니다.','발자국 끝에서 사람 목소리를 흉내 내는 미끼를 확인했습니다. 등불과 다른 박자로 흔들리는 것이 단서입니다.'),
       ('그랜드 피셔의 미끼와 구조 등불을 구분했습니다. 미끼를 거둔 뒤 드러나는 본체와 첫 균열의 연결을 끊습니다.','첫 균열이 닫혔습니다. 회수한 이름은 기록함에 보존했고, 검은 틈의 흔적을 마을에서 대조할 차례입니다.')
      ],[
       ('항구의 검은 틈이 야간 순찰로에 겹칩니다. 에드라드와 일폴트의 기억이 남긴 진입 방향부터 확인합니다.','서로 다른 습격 기록이 같은 지붕 위 길로 모입니다. 프라시온의 발자국을 더 따라갑니다.'),
       ('나킴과 디 로이의 조우 지점에는 소니도의 잔향이 남아 있습니다. 누가 통로의 방향을 바꾸는지 알아냅니다.','잔향의 간격이 샤우롱의 정찰 경로와 일치합니다. 교차선이 시작되는 지점으로 이동합니다.'),
       ('샤우롱의 교차 절단이 정찰 경로를 가립니다. 사선 사이의 빈틈으로 접근해 통로의 끝을 확인합니다.','정찰 경로 끝에서 그림죠의 영압을 찾았습니다. 닫힌 가르간타를 안정시킬 두 관문을 찾아야 합니다.'),
       ('로리와 메노리의 기억이 가르간타 관문을 붙잡습니다. 서로 다른 견제를 끊고 경계를 통과합니다.','가르간타 안쪽에는 두 감시 기록이 남아 있습니다. 통로가 무너지기 전에 각각의 방을 정리합니다.'),
       ('데모우라와 아이스링거는 별도 관문을 지킵니다. 넓은 공격과 원거리 견제를 나누어 읽고 돌아갈 길을 확보합니다.','왕복 좌표가 고정됐습니다. 이제 통로를 붙잡은 그림죠의 기록에 접근할 수 있습니다.'),
       ('그림죠의 재현이 통로를 고정하고 있습니다. 도약의 착지와 돌진이 멈추는 순간을 노려 기록을 분리합니다.','그림죠의 기록 뒤에서 라스 노체스의 좌표가 남았습니다. 마을에서 좌표를 확인한 뒤 다음 층으로 갑니다.')
      ],[
       ('라스 노체스 좌표는 맞지만 사막의 표지가 반복됩니다. 도르도니와 치루치의 기억을 정화해 실제 관문의 방향을 찾습니다.','관문의 좌표가 이어졌습니다. 무너진 회랑에서 돌아갈 표지가 끊긴 곳을 확인합니다.'),
       ('간텐바인과 테슬러의 기억이 회랑을 막습니다. 길 잃은 정령에게 귀환로를 남기고 원본 신호를 따라갑니다.','회랑 아래의 낫 자국이 노이트라의 기록으로 이어집니다. 마지막 연격까지 확인할 수 있는 공간을 확보합니다.'),
       ('노이트라의 연격은 기록을 붙잡는 표식 주위를 돕니다. 연격의 끝을 버텨 표식의 위치를 확인합니다.','원본을 붙드는 표식을 찾았습니다. 기록고와 분석실의 왜곡을 정리해야 표식을 안전하게 회수할 수 있습니다.'),
       ('기록고에는 루드본·아로니로·조마리의 기억이 별도 방으로 겹칩니다. 각 방의 표지를 대조해 가짜 연결을 걷어 냅니다.','기록고의 표지가 분석실을 가리킵니다. 서로 다른 이름을 한 기록으로 합치지 않고 보존합니다.'),
       ('분석실과 정원에 남은 자엘아폴로·핀돌·지오의 기억을 정리합니다. 원본에 덧씌운 관측 흔적을 제거합니다.','덧씌운 흔적이 사라지자 천개 위의 기록실이 드러났습니다. 우르키오라의 기록 아래에 원본이 있습니다.'),
       ('우르키오라의 영압이 세 단계로 변하며 천개를 막습니다. 빛의 창과 착지 뒤의 빈틈을 읽고 원본을 회수합니다.','원본 기록을 회수했습니다. 마을에서 여섯 사람의 영압과 맞추면 각성기를 강화하고 환영층을 대조할 수 있습니다.')
      ],[
       ('원본과 다른 표지가 교차로에 놓여 있습니다. 스타크와 바라간의 서로 다른 기억을 분리해 첫 어긋남을 찾습니다.','표지의 어긋남이 정지한 수로까지 이어집니다. 보이는 풍경 대신 원본의 순서를 따라갑니다.'),
       ('할리벨과 야미의 기억이 수로를 가립니다. 물결과 무거운 강타를 구분해 검흔이 이어지는 다리로 접근합니다.','긴 칼날이 남긴 자국을 찾았습니다. 란기쿠가 가진 기억과 현장의 자국을 함께 확인합니다.'),
       ('긴의 재현은 살아 돌아온 그가 아닙니다. 란기쿠의 기억을 존중하며, 신창의 궤적이 가리키는 실제 통로를 찾습니다.','긴의 자국과 가짜 표지가 어긋나는 지점을 찾았습니다. 란기쿠는 남은 말을 가지고 동료와 돌아가기로 합니다.'),
       ('원더와이스·아파치·미라 로즈의 기록이 같은 광장에 겹칩니다. 소리와 그림자를 분리해 원본의 순서를 되찾습니다.','뒤섞인 기록이 각각의 이름으로 돌아갔습니다. 남은 선순·아욘·샤를로테의 기록을 지나 환영의 중심으로 갑니다.'),
       ('선순·아욘·샤를로테가 남긴 영압의 방향을 대조합니다. 환영을 유지하는 표지와 실제 출구를 구분합니다.','두 기록이 어긋난 중심을 찾았습니다. 아이젠의 재현과 균열을 잇는 고리만 분리할 수 있습니다.'),
       ('원본을 대조해 옅은 환영과 진한 실체를 구분합니다. 아이젠의 권능 자체를 없애는 대신 이 균열의 연결을 끊습니다.','환영 뒤에서 퀸시 문장과 그림자 통로를 찾았습니다. 회수한 기록으로 연결이 다시 열리는 이유를 확인합니다.')
      ],[
       ('그림자 통로가 봉인한 층에 다시 닿습니다. 킬게와 에스 노트의 기억을 지나 재현을 되돌리는 왕문의 방향을 찾습니다.','왕문까지 이어지는 전선이 드러났습니다. 폭발과 화염으로 가려진 표지를 먼저 정리합니다.'),
       ('밤비에타의 폭발과 버즈비의 화염이 서로 다른 구역을 막습니다. 두 예고를 구분해 저울 문장까지 접근합니다.','되돌림의 중심은 하쉬발트가 지키는 연결 고리입니다. 고리를 끊으면 같은 기록이 다시 돌아오지 않습니다.'),
       ('하쉬발트의 세 번째 타격 뒤 되돌림이 예고됩니다. 무작정 연타하지 않고 고리 밖으로 물러날 순간을 만듭니다.','되돌림의 고리를 해제했습니다. 왕성 회랑과 미래의 정원을 지나 마지막 기록실까지 길이 열렸습니다.'),
       ('창 두·BG9·마스크의 서로 다른 전투 기록이 회랑에 겹칩니다. 방어·사격·돌진을 나누어 상대하고 귀환 표지를 남깁니다.','회랑이 안정됐습니다. 가능한 미래가 겹치는 정원에서 확정되기 전의 신호를 대조합니다.'),
       ('캔디스·그레미·아스킨의 기록은 다른 미래를 보여 줍니다. 원본 다섯 묶음을 대조해 실제 왕실의 좌표를 정합니다.','왕실 좌표가 한곳으로 모였습니다. 되찾은 이름과 여섯 사람의 귀환 약속을 기록함에 고정합니다.'),
       ('가능한 사선이 확정되는 순간을 확인합니다. 유하바하의 재현과 다섯 균열을 잇는 기록 기능을 봉인하고 함께 돌아갑니다.','재현 기능이 멈췄습니다. 구조 신호가 귀환로를 밝힙니다. 마지막 보고로 이름을 돌려주고 여섯 사람의 귀환을 남깁니다.')
      ]]
    backgrounds=[['act1_river_v1','battle_act_2','battle_act_1','battle_act_2','act1_lighthouse_v1','act1_lighthouse_v1'],['act2_rooftops_v1','battle_act_2','act2_rooftops_v1','act2_gate_v1','act2_gate_v1','battle_act_2'],['act3_dunes_v1','battle_act_3','act3_dunes_v1','act3_lab_v1','act3_lab_v1','battle_act_3'],['battle_act_4','act4_canals_v1','act4_canals_v1','battle_act_4','act4_mirror_v1','act4_mirror_v1'],['act5_shadows_v1','act5_shadows_v1','battle_act_5','battle_act_5','act5_garden_v1','battle_act_5']]
    hazards=['tide','rubble','wind','barrier','shadow'];layouts=['staggered','flank','ranged','pincer','guarded','open']
    for d in c['stages']:
        a,n=d['act']-1,d['number']-1;d['background']=backgrounds[a][n];d['reason'],d['resolution']=beats[a][n];d['objective']=d['reason'];d['landmark']=d['title'];
        if a==2 and n==1:d['title']='무너진 회랑의 귀환로'
        d['landmark']=d['title']
        for i,r in enumerate(room for room in d['rooms'] if not room.get('extension')):
            r['title']=r['title'].replace('무너진 회랑의 호위','무너진 회랑의 귀환로')
            r['layout']=layouts[(n+i+a)%6];r['hazard']=hazards[a] if r['kind']=='troops' and i==1 else ''
            r['echo']=i==0;r['ward']=bool(r['hazard']);r['reinforcement']=r['kind']=='troops' and i==1 and n in (1,4)
    # The named encounter distribution is authoritative, so objectives match the actual rooms.
    story['note']='성인 평행세계 팬게임의 창작 대사. 다섯 액트 모두 플레이하며, 원작의 생사·동맹을 바꾸지 않는 전투 기억의 재현이다.'
    departure=story['acts'][0]['departure']
    if not any('영해는' in x['text'] for x in departure):departure.insert(0,{'speaker':'우라하라','text':'영해는 사라진 영압과 전투 기억이 쌓이는 바다입니다. 그 기억이 흘러넘쳐 지금 살아 있는 사람의 이름과 뒤섞이고 있어요.'})
    if not any('선두 한 분' in x['text'] for x in departure):departure.insert(2,{'speaker':'우라하라','text':'여섯 분의 영압을 신호로 묶겠습니다. 선두 한 분이 전투를 맡고 다른 동료들은 구조 신호를 유지해 주세요. 마을에서 선두를 바꿀 수 있습니다.'})
    for line in departure:
        if line['speaker'].startswith('쿠카쿠'):line['text']='강변 피난로와 주택가부터 확인해. 거기서 찾은 신호를 따라 사역체의 근원을 끊는다. 구조 등불은 일정하게 빛나니 목소리만 따라가지는 마.'
    story['acts'][2]['after_boss']=beats[2][5][1]
    if not any('각성기' in x['text'] for x in story['acts'][2]['report']):story['acts'][2]['report'].insert(0,{'speaker':'우라하라','text':'원본과 여러분의 영압을 맞췄습니다. 지금부터 여섯 분의 각성기가 강화됩니다. 이름을 소모한 것이 아니라 원래 기록의 힘을 되찾은 겁니다.'})
    starts=[
      ['가족을 부르는 목소리부터 지킨다. 얼려야 할 것은 돌아갈 길이 아니다.','목소리만으로 누군지 정하지 않을게요. 지금 여기 있는 사람을 먼저 확인해요.','앞서 길을 열 테니 서로 떨어지지 마라. 등불이 보이는 쪽으로 움직이자.','정찰 경로와 구조 경로를 분리합니다. 본체를 특정하기 전에는 신호를 단정하지 않겠습니다.','그리운 목소리일수록 천천히 확인하자. 돌아가서 직접 들으면 되니까.','상처와 두려움을 함께 살피죠. 여섯 사람의 귀환을 먼저 약속합시다.'],
      ['흰 제복의 기록은 따로 남긴다. 침입했던 시간과 지금의 거리를 혼동하지 마라.','문이 열려도 돌아올 표지는 남겨 둘게요. 뒤따라오는 사람도 있으니까요.','지붕 위는 내게 맡겨라. 통로를 찾으면 다 같이 건널 수 있게 표시하마.','이동 흔적을 찾았습니다. 빠르게 접근하되 도약이 끝나는 위치에서 합류하겠습니다.','이번엔 미끼 목소리가 없네. 그래도 서로 부르는 건 빼먹지 말자.','경계를 넘기 전에 호흡을 맞추세요. 빨리 가는 것과 무사히 가는 것은 함께할 수 있습니다.'],
      ['과거의 방이 지금의 동료를 가두게 두진 않겠다. 출구를 함께 찾는다.','그곳에서 기다리기만 하던 기억도 제 것이에요. 이번에는 제가 돌아갈 길을 남길게요.','하얀 사막은 방향을 속이기 좋지. 발자국과 동료의 영압을 함께 보자.','널찍한 곳에서도 방심하지 마십시오. 원본 신호와 실제 관문을 따로 확인합니다.','빈 방이어도 잊힌 사람은 있어. 각자의 이름으로 기록해서 가져가자.','기억을 지운다고 상처가 낫지는 않죠. 왜곡된 부분만 걷어 내고 가지고 돌아갑시다.'],
      ['보이는 것과 확인한 것을 구분하자. 서로 같은 말을 하는지도 대조한다.','저는 제 기록을 읽을게요. 다른 장면이 보여도 동료가 거짓말한다고 정하지 않겠어요.','가짜 표지는 두 장을 맞추면 드러난다. 혼자 알아냈다고 먼저 뛰지 말자.','눈앞의 지시보다 우리가 합의한 신호를 따릅니다. 대조가 끝난 뒤 움직입니다.','긴의 모습이 보이면 내가 먼저 말할게. 혼자 따라가지 않겠다고 여기서 약속하자.','슬픔은 대조해서 없앨 수 있는 것이 아니죠. 동료가 말할 때 기다려 줍시다.'],
      ['되찾은 이름을 다시 빼앗기게 두진 않는다. 마지막까지 귀환로를 남긴다.','어떤 미래가 보여도 지금 옆에 있는 사람부터 확인해요. 같이 돌아가기로 했으니까요.','가능한 길이 많으면 확정된 것부터 보자. 빈틈이 열릴 때 모두 함께 간다.','공격의 확정을 확인한 뒤 이동합니다. 마지막 출정에서도 절차를 생략하지 않겠습니다.','돌아가서 할 말이 아직 많아. 오늘의 이야기는 우리가 마을에서 끝내자.','끝이 보일수록 서두르지 마세요. 기록할 이름에는 여러분 여섯 분도 포함됩니다.']]
    after=[['등대 쪽으로 길이 이어진다. 지금 얻은 사실만 가지고 다음으로 간다.','미끼와 다른 진짜 신호를 찾았어요. 그 목소리가 돌아갈 곳도 지켜요.','본체 위치가 나왔다. 다음 길은 합류하고 나서 정하자.','신호의 근원을 특정했습니다. 함정을 정화한 구간은 귀환로로 남깁니다.','한 단계는 끝났네. 주운 이름까지 챙겨 가자.','중간 기록을 확보했군요. 상처를 살핀 뒤 다음으로 갑시다.']]
    voices=[]
    ids=['rukia','orihime','yoruichi','soifon','rangiku','unohana']
    for act in range(1,6):
        for h,id in enumerate(ids):
            mid=after[0][h] if act==1 else ['얻은 단서를 원본과 맞춘다. 다음 통로에서도 함께 확인하자.','찾았어요. 이제 기다리는 사람에게 돌아갈 길을 더 잇는 거예요.','길이 열렸다. 숨을 고르고 합류하자.','연결 위치를 확인했습니다. 다음 구역에서도 각자의 신호를 유지합니다.','하나씩 이어지고 있네. 말하지 못한 건 내가 가지고 돌아갈게.','기록도 사람도 놓치지 않았군요. 다음 길을 함께 준비하죠.'][h]
            if act==4 and h==4:mid='긴에게 하고 싶던 말은 내가 가지고 돌아갈게. 지금 내 곁에 있는 너희와 함께.'
            end=['모두의 이름과 귀환을 확인했다. 이 기록은 함께 남긴다.','돌아왔어요. 이제 기다리던 사람들에게 직접 이름을 불러 줘요.','전원 귀환이다. 다음 이야기는 따뜻한 차를 마시면서 하자.','임무와 인원 확인을 마쳤습니다. 돌아온 사람부터 기록하겠습니다.','돌아와서 부를 이름이 많아졌네. 기다리던 사람들한테 먼저 가자.','기록도 사람도 무사하군요. 보고가 끝나면 상처를 제대로 돌봅시다.'][h]
            voices.append({'id':id,'act':act,'departure':starts[act-1][h],'after_mid':mid,'after_boss':end})
    foes={f['id']:f for f in c['foes']};scenes=[]
    for a in story['acts']:
        for key,kind in [('midboss_id','mid'),('boss_id','boss')]:
            id=a[key];f=foes[id];scenes.append({'id':id,'act':a['act'],'intro':[{'speaker':f['name']+'의 기록','text':f['tell']},{'speaker':'쿠카쿠 · 통신','text':'이것은 당시 전투의 재현이다. '+f['counter']}],'outro':[{'speaker':'우라하라 · 통신','text':a['after_mid'] if kind=='mid' else a['after_boss']}]})
    write(DATA/'experience.json',{'schema':1,'clues':[{'id':k,'text':v} for k,v in clues],'voices':voices,'encounters':scenes})
    from expand_campaign import expand
    write(DATA/'campaign.json',expand(c));write(DATA/'story_flow.json',story)
    write(ROOT/'Docs/experience-audit.json',{'baseline':'0.3.0','observed_gaps':['internal clue IDs displayed in dialogue and journal','repeated scenery within each act','town services without visible NPCs','missing midboss introductions and later-act hero reactions','single-pose motion and weak impact','identical troop room pacing','some objectives did not match named encounter rooms'],'requirements':['causal reason and resolution for every dungeon','localized clues and all six voices in all five acts','visible town services and rescue follow-up','at least fifteen distinct combat environments with room layout and hazard differences','hit stop, knockback, stagger, juggle, cancel and distinct effects/audio','normal-health campaign and native UI evidence']})
    if (DATA/'dialogue.json').is_file():
        from prepare_dialogue import main as refresh_dialogue
        refresh_dialogue()
    print(json.dumps({'dungeons':len(c['stages']),'backgrounds':len({s['background'] for s in c['stages']}),'voices':len(voices),'encounter_scenes':len(scenes),'clues':len(clues)}))
if __name__=='__main__':main()
