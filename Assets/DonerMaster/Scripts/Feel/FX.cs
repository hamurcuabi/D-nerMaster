using UnityEngine;

namespace DonerMaster
{
    /// <summary>Runtime-built particle bursts (grease, smoke, sparkles, hearts, coins).</summary>
    public static class FX
    {
        static ParticleSystem _grease, _smoke, _sparkle, _crumbs;
        static Transform _root;

        static Texture2D _softDot;

        /// <summary>Procedural round, soft-edged particle texture.</summary>
        static Texture2D SoftDot
        {
            get
            {
                if (_softDot != null) return _softDot;
                const int size = 64;
                _softDot = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color[size * size];
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f, dy = (y + 0.5f) / size * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    px[y * size + x] = new Color(1f, 1f, 1f, a * a);
                }
                _softDot.SetPixels(px);
                _softDot.Apply();
                return _softDot;
            }
        }

        static Transform Root
        {
            get
            {
                if (_root == null) _root = new GameObject("[FX]").transform;
                return _root;
            }
        }

        static ParticleSystem Create(string name, Color color, float size, float speed, float life, float gravity, bool cube = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(Root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = true; // stays alive; we only Emit() bursts manually
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = color;
            main.gravityModifier = gravity;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;

            var emission = ps.emission;
            emission.rateOverTime = 0f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.03f;

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.2f));

            var r = go.GetComponent<ParticleSystemRenderer>();
            var mat = Mats.ParticleMat(Color.white);
            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", SoftDot);
            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", SoftDot);
            r.material = mat;
            if (cube)
            {
                r.renderMode = ParticleSystemRenderMode.Mesh;
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
                r.mesh = tmp.GetComponent<MeshFilter>().sharedMesh;
                r.material = Mats.Make(Color.white, 0.1f);
                Object.Destroy(tmp);
            }
            ps.Play();
            return ps;
        }

        static void Burst(ParticleSystem ps, Vector3 pos, int count, Color? color = null)
        {
            var ep = new ParticleSystem.EmitParams { position = pos, applyShapeToPosition = true };
            if (color.HasValue) ep.startColor = color.Value;
            ps.Emit(ep, count);
        }

        public static void Grease(Vector3 pos, int count = 6)
        {
            if (_grease == null) _grease = Create("Grease", new Color(1f, 0.85f, 0.45f, 0.9f), 0.035f, 1.6f, 0.45f, 1.2f);
            Burst(_grease, pos, count);
        }

        public static void Smoke(Vector3 pos, int count = 4, Color? color = null)
        {
            if (_smoke == null)
            {
                _smoke = Create("Smoke", new Color(0.25f, 0.22f, 0.2f, 0.55f), 0.35f, 0.35f, 1.6f, -0.08f);
                var sol = _smoke.sizeOverLifetime;
                sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.4f, 1f, 1.6f));
            }
            Burst(_smoke, pos, count, color);
        }

        public static void Sparkle(Vector3 pos, int count = 14, Color? color = null)
        {
            if (_sparkle == null) _sparkle = Create("Sparkle", new Color(1f, 0.9f, 0.3f, 1f), 0.09f, 2.4f, 0.6f, 0.4f);
            Burst(_sparkle, pos, count, color);
        }

        public static void Crumbs(Vector3 pos, Color color, int count = 6)
        {
            if (_crumbs == null) _crumbs = Create("Crumbs", Color.white, 0.04f, 1.2f, 0.4f, 1.5f, true);
            Burst(_crumbs, pos, count, color);
        }
    }
}
