using UnityEngine;

namespace SpiritTide
{
    internal sealed class TideChestMotion
    {
        // Centers and radii in the original bind textures, in roster order.
        private static readonly Vector4[] Regions =
        {
            new Vector4(370,384,70,64), new Vector4(486,384,42,61),
            new Vector4(501,377,68,68), new Vector4(618,374,49,65),
            new Vector4(501,433,63,71), new Vector4(623,435,49,66),
            new Vector4(384,383,57,61), new Vector4(486,380,38,61),
            new Vector4(397,374,70,72), new Vector4(510,372,53,68),
            new Vector4(376,374,58,66), new Vector4(521,375,48,64)
        };
        private static readonly float[] Response = {.90f,1.08f,1f,.80f,1.12f,.96f};
        private Vector2 anchor, anchorVelocity, offset, velocity;

        public Vector2 LocalOffset(int facing) => new Vector2(offset.x*facing,offset.y);

        public void Reset(Vector2 center)
        {
            anchor=center;
            anchorVelocity=offset=velocity=Vector2.zero;
        }

        public void Rebase(Vector2 translation) => anchor+=translation;

        public void Advance(Vector2 center,float dt,int hero)
        {
            if(dt<=0){Reset(center);return;}
            Vector2 measured=Vector2.ClampMagnitude((center-anchor)/dt,520);
            anchor=center;
            Vector2 nextVelocity=Vector2.LerpUnclamped(anchorVelocity,measured,1-Mathf.Exp(-34*dt));
            Vector2 impulse=nextVelocity-anchorVelocity;
            anchorVelocity=nextVelocity;
            float response=Response[hero];
            velocity-=Vector2.Scale(impulse,new Vector2(.115f,.47f))*response;

            // Exact damped spring step for this velocity impulse.
            const float damping=15, frequency=18;
            float decay=Mathf.Exp(-damping*dt),sin=Mathf.Sin(frequency*dt),cos=Mathf.Cos(frequency*dt);
            Vector2 tangent=(velocity+damping*offset)/frequency;
            velocity=decay*(velocity*cos-(frequency*offset+damping*tangent)*sin);
            Vector2 displacement=decay*(offset*cos+tangent*sin);
            offset=new Vector2(
                Mathf.Clamp(displacement.x,-.80f*response,.80f*response),
                Mathf.Clamp(displacement.y,-2.8f*response,2.8f*response));
            if(offset.x!=displacement.x)velocity.x=0;
            if(offset.y!=displacement.y)velocity.y=0;
        }

        public static float Influence(TideRigDef rig,TideRigVertex vertex)
        {
            float torso=(vertex.a<=1?vertex.w:0)+(vertex.b<=1?1-vertex.w:0);
            if(torso<=0)return 0;
            var texel=new Vector2(vertex.u*rig.source_width,(1-vertex.v)*rig.source_height);
            float near=Lobe(texel,Regions[rig.hero*2]),far=Lobe(texel,Regions[rig.hero*2+1]);
            return torso*(near+far-near*far);
        }

        private static float Lobe(Vector2 texel,Vector4 region)
        {
            var distance=new Vector2((texel.x-region.x)/region.z,(texel.y-region.y)/region.w);
            float t=Mathf.Clamp01(1-distance.sqrMagnitude);
            return t*t*(3-2*t);
        }
    }
}
