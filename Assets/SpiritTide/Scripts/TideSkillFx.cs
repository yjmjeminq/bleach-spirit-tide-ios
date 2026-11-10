using System;
using UnityEngine;

namespace SpiritTide
{
    public sealed class TideSkillFxDef
    {
        public readonly int hero,slot;public readonly string id,motif;public readonly Color core,accent;public readonly float tail;
        public TideSkillFxDef(int hero,int slot,string id,string motif,Color core,Color accent)
        {this.hero=hero;this.slot=slot;this.id=id;this.motif=motif;this.core=core;this.accent=accent;tail=slot==4?1.1f:slot==1||slot==8?.85f:.5f;}
    }
    public static class TideSkillFx
    {
        public const int PoolSize=6,MaxVerticesPerLayer=12000;
        private static readonly string[][] motifs={
            new[]{"frost-twin-crescents","snowflake-seal","ice-footfall-trail","three-crystal-pillars","winter-crown","snowflake-wave","skating-edge","crystal-wall","shattered-frost"},
            new[]{"three-petal-barrier","paired-healing-flowers","petal-lance","four-petal-counter","six-flower-sanctuary","nested-hexagons","petal-fan","returning-petals","rising-sanctuary"},
            new[]{"thunder-double-kick","lightning-impact","three-lightning-dashes","black-cat-evasion","thunder-god-descent","forked-kick","four-stroke-thunder","feline-counter-trail","thunderfall"},
            new[]{"wasp-needle","winged-afterimage","five-sting-volley","butterfly-seal-burst","hornet-cannon","ranged-sting-seal","guard-piercing-needle","retreating-needles","four-corner-seal"},
            new[]{"ash-blade-fan","drifting-ash-garden","inward-blade-vortex","ash-chain-detonation","silver-ash-cyclone","distant-ashfall","inward-ash-stream","armored-ash-shell","independent-ash-bursts"},
            new[]{"healing-water-sigil","silent-crimson-cleave","crossed-counter-blades","twin-stance-lotus","crimson-sword-domain","healing-pulse","crimson-needle","alternating-counter","lotus-cleave"}};
        private static readonly Color[] cores={new Color(.56f,.9f,1),new Color(1,.8f,.32f),new Color(.7f,.45f,1),new Color(1,.8f,.18f),new Color(.77f,.88f,.98f),new Color(1,.23f,.4f)};
        private static readonly Color[] accents={new Color(.12f,.48f,1),new Color(1,.4f,.14f),new Color(.25f,.8f,1),new Color(.9f,.41f,.14f),new Color(1,.55f,.72f),new Color(.32f,1,.76f)};
        public static readonly TideSkillFxDef[] All=Build();
        private static TideSkillFxDef[] Build()
        {
            var defs=new TideSkillFxDef[motifs.Length*CharacterDef.SkillCount];
            for(int hero=0;hero<motifs.Length;hero++)for(int skill=0;skill<CharacterDef.SkillCount;skill++)
                defs[Index(hero,skill)]=new TideSkillFxDef(hero,skill,TideCatalog.Characters[hero].Skill(skill).id,motifs[hero][skill],cores[hero],accents[hero]);
            return defs;
        }
        public static int Index(int hero,int skill)=>hero*CharacterDef.SkillCount+skill;
        public static TideSkillFxDef Get(int hero,int slot)=>All[Index(hero,slot)];
        public static AudioClip Sound(TideSkillFxDef def)
        {
            float length=def.slot==4?.85f:.26f+def.slot*.045f;int count=Mathf.CeilToInt(length*22050);float[] data=new float[count];
            uint seed=(uint)(731+def.hero*37+def.slot*7);float low=0;
            for(int i=0;i<count;i++)
            {
                float t=i/22050f,p=t/length;seed=seed*1664525+1013904223;float noise=((seed>>8)&65535)/32768f-1;low=Mathf.Lerp(low,noise,.06f);
                float env=Mathf.Min(1,p*35)*Mathf.Pow(1-p,1.5f),hz=220+def.slot*51,signal=0;
                switch(def.hero)
                {
                    case 0:signal=Mathf.Sin(t*2*Mathf.PI*(1180+def.slot*170))*.17f+Mathf.Sin(t*2*Mathf.PI*hz*2.76f)*.12f+noise*.06f;break;
                    case 1:float bell=440*Mathf.Pow(2,(def.slot*2+Mathf.Floor(p*6))/12);signal=(Mathf.Sin(t*2*Mathf.PI*bell)+Mathf.Sin(t*2*Mathf.PI*bell*1.5f)*.35f)*.18f;break;
                    case 2:signal=noise*.19f*Mathf.Pow(Mathf.Abs(Mathf.Sin(p*Mathf.PI*(def.slot==4?7:3))),2)+Mathf.Sin(t*2*Mathf.PI*(140+380*(1-p)))*.18f;break;
                    case 3:signal=Mathf.Sin(t*2*Mathf.PI*(1500-1000*p))*.16f+noise*.1f*Mathf.Pow(Mathf.Abs(Mathf.Sin(p*Mathf.PI*(def.slot+2))),4);break;
                    case 4:signal=low*.55f+Mathf.Sin(t*2*Mathf.PI*hz*.65f)*.12f+Mathf.Sin(t*2*Mathf.PI*hz*4.1f)*.045f;break;
                    case 5:signal=Mathf.Sin(t*2*Mathf.PI*(def.slot==0?330:110))*.2f+Mathf.Sin(t*2*Mathf.PI*hz*1.5f)*.08f+low*.17f;break;
                }
                data[i]=signal*env;
            }
            var clip=AudioClip.Create("Skill-"+def.id,count,1,22050,false);clip.SetData(data,0);return clip;
        }
    }
}
