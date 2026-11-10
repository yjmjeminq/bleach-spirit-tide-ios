using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace SpiritTide
{
    // Crop the same painted icon to a soft circle without adding stencil masks to the HUD.
    public sealed class TideSkillMedallion : Image
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            Sprite art=overrideSprite!=null?overrideSprite:sprite;
            if(art==null){base.OnPopulateMesh(mesh);return;}
            mesh.Clear();

            Rect rect=GetPixelAdjustedRect();
            float radius=Mathf.Min(rect.width,rect.height)*.5f;
            if(radius<=0)return;
            float inner=Mathf.Max(0,radius-1);
            Vector4 uv=DataUtility.GetOuterUV(art);
            Vector2 uvCenter=new Vector2((uv.x+uv.z)*.5f,(uv.y+uv.w)*.5f);
            float sourceWidth=art.rect.width,sourceHeight=art.rect.height;
            Vector2 uvRadius=new Vector2((uv.z-uv.x)*Mathf.Min(1,sourceHeight/sourceWidth),
                (uv.w-uv.y)*Mathf.Min(1,sourceWidth/sourceHeight))*.5f;
            Color edge=color;edge.a=0;

            const int segments=36;
            mesh.AddVert(rect.center,color,uvCenter);
            for(int i=0;i<=segments;i++)
            {
                float angle=i*(2*Mathf.PI/segments);
                Vector2 direction=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle));
                Vector2 sample=Vector2.Scale(direction,uvRadius);
                mesh.AddVert(rect.center+direction*inner,color,uvCenter+sample*(inner/radius));
                mesh.AddVert(rect.center+direction*radius,edge,uvCenter+sample);
                if(i==0)continue;
                int current=i*2+1,previous=current-2;
                mesh.AddTriangle(0,current,previous);
                mesh.AddTriangle(previous,current,previous+1);
                mesh.AddTriangle(previous+1,current,current+1);
            }
        }
    }
}
