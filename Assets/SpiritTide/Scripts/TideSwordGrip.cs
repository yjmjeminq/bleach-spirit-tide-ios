using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    // The forearm ends at the original painted cuff. The palm and thumb overlap
    // the handle so the grip reads even when the sword is foreshortened.
    internal sealed class TideSwordGrip : MaskableGraphic
    {
        private static readonly Color32 Ink=new Color32(127,79,66,255);
        private static readonly Vector2[] Knuckles={new Vector2(-3.9f,-3.5f),new Vector2(-1.7f,-4.9f),
            new Vector2(2.9f,-4.4f),new Vector2(4.5f,-2.3f),new Vector2(4.6f,2.3f),
            new Vector2(2.6f,4.3f),new Vector2(-1.8f,4.4f),new Vector2(-4.2f,2.1f)};
        private Vector2 wrist,grip,blade;
        private Color32 skin;
        internal Vector2 GripCenter=>grip;
        internal int VertexCount {get;private set;}

        internal void PoseChanged(int hero,Vector2 wrist,Vector2 grip,Vector2 blade)
        {
            this.wrist=wrist;this.grip=grip;this.blade=blade;
            skin=hero==4?new Color32(246,190,161,255):new Color32(242,198,176,255);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            VertexCount=0;
            if(blade.sqrMagnitude<.5f)return;
            Vector2 forearm=(grip-wrist).normalized,across=new Vector2(-forearm.y,forearm.x);
            Vector2 baseWrist=wrist-forearm*.8f;
            Color32 shadow=Color32.Lerp(skin,Ink,.28f);
            Vector2 side=new Vector2(-blade.y,blade.x);
            Palm(mesh,side,1,Ink);
            Quad(mesh,baseWrist+across*3.05f,baseWrist-across*3.05f,grip-across*3.7f,grip+across*3.7f,skin);
            Quad(mesh,baseWrist-across*2.8f,baseWrist-across*3.55f,grip-across*3.85f,grip-across*2.8f,shadow);
            Quad(mesh,baseWrist+across*3.05f,baseWrist+across*3.45f,grip+across*3.65f,grip+across*2.9f,shadow);

            Palm(mesh,side,.89f,skin);
            for(int finger=0;finger<3;finger++)
            {
                float along=-2.65f+finger*1.87f;
                Quad(mesh,At(.7f,along,side),At(3.65f,along+.22f,side),At(3.65f,along+.66f,side),At(.7f,along+.36f,side),shadow);
            }
            Quad(mesh,At(-3.3f,.3f,side),At(-3.55f,2.15f,side),At(-.4f,3.45f,side),At(1.6f,1.64f,side),shadow);
            Quad(mesh,At(-3.05f,.65f,side),At(-3.16f,2.05f,side),At(-.47f,3.05f,side),At(.77f,1.78f,side),Color32.Lerp(skin,Color.white,.16f));
            VertexCount=mesh.currentVertCount;
        }

        private Vector2 At(float across,float along,Vector2 side)=>grip+side*across+blade*along;

        private void Palm(VertexHelper mesh,Vector2 side,float scale,Color32 color)
        {
            int first=mesh.currentVertCount;
            mesh.AddVert(grip,color,Vector2.zero);
            foreach(Vector2 p in Knuckles)mesh.AddVert(At(p.x*scale,p.y*scale,side),color,Vector2.zero);
            for(int i=0;i<Knuckles.Length;i++)mesh.AddTriangle(first,first+1+i,first+1+(i+1)%Knuckles.Length);
        }

        private static void Quad(VertexHelper mesh,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color32 color)
        {
            int first=mesh.currentVertCount;
            mesh.AddVert(a,color,Vector2.zero);mesh.AddVert(b,color,Vector2.zero);
            mesh.AddVert(c,color,Vector2.zero);mesh.AddVert(d,color,Vector2.zero);
            mesh.AddTriangle(first,first+1,first+2);mesh.AddTriangle(first,first+2,first+3);
        }
    }
}
