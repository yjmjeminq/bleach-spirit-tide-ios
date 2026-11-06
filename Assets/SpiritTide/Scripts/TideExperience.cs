using System;
using UnityEngine;
namespace SpiritTide
{
    [Serializable] public class TideClue{public string id,text;}
    [Serializable] public class TideActVoice{public string id,departure,after_mid,after_boss;public int act;}
    [Serializable] public class TideEncounterScene{public string id;public int act;public StoryLine[] intro,outro;}
    [Serializable] public class TideExperienceData{public int schema;public TideClue[] clues;public TideActVoice[] voices;public TideEncounterScene[] encounters;}
    public static class TideExperience
    {
        public static TideExperienceData Data {get;private set;}
        public static void Load()
        {
            var asset=Resources.Load<TextAsset>("SpiritTide/Data/experience");if(asset==null)throw new InvalidOperationException("Experience data missing");Data=JsonUtility.FromJson<TideExperienceData>(asset.text);
            if(Data.schema!=1||Data.voices.Length!=30||Data.encounters.Length!=10||Data.clues.Length!=11)throw new InvalidOperationException("Incomplete experience data");
        }
        public static string Clue(string id){foreach(var clue in Data.clues)if(clue.id==id)return clue.text;throw new InvalidOperationException("Untranslated clue "+id);}
        public static TideActVoice Voice(int hero,int act){string id=TideCatalog.Characters[Mathf.Clamp(hero,0,5)].id;foreach(var v in Data.voices)if(v.id==id&&v.act==act)return v;throw new InvalidOperationException("Missing companion voice "+id+" / "+act);}
        public static TideEncounterScene Encounter(string id){foreach(var scene in Data.encounters)if(scene.id==id)return scene;return null;}
        public static int EncounterBit(string id){for(int i=0;i<Data.encounters.Length;i++)if(Data.encounters[i].id==id)return 1<<i;return 0;}
    }
}
