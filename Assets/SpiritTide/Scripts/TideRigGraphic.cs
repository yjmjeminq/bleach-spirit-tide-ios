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
        private bool hairLayer;
        private float hairProjection=1,bladeProjection=1,shoeProjection;
        private float[] chestWeights,shoeOffsets;
        private TideRigVertex[] vertices;private int[] triangles;
        private Vector2[] skinned=System.Array.Empty<Vector2>();
        private readonly Vector2[] basisX=new Vector2[18],basisY=new Vector2[18],offsets=new Vector2[18];
        public override Texture mainTexture=>source!=null?source:Texture2D.whiteTexture;
        public int VertexCount=>skinned.Length;
        public Vector2 ChestOffset {get;private set;}
        public void Bind(int hero,bool hair=false)
        {
            Rig=TideRigData.Get(hero);source=TideRigData.Texture(hero,hair);
            hairLayer=hair;hairProjection=bladeProjection=1;shoeProjection=0;
            vertices=hair?Rig.hair_vertices:Rig.vertices;triangles=hair?Rig.hair_triangles:Rig.triangles;skinned=new Vector2[vertices.Length];
            ChestOffset=Vector2.zero;
            chestWeights=hair?null:new float[vertices.Length];
            shoeOffsets=hair?null:new float[vertices.Length];
            if(!hair)
            {
                Vector2 sole=Rig.soles[0].Value;
                for(int i=0;i<vertices.Length;i++)
                {
                    var vertex=vertices[i];
                    chestWeights[i]=TideChestMotion.Influence(Rig,vertex);
                    if(vertex.a!=12&&vertex.b!=12)continue;
                    // Turn the painted foreground shoe towards travel. The ankle
                    // and sole pivot keep their original positions on the rig.
                    float length=vertex.p.x-sole.x;
                    float influence=Mathf.SmoothStep(0,1,Mathf.InverseLerp(Rig.bones[12].y-.8f,sole.y+2,vertex.p.y));
                    shoeOffsets[i]=length*(length>0?.85f:-.22f)*influence;
                }
            }
            for(int i=0;i<18;i++){BonePositions[i]=Rig.bones[i].Value;BoneAngles[i]=0;}
            raycastTarget=false;SetAllDirty();
        }
        public static Vector2 Rotate(Vector2 value,float degrees)
        {
            float rad=degrees*Mathf.Deg2Rad,s=Mathf.Sin(rad),c=Mathf.Cos(rad);
            return new Vector2(value.x*c-value.y*s,value.x*s+value.y*c);
        }
        public Vector2 Skin(Vector2 value,int bone)=>BonePositions[bone]+Rotate(value-Rig.bones[bone].Value,BoneAngles[bone])*(bone==17?bladeProjection:1);
        public void PoseChanged(float hairFacing=1,float bladeScale=1,Vector2 chestOffset=default,float shoeYaw=0)
        {
            hairProjection=hairFacing;bladeProjection=bladeScale;shoeProjection=shoeYaw;
            ChestOffset=chestOffset;
            if(Rig!=null)SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();if(Rig==null)return;
            for(int i=0;i<18;i++)
            {
                float angle=BoneAngles[i]*Mathf.Deg2Rad,c=Mathf.Cos(angle),s=Mathf.Sin(angle);
                float projection=i==17?bladeProjection:1;
                basisX[i]=new Vector2(c,s)*projection;basisY[i]=new Vector2(-s,c)*projection;
                Vector2 rest=Rig.bones[i].Value;offsets[i]=BonePositions[i]-basisX[i]*rest.x-basisY[i]*rest.y;
            }
            Vector2 chestShift=basisX[1]*ChestOffset.x+basisY[1]*ChestOffset.y;
            bool morph=chestWeights!=null&&chestShift.sqrMagnitude>0;
            for(int i=0;i<vertices.Length;i++)
            {
                var v=vertices[i];
                float x=v.p.x+(shoeOffsets==null?0:shoeOffsets[i]*shoeProjection);
                Vector2 a=offsets[v.a]+basisX[v.a]*x+basisY[v.a]*v.p.y,b=offsets[v.b]+basisX[v.b]*x+basisY[v.b]*v.p.y;
                Vector2 p=Vector2.LerpUnclamped(b,a,v.w);
                if(hairLayer)p.x=Mathf.LerpUnclamped(BonePositions[16].x,p.x,hairProjection);
                if(morph)p+=chestShift*chestWeights[i];
                skinned[i]=p;
                var vertex=UIVertex.simpleVert;vertex.position=p;vertex.uv0=new Vector2(v.u,v.v);vertex.color=color;helper.AddVert(vertex);
            }
            for(int i=0;i<triangles.Length;i+=3)helper.AddTriangle(triangles[i],triangles[i+1],triangles[i+2]);
        }
        public Vector2 SkinnedVertex(int index)=>skinned[index];
    }
}
