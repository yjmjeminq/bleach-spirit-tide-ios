using System.Collections;
using System.Collections.Generic;
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
            var stickOpacity=joy.GetComponent<CanvasGroup>();
            yield return new WaitForSecondsRealtime(.2f);
            float restingOpacity=stickOpacity.alpha;
            Check("idle joystick has a faint fill and reduced opacity",restingOpacity<.55f&&joy.GetComponent<Image>().color.a<.2f);
            var edgeTouch=new PointerEventData(EventSystem.current)
                {position=RectTransformUtility.WorldToScreenPoint(null,joy.TransformPoint(new Vector2(103,0)))};
            var hits=new List<RaycastResult>();
            EventSystem.current.RaycastAll(edgeTouch,hits);
            Check("the faded movement pad still receives touch near its outer edge",hits.Count>0&&hits[0].gameObject==joy.gameObject);
            var left=new PointerEventData(EventSystem.current){pointerId=10,position=RectTransformUtility.WorldToScreenPoint(null,joy.TransformPoint(new Vector2(65,0)))};
            var right=new PointerEventData(EventSystem.current){pointerId=11,position=HitPoint(game.Buttons["attack"])};
            ExecuteEvents.Execute(joy.gameObject,left,ExecuteEvents.pointerDownHandler);
            ExecuteEvents.Execute(game.Buttons["attack"].gameObject,right,ExecuteEvents.pointerDownHandler);
            float x=game.Session.X;yield return new WaitForSecondsRealtime(.45f);
            Check("two independent touch pointers move and attack together",game.Joystick.Owner==10&&game.Joystick.Value.x>.2f&&game.AttackHold.Held&&game.Session.X>x&&game.Session.AttackSerial>0);
            Check("joystick lights the direction ring and displaces the thumb cap",game.Joystick.DirectionRing.enabled&&game.Joystick.DirectionRing.fillAmount>.09f&&game.Joystick.Knob.anchoredPosition.x>40);
            Check("joystick brightens while the movement finger stays down",stickOpacity.alpha>.94f&&stickOpacity.alpha>restingOpacity+.4f);
            Transform attackGlyph=game.Buttons["attack"].transform.Find("AttackGlyph");
            var glint=game.Buttons["attack"].transform.Find("AttackGlint").GetComponent<TideArc>();
            Check("the attack glyph responds to the held touch",glint.color.a>.8f&&
                (game.Session.Profile.reducedEffects||attackGlyph.localScale.x<.96f));
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
            RectTransform skill=(RectTransform)game.Buttons["skill-0"].transform;
            var corner=new PointerEventData(EventSystem.current)
                {position=RectTransformUtility.WorldToScreenPoint(null,skill.TransformPoint(new Vector2(-39,-39)))};
            hits.Clear();EventSystem.current.RaycastAll(corner,hits);
            Check("the circular skill art keeps the full rectangular touch target",hits.Count>0&&hits[0].gameObject==skill.gameObject);
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
                for(int slot=0;slot<TideLoadouts.SlotCount;slot++)
                {
                    string id="skill-"+slot;
                    CheckHit(id);
                    Rect bounds=Bounds((RectTransform)game.Buttons[id].transform);
                    bool reachable=Vector2.Distance(attack,HitPoint(game.Buttons[id]))<=130*canvas.scaleFactor*scale;
                    bool large=Screen.width!=2556||Screen.height!=1179||bounds.width/3>=44&&bounds.height/3>=44;
                    Check("skill button stays in reach of attack at scale "+scale+" / "+slot,reachable&&large);
                    var size=((RectTransform)game.Buttons[id].transform).rect.size;
                    Check("skill tap target has the compact footprint "+scale+" / "+slot,size.x<=90&&size.y<=90);
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
            Check("releasing the joystick immediately clears movement and heading",game.Joystick.Value==Vector2.zero&&!game.Joystick.DirectionRing.enabled);
            yield return new WaitForSecondsRealtime(.24f);
            Check("joystick returns to its faint idle appearance",Mathf.Abs(stickOpacity.alpha-restingOpacity)<.03f);
            foreach(string id in new[]{"heavy","jump","dodge"})
            {
                CheckHit(id);
                var button=game.Buttons[id];
                Check("rounded utility shows its action glyph "+id,button.transform.Find("UtilityGlyph")!=null&&
                    !button.transform.Find("UtilityPlate").GetComponent<Image>().raycastTarget);
            }
            untilReady=0;
            while(game.Session.ActionRemaining>0&&untilReady++<120)yield return null;
            var heavy=game.Buttons["heavy"];
            var chargeTouch=new PointerEventData(EventSystem.current){pointerId=14,position=HitPoint(heavy)};
            ExecuteEvents.Execute(heavy.gameObject,chargeTouch,ExecuteEvents.pointerDownHandler);
            yield return new WaitForSecondsRealtime(.24f);
            var charge=heavy.transform.Find("ChargeTrack/ChargeFill").GetComponent<Image>();
            Check("heavy attack shows its charge in a live radial ring",game.Session.IsCharging&&
                charge.gameObject.activeInHierarchy&&charge.fillMethod==Image.FillMethod.Radial360&&
                charge.fillAmount>0&&Mathf.Abs(charge.fillAmount-game.Session.HeavyProgress)<.03f);
            ExecuteEvents.Execute(heavy.gameObject,chargeTouch,ExecuteEvents.pointerUpHandler);
            yield return null;
            Check("releasing heavy attack resolves the held input and hides its ring",
                game.Session.Action==MoveKind.Heavy&&!game.Session.IsCharging&&!charge.gameObject.activeInHierarchy);
            yield return ReviewBasicTaps();
            game.ReturnTown();JsonUtility.FromJsonOverwrite(saved,game.Session.Profile);game.Session.ClearQuestNotifications();game.ReturnTown();yield return null;
        }

        private IEnumerator ReviewBasicTaps()
        {
            var session=game.Session;
            game.ReviewStepping=true;
            session.Profile.recruitedHeroes=TideProgress.AllHeroes;
            try
            {
                for(int hero=0;hero<6;hero++)
                {
                    game.ReturnTown();session.SelectHero(hero);game.StartBattle(true);
                    session.Profile.holdAttack=false;
                    session.EnemyX=session.X+1.15f;session.EnemyY=session.Y;
                    yield return null;
                    CheckHit("attack");
                    var button=game.Buttons["attack"];
                    var finger=new PointerEventData(EventSystem.current){pointerId=21,position=HitPoint(button)};
                    var joy=(RectTransform)game.Joystick.transform;
                    var diagonal=new PointerEventData(EventSystem.current){pointerId=20,
                        position=RectTransformUtility.WorldToScreenPoint(null,joy.TransformPoint(new Vector2(70,70)))};
                    if(hero==4)ExecuteEvents.Execute(joy.gameObject,diagonal,ExecuteEvents.pointerDownHandler);
                    int serial=session.AttackSerial,damage=session.DamageEvents;
                    var chain=new List<int>(3);
                    for(int frame=0;frame<88;frame++)
                    {
                        if(frame==0||frame==4||frame==8)
                        {
                            ExecuteEvents.Execute(button.gameObject,finger,ExecuteEvents.pointerDownHandler);
                            ExecuteEvents.Execute(button.gameObject,finger,ExecuteEvents.pointerUpHandler);
                        }
                        if(frame==44&&hero==4)ExecuteEvents.Execute(joy.gameObject,diagonal,ExecuteEvents.pointerUpHandler);
                        session.Step(1f/60,game.Joystick.Value,game.AttackHold.Held);
                        if(session.AttackSerial!=serial)
                        {
                            serial=session.AttackSerial;
                            chain.Add(session.Action==MoveKind.Basic?session.Combo+1:0);
                        }
                        yield return null;
                    }
                    Check("three rapid touch taps deal all three basic hits for hero "+hero+" / "+string.Join(",",chain),
                        chain.Count==3&&chain[0]==1&&chain[1]==2&&chain[2]==3&&session.DamageEvents==damage+3);
                    if(hero==0)
                    {
                        session.Profile.holdAttack=true;
                        int heldStart=session.AttackSerial;
                        ExecuteEvents.Execute(button.gameObject,finger,ExecuteEvents.pointerDownHandler);
                        for(int frame=0;frame<88&&session.AttackSerial<heldStart+3;frame++)
                        {
                            session.Step(1f/60,Vector2.zero,game.AttackHold.Held);
                            yield return null;
                        }
                        Check("holding the real attack button reaches the third strike",session.AttackSerial==heldStart+3&&session.Combo==2);
                        ExecuteEvents.Execute(button.gameObject,finger,ExecuteEvents.pointerUpHandler);
                        for(int frame=0;frame<34;frame++)
                        {
                            session.Step(1f/60,Vector2.zero,game.AttackHold.Held);
                            yield return null;
                        }
                        Check("releasing attack lets the last strike finish without repeating",session.AttackSerial==heldStart+3&&!session.HasActionPose);
                    }
                }
            }
            finally {game.ReviewStepping=false;game.ResetInput();}
        }
    }
}
