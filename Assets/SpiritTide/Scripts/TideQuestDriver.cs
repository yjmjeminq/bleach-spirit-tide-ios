// Input-only helpers shared by model and live player verification.
using UnityEngine;
namespace SpiritTide
{
    public static class TideQuestDriver
    {
        public static void PracticeTick(TideSession s,string kind,float dt=1f/60)
        {
            if(!s.IsTraining||!s.IsFighting)return;var e=s.Target;float dx=e.X-s.X,dy=e.Y-s.Y;
            var move=new Vector2(Mathf.Abs(dx)>1.35f?Mathf.Sign(dx):0,Mathf.Abs(dy)>.1f?Mathf.Sign(dy):0);
            if(kind=="perfect")
            {
                if(e.Telegraph&&e.Pattern==0&&e.Timer<.17f&&e.Timer>.07f&&s.DodgeRemaining<=0)s.Dodge(new Vector2(Mathf.Sign(dx),0));
            }
            else if(Mathf.Abs(dx)<2&&Mathf.Abs(dy)<.6f)
            {
                if(s.Action==MoveKind.Basic&&s.ActionRemaining>0&&s.ActionElapsed>s.ActionDuration*.4f&&s.DamageEvents>0)s.Request(MoveKind.Skill0);
                else if(s.ActionRemaining<=0)s.Request(MoveKind.Basic);
            }
            s.Step(dt,move,false);
        }
        public static void MoveTo(TideSession s,Vector2 destination,float dt=1f/60)
        {
            var delta=destination-new Vector2(s.X,s.Y);s.Step(dt,delta.magnitude>.15f?delta.normalized:Vector2.zero,false);
        }
    }
}
