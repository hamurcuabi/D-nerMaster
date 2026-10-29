using UnityEngine;
using UnityEngine.Rendering;

namespace DonerMaster
{
    /// <summary>
    /// Entry point. Builds the whole prototype at runtime — camera, lights, shop, systems and UI —
    /// so pressing Play in ANY scene (even an empty one) starts the game.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        /// <summary>Set to false (e.g. from a test) to stop the automatic start.</summary>
        public static bool AutoStart = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBootstrap()
        {
            if (!AutoStart) return;
            if (FindAnyObjectByType<GameBootstrap>() != null) return;
            new GameObject("DonerMaster").AddComponent<GameBootstrap>();
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            SaveSystem.Load();

            // Remove the default camera/light of an empty scene — we bring our own.
            foreach (var c in FindObjectsByType<Camera>(FindObjectsSortMode.None)) Destroy(c.gameObject);
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None)) Destroy(l.gameObject);

            var cam = BuildCameraAndLights();

            var world = new GameObject("World").transform;
            var refs = WorldBuilder.Build(world);

            var systems = new GameObject("Systems");
            systems.AddComponent<PointerInput>();
            systems.AddComponent<AudioManager>();
            var gm = systems.AddComponent<GameManager>();
            gm.Spit = refs.Spit;
            gm.Tray = refs.Tray;
            gm.Board = refs.Board;
            gm.HeatKnob = refs.Knob;
            gm.Stations.AddRange(refs.Stations);
            gm.SlotPositions = refs.Slots;

            var input = systems.AddComponent<GameplayInput>();
            input.Spit = refs.Spit;
            input.Tray = refs.Tray;

            CameraFit.Fit(cam, refs.FramePoints, 1.06f);
            cam.GetComponent<CameraShake>().Capture();

            var ui = new GameObject("UI").AddComponent<GameUI>();
            ui.Build(cam);
            AddWorldLabels(ui, refs);

            gm.Init();
        }

        static Camera BuildCameraAndLights()
        {
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.98f, 0.66f, 0.47f);   // warm dusk sky
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 60f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CameraShake>();
            camGo.transform.position = new Vector3(0.1f, 6.3f, -5.4f);
            camGo.transform.LookAt(new Vector3(0.1f, 1.4f, 1.0f));

            var sunGo = new GameObject("Sun");
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.88f, 0.75f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.6f;
            sunGo.transform.rotation = Quaternion.Euler(52f, -28f, 0f);

            var lampGo = new GameObject("ShopLamp");
            var lamp = lampGo.AddComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.85f, 0.65f);
            lamp.intensity = 1.3f;
            lamp.range = 6f;
            lampGo.transform.position = new Vector3(0.3f, 3.4f, -0.3f);

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.75f, 0.6f, 0.55f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.45f, 0.4f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.24f, 0.2f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = cam.backgroundColor;
            RenderSettings.fogStartDistance = 14f;
            RenderSettings.fogEndDistance = 32f;
            return cam;
        }

        static void AddWorldLabels(GameUI ui, WorldBuilder.Refs r)
        {
            foreach (var st in r.Stations)
            {
                var s = st;
                if (s.Kind == StationKind.HeatKnob)
                {
                    ui.AddWorldLabel(() => s.transform.position + new Vector3(0, 0.02f, -0.3f),
                        () => "HEAT " + Colorize(GameData.HeatNames[r.Spit.Heat], r.Spit.Heat == 0 ? "#8FD3FF" : r.Spit.Heat == 1 ? "#FFD25A" : "#FF6A3D"), null, 30);
                    continue;
                }
                float yOffset = s.Kind == StationKind.Ayran ? 0.62f : 0.02f;
                float zOffset = s.Kind == StationKind.Ayran ? 0f : -0.3f;
                ui.AddWorldLabel(() => s.transform.position + new Vector3(0, yOffset, zOffset), () => s.Label, null, 26);
            }

            ui.AddWorldLabel(() => r.Tray.transform.position + new Vector3(0, 0.02f, -0.48f),
                () => r.Tray.Count > 0 ? "TRAY  " + r.Tray.GoodCount + (r.Tray.BurntCount > 0 ? Colorize("  (" + r.Tray.BurntCount + " burnt)", "#FF7A5A") : "") : "TRAY",
                null, 28);

            ui.AddWorldLabel(() => r.Board.transform.position + new Vector3(0, 0.02f, -0.5f), () =>
            {
                var b = r.Board;
                if (b.Wrapped) return Colorize("READY - tap customer", "#8CFF8C");
                if (b.Product == Product.None) return b.Ayran ? "AYRAN" : "BOARD";
                return GameData.ProductName(b.Product) + "  meat " + b.Meat.Count + "/" + GameData.MeatPieces(b.Product) + (b.Meat.Count > 0 ? "  - tap to wrap" : "");
            }, null, 28);

            // Doneness of the side facing you — the most important number in the game
            ui.AddWorldLabel(() => r.Spit.transform.position + new Vector3(0, DonerSpit.MeatBottom + DonerSpit.MeatHeight + 0.42f, 0), () =>
            {
                var sp = r.Spit;
                if (sp.Reloading) return "LOADING " + Mathf.RoundToInt(sp.ReloadProgress * 100f) + "%";
                if (sp.Meat <= 0f) return Colorize("EMPTY - TAP TO RELOAD", "#FF7A5A");
                float c = sp.ZoneCook[sp.FrontZone];
                string state = c < 40f ? Colorize("RAW", "#FF9FB0") : c < 70f ? Colorize("COOKING", "#FFD25A") : c <= 90f ? Colorize("PERFECT", "#8CFF8C") : c <= 92f ? Colorize("CRISPY", "#FFB35A") : Colorize("BURNT!", "#FF5A3D");
                return state + "\n<size=22>meat " + Mathf.RoundToInt(sp.Meat * 100f) + "%</size>";
            }, null, 34);
        }

        static string Colorize(string s, string hex) => "<color=" + hex + ">" + s + "</color>";
    }

    /// <summary>Re-aims the camera and picks the field of view so every frame point is visible on any aspect ratio.</summary>
    public static class CameraFit
    {
        public static void Fit(Camera cam, System.Collections.Generic.List<Vector3> points, float margin)
        {
            if (points == null || points.Count == 0) return;
            var t = cam.transform;

            // Centre the content vertically by tilting the camera
            for (int iter = 0; iter < 3; iter++)
            {
                float minA = float.MaxValue, maxA = float.MinValue;
                foreach (var p in points)
                {
                    var l = t.InverseTransformPoint(p);
                    float a = Mathf.Atan2(l.y, l.z) * Mathf.Rad2Deg;
                    minA = Mathf.Min(minA, a);
                    maxA = Mathf.Max(maxA, a);
                }
                t.Rotate(Vector3.right, -(minA + maxA) * 0.5f, Space.Self);
            }

            float aspect = Mathf.Max(0.1f, cam.aspect);
            float need = 0f;
            foreach (var p in points)
            {
                var l = t.InverseTransformPoint(p);
                if (l.z <= 0.01f) continue;
                need = Mathf.Max(need, Mathf.Abs(l.y) / l.z);
                need = Mathf.Max(need, Mathf.Abs(l.x) / (l.z * aspect));
            }
            float fov = 2f * Mathf.Atan(need * margin) * Mathf.Rad2Deg;
            cam.fieldOfView = Mathf.Clamp(fov, 30f, 90f);
        }
    }
}
