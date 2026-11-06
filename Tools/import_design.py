import hashlib
import json
from pathlib import Path
import shutil

ROOT=Path(__file__).resolve().parent.parent
DESIGN=ROOT.parent/"bleach-spirit-tide-design"
RES=ROOT/"Assets"/"SpiritTide"/"Resources"/"SpiritTide"
ART=RES/"Art"
DATA=RES/"Data"


def main():
    ART.mkdir(parents=True,exist_ok=True)
    DATA.mkdir(parents=True,exist_ok=True)
    manifest=json.loads((DESIGN/"asset_manifest.json").read_text(encoding="utf-8"))
    records=[]
    for entry in manifest["files"]:
        if not entry["active"]:
            continue
        src=DESIGN/entry["path"]
        assert hashlib.sha256(src.read_bytes()).hexdigest()==entry["sha256"]
        target=ART/src.name
        shutil.copy2(src,target)
        records.append({"source":entry["path"],"target":target.relative_to(ROOT).as_posix(),"sha256":entry["sha256"]})
    for kind in ["characters","bosses","enemies","acts"]:
        value=json.loads((DESIGN/"data"/(kind+".json")).read_text(encoding="utf-8"))
        (DATA/(kind+".json")).write_text(json.dumps({kind:value},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    shutil.copy2(DESIGN/"data"/"story_flow.json",DATA/"story_flow.json")
    if (DATA/"experience.json").is_file():
        from prepare_experience import main as refresh_experience
        refresh_experience()
    font_root=ROOT.parent/"rift-heroines-unity"/"Assets"/"Rift"/"Resources"/"Rift"/"UI"
    font_target=RES/"UI"
    font_target.mkdir(exist_ok=True)
    for name in ["Pretendard-Regular.otf","Pretendard-SemiBold.otf","Pretendard-LICENSE.txt"]:
        if (font_root/name).is_file():
            shutil.copy2(font_root/name,font_target/name)
        elif not (font_target/name).is_file():
            raise FileNotFoundError("Bundled font missing: "+name)
    (ROOT/"BuildLogs").mkdir(exist_ok=True)
    (ROOT/"BuildLogs"/"design-import.json").write_text(json.dumps({"selected_pngs":len(records),"records":records,"character_art_revision":"adult-bikini-v2","boss_art_revision":"canon-v2","sprite_frames_generated":False},ensure_ascii=False,indent=2)+"\n",encoding="utf-8")
    print("Imported",len(records),"selected PNGs, full design data and licensed Korean fonts.")


if __name__=="__main__":
    main()
