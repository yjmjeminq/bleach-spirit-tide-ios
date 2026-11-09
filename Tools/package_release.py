"""Package the tested Windows slice and a self-contained Unity source project."""
import hashlib
import json
from pathlib import Path
import shutil
import zipfile

ROOT = Path(__file__).resolve().parent.parent
DESIGN = ROOT.parent / "bleach-spirit-tide-design"


def read(path):
    return json.loads(path.read_text(encoding="utf-8"))


def write(path, value):
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def latest_player(size, after, build, run_mode="full"):
    for path in sorted((ROOT / "BuildLogs").glob("PlayerChecks-*/report.json"), key=lambda p: p.stat().st_mtime, reverse=True):
        data = read(path)
        if data.get("runMode","full")==run_mode and path.stat().st_mtime >= after and data["device"].endswith(size) and data["passed"] and data.get("version")==build["version"] and data.get("guid","").replace("-","").lower()==build["guid"].replace("-","").lower():
            assert not data["errors"] and all(row["passed"] for row in data["checks"])
            return path, data
    raise RuntimeError("No current-build passing " + run_mode + " player report for " + size)


def make_zip(target, pairs):
    with zipfile.ZipFile(target, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
        for path, name in sorted(pairs, key=lambda p: p[1]):
            archive.write(path, name)
    with zipfile.ZipFile(target) as archive:
        assert archive.testzip() is None


def main():
    build_path = ROOT / "BuildLogs" / "windows-build.json"
    build, model = read(build_path), read(ROOT / "BuildLogs" / "model-validation.json")
    assert build["passed"] and build["errors"] == 0 and model["passed"]
    assert all(row["passed"] for row in model["checks"])
    # Both player build targets recreate this boot scene from TideBuilder.cs.
    generated_scene = ROOT / "Assets/SpiritTide/Scenes/Karakura.unity"
    for path in (ROOT / "Assets").rglob("*"):
        if path != generated_scene and path.is_file() and path.suffix in {".cs", ".json", ".unity", ".png", ".otf", ".shader", ".mat"}:
            assert path.stat().st_mtime <= build_path.stat().st_mtime, "Rebuild changed runtime source: " + str(path)
    managed=ROOT/"Builds"/"Windows"/"SpiritTide_Data"/"Managed"/"Assembly-CSharp.dll"
    assert digest(managed)==build["managed_sha256"]
    desktop_path, desktop = latest_player("1560x720", build_path.stat().st_mtime,build)
    phone_path, phone = latest_player("2556x1179", build_path.stat().st_mtime,build)
    desktop_interface_path, desktop_interface = latest_player("1560x720",build_path.stat().st_mtime,build,"interface-only")
    phone_interface_path, phone_interface = latest_player("2556x1179",build_path.stat().st_mtime,build,"interface-only")
    for report in (desktop_interface,phone_interface):
        names={row['name'] for row in report['checks'] if row['passed']}
        assert sum(name.startswith('skill button stays in reach of attack at scale ') for name in names)==10
        assert all(f"actual field response has the selected fighter's voice {hero}" in names for hero in range(6))
        assert 'joystick lights the direction ring and displaces the thumb cap' in names
        assert 'opening a modal clears joystick visuals' in names
        assert 'a skill beside attack casts while the movement finger stays down' in names
    media = read(phone_path.parent / "combat-review-encoded.json")
    assert media["guid"] == build["guid"] and media["decode_passed"]
    assert digest(phone_path.parent / "combat-review.mp4") == media["movie_sha256"]
    assert digest(phone_path.parent / "combat-review.wav") == media["wav_sha256"]
    experience_art = read(ROOT / "Docs" / "experience-art-sources.json")
    assert len(experience_art["files"]) == 13
    for row in experience_art["files"]:
        assert digest(ROOT / row["path"]) == row["sha256"]
    campaign = read(ROOT / "Assets/SpiritTide/Resources/SpiritTide/Data/campaign.json")
    room_count = sum(len(stage["rooms"]) for stage in campaign["stages"])
    assert len(campaign["stages"])==30 and room_count==264
    pace_path=ROOT/'BuildLogs/campaign-pace.json'
    deliberate_path=ROOT/'BuildLogs/campaign-pace-deliberate.json'
    pace,deliberate=read(pace_path),read(deliberate_path)
    assert all(row['rooms']==room_count and row['requests']==20 and row['finalLevel']==31 for row in (pace,deliberate))
    assert 165<=pace['estimatedMinutes']<=200 and deliberate['estimatedMinutes']>pace['estimatedMinutes']
    experience = read(ROOT / "Assets/SpiritTide/Resources/SpiritTide/Data/experience.json")
    assert len({row["background"] for row in campaign["stages"]}) == 15
    assert len(experience["voices"]) == 30 and len(experience["clues"]) == 11 and len(experience["encounters"]) == 10
    dialogue_path = ROOT / "Assets/SpiritTide/Resources/SpiritTide/Data/dialogue.json"
    dialogue, dialogue_index = read(dialogue_path), read(ROOT / "Docs/dialogue-index.json")
    script_path = ROOT / "Docs/character-dialogue-script.txt"
    script = script_path.read_text(encoding="utf-8")
    scene_count = len(dialogue["scenes"])
    assert dialogue_index["version"] == build["version"] and scene_count == 340 and len(dialogue["town"]) == 105
    opening_checks = [
        "fresh player boots at title without an active quest",
        "new story follows Rukia before any companion joins",
        "finishing prologue enters the actual first fight without a town menu",
        "paused field dialogue hides and keeps its timing",
        "resuming returns to the same field dialogue",
        "continue opens town without replaying the opening or rewriting progress",
        "title resumes the exact saved room without repeating departure",
        "only the starter technique is castable in the first fight",
        "Orihime joins through the rescue and the checkpoint becomes available",
        "recruitment and checkpoint survive checksum reload",
        "canceling report keeps the saved act ready",
        "final report naturally leads to the original caller's epilogue",
        "unfinished epilogue is available on continue",
        "epilogue is remembered without repeating the final reward",
        "all completed main companion and actual rescue scenes are navigable",
    ]
    for report in (desktop, phone):
        passed_names = {row["name"] for row in report["checks"] if row["passed"]}
        assert all(name in passed_names for name in opening_checks)
        assert all("boss result opens report directly without awarding it early " + str(act) in passed_names for act in range(1,6))
        assert sum(name.startswith("routine departure keeps live controls ") for name in passed_names) == 14
        assert sum(name.startswith("ordinary clear gives immediate results ") for name in passed_names) == 20
        assert "rescued spirit answers on the field without blocking combat" in passed_names
        assert all("lantern scene follows the real rescue without pausing in act " + str(act) in passed_names for act in range(2,6))
    quest_path = ROOT / "Assets/SpiritTide/Resources/SpiritTide/Data/quests.json"
    quests=read(quest_path);quest_script_path=ROOT/"Docs/quest-script.txt";quest_script=quest_script_path.read_text(encoding="utf-8")
    assert len(quests["main"])==5 and len(quests["side"])==45 and sum(len(q["steps"]) for q in quests["side"])==96
    assert sum(len(q["choices"]) for q in quests["side"])==48
    for main in quests["main"]:
        assert main["title"] in quest_script and main["inspect_title"] in quest_script
        assert len(main['milestones'])==5 and all(point['goal'] in quest_script for point in main['milestones'])
        for step in main["steps"]:
            actual=next(d for d in campaign["stages"] if d["act"]==main["act"] and d["number"]==step["stage"])
            assert step["goal"]==actual["reason"] and step["result"]==actual["resolution"]
    for q in quests["side"]:
        assert q["title"] in quest_script and all(s["goal"] in quest_script for s in q["steps"])
        assert all(value in quest_script for value in q["choices"]+q["choice_notes"]+q["choice_keepsakes"])
    for report in [desktop,phone]:
        names={row["name"] for row in report["checks"] if row["passed"]}
        assert "all forty-five side quests completed through live controls" in names
        assert "live main and side investigation appear together" in names
        assert "main field completion opens route and saves checkpoint" in names
        for check in ["two independent touch pointers move and attack together", "releasing attack finger keeps movement finger active", "another finger cannot release the owned joystick", "opening a modal releases all held touch pointers", "touch pause freezes position and combat clock", "resuming does not revive a released touch"]:
            assert check in names,check
        if report['device'].endswith('2556x1179'):
            assert "iPhone 15 Pro touch controls meet 44 point targets at native 3x scale" in names
        for required in ["town menu opens a service hub", "forge back restores filter sort page and selected gear", "scene list jumps directly to the selected conversation", "quest map highlights its exact destination", "destination primary enters the exact historical room", "previous story line is restored without running completion", "single room visit preserves the newer current checkpoint", "request page and filter survive a return from detail", "automatic hints match the current input layout"]:
            assert required in names, required
    for report in [desktop,phone]:
        names={row['name'] for row in report['checks'] if row['passed']}
        assert 'all thirty distinct playable skills rendered' in names
        assert sum(name.startswith('real skill cast selects its own effect ') for name in names)==30
    skill_fx=read(phone_path.parent/'skill-effects-review.json')
    skill_media=read(phone_path.parent/'skill-effects-review-encoded.json')
    assert skill_fx['passed'] and skill_fx['cast_skills']==30 and skill_fx['guid']==build['guid']
    assert skill_media['guid']==build['guid'] and skill_media['decode_passed'] and skill_media['frames']==432
    assert digest(phone_path.parent/'skill-effects-review.mp4')==skill_media['movie_sha256']
    for report in [desktop,phone]:
        names={row['name'] for row in report['checks'] if row['passed']}
        assert 'all thirty live skill icons and help icons were reviewed' in names
        assert sum(name.startswith('compact skill label renders real glyphs ') for name in names)==30
    icon_review=read(phone_path.parent/'skill-icons-review.json')
    assert icon_review['passed'] and icon_review['guid']==build['guid'] and icon_review['icons']==30
    icon_art=read(ROOT/'Docs/skill-icon-art-sources.json')
    assert icon_art['atlas_files']==6 and icon_art['logical_icons']==30
    for row in icon_art['files']:assert digest(ROOT/row['path'])==row['sha256']
    for report in [desktop,phone]:
        names={row['name'] for row in report['checks'] if row['passed']}
        assert 'journey reviewed forty one story contexts ten practices and five rests' in names
        assert 'live defeat names the real incoming attack' in names
        assert 'preparation back preserves the defeated checkpoint and clock' in names
        assert 'preparation opens the owned upgrade without changing inventory' in names
        assert 'gear comparison returns to the same preparation tab' in names
        assert 'recommended gear equips only after the explicit button' in names
        assert sum(name.startswith('live practice victory preserves rewards and progress ') for name in names)==10
        assert sum(name.startswith('midpoint rest connects a real companion scene and keeps rewards ') for name in names)==5
    journey=read(phone_path.parent/'journey-review.json')
    assert journey['passed'] and journey['guid']==build['guid'] and journey['story_contexts']==41 and journey['boss_practices']==10 and journey['midpoint_rests']==5
    for report in [desktop,phone]:
        names={row['name'] for row in report['checks'] if row['passed']}
        assert 'all six articulated heroes and thirty skill poses reviewed' in names
        assert 'all seventy canonical enemies use continuous motion' in names
        for hero in range(6):
            for stem in ['actual enemy hit triggers the hero recoil ', 'live pause freezes all eighteen bones ', 'holding into the wall stops the gait ', 'live return resets grounded locomotion ']:
                assert stem+str(hero) in names
        assert sum(n.startswith('real skill pose input accepted ') for n in names)==30
        assert all('painted scenery covers the full world at map edge '+str(side) in names and 'pause freezes the follow camera at map edge '+str(side) in names for side in [1,-1])
    motion=read(phone_path.parent/'motion-review.json');motion_media=read(phone_path.parent/'motion-review-encoded.json')
    assert motion['passed'] and motion['guid']==build['guid'] and motion['heroes']==6 and motion['skill_poses']==30 and motion['foes']==70
    assert motion_media['guid']==build['guid'] and motion_media['decode_passed'] and motion_media['frames']==1350
    assert digest(phone_path.parent/'motion-review.mp4')==motion_media['movie_sha256']
    assert digest(phone_path.parent/'motion-trace.json')==motion_media['trace_sha256']
    kinematics=read(ROOT/'BuildLogs/motion-kinematics.json')
    assert len(kinematics['heroes'])==6
    for row in kinematics['heroes']:
        assert row['finite'] and row['frozen'] and row['warp_safe'] and row['actions']==12
        assert row['maximum_foot_error']<.05 and row['maximum_contact_slide']<.05 and row['maximum_limb_error']<.02 and row['framerate_difference']<4 and row['landing_pose_step']<11 and row['cancel_wrist_step']<25 and (row['hero']==3 or row['cancel_weapon_step']<25)
    motion_art=read(ROOT/'Docs/rig-art-sources.json');motion_layers=read(ROOT/'Docs/rig-layer-art-sources.json')
    assert len(motion_art['files'])==6 and len(motion_layers['files'])==8
    for row in motion_art['files']+motion_layers['files']:assert digest(ROOT/row['path'])==row['sha256']
    for scene in dialogue["scenes"]:
        assert "[" + scene["id"] + "]" in script
        for line in scene["lines"]:
            if line["speaker"] != "$lead":
                assert line["speaker"] + ": " + line["text"] in script
        for response in scene["responses"]:
            assert response in script
    for reply in dialogue["town"]:
        assert reply["text"].replace("{next}", "[현재 출정 구역]") in script
    qa = ROOT / "QA"
    qa.mkdir(exist_ok=True)
    assert qa.resolve().parent==ROOT.resolve()
    for legacy_png in qa.glob("*.png"):
        assert legacy_png.resolve().parent==qa.resolve()
        legacy_png.unlink()
    shutil.copy2(build_path, qa / "windows-build.json")
    shutil.copy2(ROOT / "BuildLogs" / "model-validation.json", qa / "model.json")
    shutil.copy2(desktop_path, qa / "player-desktop.json")
    shutil.copy2(phone_path, qa / "player-phone-layout.json")
    shutil.copy2(desktop_interface_path,qa/'player-interface-desktop.json')
    shutil.copy2(phone_interface_path,qa/'player-interface-phone-layout.json')
    shutil.copy2(pace_path,qa/pace_path.name)
    shutil.copy2(deliberate_path,qa/deliberate_path.name)
    for filename in ["01-town.png","03-combat.png","06-equipment.png","07-departure.png","22-large-training.png","30-map-locked.png","31-room-clear.png","33-first-loot.png","35-forge.png","36-forged.png","40-act3-boss6.png","40-act4-boss6.png","40-act5-boss6.png","40-future-preview.png","40-future-locked.png","42-act5-report.png","43-ending-town.png","44-journal.png","45-replay-map.png","46-replay-result.png"]:
        shutil.copy2(phone_path.parent / filename, qa / filename)
    opening_shots = sorted([*phone_path.parent.glob("00-story-*.png"), *phone_path.parent.glob("42-*.png")])
    for shot in opening_shots:
        shutil.copy2(shot, qa / shot.name)
    write(qa / "story-opening-verification.json", {
        "passed": True, "version": build["version"], "guid": build["guid"],
        "checks": opening_checks, "starter": "rukia", "direct_routine_transitions_reviewed": 14,
        "ordinary_results": 20, "field_rescues": 5, "act_reports": 5,
        "screens": {shot.name: digest(shot) for shot in opening_shots},
        "story_data_sha256": digest(dialogue_path), "physical_iphone_tested": False,
        "scope": "Windows live UI and input tests at desktop and iPhone 15 Pro landscape layout.",
    })
    for shot in phone_path.parent.glob("5*.png"):
        shutil.copy2(shot, qa / shot.name)
    for shot in phone_interface_path.parent.glob('51-voice-guide-*.png'):
        shutil.copy2(shot,qa/shot.name)
    for shot in phone_path.parent.glob("6*.png"):
        shutil.copy2(shot, qa / shot.name)
    for shot in phone_path.parent.glob("7*.png"):
        shutil.copy2(shot, qa / shot.name)
    for shot in phone_path.parent.glob("8*.png"):
        shutil.copy2(shot, qa / shot.name)
    for shot in phone_path.parent.glob('9*.png'):
        shutil.copy2(shot,qa/shot.name)
    shutil.copy2(phone_path.parent/'skill-effects-review.mp4',qa/'skill-effects.mp4')
    shutil.copy2(phone_path.parent/'skill-effects-review.wav',qa/'skill-effects.wav')
    write(qa/'skill-effects-media.json',skill_media)
    fx_sources=['Assets/SpiritTide/Scripts/TideSkillFx.cs','Assets/SpiritTide/Scripts/TideSkillFxGraphic.cs','Assets/SpiritTide/Scripts/TideSkillFxGraphic.Heroes.cs','Assets/SpiritTide/Scripts/TideGame.SkillFx.cs','Assets/SpiritTide/Resources/SpiritTide/Vfx/SkillGlow.shader']
    write(qa/'skill-effects-verification.json',{
        'passed':True,'version':build['version'],'guid':build['guid'],'cast_skills_per_layout':30,'hero_motifs':6,'skill_sounds':30,
        'peak_vertices':skill_fx['peak_vertices'],'pool_capacity':skill_fx['pool_capacity'],
        'source_sha256':{path:digest(ROOT/path) for path in fx_sources},'catalog':read(ROOT/'Docs/skill-effects-index.json'),
        'tested':['Thirty real button-triggered casts per layout','Dedicated geometry and sound for every skill','Actual shield, cloud, stance and marked-target state','Nonblocking bounded meshes','Pool reuse and tail cleanup','Reduced effects for all six ultimates','Frozen effect clocks while paused','All six hero ultimates in the timed native showcase'],
        'scope':skill_fx['scope'],'media_scope':skill_media['scope'],'physical_iphone_tested':False
    })
    write(qa/'skill-icons-verification.json',{
        'passed':True,'version':build['version'],'guid':build['guid'],'painted_icons':30,'atlas_files':6,
        'art_manifest':'Docs/skill-icon-art-sources.json','art_sha256':{row['path']:row['sha256'] for row in icon_art['files']},
        'mapping_sha256':digest(ROOT/'Assets/SpiritTide/Resources/SpiritTide/Data/skill_icons.json'),
        'evidence':['QA/92-painted-icons-0.png','QA/92-painted-icons-5.png','QA/92-painted-help-1.png','QA/93-icon-cooldown-and-spirit.png'],
        'tested':['All thirty skill and help buttons show their own painted icon','Compact labels produce actual rendered glyphs','Icon art receives no pointer events','Radial overlay and countdown match the real cooldown','Insufficient spirit dims the art and blocks casts','Button hit areas and safe area remain valid'],
        'scope':icon_review['scope'],'physical_iphone_tested':False
    })
    for shot in phone_path.parent.glob('1*.png'):shutil.copy2(shot,qa/shot.name)
    for name in ['motion-review.mp4','motion-review.wav','motion-trace.json']:shutil.copy2(phone_path.parent/name,qa/name)
    write(qa/'motion-media.json',motion_media)
    motion_sources=['Assets/SpiritTide/Scripts/TideNaturalMotion.cs','Assets/SpiritTide/Scripts/TideRigGraphic.cs','Assets/SpiritTide/Scripts/TideRigData.cs','Assets/SpiritTide/Scripts/TideGame.Motion.cs','Assets/SpiritTide/Scripts/TideFoeMotionGraphic.cs','Assets/SpiritTide/Resources/SpiritTide/Data/motion_rigs.json','Tools/prepare_motion_rigs.py']
    write(qa/'motion-verification.json',{'passed':True,'version':build['version'],'guid':build['guid'],'managed_sha256':build['managed_sha256'],'heroes':6,'skill_poses_per_layout':30,'foes':70,'enemy_anatomies':7,'bones_per_hero':18,'source_sha256':{name:digest(ROOT/name) for name in motion_sources},'kinematics':kinematics,'art_sha256':{row['path']:row['sha256'] for row in motion_art['files']+motion_layers['files']},'native_scope':motion['scope'],'media_scope':motion_media['scope'],'physical_iphone_tested':False})
    journey_sources=['Assets/SpiritTide/Scripts/TideJourney.cs','Assets/SpiritTide/Scripts/TideGame.Journey.cs','Assets/SpiritTide/Scripts/TideSession.cs','Assets/SpiritTide/Scripts/TideSession.Campaign.cs','Assets/SpiritTide/Resources/SpiritTide/Data/journey.json','Tools/prepare_dialogue.py']
    write(qa/'rpg-flow-verification.json',{
        'passed':True,'version':build['version'],'guid':build['guid'],
        'story_contexts_per_layout':41,'boss_practices_per_layout':10,'midpoint_rests_per_layout':5,
        'revised_story_scenes':9,'added_story_lines':13,'dialogue_base_lines':dialogue_index['base_lines'],
        'source_sha256':{path:digest(ROOT/path) for path in journey_sources},
        'references':read(ROOT/'Docs/rpg-reference-sources.json'),
        'tested':['Evidence-only spoiler-aware recap and current checkpoint','Read-only preparation and correct menu return','Unencountered bosses locked in UI and session API','Ten real selected boss practice victories without rewards or resource writes','Actual incoming attack recorded on defeat','Defeat preparation preserves clock and checkpoint','Five voluntary companion rests keep rewards and open town preparation',f'All {scene_count} dialogue scenes and six leads at maximum story font'],
        'evidence':['QA/94-journey-act4.png','QA/95-preparation.png','QA/96-boss-lesson.png','QA/97-midpoint-rest.png','QA/98-defeat-advice.png'],
        'scope':journey['scope'],'physical_iphone_tested':False
    })
    shutil.copy2(phone_path.parent / "combat-review.mp4", qa / "experience-combat.mp4")
    shutil.copy2(phone_path.parent / "combat-review.wav", qa / "experience-combat.wav")
    write(qa / "experience-combat.json", media)
    summary = {
        "passed": True, "unity": build["unity"], "version":build["version"],"build_guid":build["guid"],"managed_sha256":build["managed_sha256"],"build_errors": build["errors"], "build_warnings": build["warnings"],
        "model_checks": len(model["checks"]), "desktop_checks": len(desktop["checks"]), "phone_layout_checks": len(phone["checks"]),
        "full_health_model_dungeons":model["fullHealthDungeons"],"full_health_model_rooms":model["fullHealthRooms"],
        "playable_dungeons":30,"playable_rooms":room_count,"enemy_sprites":70,"playable_heroes":6,"backgrounds":16,"combat_backgrounds":15,
        "causal_dungeon_briefings":30,"hero_act_voice_sets":30,"localized_clues":11,"encounter_scenes":10,
        "town_npcs":5,"formations":6,"environment_types":5,
        "ambush_rooms":sum(bool(room.get('reinforcement') or len(room.get('wave_sizes',[]))>1) for stage in campaign['stages'] for room in stage['rooms']),
        "maximum_font_npc_dialogues_per_layout":600,"combat_review_frames":media["frames"],"combat_review_sound_cues":media["sound_cues"],
        "dialogue_scenes":scene_count,"main_story_scenes":110,"companion_scenes":30,"rescue_scenes":5,"field_rescue_scenes":5,"epilogues":1,"side_quest_scenes":189,
        "main_chapter_quests":5,"main_investigations":30,"side_quests":45,"side_objectives":96,"side_report_outcomes":48,
        "modeled_campaign_minutes":[round(pace['estimatedMinutes'],1),round(deliberate['estimatedMinutes'],1)],
        "dialogue_base_lines":dialogue_index["base_lines"],"dialogue_lead_responses":dialogue_index["lead_responses"],"town_reply_states":105,
        "maximum_font_resolved_scene_variants_per_layout":scene_count*6,
        "story_act_links_reviewed":5,
        "tested":["six input-only normal-health campaigns","one native five-act campaign per layout","70 reachable foe IDs","troop and named separation","room gate and saved checkpoint","defeat and retry","one-time reports and loot","reduced replay rewards","real XP and gear upgrades","legacy saves and corrupt backup recovery","30 skill explanations","UI pointer targets and text bounds","future preview and lock visuals","all 15 combat settings rendered","10 boss introductions and resolutions","600 maximum-font NPC conversations per layout","all 30 journal pages","visible town NPC raycast","optional rescue reward and replay recovery","environment warning and real ward deactivation","skill cancel, hit stop, knockback, launch and poise break","Ulquiorra three-phase behavior","fixed-step input replay with effect-audio cues"],
        "player_sha256": digest(ROOT / "Builds" / "Windows" / "SpiritTide.exe"),
        "render_scope": "Live uGUI canvas rendered through the Unity camera to a texture; pointer-event simulation in Windows Direct3D11.",
        "phone_scope": "2556x1179 landscape with example safe-area insets. Not an iPhone OS/device run.",
        "implemented": f"Rukia opening, five companion recruitments, level-gated techniques and Act 3 awakening. Thirty main investigations, forty-five optional requests and forty-eight report outcomes. {scene_count} dialogue scenes with a gated archive, tracked goals and training. Reinforcement signals, room visits and saved field resume. Six articulated heroes and seventy responsive enemies.",
        "review_video_scope": media["scope"] + "; " + media["audio_scope"],
        "pending": ["production VFX and audio", "native iPhone touch and Metal/performance checks"],
        "ipa_deferred": False,
        "motion": {"articulated_heroes":6,"bones_per_hero":18,"skill_poses_per_layout":30,"responsive_foes":70,"enemy_anatomies":7,"movie_frames":1350,"movie_fps":30,"maximum_foot_error":max(r["maximum_foot_error"] for r in kinematics["heroes"])},
        "rpg_flow": {"story_contexts_per_layout":41,"boss_practices_per_layout":10,"midpoint_rests_per_layout":5,"revised_story_scenes":9,"added_story_lines":13,"recap_spoiler_gated":True,"practice_resources_preserved":True},
        "skill_icons": {"painted_icons":30,"atlas_files":6,"tool":"built-in image_gen","radial_cooldowns":True,"icon_help":True},
        "skill_effects": {"dedicated_skills":30,"hero_motifs":6,"sounds":30,"showcase_frames":432,"showcase_fps":30,"scope":"Unity mesh effects and synthesized game sounds; not iPhone hardware performance"},
    }
    ios_report=ROOT/"Artifacts"/("SpiritTide-ios-package-verification-v"+build["version"]+".json")
    if ios_report.exists():
        ios=read(ios_report);ipa=ROOT/"Artifacts"/ios["ipa"]
        assert ios["passed"] and ios["app"]["version"]==build["version"] and digest(ipa)==ios["sha256"]
        assert ios["app"]["unsigned"] and not ios["deviceLaunched"]
        state=read(ROOT/f".runtime/ios-build-state-v{build['version']}.json")
        export=read(ROOT/"BuildLogs/ios-export.json")
        assert export["passed"] and export["version"]==build["version"]
        summary["ios"]={"version":ios["app"]["version"],"build":ios["app"]["build"],"minimum_ios":ios["app"]["minimumIOS"],"ipa":ipa.name,"bytes":ipa.stat().st_size,"sha256":ios["sha256"],"unsigned":True,"physical_iphone_tested":False,"source_revision":state["revision"],"run_url":state["runURL"],"unity_export_guid":export["guid"]}
        shutil.copy2(ios_report,qa/"ios-verification.json")
        shutil.copy2(ROOT/"Artifacts"/("SpiritTide-ios-build-report-v"+build["version"]+".json"),qa/"ios-build-report.json")
        shutil.copy2(ROOT/"Artifacts"/("SpiritTide-ios-input-verification-v"+build["version"]+".json"),qa/"ios-input-verification.json")
        shutil.copy2(ROOT/"BuildLogs/ios-export.json",qa/"ios-export.json")
    write(qa / "verification.json", summary)
    dialogue_qa = {
        "passed": True, "version": build["version"], "guid": build["guid"],
        "source": "Tools/prepare_dialogue.py", "runtime_sha256": digest(dialogue_path), "script_sha256": digest(script_path),
        "scenes": scene_count, "fixed_lines": dialogue_index["base_lines"], "lead_responses": dialogue_index["lead_responses"], "town_replies": 105,
        "checks": {"model":len(model["checks"]),"desktop":len(desktop["checks"]),"phone_layout":len(phone["checks"])},
        "evidence": ["Every scene resolves for all six leads and has multiple speakers", f"All {scene_count*6} resolved scene variants fit maximum story and archive fonts", "Native campaign records all 110 main scenes", "Real optional conversations for all 30 act/hero combinations", "151 completed core scenes and 186 played side scenes navigated in archive", "First-view lead survives character switch and storage reload", "Old ending saves restore 141 scenes with no invented side progress", "Unplayed acts and unchosen personal outcomes stay hidden", "Archive reads do not change rewards or progress", "Abandon skip and duplicate-click behavior verified", "Script book contains every authored runtime line and response"],
        "scope": "Original Korean writing and Windows live UI/pointer checks at two resolutions. No voice acting or iPhone device execution.",
    }
    write(qa / "dialogue-verification.json",dialogue_qa)
    write(qa/"quest-verification.json",{
        "passed":True,"version":build["version"],"guid":build["guid"],"runtime_sha256":digest(quest_path),"script_sha256":digest(quest_script_path),
        "main_chapters":5,"main_dungeon_stages":30,"required_field_investigations":30,"optional_quests":45,"optional_steps":96,"report_outcomes":48,
        "evidence":["Six normal-health full campaigns include mandatory field investigations", "Main gates and checkpoints cannot skip unexamined clues", "All forty-eight side outcomes exercised with real objectives and one-time rewards in model", "All forty-five side quests accepted performed and reported with live controls per layout", "Actual training cancel and perfect dodge against real attack telegraphs", "Main and side objectives coexist in an ongoing Act 2 fixture", "Single-room visits leave main completion and first-clear bits unchanged", "Chosen aftermath appears in the quest book and town memory", "Checksum reload preserves quest step report choice and payout flag", "All 186 played side conversations available in archive"],
        "scope":"Windows pointer and rendering checks at two resolutions; normal campaign plus endgame catch-up and ongoing-campaign fixtures. Not iPhone hardware validation.",
    })
    ui_evidence={}
    for layout,report in [("desktop",desktop),("phone_layout",phone)]:
        ui_evidence[layout]=[row["name"] for row in report["checks"] if any(term in row["name"] for term in ["service hub", "restores home", "filter", "gear comparison", "forge back", "auto equips", "scene list", "scene index", "quest map", "map back", "previewing a route", "story line", "destination primary", "ward route", "route explains", "hint", "truthful indicator", "new loot", "touch", "finger", "44 point"]) ]
    write(qa/"ui-verification.json",{
        "passed":True,"version":build["version"],"guid":build["guid"],"managed_sha256":build["managed_sha256"],
        "checks":ui_evidence,"interface_checks":{"desktop":len(desktop_interface['checks']),"phone_layout":len(phone_interface['checks'])},
        "evidence":["QA/00-story-first-fight.png","QA/86-combat-dock-active.png","QA/80-home.png","QA/81-gear-comparison.png","QA/82-dialogue-scene-list.png","QA/83-quest-destination.png","QA/84-story-previous.png","QA/85-touch-controls.png"],
        "scope":"Windows Direct3D11 live uGUI rendering and simulated pointer events, 1560x720 and 2556x1179 with example safe insets. Not iPhone hardware or iOS execution."
    })
    audit_path = ROOT / "Docs" / "experience-audit.json"
    audit = read(audit_path)
    audit.update({
        "verified_version": build["version"], "build_guid": build["guid"],
        "checks": {"model": len(model["checks"]), "desktop": len(desktop["checks"]), "phone_layout": len(phone["checks"])},
        "review_results": {
            "story": "Thirty causal dungeon beats, localized clues, five act links, ten encounter introductions/resolutions and final returned-names journal verified.",
            "town": "Five visible NPC services, physical NPC pointer hit, six hundred maximum-font dialogue cases per layout, rescue follow-up and five report lights verified.",
            "maps": "Fifteen combat settings rendered, six formations and five hazards validated, two-wave ambush and actual ward deactivation verified.",
            "combat": "Connected cancel, frozen simulation during hit stop, actual knockback, launch/air follow-up and boss poise break verified; normal-health replay and full campaigns passed.",
        },
        "evidence": ["QA/model.json", "QA/player-desktop.json", "QA/player-phone-layout.json", "QA/experience-combat.mp4", "Docs/experience-review.md"],
        "remaining": summary["pending"],
    })
    write(audit_path, audit)
    prompts = {row["id"]: row["prompt"] for row in read(DESIGN / "prompts.json")["prompts"]}
    records = []
    for row in read(DESIGN / "asset_manifest.json")["files"]:
        if not row["active"]:
            continue
        image = ROOT / "Assets" / "SpiritTide" / "Resources" / "SpiritTide" / "Art" / Path(row["path"]).name
        assert digest(image) == row["sha256"]
        records.append({"path": image.relative_to(ROOT).as_posix(), "sha256": row["sha256"], "tool": "built-in image_gen", "prompt": prompts[image.stem], "status": "Original source preserved; current motion uses articulated hero rigs or continuous enemy surfaces"})
    assert len(records) == 16
    campaign_art=read(ROOT/"Docs"/"campaign-art-sources.json")
    assert len(campaign_art["files"])==11
    for row in campaign_art["files"]:assert digest(ROOT/row["path"])==row["sha256"]
    write(ROOT / "Docs" / "art-sources.json", {"selected_pngs": 16, "revision": "adult-bikini-v2 / canon-bosses-v2", "files": records})
    shutil.copy2(DESIGN / "canon_sources.json", ROOT / "Docs" / "canon-sources.json")
    artifacts = ROOT / "Artifacts"
    artifacts.mkdir(exist_ok=True)
    source = artifacts / ("SpiritTide-Unity-"+build["version"]+".zip")
    windows = artifacts / ("SpiritTide-Windows-"+build["version"]+".zip")
    source_files = [(ROOT / "README.md", "bleach-spirit-tide-unity/README.md")]
    for folder in ["Assets", "Packages", "ProjectSettings", "Tools", "Docs", "QA", ".github"]:
        for path in (ROOT / folder).rglob("*"):
            if path.is_file() and "__pycache__" not in path.parts and path.suffix != ".pyc":
                source_files.append((path, "bleach-spirit-tide-unity/" + path.relative_to(ROOT).as_posix()))
    source_files.append((ROOT / ".gitignore", "bleach-spirit-tide-unity/.gitignore"))
    make_zip(source, source_files)
    windows_files = []
    for path in (ROOT / "Builds" / "Windows").rglob("*"):
        if path.is_file() and "BackUpThisFolder" not in str(path) and path.suffix not in {".pdb", ".mdb"}:
            windows_files.append((path, "SpiritTide-Windows/" + path.relative_to(ROOT / "Builds" / "Windows").as_posix()))
    notice = artifacts / "PLAY.txt"
    notice.write_text("BLEACH: 영해의 균열 · 캠페인 검증본 "+build["version"]+"\n\nZIP 전체를 압축 해제한 다음 SpiritTide.exe를 여세요.\n방향키 이동 / Z 공격 / X 모으기 / C 점프 / Space 회피\nA S D F 기술 / R 각성기 / V 회복 / E 대화·장치·다음 방 / H 도움말\n메뉴: 방향키·Tab 이동 / Enter 확인 / Esc 뒤로\n\n우라하라 의뢰 → 쿠카쿠의 균열 지도 → 6개 던전 → 액트 보고 → 다음 액트.\n방 정화 뒤 오른쪽 출구로 이동해 다음 방을 누르세요.\n장비 비교·강화는 텟사이에서, 겹친 적 선택은 대상 변경 버튼을 사용하세요.\n귀환 등불을 두 곳 안내하고 콘에게 구조 소식을 보고하세요.\n환경 예고를 피하거나 영압 장치를 끄세요.\n기본 공격 명중 직후 기술로 연계하고, 강공격·반격으로 보스 자세를 무너뜨릴 수 있습니다.\n\n5액트 30던전 114방 · 적 70종 · 전투 배경 15종 · 파티 성장·장비 강화.\n여섯 캐릭터는 관절 기반 연속 동작, 실제 지면의 발 고정과 연결 공격을 사용합니다. 아이폰용 IPA는 별도 배포 파일을 사용하세요.\n",encoding="utf-8-sig")
    with notice.open("a",encoding="utf-8") as paper:
        paper.write("\n대화록: 뱌쿠야 → 조사 기록 확인 → 대화록.\n동료 대화: 현재 액트 의뢰 수락 후 콘 → 동료와 이야기.\n전체 대본은 character-dialogue-script.txt에 있으며 결말을 포함합니다.\n")
        paper.write("\n빠른 메뉴: 마을의 메뉴에서 의뢰·장비·지도·기록·보급·설정을 직접 엽니다.\n부탁의 목적지 보기 → 표시한 목적지로 출정. 장비는 슬롯 필터·정렬·착용 수치 차이를 확인하세요.\n진행 대화는 이전 문장, 대화록은 장면 목록을 사용할 수 있습니다.\n키 안내는 설정에서 자동·표시·숨김을 선택합니다.\n")
        paper.write("\n메인·서브 의뢰: 마을의 의뢰 수첩 또는 Q, NPC 대화창 오른쪽 위의 부탁.\n각 액트 네 번째 던전 첫 통로는 전투 후 주요 표지를 가까이서 살펴 다음 길로 갑니다.\n서브퀘스트는 선택 사항이며 지나온 지역에서도 수행할 수 있습니다.\n수련 공격 예고: 왼쪽 버튼 또는 T. 정밀 회피 수련 의뢰 중에는 자동으로 켜집니다.\n메인·서브 퀘스트 전체 대본은 quest-script.txt에 있습니다.\n")
    windows_files.append((notice, "SpiritTide-Windows/PLAY.txt"))
    windows_files.append((script_path, "SpiritTide-Windows/character-dialogue-script.txt"))
    windows_files.append((quest_script_path,"SpiritTide-Windows/quest-script.txt"))
    windows_files.append((qa / "verification.json", "SpiritTide-Windows/verification.json"))
    make_zip(windows, windows_files)
    outputs=[source,windows]
    if "ios" in summary:outputs.append(ROOT/"Artifacts"/summary["ios"]["ipa"])
    release = {"files": [{"name": p.name, "bytes": p.stat().st_size, "sha256": digest(p)} for p in outputs], "qa": summary}
    write(artifacts / "release.json", release)
    write(artifacts / ("release-"+build["version"]+".json"),release)
    print(json.dumps({"model_checks": summary["model_checks"], "player_checks": [summary["desktop_checks"], summary["phone_layout_checks"]], "artifacts": [{"name": p.name, "bytes": p.stat().st_size} for p in [source, windows]]}))


if __name__ == "__main__":
    main()
