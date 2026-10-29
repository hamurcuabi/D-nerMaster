using UnityEngine;

namespace DonerMaster
{
    /// <summary>
    /// Turns the single pointer into gameplay:
    ///  • press on the doner and drag DOWN  → shave meat (speed decides slice thickness)
    ///  • press on the doner and drag SIDEWAYS → spin the spit to bring a better-cooked side forward
    ///  • short tap anywhere → tap the object under the finger (stations, tray, board, customers)
    /// </summary>
    public class GameplayInput : MonoBehaviour
    {
        enum Mode { None, Pending, Slicing, Rotating }

        public DonerSpit Spit;
        public MeatTray Tray;

        Camera _cam;
        Mode _mode;
        Vector2 _downPos, _prevPos;
        float _downTime;
        bool _downOnSpit;
        float _lastWorldY;
        float _accum;
        float _speedEma;
        float _lastRotateDelta;
        Transform _knife;
        Vector3 _knifeTargetPos;
        Quaternion _knifeTargetRot;
        float _knifeJitter;

        public static bool IsSlicing { get; private set; }

        void Start()
        {
            _cam = Camera.main;
            BuildKnife();
        }

        void BuildKnife()
        {
            _knife = new GameObject("Knife").transform;
            var steel = new Color(0.85f, 0.87f, 0.9f);
            Build.Prim(PrimitiveType.Cube, "Blade", _knife, new Vector3(-0.12f, 0, 0), new Vector3(0.5f, 0.07f, 0.008f), steel, false, 0.95f, 0.95f);
            Build.Prim(PrimitiveType.Cube, "Edge", _knife, new Vector3(-0.12f, -0.036f, 0), new Vector3(0.5f, 0.006f, 0.004f), Color.white, false, 1f, 1f);
            Build.Prim(PrimitiveType.Cube, "Bolster", _knife, new Vector3(0.14f, 0, 0), new Vector3(0.025f, 0.08f, 0.025f), new Color(0.6f, 0.62f, 0.65f), false, 0.9f, 0.9f);
            Build.Prim(PrimitiveType.Cube, "Handle", _knife, new Vector3(0.25f, -0.005f, 0), new Vector3(0.2f, 0.05f, 0.035f), new Color(0.15f, 0.1f, 0.08f), false, 0.4f);
            RestKnife(true);
        }

        void RestKnife(bool snap)
        {
            if (Spit == null) return;
            _knifeTargetPos = Spit.transform.position + Spit.transform.right * 0.62f + Vector3.up * 0.05f + Vector3.back * 0.25f;
            _knifeTargetRot = Quaternion.Euler(90f, -20f, 0f);
            if (snap)
            {
                _knife.position = _knifeTargetPos;
                _knife.rotation = _knifeTargetRot;
            }
        }

        void Update()
        {
            var gm = GameManager.I;
            if (gm == null || !gm.IsPlaying || _cam == null)
            {
                _mode = Mode.None;
                IsSlicing = false;
                RestKnife(false);
                AnimateKnife();
                return;
            }

            var pos = PointerInput.Position;
            float dt = Mathf.Max(Time.deltaTime, 0.0001f);

            if (PointerInput.Down && !PointerInput.PressStartedOnUI)
            {
                _mode = Mode.Pending;
                _downPos = _prevPos = pos;
                _downTime = Time.time;
                _downOnSpit = RaycastSpit(pos);
                _speedEma = 0f;
            }

            if (PointerInput.Pressed && _mode != Mode.None)
            {
                var delta = pos - _downPos;
                float threshold = Screen.height * 0.012f;

                if (_mode == Mode.Pending && _downOnSpit && delta.magnitude > threshold)
                {
                    if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x) * 0.8f && delta.y < 0f) BeginSlicing(pos);
                    else _mode = Mode.Rotating;
                }

                if (_mode == Mode.Slicing) UpdateSlicing(pos, dt);
                else if (_mode == Mode.Rotating)
                {
                    float d = -(pos.x - _prevPos.x) / Screen.width * 360f;
                    Spit.RotateBy(d);
                    _lastRotateDelta = d / dt;
                }
            }

            if (PointerInput.Up && _mode != Mode.None)
            {
                if (_mode == Mode.Pending && (pos - _downPos).magnitude < Screen.height * 0.03f && Time.time - _downTime < 0.6f)
                    TapAt(pos);
                if (_mode == Mode.Rotating) Spit.AddManualSpin(Mathf.Clamp(_lastRotateDelta * 0.4f, -400f, 400f));
                _mode = Mode.None;
            }

