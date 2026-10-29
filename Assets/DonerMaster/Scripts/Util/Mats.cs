using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DonerMaster
{
    /// <summary>
    /// Runtime material factory. Works with both the Built-in pipeline and URP,
    /// so the project runs whether or not a URP asset is assigned.
    /// </summary>
    public static class Mats
    {
        static Shader _lit;
        static Shader _unlit;
        static Shader _particle;
        static readonly Dictionary<int, Material> Shared = new Dictionary<int, Material>();

        static bool IsSrp => GraphicsSettings.currentRenderPipeline != null;

        public static Shader Lit
        {
            get
            {
                if (_lit == null)
                    _lit = Find(IsSrp
                        ? new[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit", "Standard" }
                        : new[] { "Standard", "Legacy Shaders/Diffuse" });
                return _lit;
            }
        }

        public static Shader Unlit
        {
            get
            {
                if (_unlit == null)
                    _unlit = Find(IsSrp
                        ? new[] { "Universal Render Pipeline/Unlit", "Unlit/Color" }
                        : new[] { "Unlit/Color", "Sprites/Default" });
                return _unlit;
            }
        }

        public static Shader Particle
        {
            get
            {
                if (_particle == null)
                    _particle = Find(IsSrp
                        ? new[] { "Universal Render Pipeline/Particles/Unlit", "Sprites/Default" }
                        : new[] { "Legacy Shaders/Particles/Alpha Blended", "Particles/Standard Unlit", "Sprites/Default" });
                return _particle;
            }
        }

        static Shader Find(string[] names)
        {
            foreach (var n in names)
            {
                var s = Shader.Find(n);
                if (s != null && s.isSupported) return s;
            }
            // Shader.Find only sees shaders included in the build — the default primitive material always is.
            var tmp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var fallback = tmp.GetComponent<Renderer>().sharedMaterial.shader;
            Object.Destroy(tmp);
            return fallback != null ? fallback : Shader.Find("Sprites/Default");
        }

        /// <summary>New, unshared lit material (use when the color changes at runtime).</summary>
        public static Material Make(Color c, float smoothness = 0.25f, float metallic = 0f)
        {
            var m = new Material(Lit);
            SetColor(m, c);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            return m;
        }

        /// <summary>Cached lit material for static objects of the same look.</summary>
        public static Material Get(Color c, float smoothness = 0.25f, float metallic = 0f)
        {
            int key = ((Color32)c).GetHashCode() ^ Mathf.RoundToInt(smoothness * 100) * 7919 ^ Mathf.RoundToInt(metallic * 100) * 104729;
            if (Shared.TryGetValue(key, out var m) && m != null) return m;
            m = Make(c, smoothness, metallic);
            Shared[key] = m;
            return m;
        }

        public static Material Emissive(Color c, float intensity = 1.5f)
        {
            var m = Make(c, 0.1f);
            SetEmission(m, c * intensity);
            return m;
        }

        public static void SetColor(Material m, Color c)
        {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        }

        public static void SetEmission(Material m, Color c)
        {
            if (m == null) return;
            m.EnableKeyword("_EMISSION");
            m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", c);
        }

        public static Material ParticleMat(Color c)
        {
            var m = new Material(Particle);
            SetColor(m, c);
            if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", c);
            if (m.HasProperty("_Surface"))
            {
                // URP: switch to alpha-blended transparent
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                if (m.HasProperty("_SrcBlend")) m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                if (m.HasProperty("_DstBlend")) m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            return m;
        }
    }
}
