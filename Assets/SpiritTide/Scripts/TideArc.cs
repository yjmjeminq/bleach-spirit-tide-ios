using UnityEngine;
using UnityEngine.UI;
namespace SpiritTide
{
    public sealed class TideArc:MaskableGraphic
    {
        public float sweep=125,thickness=.08f;public int segments=24;
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();int count=Mathf.Max(3,segments);Vector2 radius=rectTransform.rect.size*.5f;
            for(int i=0;i<=count;i++)
            {
                float t=i/(float)count,angle=(-sweep*.5f+sweep*t)*Mathf.Deg2Rad;Vector2 p=new Vector2(Mathf.Cos(angle)*radius.x,Mathf.Sin(angle)*radius.y);Color tint=color;tint.a*=Mathf.Sin(t*Mathf.PI)*.75f+.25f;
                vh.AddVert(p,tint,Vector2.zero);vh.AddVert(p*(1-thickness),tint,Vector2.zero);
                if(i>0){int v=i*2;vh.AddTriangle(v-2,v-1,v);vh.AddTriangle(v,v-1,v+1);}
            }
        }
        public void Shape(float arc,float width,int count=24){sweep=arc;thickness=width;segments=count;SetVerticesDirty();}
    }
}
