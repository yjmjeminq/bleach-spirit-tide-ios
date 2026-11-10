using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    // One pooled mesh per plane: no particle GameObjects or per-frame materials.
    public sealed partial class TideSkillFxGraphic:MaskableGraphic
    {
        private TideSkillFxDef effect;private float age,duration,fade,power;private bool ground,reduced;
        private Vector2 source,target;private int facing,seed;private VertexHelper mesh;
        public int VertexCount {get;private set;}
        public string Motif=>effect==null?"":effect.motif;
        public void Frame(TideSkillFxDef effect,float age,float duration,Vector2 source,Vector2 target,int facing,bool ground,bool reduced,int seed)
        {
            this.effect=effect;this.age=age;this.duration=duration;this.source=source;this.target=target;this.facing=facing;
            this.ground=ground;this.reduced=reduced;this.seed=seed;raycastTarget=false;SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();VertexCount=0;if(effect==null)return;mesh=vh;
            float end=duration+effect.tail;fade=Mathf.Clamp01(age/.055f)*Mathf.Clamp01((end-age)/.32f)*(reduced?.46f:1);
            if(fade<=0)return;power=effect.slot==4?1.35f:1;
            if(effect.slot>=CharacterDef.FirstExtraSkill)ExtraEffect();
            else switch(effect.hero){case 0:Frost();break;case 1:Flowers();break;case 2:Thunder();break;case 3:Hornet();break;case 4:Ash();break;case 5:Crimson();break;}
            VertexCount=vh.currentVertCount;
        }
        private float Progress=>age/Mathf.Max(.01f,duration);
        private float Burst=>Mathf.Clamp01((age-duration*.32f)/.18f);
        private Color C(Color c,float alpha=1){c.a=alpha*fade;return c;}
        private Vector2 Dir(float degrees){float a=degrees*Mathf.Deg2Rad;return new Vector2(Mathf.Cos(a),Mathf.Sin(a));}
        private Vector2 Forward(float x,float y=0)=>source+new Vector2(facing*x,y);
        private void Triangle(Vector2 a,Vector2 b,Vector2 c,Color ca,Color cb,Color cc)
        {
            if(mesh.currentVertCount+3>TideSkillFx.MaxVerticesPerLayer)return;
            int start=mesh.currentVertCount;mesh.AddVert(a,ca,Vector2.zero);mesh.AddVert(b,cb,Vector2.zero);mesh.AddVert(c,cc,Vector2.zero);mesh.AddTriangle(start,start+1,start+2);
        }
        private void Line(Vector2 a,Vector2 b,float width,Color color)
        {
            if(mesh.currentVertCount+4>TideSkillFx.MaxVerticesPerLayer)return;
            Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;int s=mesh.currentVertCount;
            mesh.AddVert(a+n,color,Vector2.zero);mesh.AddVert(a-n,color,Vector2.zero);mesh.AddVert(b-n,color,Vector2.zero);mesh.AddVert(b+n,color,Vector2.zero);
            mesh.AddTriangle(s,s+1,s+2);mesh.AddTriangle(s,s+2,s+3);
        }
        private void Beam(Vector2 a,Vector2 b,float width,Color tint)
        {if(!reduced)Line(a,b,width*4,C(tint,.08f));Line(a,b,width,C(tint,.66f));Line(a,b,Mathf.Max(1,width*.2f),C(Color.white,.75f));}
        private void Arc(Vector2 center,Vector2 radius,float start,float sweep,float width,Color color,int segments=28)
        {
            if(reduced)segments=Mathf.Min(segments,16);Vector2 a=center+Vector2.Scale(Dir(start),radius);
            for(int i=1;i<=segments;i++){Vector2 b=center+Vector2.Scale(Dir(start+sweep*i/segments),radius);Line(a,b,width,color);a=b;}
        }
        private void Ring(Vector2 center,Vector2 radius,Color color,float width=2)
        {Arc(center,radius,0,360,width,color,36);}
        private void Glow(Vector2 at,Vector2 radius,Color color,float opacity=.12f)
        {
            int count=reduced?12:24;Color edge=C(color,0),core=C(color,opacity);
            for(int i=0;i<count;i++)Triangle(at,at+Vector2.Scale(Dir(i*360f/count),radius),at+Vector2.Scale(Dir((i+1)*360f/count),radius),core,edge,edge);
        }
        private void Crystal(Vector2 at,float height,float width,float tilt,Color tint)
        {
            Vector2 up=Dir(tilt)*height,right=Dir(tilt-90)*width,baseAt=at-up*.16f;
            Triangle(at+up,baseAt-right,at,C(Color.white,.8f),C(tint,.12f),C(tint,.55f));
            Triangle(at+up,at,baseAt+right,C(tint,.9f),C(tint,.6f),C(effect.accent,.18f));
            Line(at+up,baseAt-right,1,C(Color.white,.6f));
        }
        private void Snow(Vector2 at,float radius,float turn,Color color)
        {
            for(int i=0;i<6;i++)
            {Vector2 d=Dir(turn+i*60);Line(at,at+d*radius,1.5f,color);Vector2 tip=at+d*radius*.65f;Line(tip,tip+Dir(turn+i*60+135)*radius*.3f,1,color);Line(tip,tip+Dir(turn+i*60-135)*radius*.3f,1,color);}
        }
        private void Flower(Vector2 at,float radius,int petals,float turn,Color color)
        {
            for(int i=0;i<petals;i++)
            {float angle=turn+i*360f/petals;Vector2 tip=at+Dir(angle)*radius,side=Dir(angle+90)*radius*.19f,mid=at+Dir(angle)*radius*.52f;Triangle(at,mid+side,tip,C(color,.12f),color,C(Color.white,.75f));Triangle(at,tip,mid-side,C(color,.12f),C(Color.white,.75f),color);}
            Ring(at,Vector2.one*radius*.2f,C(Color.white,.7f));
        }
        private void Lightning(Vector2 from,Vector2 to,int bends,float offset,Color tint)
        {
            Vector2 direction=to-from,n=new Vector2(-direction.y,direction.x).normalized,previous=from;
            for(int i=1;i<=bends;i++)
            {float t=i/(float)bends;Vector2 next=Vector2.Lerp(from,to,t)+n*(i==bends?0:Mathf.Sin(i*12.32f+seed+Mathf.Floor(age*18))*offset);Beam(previous,next,reduced?1.6f:2.3f,tint);if(i%3==0&&!reduced)Line(next,next+n*offset*.7f,1,C(Color.white,.3f));previous=next;}
        }
        private void Sparks(Vector2 at,int count,float span,Color tint,bool inward=false)
        {
            if(reduced)count=Mathf.Max(3,count/3);
            for(int i=0;i<count;i++)
            {
                float p=Mathf.Repeat(age*(1.3f+i*.06f)+i*.071f,1),angle=i*137.5f+seed;
                Vector2 dir=Dir(angle),pos=at+new Vector2(dir.x,dir.y*.65f)*(inward?(1-p)*span:p*span);
                float a=Mathf.Sin(p*Mathf.PI);Line(pos-dir*(3+p*12),pos+dir*3,1.5f,C(tint,a*.65f));
            }
        }
        private void Sigil(Vector2 at,float radius,Color tint,int points=6)
        {
            Ring(at,new Vector2(radius,radius*.28f),C(tint,.5f));Ring(at,new Vector2(radius*.8f,radius*.22f),C(tint,.2f));
            for(int i=0;i<points;i++){Vector2 a=at+Vector2.Scale(Dir(i*360f/points),new Vector2(radius,radius*.28f));Vector2 b=at+Vector2.Scale(Dir((i+2)*360f/points),new Vector2(radius,radius*.28f));Line(a,b,1,C(tint,.36f));}
        }
        private void Slash(Vector2 at,float radius,float turn,Color tint,float width=6)
        {Arc(at,new Vector2(radius,radius*.65f),turn,150,width,C(tint,.7f));Arc(at,new Vector2(radius+4,radius*.65f+4),turn+8,130,1.4f,C(Color.white,.8f));}
    }
}
