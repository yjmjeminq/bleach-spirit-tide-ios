using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritTide
{
    // A continuous, source-preserving surface for the existing enemy paintings.
    // Locomotion follows distance; tells, attacks and recoil follow combat events.
    public sealed class TideFoeMotionGraphic:Image
    {
        public enum Anatomy { Humanoid, Robed, Heavy, Winged, Serpent, Tentacles, Beast }
        private readonly List<UIVertex> quad=new List<UIVertex>(6);
        private TideEnemy enemy;private string definition;
        private Vector2 previous,stepDirection=Vector2.left;
        private float worldPixels=48,bodyWidth=150,bodyHeight=190;
        private float clock,phase,travel,lean,recoil,recoilPower,follow,tell,turnAge;
        private int epoch=-1,attacks,damage,recoilSide;
        public int VertexCount {get;private set;}
        public float Travel=>travel;
        public float MotionPhase=>phase;
        public int DisplayFacing {get;private set;}=1;
        public Anatomy BodyType {get;private set;}
        public static Anatomy Classify(string id)
        {
            switch(id)
            {
                case "a1_e09":case "a1_mid_shrieker":case "a3_boss_ulquiorra":return Anatomy.Winged;
                case "a1_e02":return Anatomy.Serpent;
                case "a1_e04":case "a1_e06":case "a1_e11":case "a5_e06":return Anatomy.Tentacles;
                case "a1_e03":case "a1_e10":case "a2_e12":case "a3_e04":case "a3_e10":case "a4_e09":return Anatomy.Beast;
                case "a1_e01":case "a1_e05":case "a1_e08":case "a1_e12":case "a1_boss_grand_fisher":case "a2_e01":case "a2_e03":case "a2_e07":case "a3_e03":case "a4_e04":case "a5_e07":return Anatomy.Heavy;
                case "a1_e07":case "a2_e02":case "a2_e04":case "a2_e06":case "a2_e08":case "a3_e05":case "a3_e06":case "a3_e07":case "a3_e08":case "a3_e11":case "a3_e12":case "a4_e02":case "a4_e03":case "a4_e05":case "a4_e08":case "a4_e10":case "a4_mid_gin":case "a4_boss_aizen":case "a5_e01":case "a5_e02":case "a5_e09":case "a5_e10":case "a5_mid_haschwalth":case "a5_boss_yhwach":return Anatomy.Robed;
                default:return Anatomy.Humanoid;
            }
        }
        public void Tick(TideEnemy value,int worldEpoch,float time,float pixelsPerWorldUnit,int look)
        {
            worldPixels=pixelsPerWorldUnit;
            if(enemy!=value||epoch!=worldEpoch||definition!=value.Def.id)
            {
                enemy=value;definition=value.Def.id;epoch=worldEpoch;previous=value.Position;clock=time;
                attacks=value.Attacks;damage=value.DamageSerial;phase=value.Uid*.71f;travel=lean=recoil=recoilPower=follow=tell=turnAge=0;
                DisplayFacing=look;stepDirection=Vector2.left;
                BodyType=Classify(value.Def.id);SetVerticesDirty();return;
            }
            float dt=Mathf.Clamp(time-clock,0,.05f);if(dt<=0)return;
            Vector2 delta=value.Position-previous;Vector2 pixels=new Vector2(delta.x*worldPixels,delta.y*35);float distance=pixels.magnitude/worldPixels;
            if(damage!=value.DamageSerial)
            {
                damage=value.DamageSerial;
                if(value.ImpactStrength!=TideImpactStrength.None)
                {
                    recoil=1;recoilPower=.52f+(int)value.ImpactStrength*.26f;
                    recoilSide=value.ImpactDirection*DisplayFacing;
                }
            }
            if(attacks!=value.Attacks){attacks=value.Attacks;follow=1;}
            else follow=Mathf.Max(0,follow-dt*(BodyType==Anatomy.Heavy?3.5f:4.5f));
            recoil=Mathf.Max(0,recoil-dt*4.8f);
            // Look away only after finishing the committed attack. Retreat and
            // knockback may move opposite to the direction of the silhouette.
            turnAge=look==DisplayFacing||value.Telegraph||follow>.24f||recoil>.35f?0:turnAge+dt;
            if(turnAge>.065f){DisplayFacing=look;turnAge=0;}
            bool walking=distance<.65f&&value.Lift<.03f&&value.Stagger<=0&&Mathf.Abs(value.KnockVelocity)<.12f;
            if(walking&&pixels.sqrMagnitude>.000001f)
            {
                Vector2 direction=new Vector2(pixels.x*DisplayFacing,pixels.y).normalized;
                stepDirection=Vector2.Lerp(stepDirection,direction,1-Mathf.Exp(-19*dt));
                phase=Mathf.Repeat(phase+distance*(BodyType==Anatomy.Heavy?5.1f:7.2f),2*Mathf.PI);
            }
            float moving=walking?Mathf.Clamp01(distance/Mathf.Max(.001f,dt)/.8f):0;
            travel=Mathf.Lerp(travel,moving,1-Mathf.Exp(-17*dt));
            float warningProgress=1-value.Timer/Mathf.Max(.01f,value.TellDuration);
            tell=value.Telegraph?Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.88f,warningProgress)):0;
            float strike=Mathf.SmoothStep(0,1,follow);
            float target=-tell*(value.Pattern==1?4:8)+strike*(value.Pattern==1?7:14)-Mathf.Sin(recoil*Mathf.PI)*9*recoilPower*recoilSide;
            lean=Mathf.Lerp(lean,target,1-Mathf.Exp(-(strike>0?32:19)*dt));
            previous=value.Position;clock=time;SetVerticesDirty();
        }
        public Vector2 Deform(Vector2 p)
        {
            float t=clock,cycle=Mathf.Sin(phase),side=(float)System.Math.Tanh((p.x-.5f)*18);
            float strike=Mathf.SmoothStep(0,1,follow);
            float torso=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.29f,.66f,p.y));
            Vector2 pivot=new Vector2(.5f,.4f);
            Vector2 rotated=TideRigGraphic.Rotate(p-pivot,lean)+pivot;
            p=Vector2.Lerp(p,rotated,torso);
            float breath=Mathf.Sin(t*2.1f+(enemy==null?0:enemy.Uid))*.0022f;
            float flinch=recoil*recoil*recoilPower*torso;
            p.x+=flinch*recoilSide*.044f;
            p.y+=torso*(breath-.013f*tell-.009f*strike)-flinch*.017f;
            float below=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.44f,p.y));
            if(BodyType==Anatomy.Winged)
            {
                float outer=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.18f,.43f,Mathf.Abs(p.x-.5f)))*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,.77f,p.y));
                p.x+=side*outer*(Mathf.Sin(t*4.6f)*.018f-.035f*tell+.048f*strike);
                p.y+=outer*(Mathf.Sin(t*4.6f+.45f)*.026f+.038f*tell-.04f*strike);
            }
            if(BodyType==Anatomy.Serpent||BodyType==Anatomy.Tentacles)
            {
                float edge=BodyType==Anatomy.Serpent?1-torso:Mathf.SmoothStep(0,1,Mathf.InverseLerp(.16f,.4f,Mathf.Abs(p.x-.5f)));
                p.x+=Mathf.Sin(t*3.4f+p.y*9)*.013f*edge;p.y+=Mathf.Sin(t*3.4f+p.x*8)*.009f*edge;
                p.x-=(.075f*strike-.024f*tell)*torso*(BodyType==Anatomy.Serpent?1:edge);
            }
            else
            {
                // Linear support motion cancels the actual ground displacement;
                // the return arc lifts the other foot instead of sliding both.
                float stride=2*Mathf.PI/(BodyType==Anatomy.Heavy?5.1f:7.2f)*worldPixels;
                Vector2 near=FootCycle(phase/(2*Mathf.PI),stride),far=FootCycle(phase/(2*Mathf.PI)+.5f,stride);
                Vector2 feet=Vector2.Lerp(near,far,side*.5f+.5f)*travel*below;
                p.x+=feet.x/Mathf.Max(1,bodyWidth);p.y+=feet.y/Mathf.Max(1,bodyHeight);
                float arm=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.2f,.42f,Mathf.Abs(p.x-.5f)))*Mathf.Sin(Mathf.Clamp01((p.y-.34f)/.55f)*Mathf.PI);
                p.x+=arm*(.024f*tell-.055f*strike);
                p.y+=arm*(-cycle*side*travel*.009f+.032f*tell-.030f*strike);
            }
            return p;
        }
        private Vector2 FootCycle(float cycle,float stride)
        {
            float c=Mathf.Repeat(cycle,1),support=BodyType==Anatomy.Heavy?.64f:.58f,off,lift=0;
            if(c<support)off=(support*.5f-c)*stride;
            else {float t=(c-support)/(1-support),q=t*t*t*(t*(t*6-15)+10);off=(q-.5f)*support*stride;lift=Mathf.Sin(t*Mathf.PI)*(BodyType==Anatomy.Robed?4:7);}
            return stepDirection*off+Vector2.up*lift;
        }
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            base.OnPopulateMesh(helper);quad.Clear();helper.GetUIVertexStream(quad);if(quad.Count<3||enemy==null)return;
            Vector2 min=quad[0].position,max=min,uvMin=quad[0].uv0,uvMax=uvMin;
            foreach(var v in quad){min=Vector2.Min(min,v.position);max=Vector2.Max(max,v.position);uvMin=Vector2.Min(uvMin,v.uv0);uvMax=Vector2.Max(uvMax,v.uv0);}
            bodyWidth=max.x-min.x;bodyHeight=max.y-min.y;
            helper.Clear();const int columns=16,rows=24;VertexCount=(columns+1)*(rows+1);
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                Vector2 at=new Vector2(x/(float)columns,y/(float)rows),p=Deform(at);var v=UIVertex.simpleVert;
                v.position=min+Vector2.Scale(max-min,p);v.uv0=uvMin+Vector2.Scale(uvMax-uvMin,at);v.color=color;helper.AddVert(v);
            }
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++){int i=y*(columns+1)+x;helper.AddTriangle(i,i+columns+2,i+1);helper.AddTriangle(i,i+columns+1,i+columns+2);}
        }
    }
}
