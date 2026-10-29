using System;
using System.Collections;
using UnityEngine;

namespace DonerMaster
{
    /// <summary>
    /// Tiny dependency-free tween helper (stand-in for DOTween). All animations run on one host object.
    /// </summary>
    public class Juice : MonoBehaviour
    {
        static Juice _host;

        static Juice Host
        {
            get
            {
                if (_host == null)
                {
                    var go = new GameObject("[Juice]");
                    _host = go.AddComponent<Juice>();
                }
                return _host;
            }
        }

        public static Coroutine Run(IEnumerator routine) => Host.StartCoroutine(routine);

        public static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
        }

        public static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3);
        public static float EaseInOutQuad(float x) => x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2) / 2f;

        /// <summary>Elastic "boing" on scale; safe to call repeatedly on the same object.</summary>
        public static void Punch(Transform t, float amount = 0.18f, float duration = 0.28f)
        {
            if (t == null) return;
            var mem = t.GetComponent<ScaleMemory>();
            if (mem == null) { mem = t.gameObject.AddComponent<ScaleMemory>(); mem.BaseScale = t.localScale; }
            if (mem.Running != null) Host.StopCoroutine(mem.Running);
            mem.Running = Host.StartCoroutine(PunchRoutine(t, mem, amount, duration));
        }

        static IEnumerator PunchRoutine(Transform t, ScaleMemory mem, float amount, float duration)
        {
            float time = 0f;
            while (time < duration)
            {
                if (t == null) yield break;
                time += Time.deltaTime;
                float k = time / duration;
                float s = 1f + amount * Mathf.Sin(k * Mathf.PI * 3f) * (1f - k);
                t.localScale = mem.BaseScale * s;
                yield return null;
            }
            if (t != null) t.localScale = mem.BaseScale;
            mem.Running = null;
        }

        /// <summary>Scale from zero to the current scale with overshoot.</summary>
        public static void PopIn(Transform t, float duration = 0.3f, float delay = 0f)
        {
            if (t == null) return;
            var target = t.localScale;
            t.localScale = Vector3.zero;
            Host.StartCoroutine(PopRoutine(t, target, duration, delay));
        }

        static IEnumerator PopRoutine(Transform t, Vector3 target, float duration, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            float time = 0f;
            while (time < duration)
            {
                if (t == null) yield break;
                time += Time.deltaTime;
                t.localScale = target * EaseOutBack(Mathf.Clamp01(time / duration));
                yield return null;
            }
            if (t != null) t.localScale = target;
        }

        /// <summary>Move along a parabola (world space) — used for ingredients and meat flying onto the board.</summary>
        public static void Arc(Transform t, Vector3 to, float height, float duration, Action onDone = null, bool spin = false)
        {
            if (t == null) return;
            Host.StartCoroutine(ArcRoutine(t, t.position, to, height, duration, onDone, spin));
        }

        /// <summary>Arc that follows a moving target (local position under a parent).</summary>
        public static void ArcToLocal(Transform t, Transform parent, Vector3 localTo, float height, float duration, Action onDone = null)
        {
            if (t == null) return;
            Host.StartCoroutine(ArcLocalRoutine(t, parent, localTo, height, duration, onDone));
        }

        static IEnumerator ArcRoutine(Transform t, Vector3 from, Vector3 to, float height, float duration, Action onDone, bool spin)
        {
            float time = 0f;
            var rot0 = t.rotation;
            var spinAxis = UnityEngine.Random.onUnitSphere;
            while (time < duration)
            {
                if (t == null) yield break;
                time += Time.deltaTime;
                float k = EaseInOutQuad(Mathf.Clamp01(time / duration));
                var p = Vector3.Lerp(from, to, k);
                p.y += Mathf.Sin(k * Mathf.PI) * height;
                t.position = p;
                if (spin) t.rotation = rot0 * Quaternion.AngleAxis(k * 540f, spinAxis);
                yield return null;
            }
            if (t != null) t.position = to;
            onDone?.Invoke();
        }

        static IEnumerator ArcLocalRoutine(Transform t, Transform parent, Vector3 localTo, float height, float duration, Action onDone)
        {
            float time = 0f;
            var from = t.position;
            while (time < duration)
            {
                if (t == null || parent == null) yield break;
                time += Time.deltaTime;
                float k = EaseInOutQuad(Mathf.Clamp01(time / duration));
                var to = parent.TransformPoint(localTo);
                var p = Vector3.Lerp(from, to, k);
                p.y += Mathf.Sin(k * Mathf.PI) * height;
                t.position = p;
                yield return null;
            }
            if (t != null && parent != null)
            {
                t.SetParent(parent, true);
                t.localPosition = localTo;
            }
            onDone?.Invoke();
        }

        public static void MoveTo(Transform t, Vector3 to, float duration, Action onDone = null)
        {
            if (t == null) return;
            Host.StartCoroutine(MoveRoutine(t, t.position, to, duration, onDone));
        }

        static IEnumerator MoveRoutine(Transform t, Vector3 from, Vector3 to, float duration, Action onDone)
        {
            float time = 0f;
            while (time < duration)
            {
                if (t == null) yield break;
                time += Time.deltaTime;
                t.position = Vector3.Lerp(from, to, EaseOutCubic(Mathf.Clamp01(time / duration)));
                yield return null;
            }
            if (t != null) t.position = to;
            onDone?.Invoke();
        }

        /// <summary>Shrink to nothing and destroy.</summary>
        public static void ShrinkAndDestroy(GameObject go, float duration = 0.2f, float delay = 0f)
        {
            if (go == null) return;
            Host.StartCoroutine(ShrinkRoutine(go, duration, delay));
        }

        static IEnumerator ShrinkRoutine(GameObject go, float duration, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            if (go == null) yield break;
            var t = go.transform;
            var s0 = t.localScale;
            float time = 0f;
            while (time < duration)
            {
                if (go == null) yield break;
                time += Time.deltaTime;
                t.localScale = s0 * (1f - EaseOutCubic(Mathf.Clamp01(time / duration)));
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        public static void Delay(float seconds, Action action) => Host.StartCoroutine(DelayRoutine(seconds, action));

        static IEnumerator DelayRoutine(float s, Action a)
        {
            yield return new WaitForSeconds(s);
            a?.Invoke();
        }

        public static void Shake(Transform t, float amount = 0.05f, float duration = 0.25f)
        {
            if (t == null) return;
            Host.StartCoroutine(ShakeRoutine(t, amount, duration));
        }

        static IEnumerator ShakeRoutine(Transform t, float amount, float duration)
        {
            var basePos = t.localPosition;
            float time = 0f;
            while (time < duration)
            {
                if (t == null) yield break;
                time += Time.deltaTime;
                float k = 1f - time / duration;
                t.localPosition = basePos + new Vector3(Mathf.Sin(time * 70f), 0f, 0f) * amount * k;
                yield return null;
            }
            if (t != null) t.localPosition = basePos;
        }
    }

    public class ScaleMemory : MonoBehaviour
    {
        public Vector3 BaseScale;
        public Coroutine Running;
    }
}
