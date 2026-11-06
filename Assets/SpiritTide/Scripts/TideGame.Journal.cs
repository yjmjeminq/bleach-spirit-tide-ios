using UnityEngine;
namespace SpiritTide
{
    public sealed partial class TideGame
    {
        private int journalAct,journalPage,dialogueScene,dialogueLine;
        private bool journalDialogue;
        private void OpenJournal(){journalDialogue=false;archiveIndex=false;journalAct=Session.Profile.campaignAct;journalPage=Session.Profile.campaignStage;OpenMenu(TideMode.Journal);}
        private void BuildRecordJournal()
        {
            if(journalDialogue){BuildDialogueArchive();return;}
            if(journalAct<1){journalAct=Session.Profile.campaignAct;journalPage=Session.Profile.campaignStage;}
            journalAct=Mathf.Clamp(journalAct,1,5);journalPage=Mathf.Clamp(journalPage,1,6);
            int first=(journalAct-1)*6;bool known=journalAct<=Session.Profile.campaignAct,mid=(Session.Profile.clearedDungeons&(1<<(first+2)))!=0,boss=(Session.Profile.clearedDungeons&(1<<(first+5)))!=0;
            var act=TideStory.Data.acts[journalAct-1];string clue=known?TideExperience.Clue(boss?act.boss_clue:mid?act.mid_clue:act.entry_clue):"이전 지역의 보고 뒤 확인할 기록";
            MenuTitle("조사 기록 · Act "+journalAct,act.title+" · "+clue);
            for(int a=1;a<=5;a++){int target=a;Button("journal-act-"+a,"Act "+a,panel,new Vector2(.5f,.5f),new Vector2((a-3)*185,94),new Vector2(168,84),()=>{journalAct=target;journalPage=target<Session.Profile.campaignAct?6:1;Refresh();},a==journalAct);}
            var dungeon=TideCampaign.Dungeon(journalAct,journalPage);bool clear=(Session.Profile.clearedDungeons&(1<<dungeon.index))!=0,current=journalAct==Session.Profile.campaignAct&&journalPage==Session.Profile.campaignStage;
            Label(journalPage+" / 6 · "+dungeon.title+" · "+(clear?"기록 확보":known&&current?"조사 중":"미확인"),panel,new Vector2(.5f,.5f),new Vector2(0,20),new Vector2(950,36),25,gold,true);
            string body=clear?TideStory.DungeonOutcome(dungeon,Session.Profile):known&&current?dungeon.reason:"아직 이 구역의 기록을 확보하지 못했습니다. 앞 구역을 정화하고 확인한 단서를 따라가세요.";
            Label(body,panel,new Vector2(.5f,.5f),new Vector2(0,-86),new Vector2(950,172),23);
            Button("journal-prev","‹",panel,new Vector2(.5f,.5f),new Vector2(-435,-226),new Vector2(88,84),()=>{journalPage--;Refresh();}).interactable=journalPage>1;
            Button("journal-next","›",panel,new Vector2(.5f,.5f),new Vector2(-325,-226),new Vector2(88,84),()=>{journalPage++;Refresh();}).interactable=journalPage<6;
            Button("journal-map","균열 지도",panel,new Vector2(.5f,.5f),new Vector2(-160,-226),new Vector2(180,84),()=>{mapAct=journalAct;OpenMenu(TideMode.Missions);},true);
            Button("journal-dialogues","대화록",panel,new Vector2(.5f,.5f),new Vector2(40,-226),new Vector2(180,84),()=>{journalDialogue=true;archiveIndex=false;archivePage=archiveFilter=0;dialogueScene=dialogueLine=0;Refresh();},true);
            Button("back","뒤로",panel,new Vector2(.5f,.5f),new Vector2(330,-226),new Vector2(280,84),NavigateBack);
        }
        private void BuildDialogueArchive()
        {
            if(archiveIndex){BuildArchiveIndex();return;}
            journalAct=Mathf.Clamp(journalAct,1,5);var available=TideDialogue.Available(Session.Profile,journalAct);
            string leadNote="본편 순서 · 열린 장면 "+available.Count+"개";
            if(available.Count>0){dialogueScene=Mathf.Clamp(dialogueScene,0,available.Count-1);var record=TideDialogue.Seen(Session.Profile,available[dialogueScene].id);leadNote+=" · "+(record==null?"현재 선두의 응답으로 복원":"당시 선두 · "+TideStory.HeroName(record.hero));}
            MenuTitle("대화록 · Act "+journalAct,leadNote);
            for(int a=1;a<=5;a++){int target=a;Button("dialogue-act-"+a,"Act "+a,panel,new Vector2(.5f,.5f),new Vector2((a-3)*185,94),new Vector2(168,84),()=>{journalAct=target;dialogueScene=dialogueLine=0;Refresh();},a==journalAct);}
            if(available.Count==0)
            {
                Label("아직 열린 대화가 없습니다.\n해당 액트의 의뢰를 받고 이야기를 진행해 주세요.",panel,new Vector2(.5f,.5f),new Vector2(0,-70),new Vector2(950,150),28);
            }
            else
            {
                dialogueScene=Mathf.Clamp(dialogueScene,0,available.Count-1);var scene=available[dialogueScene];
                var lines=TideDialogue.Lines(scene.id,TideDialogue.ArchiveHero(Session.Profile,scene.id));dialogueLine=Mathf.Clamp(dialogueLine,0,lines.Length-1);
                Button("dialogue-scene-prev","‹",panel,new Vector2(.5f,.5f),new Vector2(-435,4),new Vector2(88,84),()=>{dialogueScene--;dialogueLine=0;Refresh();}).interactable=dialogueScene>0;
                Button("dialogue-scene-next","›",panel,new Vector2(.5f,.5f),new Vector2(435,4),new Vector2(88,84),()=>{dialogueScene++;dialogueLine=0;Refresh();}).interactable=dialogueScene<available.Count-1;
                Label((dialogueScene+1)+" / "+available.Count+" · "+scene.title,panel,new Vector2(.5f,.5f),new Vector2(0,4),new Vector2(740,44),24,gold,true);
                var current=lines[dialogueLine];var speaker=Label(current.speaker+" · "+(dialogueLine+1)+" / "+lines.Length,panel,new Vector2(.5f,.5f),new Vector2(0,-55),new Vector2(950,30),25,gold,true);speaker.name="DialogueArchiveSpeaker";
                var body=Label(current.text,panel,new Vector2(.5f,.5f),new Vector2(0,-126),new Vector2(950,112),Mathf.RoundToInt(25*Session.Profile.uiScale));body.name="DialogueArchiveText";body.alignment=TextAnchor.UpperLeft;
                Button("dialogue-line-prev","‹",panel,new Vector2(.5f,.5f),new Vector2(-435,-226),new Vector2(88,84),()=>{dialogueLine--;Refresh();}).interactable=dialogueLine>0;
                Button("dialogue-line-next","›",panel,new Vector2(.5f,.5f),new Vector2(-325,-226),new Vector2(88,84),()=>{dialogueLine++;Refresh();}).interactable=dialogueLine<lines.Length-1;
            }
            Button("dialogue-index","장면 목록",panel,new Vector2(.5f,.5f),new Vector2(-140,-226),new Vector2(180,84),()=>{archiveIndex=true;Refresh();},true);
            Button("dialogue-records","조사 기록",panel,new Vector2(.5f,.5f),new Vector2(60,-226),new Vector2(180,84),()=>{journalDialogue=false;Refresh();});
            Button("back","뒤로",panel,new Vector2(.5f,.5f),new Vector2(330,-226),new Vector2(280,84),NavigateBack);
        }
    }
}
