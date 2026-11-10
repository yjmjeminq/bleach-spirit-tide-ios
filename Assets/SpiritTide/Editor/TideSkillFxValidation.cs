using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;
namespace SpiritTide.EditorTools
{
    public static class TideSkillFxValidation
    {
        public static void Run(Action<string,bool> check)
        {
            var shader=Resources.Load<Shader>("SpiritTide/Vfx/SkillGlow");
            check("skill glow shader is present and has no compiler errors",shader!=null&&!ShaderUtil.ShaderHasError(shader));
            var ids=new HashSet<string>();var motifs=new HashSet<string>();var waves=new HashSet<string>();
            foreach(var fx in TideSkillFx.All)
            {
                check("effect maps to the real playable skill "+fx.id,TideCatalog.Characters[fx.hero].Skill(fx.slot).id==fx.id&&ids.Add(fx.id));
                check("skill has a dedicated visual motif "+fx.id,motifs.Add(fx.motif));
                var clip=TideSkillFx.Sound(fx);var data=new float[clip.samples];clip.GetData(data,0);float energy=0,peak=0;
                foreach(float sample in data){energy+=sample*sample;peak=Mathf.Max(peak,Mathf.Abs(sample));}
                byte[] bytes=new byte[data.Length*4];Buffer.BlockCopy(data,0,bytes,0,bytes.Length);
                string hash;using(var sha=SHA256.Create())hash=Convert.ToBase64String(sha.ComputeHash(bytes));
                check("dedicated skill sound is audible distinct and unclipped "+fx.id,energy/data.Length>.00002f&&peak<.95f&&waves.Add(hash));
                UnityEngine.Object.DestroyImmediate(clip);
            }
            int expected=TideCatalog.Characters.Length*CharacterDef.SkillCount;
            check("all fifty-four skills have authored visuals and sound",ids.Count==expected&&motifs.Count==expected&&waves.Count==expected);
        }
    }
}
