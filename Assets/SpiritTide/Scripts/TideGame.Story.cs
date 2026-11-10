using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private static readonly Vector2 Center = new Vector2(.5f, .5f);
        private Image openingBackdrop;
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

        private void ContinueFromTitle()
        {
            if (Session.Mode != TideMode.Title) return;
            if (FreshCampaign)
            {
                Session.SelectHero(0);
                ShowDialogueScene(TideQuests.Main(1).accept_scene, () =>
                {
                    if (!Session.AcceptQuest()) { ReturnTown(); return; }
                    if (BeginDungeonNow(1, 1, false))
                        StartFieldScene("a1_d1_entry");
                }, "강변으로", () => { Session.Mode = TideMode.Title; Session.Revision++; Refresh(); }, "타이틀로");
                return;
            }

            Session.Mode = TideMode.Town;
            Session.Revision++;
            if (UnreadEnding)
                ShowDialogueScene("a5_epilogue", ReturnTown, "상점으로");
            else
                Refresh();
        }

        private void BuildTitle()
        {
            Clear(panel);
            var cover=Surface("TitleCover",panel,Center,new Vector2(-152,4),new Vector2(744,464),
                new Color(.14f,.24f,.30f,.98f)).rectTransform;
            Picture("TitleAccent",cover,Center,new Vector2(-370,115),new Vector2(4,132),gold);
            Label("BLEACH",cover,Center,new Vector2(0,152),new Vector2(648,37),28,gold,true);
            Label("영해의 균열",cover,Center,new Vector2(0,79),new Vector2(648,98),76,ink,true);
            Picture("TitleRule",cover,Center,new Vector2(-242,14),new Vector2(164,2),gold);
            string note = FreshCampaign ? "루키아  ·  카라쿠라\n평소라면 대답이 돌아올 시간이었다."
                : Session.CampaignFinished ? "다섯 균열의 조사를 마쳤습니다."
                : "ACT " + Session.Profile.campaignAct.ToString("00") + "  ·  " + TideStory.HeroName(Session.Profile.hero) + "\n"
                    + (Session.Quest == QuestState.Ready ? "우라하라 상점으로 돌아갈 때" : TideJourney.Current(Session.Profile).title
                        + " · " + (Session.Profile.roomCheckpoint+1) + "방");
            Label(note,cover,Center,new Vector2(0,-50),new Vector2(648,83),26,ink);
            bool field=Session.Quest==QuestState.Active;
            Button("title-start", FreshCampaign ? "이야기 시작" : UnreadEnding ? "후일담 이어보기" : "이어서 하기",
                cover,Center,new Vector2(field?-166:0,-145),new Vector2(field?314:648,88),ContinueFromTitle,true);
            if(field)
                Button("title-resume-field","현장 이어가기",cover,Center,new Vector2(166,-145),new Vector2(316,88),
                    () => { if (Session.Mode == TideMode.Title) StartBattle(false); });
            if (!string.IsNullOrEmpty(lastSaveNotice))
                Label(lastSaveNotice,cover,Center,new Vector2(0,-209),new Vector2(648,38),19,gold);
        }

        private void BuildStory()
        {
            var scene = TideDialogue.Scene(storyRecordId);
            int page = storyPage, epoch = storyEpoch;
            bool last = page == storyLines.Length - 1;
            StoryLine line = storyLines[page];
            bool field = Session.InBattle || scene.kind == "entry" || scene.kind == "intro" || scene.kind == "clear";
            var dungeon = TideCampaign.Dungeon(scene.act, scene.stage);
            var stage=OpenDialogueStage("StoryStage",field?dungeon.background:"town");
            bool encounter=scene.kind=="intro";
            string listener=page>0?storyLines[page-1].speaker:null;
            Sprite portrait=PortraitCrop(line.speaker,dungeon,encounter);
            if(listener!=null&&listener!=line.speaker)
                PlaceDialoguePortrait(stage,PortraitCrop(listener,dungeon,encounter),listener,false);
            else if(portrait==null&&scene.id=="main_a1_accept")
            {
                string lead=TideStory.HeroName(storyHero);
                PlaceDialoguePortrait(stage,PortraitCrop(lead),lead,false);
            }
            PlaceDialoguePortrait(stage,portrait,line.speaker,true);
            if(portrait==null)shownSpeaker=line.speaker;
            DialogueHeading(stage,"ACT "+scene.act.ToString("00")+"  ·  "+(field?dungeon.title:scene.place),scene.title);
            string channel="";
            if (field && scene.kind != "main_checkpoint")
                for (int i = 0; i < heroSprites.Length; i++)
                    if (i != storyHero && line.speaker == TideStory.HeroName(i))
                    {
                        channel="동료 통신";
                        break;
                    }
            if (scene.kind.StartsWith("side_", StringComparison.Ordinal) && (scene.act < Session.Profile.campaignAct || Session.Profile.claimed))
                channel="지난 부탁";
            float heading=DrawDialogueSpeech(stage,line.speaker,line.text,-146,channel);
            var count=Label((page+1)+" / "+storyLines.Length,stage,Center,new Vector2(459,heading),new Vector2(90,36),19,muted);
            count.alignment = TextAnchor.MiddleRight;
            for(int i=0;i<storyLines.Length;i++)
                Picture("StoryPage",stage,Center,new Vector2(400-(storyLines.Length-1-i)*14,heading),
                    new Vector2(i==page?11:5,3),i<=page?SpeakerColor(line.speaker):new Color(.40f,.53f,.58f,.65f));

            DialogueButton("story-previous", "이전 문장", stage, new Vector2(-414,-264),new Vector2(192,84),() =>
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
                DialogueButton("choice-1",skip,stage,new Vector2(-176,-264),new Vector2(258,84),
                    () => { if (epoch == storyEpoch) FinishStory(); });
            }
            if(!last||storyCancelLabel!=storyEndLabel)
                DialogueButton(last?"choice-1":"choice-2",storyCancelLabel,stage,new Vector2(74,-264),new Vector2(216,84),CancelStory);
            DialogueButton("choice-0",last?storyEndLabel:"계속",stage,new Vector2(354,-264),new Vector2(312,84),
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
                else
                {
                    string completion=Session.Status;
                    ReturnTown();
                    Session.Status=completion;
                }
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

        private string FieldStatus(out StoryLine call)
        {
            call=null;
            if (!Session.IsFighting || !Session.IsCampaign) return Session.Status;
            if (fieldLines != null && fieldEpoch == Session.PresentationEpoch)
            {
                if (Session.PresentationClock >= fieldNext && ++fieldPage < fieldLines.Length)
                    fieldNext = Session.PresentationClock + FieldLineTime(fieldLines[fieldPage]);
                if (fieldPage < fieldLines.Length)
                {
                    call=fieldLines[fieldPage];
                    return "";
                }
                fieldLines = null;
            }
            if(Session.RoomCleared&&Session.MainPoint!=null)return Session.MainPoint.goal;
            return openingRoomEpoch == Session.PresentationEpoch && Session.EchoAvailable
                ? "길을 잃은 정령입니다. 등불에 다가가 안내해 주세요."
                : Session.Status;
        }

        private string OpeningHint()
        {
            if (!Session.IsFighting || Session.RoomCleared || openingRoomEpoch != Session.PresentationEpoch)
                return null;
            firstMove |= Mathf.Abs(Session.X + 3f) + Mathf.Abs(Session.Y) > .55f;
            if (!firstMove) return ShowKeyboardHints ? "방향키로 가면에게 다가가세요." : "왼쪽 패드로 가면에게 다가가세요.";
            if (Session.AttackSerial == firstAttack)
                return ShowKeyboardHints ? "가까이서 Z로 공격하세요." : "가까이서 오른쪽의 공격 버튼을 누르세요.";
            return null;
        }
    }
}
