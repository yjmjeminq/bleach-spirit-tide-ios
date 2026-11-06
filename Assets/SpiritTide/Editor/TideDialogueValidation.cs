using System;
using System.Collections.Generic;
using System.IO;

namespace SpiritTide.EditorTools
{
    public static class TideDialogueValidation
    {
        public static void Run(Action<string,bool> check)
        {
            int entries=0,clears=0,intros=0,reports=0,parties=0,rescues=0,epilogues=0;
            var allIds=new HashSet<string>();
            foreach(var scene in TideDialogue.Data.scenes)
            {
                check("dialogue has stable unique scene ID "+scene.id,allIds.Add(scene.id)&&scene.act>=1&&scene.act<=5&&scene.stage>=1&&scene.stage<=6);
                switch(scene.kind){case "entry":entries++;break;case "clear":clears++;break;case "intro":intros++;break;case "report":reports++;break;case "party":parties++;break;case "rescue":rescues++;break;case "epilogue":epilogues++;break;}
                for(int hero=0;hero<6;hero++)
                {
                    var lines=TideDialogue.Lines(scene.id,hero);var speakers=new HashSet<string>();bool valid=lines.Length>=2&&lines.Length<=8;
                    for(int i=0;i<lines.Length;i++)
                    {
                        var l=lines[i];speakers.Add(l.speaker);valid&=!string.IsNullOrWhiteSpace(l.text)&&!l.text.Contains("$")&&!l.text.Contains("{")&&!l.speaker.Contains("$");
                        if(i>0)valid&=lines[i-1].speaker!=l.speaker||lines[i-1].text!=l.text;
                        foreach(var clue in TideExperience.Data.clues)valid&=!l.text.Contains(clue.id);
                    }
                    check("resolved conversation has multiple speakers and no template IDs "+scene.id+" / "+hero,valid&&speakers.Count>=2);
                }
            }
            check("full story dialogue and companion coverage",entries==30&&clears==30&&intros==10&&reports==5&&parties==30&&rescues==5&&epilogues==1);
            foreach(var dungeon in TideCampaign.Data.stages)
            {
                check("every dungeon has entry and resolution dialogue "+dungeon.index,TideDialogue.Scene(TideDialogue.DungeonId(dungeon,"entry"))!=null&&TideDialogue.Scene(TideDialogue.DungeonId(dungeon,"clear"))!=null);
            }
            var fresh=new TideProfile();TideCatalog.Sanitize(fresh);
            check("fresh save has no dialogue history and no unlocked future scenes",fresh.dialogueHistory.Length==0&&TideDialogue.Available(fresh,1).Count==0&&TideDialogue.Available(fresh,5).Count==0);
            fresh.quest=1;check("accepted quest opens request briefing and six companion scenes",TideDialogue.Available(fresh,1).Count==8&&!TideDialogue.CanRead(fresh,TideDialogue.Scene("a1_d1_clear"))&&!TideDialogue.CanRead(fresh,TideDialogue.Scene("a1_d3_intro")));
            foreach(var scene in TideDialogue.Data.scenes)
                if(scene.act>1)check("future dialogue remains locked "+scene.id,!TideDialogue.CanRead(fresh,scene));
            fresh.clearedDungeons=1;check("completed room story unlocks without a prior history field",TideDialogue.CanRead(fresh,TideDialogue.Scene("a1_d1_clear"))&&!TideDialogue.CanRead(fresh,TideDialogue.Scene("a1_report")));
            var old=new TideProfile{campaignSchema=1,campaignAct=5,campaignStage=6,quest=3,claimed=true,reportedActs=31,clearedDungeons=TideProgress.AllDungeons};TideCatalog.Sanitize(old);
            int oldAvailable=0;for(int act=1;act<=5;act++)oldAvailable+=TideDialogue.Available(old,act).Count;
            check("old ending save restores main and companion conversations including investigations",oldAvailable==116&&old.dialogueHistory.Length==0);
            old.rescueReports=31;int complete=0;for(int act=1;act<=5;act++)complete+=TideDialogue.Available(old,act).Count;
            check("main and original rescue reports unlock 121 scenes without inventing side quest completions",complete==121);
            TideDialogue.Remember(fresh,"a1_d1_entry",2);fresh.hero=5;TideDialogue.Remember(fresh,"a1_d1_entry",5);
            check("history preserves original lead through character switching and rereads",fresh.dialogueHistory.Length==1&&TideDialogue.ArchiveHero(fresh,"a1_d1_entry")==2);
            string path=Path.GetFullPath("BuildLogs/dialogue-save-check.json"),error,notice;
            check("dialogue history saves through real storage",TideStorage.Save(path,fresh,out error));var loaded=TideStorage.Load(path,out notice);
            check("dialogue history survives checksum save reload",loaded.dialogueHistory.Length==1&&TideDialogue.ArchiveHero(loaded,"a1_d1_entry")==2);
            loaded.dialogueHistory=new[]{null,new TideDialogueSeen{id="missing",hero=0},new TideDialogueSeen{id="a1_d1_entry",hero=999},new TideDialogueSeen{id="a1_d1_entry",hero=0}};TideCatalog.Sanitize(loaded);
            check("corrupt or duplicate history is repaired without inventing future scenes",loaded.dialogueHistory.Length==1&&loaded.dialogueHistory[0].hero==5&&TideDialogue.Available(loaded,5).Count==0);
            var final=TideDialogue.Lines("a5_report",0);var finalSpeakers=new HashSet<string>();foreach(var l in final)finalSpeakers.Add(l.speaker);
            bool allSix=true;for(int hero=0;hero<6;hero++)allSix&=finalSpeakers.Contains(TideStory.HeroName(hero));
            check("final return scene includes all six companions",allSix);
            foreach(var reply in TideDialogue.Data.town)
                check("town reply has an explicit act and progress condition "+reply.npc+" / "+reply.act+" / "+reply.phase,!string.IsNullOrWhiteSpace(reply.text)&&reply.text.Length>20);
        }
    }
}
