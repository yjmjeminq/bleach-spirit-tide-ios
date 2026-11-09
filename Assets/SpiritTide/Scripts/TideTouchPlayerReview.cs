using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed partial class TidePlayerSmoke
    {
        private IEnumerator ReviewTouchInput()
        {
            string saved=JsonUtility.ToJson(game.Session.Profile);game.ReturnTown();game.StartBattle(true);yield return null;
            RectTransform joy=(RectTransform)game.Joystick.transform;
            var left=new PointerEventData(EventSystem.current){pointerId=10,position=RectTransformUtility.WorldToScreenPoint(null,joy.TransformPoint(new Vector2(65,0)))};
            var right=new PointerEventData(EventSystem.current){pointerId=11,position=HitPoint(game.Buttons["attack"])};
            ExecuteEvents.Execute(joy.gameObject,left,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(game.Buttons["attack"].gameObject,right,ExecuteEvents.pointerDownHandler);
            float x=game.Session.X;yield return new WaitForSecondsRealtime(.45f);
            Check("two independent touch pointers move and attack together",game.Joystick.Owner==10&&game.Joystick.Value.x>.2f&&game.AttackHold.Held&&game.Session.X>x&&game.Session.AttackSerial>0);
            Check("joystick lights the direction ring and displaces the thumb cap",game.Joystick.DirectionRing.enabled&&game.Joystick.DirectionRing.fillAmount>.09f&&game.Joystick.Knob.anchoredPosition.x>40);
            ExecuteEvents.Execute(game.Buttons["attack"].gameObject,right,ExecuteEvents.pointerUpHandler);
            Check("releasing attack finger keeps movement finger active",!game.AttackHold.Held&&game.Joystick.Owner==10&&game.Joystick.Value.x>.2f);
            var other=new PointerEventData(EventSystem.current){pointerId=12,position=left.position};
            ExecuteEvents.Execute(joy.gameObject,other,ExecuteEvents.pointerUpHandler);
            Check("another finger cannot release the owned joystick",game.Joystick.Owner==10&&game.Joystick.Value.x>.2f);
            ExecuteEvents.Execute(game.Buttons["attack"].gameObject,right,ExecuteEvents.pointerDownHandler);yield return null;
            yield return TapUi("pause");
            Check("opening a modal releases all held touch pointers",game.Joystick.Owner==int.MinValue&&!game.AttackHold.Held&&game.Joystick.Value==Vector2.zero);
            Check("opening a modal clears joystick visuals",!game.Joystick.DirectionRing.enabled&&game.Joystick.Knob.anchoredPosition==Vector2.zero);
            x=game.Session.X;float clock=game.Session.Clock;yield return null;
            Check("touch pause freezes position and combat clock",game.Session.IsPaused&&game.Session.X==x&&game.Session.Clock==clock);
            yield return TapUi("resume");yield return null;
            Check("resuming does not revive a released touch",game.Joystick.Value==Vector2.zero&&!game.AttackHold.Held&&game.Session.X==x);
            if(Screen.width==2556&&Screen.height==1179)
            {
                Canvas canvas=game.GetComponentInChildren<Canvas>();bool large=true;
                foreach(Button button in game.GetComponentsInChildren<Button>())
                {
                    if(button.name.StartsWith("target-")&&button.name!="target-next")continue;
                    Rect r=Bounds((RectTransform)button.transform);large&=r.width/3>=44&&r.height/3>=44;
                }
                Check("iPhone 15 Pro touch controls meet 44 point targets at native 3x scale",large&&canvas.scaleFactor>1);
            }
            yield return Capture("86-multitouch-training");
            foreach(float scale in new[]{1f,1.1f})
            {
                game.Session.Profile.controlScale=scale;game.Session.Revision++;yield return null;
                Canvas canvas=game.GetComponentInChildren<Canvas>();
                Vector2 attack=HitPoint(game.Buttons["attack"]);
                for(int slot=0;slot<5;slot++)
                {
                    string id="skill-"+slot;
                    CheckHit(id);
                    Rect bounds=Bounds((RectTransform)game.Buttons[id].transform);
                    bool reachable=Vector2.Distance(attack,HitPoint(game.Buttons[id]))<=180*canvas.scaleFactor*scale;
                    bool large=Screen.width!=2556||Screen.height!=1179||bounds.width/3>=44&&bounds.height/3>=44;
                    Check("skill button stays in reach of attack at scale "+scale+" / "+slot,reachable&&large);
                }
                CheckTouchLayout("attack cluster scale "+scale);
            }
            game.Session.Profile.controlScale=1;game.Session.Revision++;yield return null;
            left.position=RectTransformUtility.WorldToScreenPoint(null,joy.TransformPoint(new Vector2(65,0)));
            ExecuteEvents.Execute(joy.gameObject,left,ExecuteEvents.pointerDownHandler);
            int untilReady=0;while(game.Session.ActionRemaining>0&&untilReady++<120)yield return null;
            int serial=game.Session.AttackSerial;
            yield return TapUi("skill-0");
            Check("a skill beside attack casts while the movement finger stays down",game.Session.AttackSerial>serial
                &&game.Session.Action==MoveKind.Skill0&&game.Joystick.Owner==10&&game.Joystick.DirectionRing.enabled);
            yield return Capture("86-combat-dock-active");
            ExecuteEvents.Execute(joy.gameObject,left,ExecuteEvents.pointerUpHandler);
            game.ReturnTown();JsonUtility.FromJsonOverwrite(saved,game.Session.Profile);game.Session.ClearQuestNotifications();game.ReturnTown();yield return null;
        }
    }
}
