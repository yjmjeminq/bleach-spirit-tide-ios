using System.Collections;
using System.IO;
using UnityEngine;

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
            yield return TapUi("title-start");
            Check("new story opens the six-lead selection", game.Session.Mode == TideMode.ChooseLead);
            yield return Capture("00-story-roster");
            yield return TapUi("lead-back");
            Check("lead selection can return to title without a quest", game.Session.Mode == TideMode.Title && profile.dialogueHistory.Length == 0);
            yield return TapUi("title-start");

            const string request = "main_a1_accept";
            for (int hero = 0; hero < 6; hero++)
            {
                yield return TapUi("lead-" + hero);
                yield return TapUi("lead-begin");
                Check("first conversation is the live missing-name incident " + hero,
                    game.Session.Mode == TideMode.Story && game.Session.Quest == QuestState.Available
                    && VisibleText(TideDialogue.Lines(request, hero)[0].text));
                if (hero == 0) yield return Capture("00-story-hook");
                var lines = TideDialogue.Lines(request, hero);
                for (int page = 1; page < lines.Length; page++) yield return TapUi("choice-0");
                Check("opening lead answers the incident in character " + hero,
                    game.Session.Profile.hero == hero && VisibleText(lines[lines.Length - 1].text) && DialogueFits());
                yield return TapUi("choice-1");
                Check("canceling first conversation does not accept the quest " + hero,
                    game.Session.Mode == TideMode.ChooseLead && game.Session.Quest == QuestState.Available
                    && TideDialogue.Seen(profile, request) == null);
            }

            yield return TapUi("lead-1");
            yield return TapUi("lead-begin");
            yield return TapUi("choice-0");
            yield return TapUi("story-previous");
            Check("previous opening line keeps the request unaccepted", game.StoryPage == 0 && game.Session.Quest == QuestState.Available);
            yield return ReadStory();
            Check("finishing prologue enters the actual first fight without a town menu",
                game.Session.IsFighting && game.Session.Dungeon.index == 0 && game.Session.RoomIndex == 0
                && game.Session.Quest == QuestState.Active && game.Session.AliveCount > 0);
            Check("field call is visible while movement remains available",
                VisibleText(TideDialogue.Lines("a1_d1_entry", 1)[0].text) && game.Joystick.isActiveAndEnabled);
            yield return Capture("00-story-first-fight");
            string call = TideDialogue.Lines("a1_d1_entry", 1)[0].text;
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
            var saved = TideStorage.Load(Path.Combine(output, "qa-profile.json"), out _);
            Check("opening saves the selected hero and the actual story departure",
                saved.hero == 1 && saved.quest == (int)QuestState.Active
                && TideDialogue.ArchiveHero(saved, request) == 1 && TideDialogue.Seen(saved, "a1_d1_entry") != null);

            game.ReturnTown();
            saved.campaignStage = 2;
            saved.roomCheckpoint = 1;
            saved.clearedDungeons = 1;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(saved), profile);
            game.Session.Mode = TideMode.Title;
            game.Session.Revision++;
            yield return null;
            Check("returning player sees the current lead and destination", VisibleText("이어서 하기")
                && VisibleText(TideCampaign.Dungeon(1, 2).title) && VisibleText("오리히메"));
            string before = JsonUtility.ToJson(profile);
            yield return TapUi("title-start");
            Check("continue opens town without replaying the opening or rewriting progress",
                game.Session.Mode == TideMode.Town && before == JsonUtility.ToJson(profile));
            game.StartBattle(false);
            yield return null;
            Check("continuing a saved room does not replay a departure scene",
                game.Session.IsFighting && game.Session.Dungeon.number == 2 && game.Session.RoomIndex == 1
                && !VisibleText(call));

            JsonUtility.FromJsonOverwrite(fresh, profile);
            game.ReturnTown();
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

        private IEnumerator ReviewFirstLantern(int act)
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
            for (int i = 0; i < 100 && !session.NearInteraction; i++)
                session.Step(.05f, (new Vector2(session.EchoX - session.X, -session.Y)).normalized, false);
            yield return TapUi("field-interact");
            Check("lantern scene follows the real rescue without pausing in act " + act,
                session.IsFighting && !session.EchoAvailable && VisibleText(TideDialogue.Lines(id, session.Profile.hero)[0].text)
                && TideDialogue.Seen(session.Profile, id) != null);
            yield return Capture("51-story-lantern-act" + act);
        }
    }
}