            IsSlicing = _mode == Mode.Slicing;
            if (!IsSlicing) RestKnife(false);
            _prevPos = pos;
            AnimateKnife();
        }

        void AnimateKnife()
        {
            if (_knife == null) return;
            float k = 1f - Mathf.Exp(-Time.deltaTime * (IsSlicing ? 30f : 10f));
            var jitter = _knifeJitter > 0f ? Random.insideUnitSphere * 0.01f * _knifeJitter : Vector3.zero;
            _knifeJitter = Mathf.Max(0f, _knifeJitter - Time.deltaTime * 8f);
            _knife.position = Vector3.Lerp(_knife.position, _knifeTargetPos, k) + jitter;
            _knife.rotation = Quaternion.Slerp(_knife.rotation, _knifeTargetRot, k);
        }

        bool RaycastSpit(Vector2 screen)
        {
            var ray = _cam.ScreenPointToRay(screen);
            if (Physics.Raycast(ray, out var hit, 50f))
                return hit.collider != null && hit.collider.GetComponentInParent<DonerSpit>() == Spit;
            return false;
        }

        Vector3 ToCamFlat()
        {
            var v = _cam.transform.position - Spit.transform.position;
            v.y = 0f;
            return v.normalized;
        }

        float PointerWorldY(Vector2 screen)
        {
            var normal = ToCamFlat();
            var plane = new Plane(normal, Spit.transform.position);
            var ray = _cam.ScreenPointToRay(screen);
            if (plane.Raycast(ray, out float enter)) return ray.GetPoint(enter).y;
            return _lastWorldY;
        }

        void BeginSlicing(Vector2 pos)
        {
            if (Spit.Meat <= 0f)
            {
                _mode = Mode.None;
                GameManager.I?.OnSpitEmptyTouched();
                return;
            }
            _mode = Mode.Slicing;
            _lastWorldY = PointerWorldY(pos);
            _accum = 0f;
            AudioManager.Play(Sfx.Slice, 0.35f, 1.4f);
        }

        void UpdateSlicing(Vector2 pos, float dt)
        {
            float vScreen = (_prevPos.y - pos.y) / Screen.height / dt;   // screen heights per second, + = down
            _speedEma = Mathf.Lerp(_speedEma, Mathf.Max(0f, vScreen), 1f - Mathf.Exp(-dt * 12f));

            float worldY = PointerWorldY(pos);
            float dy = _lastWorldY - worldY;
            if (dy > 0f) _accum += dy;
            else if (dy < -0.02f) _accum = 0f;   // moving back up: start a new stroke without lifting
            _lastWorldY = worldY;

            // Knife sits on the meat surface at the finger's height
            float y = Mathf.Clamp(worldY, Spit.MeatMinY + 0.03f, Spit.MeatMaxY - 0.03f);
            var toCam = ToCamFlat();
            var right = Vector3.Cross(Vector3.up, toCam).normalized * -1f;
            var surface = new Vector3(Spit.transform.position.x, y, Spit.transform.position.z) + toCam * (Spit.RadiusAt(y) + 0.02f);
            _knifeTargetPos = surface + right * 0.1f;
            float tilt = Mathf.Clamp(_speedEma * 12f, 0f, 20f);
            _knifeTargetRot = Quaternion.LookRotation(-toCam, Vector3.up) * Quaternion.Euler(0f, 0f, -12f - tilt);

            while (_accum >= GameData.SliceStep)
            {
                _accum -= GameData.SliceStep;
                if (worldY < Spit.MeatMinY - 0.05f || worldY > Spit.MeatMaxY + 0.05f) continue;
                ShaveAt(y, toCam);
            }
        }

        void ShaveAt(float y, Vector3 toCam)
        {
            float cook = Spit.ShaveFront();
            if (cook < 0f)
            {
                _mode = Mode.None;
                GameManager.I?.OnSpitEmptyTouched();
                return;
            }

            float widen = GameData.KnifeWiden(SaveSystem.Data.Level(UpgradeId.Knife));
            float max = GameData.IdealSpeedMax * widen;
            float speed = _speedEma;
            float thick;
            if (speed < GameData.IdealSpeedMin) thick = Mathf.Lerp(0.8f, 1f, speed / GameData.IdealSpeedMin);
            else if (speed <= max) thick = 1f;
            else thick = Mathf.Clamp(1f - (speed - max) / (max * 1.5f), 0.15f, 1f);
            bool chunky = speed > max * 1.8f;

            var surface = new Vector3(Spit.transform.position.x, y, Spit.transform.position.z) + toCam * (Spit.RadiusAt(y) + 0.03f);
            var slice = MeatSlice.Spawn(surface, toCam, cook, thick, chunky);
            Tray.Add(slice);

            AudioManager.Play(Sfx.Slice, chunky ? 1f : 0.8f, chunky ? 0.75f : Mathf.Lerp(1.25f, 0.95f, Mathf.Clamp01(speed / max)), 0.08f, 0.01f);
            FX.Grease(surface, chunky ? 6 : 3);
            if (cook > 92f)
            {
                FX.Smoke(surface, 2);
                AudioManager.Play(Sfx.Buzz, 0.25f, 1.6f);
            }
            Haptics.Light();
            _knifeJitter = 1f;
            GameManager.I?.OnSliced(slice);
        }

        void TapAt(Vector2 screen)
        {
            var bubbleOwner = GameUI.I != null ? GameUI.I.BubbleAt(screen) : null;
            if (bubbleOwner != null)
            {
                bubbleOwner.OnTap();
                return;
            }
            var ray = _cam.ScreenPointToRay(screen);
            var hits = Physics.RaycastAll(ray, 50f);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider == null) continue;
                // Meat slices are physics props, not buttons — tapping them means "the tray"
                if (h.collider.GetComponent<MeatSlice>() != null) { Tray.OnTap(); return; }
                var tappable = h.collider.GetComponentInParent<ITappable>();
                if (tappable != null)
                {
                    tappable.OnTap();
                    return;
                }
            }
        }
    }
}
