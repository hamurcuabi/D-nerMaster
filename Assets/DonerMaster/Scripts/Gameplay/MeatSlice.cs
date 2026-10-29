using UnityEngine;

namespace DonerMaster
{
    public interface ITappable
    {
        void OnTap();
    }

    /// <summary>One shaving of doner meat. Carries the data the score needs: doneness, thickness, temperature.</summary>
    public class MeatSlice : MonoBehaviour
    {
        public float Cook;          // 0..100 doneness when cut
        public float Thickness;     // 0..1 (1 = perfect thin shaving)
        public float CutTime;
        public bool Chunky;

        Rigidbody _rb;
        Material _mat;
        Color _baseColor;

        public bool IsBurnt => Cook > 92f;

        /// <summary>1 = piping hot, 0 = cold. The heated-tray upgrade extends the hot window.</summary>
        public float Warmth
        {
            get
            {
                float age = Time.time - CutTime;
                float hold = GameData.HeatHold(SaveSystem.Data.Level(UpgradeId.HeatedTray));
                if (age <= hold) return 1f;
                return Mathf.Clamp01(1f - (age - hold) / 25f);
            }
        }

        public static Color CookColor(float cook)
        {
            var raw = new Color(0.86f, 0.47f, 0.45f);
            var light = new Color(0.78f, 0.52f, 0.33f);
            var done = new Color(0.58f, 0.33f, 0.16f);
            var burnt = new Color(0.16f, 0.1f, 0.07f);
            if (cook < 40f) return Color.Lerp(raw, light, cook / 40f);
            if (cook < 80f) return Color.Lerp(light, done, (cook - 40f) / 40f);
            if (cook < 92f) return Color.Lerp(done, done * 0.85f, (cook - 80f) / 12f);
            return Color.Lerp(done * 0.85f, burnt, Mathf.Clamp01((cook - 92f) / 8f));
        }

        public static MeatSlice Spawn(Vector3 pos, Vector3 outward, float cook, float thickness, bool chunky)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "MeatSlice";
            go.transform.position = pos;
            go.transform.rotation = Quaternion.LookRotation(outward) * Quaternion.Euler(Random.Range(-20f, 20f), Random.Range(-25f, 25f), Random.Range(-35f, 35f));

            float thick = Mathf.Lerp(0.05f, 0.018f, thickness) * (chunky ? 1.6f : 1f);
            float len = Random.Range(0.17f, 0.24f) * (chunky ? 0.75f : 1f);
            go.transform.localScale = new Vector3(Random.Range(0.08f, 0.12f), len, thick);

            var s = go.AddComponent<MeatSlice>();
            s.Cook = cook;
            s.Thickness = thickness;
            s.CutTime = Time.time;
            s.Chunky = chunky;

            s._baseColor = CookColor(cook);
            s._mat = Mats.Make(s._baseColor, 0.55f);
            go.GetComponent<Renderer>().sharedMaterial = s._mat;

            s._rb = go.AddComponent<Rigidbody>();
            s._rb.mass = 0.03f;
            Phys.SetDamping(s._rb, 0.8f, 2f);
            s._rb.interpolation = RigidbodyInterpolation.Interpolate;
            s._rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            Phys.SetVelocity(s._rb, outward * Random.Range(0.4f, 0.8f) + Vector3.down * 0.3f);
            s._rb.angularVelocity = Random.insideUnitSphere * 6f;
            return s;
        }

        /// <summary>Stop physics (meat is being moved onto the board).</summary>
        public void Freeze()
        {
            if (_rb != null) { Destroy(_rb); _rb = null; }
            var c = GetComponent<Collider>();
            if (c != null) Destroy(c);
        }

        void Update()
        {
            // Cold meat looks a little greyer — a visual hint before the score punishes it.
            if (_mat == null) return;
            float w = Warmth;
            var grey = new Color(0.55f, 0.48f, 0.44f);
            Mats.SetColor(_mat, Color.Lerp(Color.Lerp(_baseColor, grey, 0.45f), _baseColor, w));
        }

        void OnDestroy()
        {
            if (_mat != null) Destroy(_mat);
        }
    }

    /// <summary>Unity 6 renamed Rigidbody.velocity/drag; keep both versions compiling.</summary>
    public static class Phys
    {
        public static void SetVelocity(Rigidbody rb, Vector3 v)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearVelocity = v;
#else
            rb.velocity = v;
#endif
        }

        public static void SetDamping(Rigidbody rb, float linear, float angular)
        {
#if UNITY_6000_0_OR_NEWER
            rb.linearDamping = linear;
            rb.angularDamping = angular;
#else
            rb.drag = linear;
            rb.angularDrag = angular;
#endif
        }
    }
}
