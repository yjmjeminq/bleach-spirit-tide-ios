using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private static readonly Vector2 Center = new Vector2(.5f, .5f);
        private Image openingBackdrop;
        private int openingLead;
        private StoryLine[] fieldLines;
        private int fieldPage, fieldEpoch, openingRoomEpoch, firstAttack;
        private float fieldNext;
        private bool firstMove;

        private bool FreshCampaign => Session.Quest == QuestState.Available
            && Session.Profile.campaignAct == 1 && Session.Profile.campaignStage == 1
            && Session.Profile.clearedDungeons == 0 && Session.Profile.sideQuests.Length == 0;

        private bool UnreadEnding => Session.CampaignFinished
            && TideDialogue.Seen(Session.Profile, "a5_report") != null
            && TideDialogue.Seen(Session.Profile, "a5_epilogue") == null;

        private bool FirstLanternInSight => Session.IsFighting && !Session.RoomCleared
            && openingRoomEpoch == Session.PresentationEpoch && (Session.Profile.rescuedDungeons & 1) == 0;

        private Image SceneBackground(string name, Transform parent, string background)
        {
            var picture = Stretch(name, parent).gameObject.AddComponent<Image>();
            picture.sprite = TideArt.Background(background);
            picture.color = picture.sprite != null ? Color.white : bg;
            picture.raycastTarget = false;
            return picture;
        }

        private void SetOpeningMode(TideMode mode)
        {
            ResetInput();
            Session.Mode = mode;
            Session.Revision++;
            Refresh();
        }

        private void ContinueFromTitle()
        {
            if (Session.Mode != TideMode.Title) return;
            if (FreshCampaign)
            {
                openingLead = Session.Profile.hero;
                SetOpeningMode(TideMode.ChooseLead);
                return;
            }

            Session.Mode = TideMode.Town;
            Session.Revision++;
            if (UnreadEnding)
                ShowDialogueScene("a5_epilogue", ReturnTown, "상점으로");
            else
                Refresh();
        }

        private void StartOpening()
        {
            if (Session.Mode != TideMode.ChooseLead || !FreshCampaign) return;
            Session.SelectHero(openingLead);
            ShowDialogueScene(TideQuests.Main(1).accept_scene, () =>
            {
                if (!Session.AcceptQuest()) { ReturnTown(); return; }
                if (BeginDungeonNow(1, 1, false))
                    StartFieldScene("a1_d1_entry");
            }, "강변으로", () => SetOpeningMode(TideMode.ChooseLead), "선두 선택");
        }

        private void BuildTitle()
        {
            Clear(panel);
            Label("BLEACH", panel, Center, new Vector2(-280, 141), new Vector2(422, 44), 32, gold, true);
            Label("영해의 균열", panel, Center, new Vector2(-170, 66), new Vector2(642, 104), 74, ink, true);
            Picture("TitleRule", panel, Center, new Vector2(-370, 3), new Vector2(240, 3), gold);
            string note = FreshCampaign ? "카라쿠라 · 해 질 무렵\n평소라면 대답이 돌아올 시간이었다."
                : Session.CampaignFinished ? "다섯 균열의 조사를 마쳤습니다."
                : "ACT " + Session.Profile.campaignAct + "  ·  " + TideStory.HeroName(Session.Profile.hero) + "\n"
                    + (Session.Quest == QuestState.Ready ? "우라하라 상점으로 돌아갈 때" : TideJourney.Current(Session.Profile).title);
            Label(note, panel, Center, new Vector2(-183, -58), new Vector2(616, 96), 26, muted);
            Button("title-start", FreshCampaign ? "이야기 시작" : UnreadEnding ? "후일담 이어보기" : "이어서 하기",
                panel, Center, new Vector2(-313, -165), new Vector2(360, 86), ContinueFromTitle, true);
            if (!string.IsNullOrEmpty(lastSaveNotice))
                Label(lastSaveNotice, panel, Center, new Vector2(-128, -240), new Vector2(730, 52), 20, gold);
        }

        private void BuildLeadSelection()
        {
            Clear(panel);
            Label("누가 강변으로 갈까요?", panel, Center, new Vector2(-40, 220), new Vector2(870, 65), 42, ink, true);
            Label("한 사람은 현장으로. 다른 동료들은 통신을 맡습니다.", panel, Center,
                new Vector2(-8, 159), new Vector2(934, 48), 24, muted);

            for (int i = 0; i < 6; i++)
            {
                int hero = i;
                bool active = i == openingLead;
                var tile = Button("lead-" + i, "", panel, Center, new Vector2((i - 2.5f) * 161, -8),
                    new Vector2(148, 244), () => { openingLead = hero; Refresh(); }, active);
                var portrait = Picture("LeadPortrait", tile.transform, Center, new Vector2(0, 25), new Vector2(142, 184),
                    active ? Color.white : new Color(.7f, .78f, .82f), heroSprites[i]);
                portrait.preserveAspect = true;
                var name = Label(TideStory.HeroName(i), tile.transform, Center, new Vector2(0, -97),
                    new Vector2(142, 44), 26, active ? gold : ink, true);
                name.alignment = TextAnchor.MiddleCenter;
            }

            var lead = TideCatalog.Characters[openingLead];
            Label(TideStory.HeroName(openingLead) + "  ·  " + lead.role + "\n" + lead.feel,
                panel, Center, new Vector2(-4, -166), new Vector2(950, 65), 23);
            Button("lead-back", "타이틀로", panel, Center, new Vector2(-302, -237), new Vector2(372, 84),
                () => SetOpeningMode(TideMode.Title));
            Button("lead-begin", TideStory.HeroName(openingLead) + "로 시작", panel, Center, new Vector2(260, -237),
                new Vector2(460, 84), StartOpening, true);
        }

        private void BuildStory()
        {
            Clear(dialogueRoot.transform);
            var scene = TideDialogue.Scene(storyRecordId);
            int page = storyPage, epoch = storyEpoch;
            bool last = page == storyLines.Length - 1;
            StoryLine line = storyLines[page];
            bool field = Session.InBattle || scene.kind == "entry" || scene.kind == "intro" || scene.kind == "clear";
            var dungeon = TideCampaign.Dungeon(scene.act, scene.stage);
            SceneBackground("StorySetting", dialogueRoot.transform, field ? dungeon.background : "town");
            var shade = Stretch("StoryShade", dialogueRoot.transform).gameObject.AddComponent<Image>();
            shade.color = new Color(.022f, .06f, .09f, .55f);
            shade.raycastTarget = false;

            var stage = Node("StoryStage", dialogueRoot.transform, Center, Vector2.zero, new Vector2(1060, 580));
            Label("ACT " + scene.act + "   /   " + (field ? dungeon.title : scene.place), stage, Center,
                new Vector2(-20, 237), new Vector2(980, 42), 22, gold, true);
            Label(scene.title, stage, Center, new Vector2(-20, 188), new Vector2(980, 64), 43, ink, true);
            Picture("StoryRule", stage, Center, new Vector2(0, 146), new Vector2(1020, 2), new Color(.83f, .69f, .44f, .65f));

            Sprite portrait = StoryPortrait(line.speaker, dungeon, scene.kind == "intro");
            if (portrait != null)
                Picture("SpeakerPortrait", stage, Center, new Vector2(-372, -28), new Vector2(260, 315), Color.white, portrait).preserveAspect = true;

            var speech = Picture("Speech", stage, Center, new Vector2(127, -29), new Vector2(764, 278), new Color(.024f, .077f, .104f, .97f));
            Picture("SpeakerAccent", speech.transform, Center, new Vector2(-380, 0), new Vector2(4, 278), gold);
            string speaker = line.speaker;
            if (field)
                for (int i = 0; i < heroSprites.Length; i++)
                    if (i != storyHero && speaker == TideStory.HeroName(i))
                    {
                        speaker += "  ·  동료 통신";
                        break;
                    }
            if (scene.kind.StartsWith("side_", StringComparison.Ordinal) && (scene.act < Session.Profile.campaignAct || Session.Profile.claimed))
                speaker = "지난 부탁  /  " + speaker;
            Label(speaker, speech.transform, Center, new Vector2(-68, 97), new Vector2(572, 52), 28, gold, true);
            var count = Label((page + 1) + " / " + storyLines.Length, speech.transform, Center, new Vector2(312, 97),
                new Vector2(84, 48), 22, muted);
            count.alignment = TextAnchor.MiddleRight;
            var body = Label(line.text, speech.transform, Center, new Vector2(0, -20), new Vector2(704, 180),
                Mathf.RoundToInt(32 * Session.Profile.uiScale));
            body.name = "StorySpeech";
            body.alignment = TextAnchor.MiddleLeft;

            Button("story-previous", "이전 문장", stage, Center, new Vector2(-413, -232), new Vector2(192, 84), () =>
            {
                if (epoch != storyEpoch || Session.Mode != TideMode.Story || storyPage != page || page <= 0) return;
                storyPage--;
                Session.Revision++;
                Refresh();
            }).interactable = page > 0;
            if (!last)
            {
                string skip = scene.kind == "report" ? "건너뛰고 보고" : storyEndLabel == "강변으로" ? "바로 강변으로"
                    : scene.kind == "main_accept" ? "건너뛰고 수락" : "장면 건너뛰기";
                Button("choice-1", skip, stage, Center, new Vector2(-172, -232), new Vector2(264, 84),
                    () => { if (epoch == storyEpoch) FinishStory(); });
            }
            Button(last ? "choice-1" : "choice-2", storyCancelLabel, stage, Center, new Vector2(79, -232),
                new Vector2(214, 84), CancelStory);
            Button("choice-0", last ? storyEndLabel : "계속", stage, Center, new Vector2(355, -232), new Vector2(312, 84),
                () => { if (epoch == storyEpoch) AdvanceStory(page); }, true);
        }

        private Sprite StoryPortrait(string speaker, TideDungeonDef dungeon, bool encounter)
        {
            for (int i = 0; i < heroSprites.Length; i++)
                if (speaker == TideStory.HeroName(i)) return heroSprites[i];
            switch (speaker)
            {
                case "우라하라": return TideArt.Grid("town_npcs_v1", 0, 3, 2);
                case "텟사이": return TideArt.Grid("town_npcs_v1", 1, 3, 2);
                case "콘": return TideArt.Grid("town_npcs_v1", 2, 3, 2);
                case "뱌쿠야": return TideArt.Grid("town_npcs_v1", 3, 3, 2);
                case "쿠카쿠": return TideArt.Grid("town_npcs_v1", 4, 3, 2);
                case "정령": return TideArt.Grid("town_npcs_v1", 5, 3, 2);
                default: return encounter && speaker.EndsWith("의 기록", StringComparison.Ordinal)
                    ? TideArt.Enemy(dungeon.rooms[dungeon.rooms.Length - 1].foes[0]) : null;
            }
        }

        private void BeginActReport()
        {
            if (Session.Quest != QuestState.Ready || Session.InBattle) return;
            int act = Session.Profile.campaignAct;
            ShowDialogueScene("a" + act + "_report", () =>
            {
                if (!Session.ClaimReward()) { ReturnTown(); return; }
                Persist();
                if (act == 5) ShowDialogueScene("a5_epilogue", ReturnTown, "상점으로");
                else ReturnTown();
            }, act == 5 ? "차 한잔" : "보고 마치기", cancelLabel: "나중에 보고");
        }

        private void ReadResultStory()
        {
            if (Session.Mode != TideMode.Result) return;
            void Restore()
            {
                Session.Mode = TideMode.Result;
                Session.Revision++;
                Refresh();
            }
            ShowDialogueScene(TideDialogue.DungeonId(Session.Dungeon, "clear"), Restore, "결과로", Restore, "결과로");
        }

        private void PrepareFieldStory()
        {
            fieldLines = null;
            openingRoomEpoch = !Session.IsReplay && Session.Dungeon.index == 0 && Session.RoomIndex == 0
                ? Session.PresentationEpoch : -1;
            firstMove = false;
            firstAttack = Session.AttackSerial;
        }

        private void StartFieldScene(string id)
        {
            fieldLines = TideDialogue.Lines(id, Session.Profile.hero);
            fieldPage = 0;
            fieldEpoch = Session.PresentationEpoch;
            fieldNext = Session.PresentationClock + FieldLineTime(fieldLines[0]);
            TideDialogue.Remember(Session.Profile, id, Session.Profile.hero);
            Persist();
        }

        private static float FieldLineTime(StoryLine line) => Mathf.Clamp(2f + line.text.Length * .075f, 3.4f, 5.7f);

        private void InteractField()
        {
            int rescued = Session.Profile.rescuedDungeons;
            if (!Session.InteractWorld()) return;
            if (Session.IsCampaign && Session.Dungeon.number == 1 && rescued != Session.Profile.rescuedDungeons)
                StartFieldScene(TideDialogue.DungeonId(Session.Dungeon, "answer"));
            else
                Persist();
            Refresh();
        }

        private string FieldStatus()
        {
            if (!Session.IsFighting || !Session.IsCampaign) return Session.Status;
            if (fieldLines != null && fieldEpoch == Session.PresentationEpoch)
            {
                if (Session.PresentationClock >= fieldNext && ++fieldPage < fieldLines.Length)
                    fieldNext = Session.PresentationClock + FieldLineTime(fieldLines[fieldPage]);
                if (fieldPage < fieldLines.Length)
                {
                    StoryLine line = fieldLines[fieldPage];
                    return line.speaker + "  ·  " + line.text;
                }
                fieldLines = null;
            }
            return openingRoomEpoch == Session.PresentationEpoch && Session.EchoAvailable
                ? "길을 잃은 정령입니다. 등불에 다가가 안내해 주세요."
                : Session.Status;
        }

        private string OpeningHint()
        {
            if (!Session.IsFighting || Session.RoomCleared || openingRoomEpoch != Session.PresentationEpoch)
                return null;
            firstMove |= Mathf.Abs(Session.X + 3f) + Mathf.Abs(Session.Y) > .55f;
            if (!firstMove) return ShowKeyboardHints ? "방향키로 강변의 가면 쪽에 다가가세요." : "왼쪽 이동 패드를 밀어 가면 쪽에 다가가세요.";
            if (Session.AttackSerial == firstAttack)
                return ShowKeyboardHints ? "가까이서 Z로 공격하세요." : "가까이서 오른쪽의 공격 버튼을 누르세요.";
            return null;
        }
    }
}
