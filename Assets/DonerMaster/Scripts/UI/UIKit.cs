using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace DonerMaster
{
    /// <summary>Code-built uGUI helpers: rounded panels, outlined labels, chunky casual-game buttons.</summary>
    public static class UIKit
    {
        static Font _font;
        static Sprite _rounded;
        static Sprite _circle;

        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { _font = null; }
                if (_font == null)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { _font = null; }
                }
                return _font;
            }
        }

        /// <summary>9-sliced rounded rectangle generated at runtime.</summary>
        public static Sprite Rounded
        {
            get
            {
                if (_rounded != null) return _rounded;
                const int size = 64, radius = 24;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                    float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                    float a = Mathf.Clamp01(radius - d + 0.5f);
                    px[y * size + x] = new Color(1, 1, 1, a);
                }
                tex.SetPixels(px);
                tex.Apply();
                _rounded = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                    new Vector4(radius, radius, radius, radius));
                return _rounded;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle != null) return _circle;
                const int size = 64;
                var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f));
                    px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(size / 2f - d));
                }
                tex.SetPixels(px);
                tex.Apply();
                _circle = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                return _circle;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }

        public static void Stretch(RectTransform rt, float inset = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }

        public static Image Panel(Transform parent, Color color, string name = "Panel", bool rounded = true, bool raycast = true)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = raycast;
            if (rounded)
            {
                img.sprite = Rounded;
                img.type = Image.Type.Sliced;
            }
            return img;
        }

        public static Text Label(Transform parent, string text, int size, Color color, TextAnchor anchor = TextAnchor.MiddleCenter,
            FontStyle style = FontStyle.Bold, bool outline = false)
        {
            var rt = Rect("Label", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0, 0, 0, 0.75f);
                o.effectDistance = new Vector2(3, -3);
            }
            return t;
        }

        public static Button Button(Transform parent, string label, Color bg, Color fg, UnityAction onClick, int fontSize = 52)
        {
            var img = Panel(parent, bg, "Button_" + label);
            var shadow = img.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.35f);
            shadow.effectDistance = new Vector2(0, -8);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var colors = btn.colors;
            colors.highlightedColor = new Color(1f, 1f, 1f, 1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            btn.colors = colors;
            if (onClick != null) btn.onClick.AddListener(onClick);
            btn.onClick.AddListener(() =>
            {
                AudioManager.Play(Sfx.Pop, 0.6f, 1.2f);
                Haptics.Light();
                Juice.Punch(btn.transform, 0.08f, 0.2f);
            });
            var t = Label(img.transform, label, fontSize, fg);
            Stretch(t.rectTransform, 8);
            return btn;
        }

        public static void SetButtonLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }
    }
}
