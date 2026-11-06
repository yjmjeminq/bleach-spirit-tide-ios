using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace SpiritTide
{
    public sealed partial class TideGame
    {
        internal bool ReviewStepping;internal float ReviewTime;
        private struct ReviewCue{public AudioClip clip;public float at,gain;}
        private readonly List<ReviewCue> reviewCues=new List<ReviewCue>();
        private void ReviewSound(AudioClip clip,float gain=1){if(SmokeMode&&ReviewStepping)reviewCues.Add(new ReviewCue{clip=clip,at=ReviewTime,gain=gain});}
        internal void BeginReviewAudio(){reviewCues.Clear();ReviewTime=0;}
        internal int ExportReviewAudio(string path,float duration)
        {
            const int rate=22050;float[] mix=new float[Mathf.CeilToInt((duration+.3f)*rate)];
            foreach(var cue in reviewCues){float[] pcm=new float[cue.clip.samples];cue.clip.GetData(pcm,0);int start=Mathf.RoundToInt(cue.at*rate);for(int i=0;i<pcm.Length&&start+i<mix.Length;i++)if(start+i>=0)mix[start+i]+=pcm[i]*cue.gain;}
            float peak=.01f;foreach(float sample in mix)peak=Mathf.Max(peak,Mathf.Abs(sample));float scale=.85f/Mathf.Max(.85f,peak);
            using(var writer=new BinaryWriter(File.Create(path)))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));writer.Write(36+mix.Length*2);writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));writer.Write(16);writer.Write((short)1);writer.Write((short)1);writer.Write(rate);writer.Write(rate*2);writer.Write((short)2);writer.Write((short)16);writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));writer.Write(mix.Length*2);foreach(float sample in mix)writer.Write((short)(Mathf.Clamp(sample*scale,-1,1)*32767));
            }
            return reviewCues.Count;
        }
    }
}
