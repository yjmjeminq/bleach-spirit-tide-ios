using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SpiritTide
{
    public sealed class TideFocusRing : MonoBehaviour,ISelectHandler,IDeselectHandler
    {
        private Outline ring;
        private void Awake(){ring=gameObject.AddComponent<Outline>();ring.effectColor=new Color(.95f,.78f,.44f);ring.effectDistance=new Vector2(2,-2);ring.enabled=false;}
        public void OnSelect(BaseEventData e){ring.enabled=true;}
        public void OnDeselect(BaseEventData e){ring.enabled=false;}
        private void OnDisable(){if(ring!=null)ring.enabled=false;}
    }
    public sealed class TideJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform Knob;
        public Image DirectionRing;
        public Vector2 Value { get; private set; }
        public int Owner { get; private set; } = int.MinValue;
        public float DeadZone = .16f;
        public void OnPointerDown(PointerEventData e)
        {
            if (Owner != int.MinValue) return;
            Owner = e.pointerId; UpdatePointer(e);
        }
        public void OnDrag(PointerEventData e) { if (Owner == e.pointerId) UpdatePointer(e); }
        public void OnPointerUp(PointerEventData e) { if (Owner == e.pointerId) ResetInput(); }
        private void UpdatePointer(PointerEventData e)
        {
            Vector2 local;
            RectTransform r = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(r, e.position, e.pressEventCamera, out local)) return;
            float radius = Mathf.Min(r.rect.width, r.rect.height) * .34f;
            Vector2 v = Vector2.ClampMagnitude(local / Mathf.Max(1, radius), 1);
            float magnitude = v.magnitude;
            Value = magnitude <= DeadZone ? Vector2.zero : v.normalized * ((magnitude - DeadZone) / (1 - DeadZone));
            if (Knob != null) Knob.anchoredPosition = v * radius;
            if (DirectionRing != null)
            {
                DirectionRing.enabled = Value.sqrMagnitude > 0;
                DirectionRing.fillAmount = Mathf.Lerp(.09f, .22f, Value.magnitude);
                float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                DirectionRing.rectTransform.localRotation = Quaternion.Euler(0, 0, angle + 180f * DirectionRing.fillAmount);
            }
        }
        public void ResetInput()
        {
            Owner = int.MinValue;
            Value = Vector2.zero;
            if (Knob != null) Knob.anchoredPosition = Vector2.zero;
            if (DirectionRing != null) DirectionRing.enabled = false;
        }
        private void OnDisable() { ResetInput(); }
    }
    public sealed class TideHold : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private readonly HashSet<int> owners = new HashSet<int>();
        public Action Down, Up;
        public bool Held => owners.Count > 0;
        public void OnPointerDown(PointerEventData e)
        {
            Selectable target=GetComponent<Selectable>();if(!isActiveAndEnabled||target!=null&&!target.IsInteractable())return;
            if (owners.Add(e.pointerId) && owners.Count == 1) Down?.Invoke();
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (owners.Remove(e.pointerId) && owners.Count == 0) Up?.Invoke();
        }
        public void ResetInput() { owners.Clear(); }
        private void OnDisable() { ResetInput(); }
    }
    public static class TideLayout
    {
        public static Rect NormalizedSafeArea(Rect area, int width, int height)
        {
            if (width <= 0 || height <= 0) return new Rect(0, 0, 1, 1);
            float left = Mathf.Clamp01(area.xMin / width), bottom = Mathf.Clamp01(area.yMin / height);
            float right = Mathf.Clamp01(area.xMax / width), top = Mathf.Clamp01(area.yMax / height);
            if (right <= left || top <= bottom) return new Rect(0, 0, 1, 1);
            return Rect.MinMaxRect(left, bottom, right, top);
        }
    }
}
