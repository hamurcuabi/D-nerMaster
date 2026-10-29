using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DonerMaster
{
    /// <summary>All screen-space UI: HUD, order bubbles, world labels, hints, floating numbers, title and day-end screens.</summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI I { get; private set; }

        static readonly Color Cream = new Color(1f, 0.97f, 0.9f);
        static readonly Color Ink = new Color(0.16f, 0.11f, 0.08f);
        static readonly Color Red = new Color(0.86f, 0.24f, 0.18f);
        static readonly Color Gold = new Color(1f, 0.78f, 0.22f);
        static readonly Color Green = new Color(0.3f, 0.72f, 0.32f);
        static readonly Color Dim = new Color(0.05f, 0.03f, 0.02f, 0.7f);

        Camera _cam;
        RectTransform _root, _worldLayer, _floatLayer;
        Text _coins, _day, _combo, _hint;
        Image _timeFill;
        RectTransform _hintRt, _comboRt;
        float _shownCoins;
        string _hintText;

        // Screens
        RectTransform _title, _dayEnd;
        Text _titleDay, _titleCoins, _soundLabel, _hapticLabel;
        Text _endTitle, _endStats, _endCoins;
        Image[] _stars = new Image[3];
        readonly List<UpgradeRow> _upgradeRows = new List<UpgradeRow>();

        class UpgradeRow
        {
            public UpgradeId Id;
            public Text Name, Desc;
            public Button Buy;
            public Image[] Pips;
        }

        class WorldLabel
        {
            public Func<Vector3> Anchor;
            public Func<string> Getter;
            public Func<bool> Visible;
            public Text Text;
            public RectTransform Rt;
        }

        readonly List<WorldLabel> _labels = new List<WorldLabel>();

        class Bubble
        {
            public RectTransform Rt;
            public Text Text;
            public Image Fill;
            public bool Shown;
        }

        readonly Dictionary<Customer, Bubble> _bubbles = new Dictionary<Customer, Bubble>();

        public void Build(Camera cam)
        {
            I = this;
            _cam = cam;

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_LEGACY_INPUT_MANAGER
                es.AddComponent<StandaloneInputModule>();
#elif ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#endif
            }

            var canvasGo = new GameObject("Canvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            _root = (RectTransform)canvasGo.transform;

            _worldLayer = UIKit.Rect("WorldLayer", _root);
            UIKit.Stretch(_worldLayer);
            BuildHud();
            _floatLayer = UIKit.Rect("FloatLayer", _root);
            UIKit.Stretch(_floatLayer);
            BuildHint();
            BuildTitle();
            BuildDayEnd();
        }

        // ------------------------------------------------------------------ HUD

        void BuildHud()
        {
            var hud = UIKit.Rect("HUD", _root);
            UIKit.Place(hud, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0, -40), new Vector2(1020, 150));

            var coinPill = UIKit.Panel(hud, new Color(0.12f, 0.08f, 0.05f, 0.85f), "Coins");
            UIKit.Place(coinPill.rectTransform, new Vector2(0, 1), new Vector2(0, 1), Vector2.zero, new Vector2(330, 100));
            var coinIcon = UIKit.Panel(coinPill.transform, Gold, "Icon", false);
            coinIcon.sprite = UIKit.Circle;
            UIKit.Place(coinIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(18, 0), new Vector2(64, 64));
            var coinInner = UIKit.Label(coinIcon.transform, "$", 40, new Color(0.6f, 0.4f, 0.05f));
            UIKit.Stretch(coinInner.rectTransform);
            _coins = UIKit.Label(coinPill.transform, "0", 56, Color.white, TextAnchor.MiddleLeft);
            UIKit.Place(_coins.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(100, 0), new Vector2(220, 90));

            var dayPill = UIKit.Panel(hud, new Color(0.12f, 0.08f, 0.05f, 0.85f), "Day");
            UIKit.Place(dayPill.rectTransform, new Vector2(1, 1), new Vector2(1, 1), Vector2.zero, new Vector2(330, 100));
            _day = UIKit.Label(dayPill.transform, "DAY 1", 46, Color.white);
            UIKit.Stretch(_day.rectTransform, 6);

            var barBg = UIKit.Panel(hud, new Color(0, 0, 0, 0.45f), "TimeBar");
            UIKit.Place(barBg.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -118), new Vector2(1020, 22));
            _timeFill = UIKit.Panel(barBg.transform, Gold, "Fill");
            var fr = _timeFill.rectTransform;
            fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = Vector2.zero; fr.offsetMax = Vector2.zero;

            _combo = UIKit.Label(hud, "", 60, Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            _comboRt = _combo.rectTransform;
            UIKit.Place(_comboRt, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(340, 80));
        }

        void BuildHint()
        {
            var bg = UIKit.Panel(_root, new Color(0.12f, 0.08f, 0.05f, 0.88f), "Hint", true, false);
            _hintRt = bg.rectTransform;
            UIKit.Place(_hintRt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(980, 130));
            _hint = UIKit.Label(bg.transform, "", 46, Color.white);
            UIKit.Stretch(_hint.rectTransform, 18);
            _hintRt.gameObject.SetActive(false);
        }

        public void ShowHint(string text)
        {
            if (_hintText == text && _hintRt.gameObject.activeSelf) return;
            _hintText = text;
            _hint.text = text;
            if (!_hintRt.gameObject.activeSelf)
            {
                _hintRt.gameObject.SetActive(true);
                _hintRt.localScale = Vector3.one;
                Juice.PopIn(_hintRt, 0.25f);
            }
            else Juice.Punch(_hintRt, 0.06f, 0.2f);
        }

        public void HideHint()
        {
            _hintText = null;
            if (_hintRt != null) _hintRt.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ World labels & bubbles

        public void AddWorldLabel(Func<Vector3> anchor, Func<string> getter, Func<bool> visible = null, int size = 30, Color? color = null)
        {
            var t = UIKit.Label(_worldLayer, "", size, color ?? Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.Place(t.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 60));
            _labels.Add(new WorldLabel { Anchor = anchor, Getter = getter, Visible = visible, Text = t, Rt = t.rectTransform });
        }

        public void AddBubble(Customer c)
        {
            var bg = UIKit.Panel(_worldLayer, Cream, "Bubble", true, false);
            var rt = bg.rectTransform;
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0f);
            var outline = bg.gameObject.AddComponent<Shadow>();
            outline.effectColor = new Color(0, 0, 0, 0.35f);
            outline.effectDistance = new Vector2(0, -6);

            var tail = UIKit.Panel(rt, Cream, "Tail", false, false);
            UIKit.Place(tail.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(30, 30));
            tail.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
            tail.transform.SetAsFirstSibling();

            var text = UIKit.Label(rt, c.Order.Describe(), 34, Ink);
            text.rectTransform.anchorMin = new Vector2(0, 0);
            text.rectTransform.anchorMax = new Vector2(1, 1);
            text.rectTransform.offsetMin = new Vector2(14, 36);
            text.rectTransform.offsetMax = new Vector2(-14, -10);

            int lines = c.Order.Describe().Split('\n').Length;
            rt.sizeDelta = new Vector2(300, 44 * lines + 58);

            var barBg = UIKit.Panel(rt, new Color(0, 0, 0, 0.18f), "Patience", true, false);
            UIKit.Place(barBg.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(250, 16));
            var fill = UIKit.Panel(barBg.transform, Green, "Fill", true, false);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            rt.gameObject.SetActive(false);
            _bubbles[c] = new Bubble { Rt = rt, Text = text, Fill = fill };
        }

        public void RemoveBubble(Customer c)
        {
            if (c == null) return;
            if (_bubbles.TryGetValue(c, out var b))
            {
                if (b.Rt != null) Destroy(b.Rt.gameObject);
                _bubbles.Remove(c);
            }
        }

        /// <summary>The customer whose order bubble is under this screen point (tapping a bubble = tapping the customer).</summary>
        public Customer BubbleAt(Vector2 screen)
        {
            foreach (var kv in _bubbles)
            {
                var b = kv.Value;
                if (kv.Key == null || b.Rt == null || !b.Rt.gameObject.activeInHierarchy) continue;
                if (RectTransformUtility.RectangleContainsScreenPoint(b.Rt, screen, null)) return kv.Key;
            }
            return null;
        }

        public void PunchBubble(Customer c)
        {
            if (_bubbles.TryGetValue(c, out var b) && b.Rt != null) Juice.Punch(b.Rt, 0.12f, 0.25f);
        }

        bool WorldToCanvas(Vector3 world, out Vector2 screen)
        {
            var sp = _cam.WorldToScreenPoint(world);
            screen = sp;
            return sp.z > 0f;
        }

        void LateUpdate()
        {
            var gm = GameManager.I;
            if (gm == null || _cam == null) return;

            // HUD
            _shownCoins = Mathf.MoveTowards(_shownCoins, SaveSystem.Data.Coins, Mathf.Max(30f, Mathf.Abs(SaveSystem.Data.Coins - _shownCoins) * 4f) * Time.deltaTime);
            _coins.text = Mathf.RoundToInt(_shownCoins).ToString();
            int secs = Mathf.CeilToInt(Mathf.Max(0f, gm.TimeLeft));
            _day.text = gm.IsPlaying ? string.Format("DAY {0}  {1}:{2:00}", SaveSystem.Data.Day, secs / 60, secs % 60) : "DAY " + SaveSystem.Data.Day;
            _timeFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(gm.TimeLeft / GameData.DayLength), 1f);
            _timeFill.color = gm.TimeLeft < 30f ? Color.Lerp(Red, Gold, Mathf.PingPong(Time.time * 2f, 1f)) : Gold;
            _combo.text = gm.Combo > 1.01f ? string.Format("COMBO x{0:0.0}", gm.Combo) : "";
            if (gm.Combo > 1.01f) _comboRt.localScale = Vector3.one * (1f + 0.04f * Mathf.Sin(Time.time * 8f) * (gm.Combo - 1f));

            // World labels
            foreach (var l in _labels)
            {
                bool vis = gm.IsPlaying && (l.Visible == null || l.Visible());
                if (vis && WorldToCanvas(l.Anchor(), out var sp))
                {
                    if (!l.Rt.gameObject.activeSelf) l.Rt.gameObject.SetActive(true);
                    l.Rt.position = sp;
                    l.Text.text = l.Getter();
                }
                else if (l.Rt.gameObject.activeSelf) l.Rt.gameObject.SetActive(false);
            }

            // Bubbles
            foreach (var kv in _bubbles)
            {
                var c = kv.Key;
                var b = kv.Value;
                if (c == null || b.Rt == null) continue;
                bool show = c.IsWaiting && gm.IsPlaying;
                if (show && WorldToCanvas(c.BubbleAnchor, out var sp))
                {
                    if (!b.Shown)
                    {
                        b.Shown = true;
                        b.Rt.gameObject.SetActive(true);
                        Juice.PopIn(b.Rt, 0.3f);
                    }
                    // keep inside the screen
                    float halfW = b.Rt.sizeDelta.x * 0.5f * _root.localScale.x;
                    sp.x = Mathf.Clamp(sp.x, halfW + 10f, Screen.width - halfW - 10f);
                    b.Rt.position = sp;
                    float p = c.Patience01;
                    b.Fill.rectTransform.anchorMax = new Vector2(p, 1f);
                    b.Fill.color = p > 0.6f ? Green : p > 0.35f ? Gold : Red;
                }
                else if (b.Rt.gameObject.activeSelf && !c.IsWaiting)
                {
                    b.Rt.gameObject.SetActive(false);
                }
            }
        }

        // ------------------------------------------------------------------ Floating feedback

        public void Float(Vector3 world, string text, Color color, int size = 60, float rise = 160f, float duration = 1f)
        {
            if (!WorldToCanvas(world, out var sp)) return;
            var t = UIKit.Label(_floatLayer, text, size, color, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.Place(t.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 120));
            t.rectTransform.position = sp;
            StartCoroutine(FloatRoutine(t, rise * _root.localScale.y, duration));
        }

        public void BigText(string text, Color color, int size = 110)
        {
            var t = UIKit.Label(_floatLayer, text, size, color, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.Place(t.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 200));
            Juice.PopIn(t.rectTransform, 0.35f);
            StartCoroutine(FloatRoutine(t, 60f * _root.localScale.y, 1.1f, 0.5f));
        }

        IEnumerator FloatRoutine(Text t, float rise, float duration, float hold = 0f)
        {
            var rt = t.rectTransform;
            var start = rt.position;
            var c = t.color;
            float time = 0f;
            while (time < duration + hold)
            {
                if (t == null) yield break;
                time += Time.deltaTime;
                float k = Mathf.Clamp01((time - hold) / duration);
                rt.position = start + Vector3.up * rise * Juice.EaseOutCubic(Mathf.Clamp01(time / (duration + hold)));
                t.color = new Color(c.r, c.g, c.b, 1f - k * k);
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
        }

        // ------------------------------------------------------------------ Title

        void BuildTitle()
        {
            var dim = UIKit.Panel(_root, Dim, "Title", false);
            _title = dim.rectTransform;
            UIKit.Stretch(_title);

            var logoBg = UIKit.Panel(_title, Red, "LogoBg");
            UIKit.Place(logoBg.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 300));
            logoBg.rectTransform.localRotation = Quaternion.Euler(0, 0, 3f);
            var logo = UIKit.Label(logoBg.transform, "DONER\nMASTER", 120, Cream, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.Stretch(logo.rectTransform);
            var sub = UIKit.Label(_title, "KEBAB SHOP", 48, Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.Place(sub.rectTransform, new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), new Vector2(0, -200), new Vector2(800, 80));

            _titleDay = UIKit.Label(_title, "DAY 1", 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.Place(_titleDay.rectTransform, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(800, 90));
            _titleCoins = UIKit.Label(_title, "", 44, Gold, TextAnchor.MiddleCenter, FontStyle.Bold, true);
            UIKit.Place(_titleCoins.rectTransform, new Vector2(0.5f, 0.45f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(800, 70));

            var play = UIKit.Button(_title, "OPEN SHOP", Green, Color.white, () => GameManager.I.StartDay(), 70);
            UIKit.Place((RectTransform)play.transform, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 170));
            StartCoroutine(Breathe(play.transform));

            var sound = UIKit.Button(_title, "", new Color(0.25f, 0.2f, 0.18f), Color.white, ToggleSound, 38);
            UIKit.Place((RectTransform)sound.transform, new Vector2(0.5f, 0.18f), new Vector2(1f, 0.5f), new Vector2(-15, 0), new Vector2(330, 100));
            _soundLabel = sound.GetComponentInChildren<Text>();
            var haptic = UIKit.Button(_title, "", new Color(0.25f, 0.2f, 0.18f), Color.white, ToggleHaptics, 38);
            UIKit.Place((RectTransform)haptic.transform, new Vector2(0.5f, 0.18f), new Vector2(0f, 0.5f), new Vector2(15, 0), new Vector2(330, 100));
            _hapticLabel = haptic.GetComponentInChildren<Text>();

            var reset = UIKit.Button(_title, "reset progress", new Color(0, 0, 0, 0.25f), new Color(1, 1, 1, 0.6f), () =>
            {
                SaveSystem.ResetAll();
                _shownCoins = 0;
                ShowTitle();
            }, 30);
            UIKit.Place((RectTransform)reset.transform, new Vector2(0.5f, 0.06f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(320, 70));
            _title.gameObject.SetActive(false);
        }

        IEnumerator Breathe(Transform t)
        {
            while (t != null)
            {
                if (t.gameObject.activeInHierarchy) t.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 3f) * 0.03f);
                yield return null;
            }
        }

        void ToggleSound()
        {
            SaveSystem.Data.Muted = !SaveSystem.Data.Muted;
            AudioManager.Muted = SaveSystem.Data.Muted;
            SaveSystem.Save();
            RefreshToggles();
        }

        void ToggleHaptics()
        {
            SaveSystem.Data.HapticsOff = !SaveSystem.Data.HapticsOff;
            Haptics.Enabled = !SaveSystem.Data.HapticsOff;
            SaveSystem.Save();
            RefreshToggles();
        }

        void RefreshToggles()
        {
            _soundLabel.text = SaveSystem.Data.Muted ? "SOUND: OFF" : "SOUND: ON";
            _hapticLabel.text = SaveSystem.Data.HapticsOff ? "VIBRATION: OFF" : "VIBRATION: ON";
        }

        public void ShowTitle()
        {
            _title.gameObject.SetActive(true);
            _titleDay.text = "DAY " + SaveSystem.Data.Day;
            _titleCoins.text = SaveSystem.Data.Coins + " coins";
            RefreshToggles();
            HideHint();
            _title.GetChild(0).localScale = Vector3.one;
            Juice.PopIn(_title.GetChild(0), 0.45f);
        }

        public void HideTitle() => _title.gameObject.SetActive(false);

        // ------------------------------------------------------------------ Day end

        void BuildDayEnd()
        {
            var dim = UIKit.Panel(_root, Dim, "DayEnd", false);
            _dayEnd = dim.rectTransform;
            UIKit.Stretch(_dayEnd);

            var card = UIKit.Panel(_dayEnd, Cream, "Card");
            UIKit.Place(card.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(980, 1640));

            _endTitle = UIKit.Label(card.transform, "DAY 1 DONE!", 72, Red);
            UIKit.Place(_endTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(900, 100));

            for (int i = 0; i < 3; i++)
            {
                var star = UIKit.Panel(card.transform, Gold, "Star" + i, false, false);
                UIKit.Place(star.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(-150 + i * 150, -215), new Vector2(90, 90));
                star.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                _stars[i] = star;
            }

            _endCoins = UIKit.Label(card.transform, "", 64, Ink);
            UIKit.Place(_endCoins.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -300), new Vector2(900, 90));
            _endStats = UIKit.Label(card.transform, "", 38, new Color(0.35f, 0.28f, 0.22f), TextAnchor.UpperCenter, FontStyle.Normal);
            UIKit.Place(_endStats.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -400), new Vector2(900, 200));

            var upTitle = UIKit.Label(card.transform, "UPGRADES", 46, Ink);
            UIKit.Place(upTitle.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -620), new Vector2(900, 70));

            for (int i = 0; i < GameData.Upgrades.Count; i++)
            {
                var def = GameData.Upgrades[i];
                var row = UIKit.Panel(card.transform, new Color(0.93f, 0.88f, 0.8f), "Upgrade_" + def.Id);
                UIKit.Place(row.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -700 - i * 170), new Vector2(900, 150));
                var name = UIKit.Label(row.transform, def.Name, 40, Ink, TextAnchor.UpperLeft);
                UIKit.Place(name.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -18), new Vector2(560, 56));
                var desc = UIKit.Label(row.transform, def.Description, 28, new Color(0.4f, 0.32f, 0.25f), TextAnchor.UpperLeft, FontStyle.Normal);
                UIKit.Place(desc.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(28, -70), new Vector2(560, 40));
                var pips = new Image[def.MaxLevel];
                for (int p = 0; p < def.MaxLevel; p++)
                {
                    var pip = UIKit.Panel(row.transform, Gold, "Pip", false, false);
                    pip.sprite = UIKit.Circle;
                    UIKit.Place(pip.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(30 + p * 36, 14), new Vector2(26, 26));
                    pips[p] = pip;
                }
                var id = def.Id;
                var buy = UIKit.Button(row.transform, "", Green, Color.white, () => OnBuy(id), 40);
                UIKit.Place((RectTransform)buy.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(260, 110));
                _upgradeRows.Add(new UpgradeRow { Id = id, Name = name, Desc = desc, Buy = buy, Pips = pips });
            }

            var next = UIKit.Button(card.transform, "NEXT DAY", Red, Color.white, () => GameManager.I.StartDay(), 64);
            UIKit.Place((RectTransform)next.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(620, 150));
            _dayEnd.gameObject.SetActive(false);
        }

        void OnBuy(UpgradeId id)
        {
            if (GameManager.I.BuyUpgrade(id))
            {
                AudioManager.Play(Sfx.Coin, 0.8f, 0.9f);
                AudioManager.Play(Sfx.Ding, 0.5f, 1.2f);
                Haptics.Medium();
            }
            else AudioManager.Play(Sfx.Buzz, 0.5f);
            RefreshUpgrades();
            _endCoins.text = SaveSystem.Data.Coins + " coins";
        }

        void RefreshUpgrades()
        {
            foreach (var r in _upgradeRows)
            {
                var def = GameData.Upgrade(r.Id);
                int lvl = SaveSystem.Data.Level(r.Id);
                for (int p = 0; p < r.Pips.Length; p++) r.Pips[p].color = p < lvl ? Gold : new Color(0, 0, 0, 0.15f);
                if (lvl >= def.MaxLevel)
                {
                    UIKit.SetButtonLabel(r.Buy, "MAX");
                    r.Buy.interactable = false;
                }
                else
                {
                    int price = def.Prices[lvl];
                    UIKit.SetButtonLabel(r.Buy, price + " $");
                    r.Buy.interactable = SaveSystem.Data.Coins >= price;
                }
            }
        }

        public void ShowDayEnd(DayStats s, int stars)
        {
            _dayEnd.gameObject.SetActive(true);
            HideHint();
            _endTitle.text = "DAY " + s.Day + " DONE!";
            _endCoins.text = "+" + s.Earned + " coins";
            float avg = s.Served > 0 ? s.QualitySum / (float)s.Served : 0f;
            _endStats.text = string.Format("Served {0}   Perfect {1}   Missed {2}\nAvg quality {3:0}%   Tips {4}   Best combo x{5:0.0}",
                s.Served, s.Perfect, s.Missed, avg, s.Tips, s.BestCombo);
            for (int i = 0; i < 3; i++)
            {
                _stars[i].rectTransform.localScale = Vector3.one;
                _stars[i].color = i < stars ? Gold : new Color(0, 0, 0, 0.15f);
                if (i < stars)
                {
                    int idx = i;
                    _stars[idx].rectTransform.localScale = Vector3.zero;
                    Juice.Delay(0.35f + i * 0.25f, () =>
                    {
                        if (_stars[idx] == null) return;
                        _stars[idx].rectTransform.localScale = Vector3.one;
                        Juice.PopIn(_stars[idx].rectTransform, 0.3f);
                        AudioManager.Play(Sfx.Ding, 0.6f, AudioManager.Semitones(idx * 4));
                    });
                }
            }
            RefreshUpgrades();
            _dayEnd.GetChild(0).localScale = Vector3.one;
            Juice.PopIn(_dayEnd.GetChild(0), 0.4f);
        }

        public void HideDayEnd() => _dayEnd.gameObject.SetActive(false);
    }
}
