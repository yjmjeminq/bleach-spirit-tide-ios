using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        private IEnumerator ReviewOpening()
        {
            TideProfile profile = game.Session.Profile;
            string fresh = JsonUtility.ToJson(profile);
            Check("fresh player boots at title without an active quest", game.Session.Mode == TideMode.Title && game.Session.Quest == QuestState.Available);
            yield return Capture("00-story-title");
            const string request = "main_a1_accept";
            var lines = TideDialogue.Lines(request, 0);
            yield return TapUi("title-start");
            Check("new story follows Rukia before any companion joins",
                game.Session.Mode == TideMode.Story && profile.hero == 0 && profile.recruitedHeroes == 1
                && !game.Session.SelectHero(1) && VisibleText(lines[0].text));
            yield return Capture("00-story-hook");
            yield return TapUi("choice-2");
            Check("leaving prologue returns to title without accepting a quest",
                game.Session.Mode == TideMode.Title && game.Session.Quest == QuestState.Available
                && TideDialogue.Seen(profile, request) == null);

            yield return TapUi("title-start");
            yield return TapUi("choice-0");
            var dialogue=game.transform.Find("GameCanvas/SafeArea/Dialogue/StoryStage");
            var portrait=dialogue.Find("SpeakerPortrait").GetComponent<Image>();
            var speech=dialogue.Find("DialogueSpeech");
            Check("story composition uses a portrait and preserves the exact speaker and line",
                portrait.sprite!=null&&!portrait.raycastTarget&&speech.Find("DialogueSpeaker").GetComponent<Text>().text==lines[1].speaker&&
                speech.Find("StorySpeech").GetComponent<Text>().text==lines[1].text);
            yield return Capture("00-story-speaking-portrait");
            yield return TapUi("story-previous");
            Check("previous opening line keeps the request unaccepted", game.StoryPage == 0 && game.Session.Quest == QuestState.Available);
            yield return ReadStory();
            Check("finishing prologue enters the actual first fight without a town menu",
                game.Session.IsFighting && game.Session.Dungeon.index == 0 && game.Session.RoomIndex == 0
                && game.Session.Quest == QuestState.Active && game.Session.AliveCount > 0);
            Check("only the starter technique is castable in the first fight",
                game.Buttons["skill-0"].interactable && !game.Buttons["skill-1"].interactable
                && !game.Buttons["skill-3"].interactable && !game.Session.Request(MoveKind.Ultimate));
            Check("locked technique art has a badge and the learned technique stays clear",
                game.Buttons["skill-1"].transform.Find("SkillLocked").gameObject.activeInHierarchy
                && !game.Buttons["skill-0"].transform.Find("SkillLocked").gameObject.activeInHierarchy);
            Check("combat HUD keeps vitals and hides the wallet and inactive field actions",
                VisibleText("루키아 · Lv.1")&&VisibleText(game.Session.EnemyName)&&!VisibleText("800 골드")
                && !game.Buttons["field-interact"].gameObject.activeInHierarchy&&!game.Buttons["gate"].gameObject.activeInHierarchy);
            Check("field call is visible while movement remains available",
                VisibleText(TideDialogue.Lines("a1_d1_entry", 0)[0].text) && game.Joystick.isActiveAndEnabled);
            var fieldCard=game.transform.Find("GameCanvas/SafeArea/FieldDialogue");
            Check("field speech separates the speaker from the line in a nonblocking strip",
                fieldCard.gameObject.activeInHierarchy&&!fieldCard.GetComponent<Image>().raycastTarget&&
                fieldCard.Find("FieldSpeaker").GetComponent<Text>().text==TideDialogue.Lines("a1_d1_entry",0)[0].speaker&&
                !game.transform.Find("GameCanvas/SafeArea/StatusBackdrop").gameObject.activeInHierarchy);
            yield return Capture("00-story-first-fight");
            string call = TideDialogue.Lines("a1_d1_entry", 0)[0].text;
            yield return TapUi("pause");
            float pausedClock = game.Session.PresentationClock;
            yield return null;
            Check("paused field dialogue hides and keeps its timing", game.Session.Mode == TideMode.Pause
                && game.Session.PresentationClock == pausedClock && !VisibleText(call));
            yield return TapUi("resume");
            Check("resuming returns to the same field dialogue", game.Session.IsFighting && VisibleText(call));

            for (int i = 0; i < 7; i++) game.Session.Step(.05f, Vector2.right, false);
            yield return null;
            Check("first field movement advances the control hint", VisibleText("가까이서"));
            Check("field call does not stop the combat clock", game.Session.Clock > 0 && game.Session.X > -3);
            var session = game.Session;
            int limit = 0;
            while (!session.RoomCleared && session.IsFighting && limit++ < 300)
            {
                for (int i = 0; i < 120 && !session.RoomCleared && session.IsFighting; i++)
                    TideCombatDriver.Tick(session, 1f / 60, false);
                yield return null;
            }
            Check("the first field rescue blocks the exit until the evidence is examined",
                session.RoomCleared && session.MainInvestigationPending && !session.GateReady && profile.roomCheckpoint == 0);
            for(int i=0;i<160&&Vector2.Distance(new Vector2(session.X,session.Y),new Vector2(-3,0))>.3f;i++)
                TideQuestDriver.MoveTo(session,new Vector2(-3,0),.05f);
            yield return null;
            var interact=game.Buttons["field-interact"];
            var gate=game.Buttons["gate"];
            Check("distant required evidence displays guidance in the shared action slot",
                interact.gameObject.activeInHierarchy&&!interact.interactable&&!gate.gameObject.activeInHierarchy&&VisibleText("조사 표지로 이동"));
            yield return Capture("00-ui-context-guide");
            yield return InspectMainPoint(true);
            Check("Orihime joins through the rescue and the checkpoint becomes available",
                profile.recruitedHeroes == 3 && profile.roomCheckpoint == 1 && session.GateReady && profile.hero == 0);
            for(int i=0;i<160&&!session.AtGate;i++)
                TideQuestDriver.MoveTo(session,new Vector2(session.GateX,0),.05f);
            yield return null;
            Rect gateBounds=Bounds((RectTransform)gate.transform);
            bool fingerSize=Screen.width!=2556||Screen.height!=1179||gateBounds.height/3>=44&&gateBounds.width/3>=44;
            Check("open gate reuses the investigation's touch target with room for a finger",
                !interact.gameObject.activeInHierarchy&&gate.interactable&&gateBounds==Bounds((RectTransform)interact.transform)&&fingerSize);
            CheckHit("gate");
            yield return Capture("00-ui-context-exit");
            game.ReturnTown();
            yield return TapUi("hero-1");
            Check("only the rescued companion becomes selectable", profile.hero == 1 && !game.Buttons["hero-2"].interactable);
            Check("wallet and save status return in town",VisibleText(profile.gold+" 골드")&&VisibleText("자동 저장"));

            var saved = TideStorage.Load(Path.Combine(output, "qa-profile.json"), out _);
            Check("recruitment and checkpoint survive checksum reload",
                saved.hero == 1 && saved.recruitedHeroes == 3 && saved.roomCheckpoint == 1
                && TideDialogue.ArchiveHero(saved, request) == 0 && TideDialogue.Seen(saved, "a1_d1_entry") != null);
            game.Session.Mode = TideMode.Title;
            game.Session.Revision++;
            yield return null;
            Check("returning player sees the current lead and destination", VisibleText("이어서 하기")
                && VisibleText(TideCampaign.Dungeon(1, 1).title) && VisibleText("오리히메"));
            string before = JsonUtility.ToJson(profile);
            yield return TapUi("title-start");
            Check("continue opens town without replaying the opening or rewriting progress",
                game.Session.Mode == TideMode.Town && before == JsonUtility.ToJson(profile));
            game.Session.Mode = TideMode.Title;
            game.Session.Revision++;
            yield return Capture("00-story-resume");
            yield return TapUi("title-resume-field");
            Check("title resumes the exact saved room without repeating departure",
                game.Session.IsFighting && game.Session.Dungeon.number == 1 && game.Session.RoomIndex == 1
                && !VisibleText(call));

            JsonUtility.FromJsonOverwrite(fresh, profile);
            game.ReturnTown();
        }

        private IEnumerator InspectMainPoint(bool capture = false)
        {
            var session = game.Session;
            Check("actual field evidence is accessible after combat", session.RoomCleared && session.MainInvestigationPending);
            var point = session.QuestPoint(0);
            string scene = point.evidence.scene;
            for (int i = 0; i < 240 && Vector2.Distance(new Vector2(session.X, session.Y), point.position) > .24f; i++)
                TideQuestDriver.MoveTo(session, point.position, .05f);
            if(capture)
            {
                yield return null;
                Check("reachable evidence takes priority over the exit",
                    game.Buttons["field-interact"].interactable&&!game.Buttons["gate"].gameObject.activeInHierarchy);
                yield return Capture("00-ui-context-investigate");
            }
            yield return TapUi("field-interact");
            Check("field interaction reveals the actual scene " + scene,
                session.Mode == TideMode.Story && VisibleText(TideDialogue.Scene(scene).title));
            if (capture) yield return Capture("00-story-first-recruit");
            yield return ReadStory();
        }

        private IEnumerator ReviewResultConversation()
        {
            var session = game.Session;
            string id = TideDialogue.DungeonId(session.Dungeon, "clear");
            Check("ordinary clear gives immediate results " + id,
                session.Mode == TideMode.Result && TideDialogue.Seen(session.Profile, id) == null);
            int gold = session.Profile.gold, next = session.Profile.campaignStage;
            yield return TapUi("result-story");
            Check("ordinary scene can be read beside its actual result " + id,
                session.Mode == TideMode.Story && VisibleText(TideDialogue.Lines(id, session.Profile.hero)[0].text));
            if (session.Dungeon.index == 0)
            {
                yield return TapUi("choice-2");
                Check("leaving optional scene restores its result without recording it",
                    session.Mode == TideMode.Result && TideDialogue.Seen(session.Profile, id) == null);
                yield return TapUi("result-story");
            }
            yield return ReadStory();
            Check("reading ordinary result keeps its reward and next area " + id,
                session.Mode == TideMode.Result && session.Profile.gold == gold && session.Profile.campaignStage == next
                && TideDialogue.Seen(session.Profile, id) != null);
        }

        private IEnumerator ReviewFirstLantern(int act,bool reviewReply=false)
        {
            var session = game.Session;
            string id = TideDialogue.DungeonId(session.Dungeon, "answer");
            int limit = 0;
            while (!session.RoomCleared && session.IsFighting && limit++ < 300)
            {
                for (int i = 0; i < 120 && !session.RoomCleared && session.IsFighting; i++)
                    TideCombatDriver.Tick(session, 1f / 60, false);
                yield return null;
            }
            Check("rescue answer waits for a real lantern interaction in act " + act,
                session.RoomIndex == 0 && session.EchoAvailable
                && !TideDialogue.CanRead(session.Profile, TideDialogue.Scene(id)));
            var lantern=new Vector2(session.EchoX,0);
            for(int i=0;i<140&&Vector2.Distance(new Vector2(session.X,session.Y),lantern)>.3f;i++)
                TideQuestDriver.MoveTo(session,lantern,.05f);
            yield return null;
            Check("the lantern action is reachable in act "+act+" ("+session.Mode+", "+session.X+", "+session.Y+")",
                session.NearInteraction&&game.Buttons["field-interact"].gameObject.activeInHierarchy&&game.Buttons["field-interact"].interactable);
            yield return TapUi("field-interact");
            int hero=session.Profile.hero;
            var lines=TideDialogue.Lines(id,hero);
            Check("lantern scene follows the real rescue without pausing in act " + act,
                session.IsFighting && !session.EchoAvailable && VisibleText(lines[0].text)
                && TideDialogue.Seen(session.Profile, id) != null);
            yield return Capture("51-story-lantern-act" + act);
            if(reviewReply)
            {
                var card=game.transform.Find("GameCanvas/SafeArea/FieldDialogue");
                var speaker=card.Find("FieldSpeaker").GetComponent<Text>();
                var speech=card.Find("FieldSpeech").GetComponent<Text>();
                bool ShowsReply()=>speaker.isActiveAndEnabled&&speech.isActiveAndEnabled&&
                    speaker.text==TideStory.HeroName(hero)&&speech.text==lines[1].text;
                for(int i=0;i<50&&!ShowsReply();i++)
                {
                    for(int step=0;step<9;step++)session.Step(1f/60,Vector2.zero,false);
                    yield return null;
                }
                Check("actual field response has the selected fighter's voice "+hero,
                    ShowsReply()&&TideDialogue.ArchiveHero(session.Profile,id)==hero);
                yield return Capture("51-voice-guide-"+hero);
            }
        }
    }
}
