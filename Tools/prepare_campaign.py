import json
from pathlib import Path

ROOT=Path(__file__).resolve().parent.parent
DESIGN=ROOT.parent/"bleach-spirit-tide-design"
DATA=ROOT/"Assets/SpiritTide/Resources/SpiritTide/Data"


def read(path):return json.loads(path.read_text(encoding="utf-8"))
def save(path,value):
    path.parent.mkdir(parents=True,exist_ok=True)
    path.write_text(json.dumps(value,ensure_ascii=False,indent=2)+"\n",encoding="utf-8")


def brain(enemy):
    role=enemy["role"]
    for words,kind in [(["비행","체공"],"fly"),(["방패","갑주","방어","강철"],"guard"),(["씨앗","촉수","감옥","독","영역","표식","공포","분석","치유","지원","소환"],"zone"),(["세로","화살","발라","사격","투사","영탄","화염","폭발","뇌격"],"ranged"),(["돌진","추격","기동","순보","도약"],"dash")]:
        if any(word in role for word in words):return kind
    return "melee"


def main():
    acts=read(DESIGN/"data/acts.json");enemies=read(DESIGN/"data/enemies.json");bosses=read(DESIGN/"data/bosses.json")
    foe_data=[];stage_data=[];prompts=[]
    for act in acts:
        number=act["act"];group=[e for e in enemies if e["act"]==number]
        assert len(group)==12
        subjects=[]
        for cell,e in enumerate(group):
            subjects.append(f"Cell {cell+1} (row {cell//3+1}, column {cell%3+1}): {e['name']}. Identity and silhouette: {e['appearance']}")
            foe_data.append({"id":e["id"],"name":e["name"],"act":number,"rank":e["rank"],"brain":brain(e),"atlas":"enemies_act_"+str(number),"cell":cell,"base_hp":150 if e["rank"]=="named" else 52,"tell":e["tell"],"counter":e["counter"],"attack":e["attack"]})
        prompt="Use case: stylized-concept. Asset type: transparent 2D anime action RPG enemy sprite atlas. Create ONE production atlas containing exactly 12 isolated full-body BLEACH enemy character cutouts. Layout: an EXACT uniform grid of THREE columns by FOUR rows, no gaps between the invisible equal-sized cells, portrait canvas. Each subject is centered inside its assigned cell with 12 percent clear transparent padding on all edges; the full head, weapons and feet fit in that cell. Every subject faces slightly left in a readable fighting idle pose. Clean inked anime cel shading, recognizable canon faces, masks, outfits and weapons. Canon named characters are their original BLEACH designs, not new original villains. Faction troops follow the described standard uniform or Hollow anatomy. No bikini changes to enemies; preserve original enemy clothing. Monsters retain monster proportions; humanoids have natural adult proportions. True transparent alpha background across the whole sheet, no scenery, no cast shadows, no floor, no text, no labels, no grid lines, no borders, no logos. Keep silhouettes separate without touching other cells. Exact cell order, top row to bottom, left to right:\n"+"\n".join(subjects)
        prompts.append({"id":"enemies_act_"+str(number),"transparent":True,"columns":3,"rows":4,"enemy_ids":[e["id"] for e in group],"prompt":prompt})
        for d in act["dungeons"]:
            troop=d["wave_enemy_ids"]
            rooms=[]
            for wave in range(2):
                count=2+((d["number"]+wave+number)%3)
                rooms.append({"title":f"{d['title']} · 통로 {wave+1}","foes":[troop[(i+wave)%len(troop)] for i in range(count)],"kind":"troops"})
            for encounter in d["named_encounters"]:
                rooms.append({"title":encounter["room"],"foes":[encounter["enemy_id"]],"kind":"named"})
            if d["encounter_id"]:rooms.append({"title":"핵심 기록 · "+d["title"],"foes":[d["encounter_id"]],"kind":"boss"})
            stage_data.append({"index":len(stage_data),"act":number,"number":d["number"],"title":d["title"],"objective":d["objective"],"background":"battle_act_"+str(number),"rooms":rooms,"xp":120+len(stage_data)*16,"gold":80+number*20+d["number"]*5})
    for b in bosses:
        foe_data.append({"id":b["id"],"name":b["name"],"act":b["act"],"rank":b["rank"],"brain":"boss","atlas":"","cell":-1,"base_hp":280 if b["rank"]=="midboss" else 400,"tell":b["phases"][0]["patterns"][0]["tell"],"counter":b["break"],"attack":b["phases"][0]["patterns"][0]["name"]})
    scenes={
        "town":"Karakura, the exterior of Urahara's modest Japanese candy shop, wooden shopfront and striped awning, warm lanterns, quiet night street, a few potted plants, no people",
        "battle_act_1":"Karakura harbor and riverbank at night, distant Japanese houses, harbor cranes and calm black water reflecting teal spirit lights, a far lighthouse",
        "battle_act_2":"Karakura residential and shopping street under a moonlit indigo sky, utility poles and rooftops, a distant black Garganta裂 opening in the sky, blue spirit light",
        "battle_act_3":"Hueco Mundo outside and inside Las Noches, bleached white sand, black night sky, enormous white palace columns and arches, cool violet shadows, a distant crescent moon",
        "battle_act_4":"Fake Karakura battle ground, familiar Japanese urban buildings and broad road damaged by spirit battles, monumental columns and subtle mirrored cracks in the sky, sunset ash and gold",
        "battle_act_5":"Wandenreich Quincy palace, immense austere white stone halls, tall blue-lit windows, silver and blue geometric architecture, shadow portals, cool royal atmosphere"
    }
    for key,scene in scenes.items():
        prompts.append({"id":key,"transparent":False,"prompt":"Use case: stylized-concept. Asset type: wide 2D belt-scrolling action RPG environment background, BLEACH fan-game setting. Scene: "+scene+". Composition: camera at a low slightly elevated side-on angle suitable for a DNF-style brawler, wide horizontal environment, a continuous flat walkable ground plane across the lower 45 percent, background architecture concentrated in the upper half. Ground is spacious and almost empty so sprites and attack warnings can be composited on it. Clean detailed anime background painting, cel-shaded edges, restrained lighting, atmospheric distant layers. No people, no enemies, no UI, no text or lettering, no logos, no watermarks. Landscape 16:9 framing. No foreground objects obstruct the fighting lane."})
    assert len(stage_data)==30 and len(foe_data)==70
    save(DATA/"campaign.json",{"schema":1,"stages":stage_data,"foes":foe_data})
    save(ROOT/"Docs/campaign-art-prompts.json",{"tool":"built-in image_gen","atlas_note":"Each atlas contains 12 named Sprite regions. Unity consumes grid regions directly without modifying the generated PNG.","prompts":prompts})
    if (DATA/"experience.json").is_file():
        from prepare_experience import main as refresh_experience
        refresh_experience()
    print(json.dumps({"stages":len(stage_data),"rooms":sum(len(s["rooms"]) for s in stage_data),"foes":len(foe_data),"atlases":5,"backgrounds":6}))


if __name__=="__main__":main()
