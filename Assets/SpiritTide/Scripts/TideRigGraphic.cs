using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    // Source pixels stay untouched. Every vertex follows two continuous joint transforms.
    public sealed class TideRigGraphic:MaskableGraphic
    {
        public TideRigDef Rig {get;private set;}
        public readonly Vector2[] BonePositions=new Vector2[18];
        public readonly float[] BoneAngles=new float[18];
        private Texture2D source;
        private TideRigVertex[] vertices;private int[] triangles;
        private Vector2[] skinned=System.Array.Empty<Vector2>();
        private readonly Vector2[] basisX=new Vector2[18],basisY=new Vector2[18],offsets=new Vector2[18];
        public override Texture mainTexture=>source!=null?source:Texture2D.whiteTexture;
        public int VertexCount=>skinned.Length;
        public void Bind(int hero,bool hair=false)
        {
            Rig=TideRigData.Get(hero);source=TideRigData.Texture(hero,hair);
            vertices=hair?Rig.hair_vertices:Rig.vertices;triangles=hair?Rig.hair_triangles:Rig.triangles;skinned=new Vector2[vertices.Length];
            for(int i=0;i<18;i++){BonePositions[i]=Rig.bones[i].Value;BoneAngles[i]=0;}
            raycastTarget=false;SetAllDirty();
        }
        public static Vector2 Rotate(Vector2 value,float degrees)
        {
            float rad=degrees*Mathf.Deg2Rad,s=Mathf.Sin(rad),c=Mathf.Cos(rad);
            return new Vector2(value.x*c-value.y*s,value.x*s+value.y*c);
        }
        public Vector2 Skin(Vector2 value,int bone)=>BonePositions[bone]+Rotate(value-Rig.bones[bone].Value,BoneAngles[bone]);
        public void PoseChanged(){if(Rig!=null)SetVerticesDirty();}
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();if(Rig==null)return;
            for(int i=0;i<18;i++)
            {
                float angle=BoneAngles[i]*Mathf.Deg2Rad,c=Mathf.Cos(angle),s=Mathf.Sin(angle);
                basisX[i]=new Vector2(c,s);basisY[i]=new Vector2(-s,c);
                Vector2 rest=Rig.bones[i].Value;offsets[i]=BonePositions[i]-basisX[i]*rest.x-basisY[i]*rest.y;
            }
            for(int i=0;i<vertices.Length;i++)
            {
                var v=vertices[i];Vector2 a=offsets[v.a]+basisX[v.a]*v.p.x+basisY[v.a]*v.p.y,b=offsets[v.b]+basisX[v.b]*v.p.x+basisY[v.b]*v.p.y;
                Vector2 p=Vector2.LerpUnclamped(b,a,v.w);skinned[i]=p;
                var vertex=UIVertex.simpleVert;vertex.position=p;vertex.uv0=new Vector2(v.u,v.v);vertex.color=color;helper.AddVert(vertex);
            }
            for(int i=0;i<triangles.Length;i+=3)helper.AddTriangle(triangles[i],triangles[i+1],triangles[i+2]);
        }
        public Vector2 SkinnedVertex(int index)=>skinned[index];
    }
}
