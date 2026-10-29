using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DonerMaster
{
    /// <summary>
    /// One-pointer abstraction over touch and mouse. Works with the legacy Input Manager
    /// and with the Input System package, whichever the project has enabled.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class PointerInput : MonoBehaviour
    {
        public static bool Pressed { get; private set; }
        public static bool Down { get; private set; }
        public static bool Up { get; private set; }
        public static Vector2 Position { get; private set; }
        /// <summary>True when the current press started on a UI element.</summary>
        public static bool PressStartedOnUI { get; private set; }

        static readonly List<RaycastResult> UiHits = new List<RaycastResult>();

        void Update()
        {
            bool prev = Pressed;
            Read(out var pos, out var pressed);
            Position = pos;
            Pressed = pressed;
            Down = pressed && !prev;
            Up = !pressed && prev;
            if (Down) PressStartedOnUI = IsOverUI(pos);
        }

        static void Read(out Vector2 pos, out bool pressed)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                pos = t.position;
                pressed = t.phase != TouchPhase.Ended && t.phase != TouchPhase.Canceled;
                return;
            }
            pos = Input.mousePosition;
            pressed = Input.GetMouseButton(0);
#elif ENABLE_INPUT_SYSTEM
            var p = UnityEngine.InputSystem.Pointer.current;
            if (p == null) { pos = Vector2.zero; pressed = false; return; }
            pos = p.position.ReadValue();
            pressed = p.press.isPressed;
#else
            pos = Vector2.zero;
            pressed = false;
#endif
        }

        public static bool IsOverUI(Vector2 screenPos)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            var data = new PointerEventData(es) { position = screenPos };
            UiHits.Clear();
            es.RaycastAll(data, UiHits);
            for (int i = 0; i < UiHits.Count; i++)
            {
                // Only block on UI that actually wants input (buttons, panels marked as raycast targets)
                if (UiHits[i].gameObject != null) return true;
            }
            return false;
        }
    }
}
