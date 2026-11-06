using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        private void CheckJourneyText(string state)
        {
            Canvas.ForceUpdateCanvases();int labels=0;
            foreach(var label in game.GetComponentsInChildren<Text>())if(label.name.StartsWith("Journey")&&label.gameObject.activeInHierarchy)
            {labels++;Check("journey text renders without clipping "+state+" / "+label.name+" ("+label.preferredHeight+" / "+label.rectTransform.rect.height+")",label.preferredHeight<=label.rectTransform.rect.height+.1f&&label.cachedTextGenerator.vertexCount>0);}
            Check("journey includes rendered context "+state,labels>0);
            CheckTouchLayout("journey "+state);
        }
        private void JourneyFixture(int act,int stage,int quest=1)
        {
            var p=new TideProfile{campaignSchema=1,campaignAct=act,campaignStage=stage,quest=quest,reportedActs=(1<<(act-1))-1,clearedDungeons=(1<<((act-1)*6+stage-1))-1,level=30,uiScale=1.25f,controlScale=1.1f};
            if(quest==2)p.clearedDungeons=(1<<(act*6))-1;
            if(quest==3){p.clearedDungeons=TideProgress.AllDungeons;p.reportedActs=31;p.claimed=true;}
            TideCatalog.Sanitize(p);JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(p),game.Session.Profile);game.Session.ClearQuestNotifications();game.ReturnTown();
        }
        private IEnumerator OpenJourneyUi(int tab=0)
        {
            yield return TapUi("pause");yield return TapUi("home-guide");if(tab>0)yield return TapUi("journey-tab-"+tab);
        }
        private IEnumerator ReviewJourney()
        {
            string original=JsonUtility.ToJson(game.Session.Profile);int contexts=0,practiced=0,rests=0;
            for(int a=1;a<=5;a++)for(int stage=1;stage<=6;stage++)
            {
                JourneyFixture(a,stage);string before=JsonUtility.ToJson(game.Session.Profile);
                yield return OpenJourneyUi();CheckJourneyText(a+" / "+stage);
                Check("live recap follows the current story objective "+a+" / "+stage,VisibleText(TideJourney.Current(game.Session.Profile).title));
                if(stage==4){Check("live recap explains mandatory evidence "+a,VisibleText(TideQuests.Main(a).inspect_title));yield return Capture("94-journey-act"+a);}
                yield return TapUi("journey-tab-1");CheckJourneyText("preparation "+a+" / "+stage);
                if(a==3&&stage==4)yield return Capture("95-preparation");
                yield return TapUi("back");Check("journey returns to the same home without writes "+a+" / "+stage,game.Session.Mode==TideMode.Home&&before==JsonUtility.ToJson(game.Session.Profile));contexts++;game.ReturnTown();
            }
            for(int a=1;a<=5;a++)for(int state=0;state<2;state++)
            {
                JourneyFixture(a,state==0?1:6,state==0?0:2);yield return OpenJourneyUi();CheckJourneyText("handoff "+a+" / "+state);contexts++;
                yield return TapUi("journey-continue");Check("journey continuation reaches the actual quest giver "+a+" / "+state,game.Session.Mode==TideMode.Dialogue);game.ReturnTown();
            }
            JourneyFixture(1,1,0);yield return OpenJourneyUi(2);CheckJourneyText("unseen bosses");
            for(int i=0;i<10;i++)Check("unseen boss has a locked spoiler free live control "+i,!game.Buttons["journey-boss-"+i].interactable&&game.Buttons["journey-boss-"+i].GetComponentInChildren<Text>().text.Contains("미확인 기록"));
            yield return Capture("96-boss-records-locked");game.ReturnTown();
            for(int i=0;i<10;i++)
            {
                JourneyFixture(5,6,3);game.Session.Profile.potions=0;game.ReturnTown();string before=JsonUtility.ToJson(game.Session.Profile);
                yield return OpenJourneyUi(2);yield return TapUi("journey-boss-"+i);CheckJourneyText("boss lesson "+i);
                if(i==7)yield return Capture("96-boss-lesson");
                yield return TapUi("journey-practice");Check("live practice spawns the selected boss "+i,game.Session.IsFighting&&!game.Session.IsCampaign&&game.Session.EnemyId==TideCatalog.Bosses[i].id);
                game.Session.Hp=50;game.Session.Revision++;yield return null;yield return TapUi("potion");
                Check("live practice heals without consuming owned supplies "+i,game.Session.Hp>50&&game.Session.Profile.potions==0&&VisibleText("연습용 · 무료"));
                for(int t=0;t<1000&&game.Session.IsFighting;t++){for(int n=0;n<60&&game.Session.IsFighting;n++)DriveNativeTick();yield return null;}
                Check("live practice victory preserves rewards and progress "+i,game.Session.Mode==TideMode.Result&&game.Session.LastXp==0&&game.Session.LastGold==0&&game.Session.LastGearUid==0&&before==JsonUtility.ToJson(game.Session.Profile));
                practiced++;yield return TapUi("next-dungeon");
            }
            JourneyFixture(5,6,3);yield return OpenJourneyUi();CheckJourneyText("completed story");contexts++;yield return TapUi("journey-tab-2");yield return TapUi("journey-boss-0");yield return TapUi("journey-practice");
            game.Session.Hp=1;game.Session.X=0;game.Session.EnemyX=1;game.Session.Y=game.Session.EnemyY=0;
            for(int t=0;t<600&&game.Session.IsFighting;t++){for(int n=0;n<10&&game.Session.IsFighting;n++)game.Session.Step(1f/60,Vector2.zero,false);yield return null;}
            Check("live defeat names the real incoming attack",game.Session.Mode==TideMode.Defeat&&!string.IsNullOrEmpty(game.Session.LastDamageCue)&&VisibleText(game.Session.LastDamageCue));
            CheckJourneyText("defeat feedback");yield return Capture("98-defeat-advice");
            float clock=game.Session.Clock;string defeated=JsonUtility.ToJson(game.Session.Profile);
            yield return TapUi("defeat-guide");CheckJourneyText("defeat preparation");yield return null;yield return TapUi("back");
            Check("preparation back preserves the defeated checkpoint and clock",game.Session.Mode==TideMode.Defeat&&game.Session.Clock==clock&&defeated==JsonUtility.ToJson(game.Session.Profile));game.ReturnTown();
            JourneyFixture(2,4);
            var owned=new System.Collections.Generic.List<TideGear>(game.Session.Profile.gear);owned.Add(new TideGear{uid=7777,tier=5,rarity=2,slot="power",name="되찾은 힘 영장"});game.Session.Profile.gear=owned.ToArray();game.ReturnTown();
            int worn=game.Session.Profile.powerGear;string gearBefore=JsonUtility.ToJson(game.Session.Profile);
            yield return OpenJourneyUi(1);Check("preparation identifies a real owned upgrade without auto equip",VisibleText("더 높은 수치의 전리품 있음")&&game.Session.Profile.powerGear==worn);yield return Capture("95-preparation-upgrade");
            yield return TapUi("journey-gear");Check("preparation opens the owned upgrade without changing inventory",game.Session.Mode==TideMode.Inventory&&VisibleText("되찾은 힘 영장")&&gearBefore==JsonUtility.ToJson(game.Session.Profile));
            yield return TapUi("back");Check("gear comparison returns to the same preparation tab",game.Session.Mode==TideMode.Journey&&VisibleText("더 높은 수치의 전리품 있음"));
            yield return TapUi("journey-gear");yield return TapUi("equip");Check("recommended gear equips only after the explicit button",game.Session.Profile.powerGear==7777);yield return TapUi("back");CheckJourneyText("after direct equip");game.ReturnTown();
            JourneyFixture(2,4);yield return TapUi("pause");yield return TapUi("home-map");yield return TapUi("mission-guide");CheckJourneyText("map entry");yield return TapUi("back");Check("map preparation back restores the map",game.Session.Mode==TideMode.Missions);game.ReturnTown();
            for(int a=1;a<=5;a++)
            {
                JourneyFixture(a,3);game.Session.Profile.hero=a==4?4:0;game.ReturnTown();game.StartBattle(false);yield return ReadStory();yield return FightDungeon();
                Check("midpoint offers a voluntary story rest "+a,game.Buttons.ContainsKey("result-breather")&&game.Buttons["result-breather"].gameObject.activeInHierarchy);CheckTouchLayout("midpoint rest "+a);
                if(a==4)yield return Capture("97-midpoint-rest");
                int gold=game.Session.Profile.gold,xp=game.Session.Profile.xp,clear=game.Session.Profile.clearedDungeons;
                yield return TapUi("result-breather");yield return ReadStory();
                Check("midpoint rest connects a real companion scene and keeps rewards "+a,game.Session.Mode==TideMode.Home&&TideDialogue.Seen(game.Session.Profile,"a"+a+"_party_"+game.Session.Hero.id)!=null&&game.Session.Profile.gold==gold&&game.Session.Profile.xp==xp&&game.Session.Profile.clearedDungeons==clear&&game.Session.Profile.campaignStage==4);
                if(a==4)yield return Capture("97-midpoint-home");rests++;
            }
            Check("journey reviewed forty one story contexts ten practices and five rests",contexts==41&&practiced==10&&rests==5);
            File.WriteAllText(Path.Combine(output,"journey-review.json"),"{\"passed\":true,\"version\":\""+Application.version+"\",\"guid\":\""+Application.buildGUID+"\",\"story_contexts\":41,\"boss_practices\":10,\"midpoint_rests\":5,\"scope\":\"Windows live UI and real combat; not iPhone hardware execution\"}");
            JsonUtility.FromJsonOverwrite(original,game.Session.Profile);game.Session.ClearQuestNotifications();game.ReturnTown();yield return null;
        }
    }
}
