using System.Collections.Generic;
using UnityEngine;

namespace DonerMaster
{
    /// <summary>
    /// Builds the grey-box doner shop from primitives: counter, spit, stations, street backdrop.
    /// Every piece is a named child so art can later replace it one by one.
    /// </summary>
    public static class WorldBuilder
    {
        public const float CounterTop = 1.02f;

        public class Refs
        {
            public DonerSpit Spit;
            public MeatTray Tray;
            public PrepBoard Board;
            public Station Knob;
            public List<Station> Stations = new List<Station>();
            public Vector3[] Slots;
            public List<Vector3> FramePoints = new List<Vector3>();
        }

        public static Refs Build(Transform root)
        {
            var r = new Refs();
            BuildShell(root);

            var counter = DonerMaster.Build.Empty("CounterTopAnchor", root, new Vector3(0, CounterTop, 0)).transform;
            r.Spit = DonerSpit.Create(counter, new Vector3(-1.45f, 0, 0.85f));
            r.Tray = MeatTray.Create(counter, new Vector3(-1.45f, 0, 0.36f), new Vector2(0.95f, 0.78f));
            r.Board = PrepBoard.Create(counter, new Vector3(0.45f, 0, 0.18f));
            r.Knob = Station.Create(counter, new Vector3(-2.3f, 0, 0.05f), StationKind.HeatKnob);
            r.Stations.Add(r.Knob);
            r.Stations.Add(Station.Create(counter, new Vector3(2.15f, 0, 0.78f), StationKind.Ayran));
            r.Stations.Add(Station.Create(counter, new Vector3(2.2f, 0, -0.12f), StationKind.Trash));

            float[] xs = { -2.1f, -1.4f, -0.7f, 0f, 0.7f, 1.4f, 2.1f };
            const float binZ = -0.88f;
            r.Stations.Add(Station.Create(counter, new Vector3(xs[0], 0, binZ), StationKind.Lavash));
            r.Stations.Add(Station.Create(counter, new Vector3(xs[1], 0, binZ), StationKind.Bread));
            r.Stations.Add(Station.Create(counter, new Vector3(xs[2], 0, binZ), StationKind.Ingredient, Ingredient.Tomato));
            r.Stations.Add(Station.Create(counter, new Vector3(xs[3], 0, binZ), StationKind.Ingredient, Ingredient.Onion));
            r.Stations.Add(Station.Create(counter, new Vector3(xs[4], 0, binZ), StationKind.Ingredient, Ingredient.Lettuce));
            r.Stations.Add(Station.Create(counter, new Vector3(xs[5], 0, binZ), StationKind.Ingredient, Ingredient.Pickle));
            r.Stations.Add(Station.Create(counter, new Vector3(xs[6], 0, binZ), StationKind.Ingredient, Ingredient.Chili));

            r.Slots = new[] { new Vector3(-0.3f, 0f, 2.35f), new Vector3(0.85f, 0f, 2.35f), new Vector3(2.0f, 0f, 2.35f) };

            // Points the camera must always show (portrait or landscape)
            r.FramePoints.Add(new Vector3(-2.45f, CounterTop, binZ - 0.3f));
            r.FramePoints.Add(new Vector3(2.45f, CounterTop, binZ - 0.3f));
            r.FramePoints.Add(new Vector3(-2.45f, CounterTop + 1.95f, 0.85f));
            r.FramePoints.Add(new Vector3(-0.85f, 3.15f, 2.35f));
            r.FramePoints.Add(new Vector3(2.55f, 3.15f, 2.35f));
            return r;
        }

        static void BuildShell(Transform root)
        {
            var shell = DonerMaster.Build.Empty("Shell", root, Vector3.zero).transform;

            // Floors
            DonerMaster.Build.Prim(PrimitiveType.Cube, "ShopFloor", shell, new Vector3(0, -0.05f, -2f), new Vector3(12f, 0.1f, 7.2f), new Color(0.36f, 0.27f, 0.22f));
            DonerMaster.Build.Prim(PrimitiveType.Cube, "Sidewalk", shell, new Vector3(0, -0.04f, 3.2f), new Vector3(30f, 0.1f, 3.4f), new Color(0.62f, 0.6f, 0.58f));
            DonerMaster.Build.Prim(PrimitiveType.Cube, "Street", shell, new Vector3(0, -0.08f, 9f), new Vector3(40f, 0.1f, 8.4f), new Color(0.22f, 0.22f, 0.25f));
            for (int i = -6; i <= 6; i++)
                DonerMaster.Build.Prim(PrimitiveType.Cube, "Lane", shell, new Vector3(i * 3f, -0.025f, 9f), new Vector3(1.4f, 0.01f, 0.12f), new Color(0.9f, 0.85f, 0.6f));
            DonerMaster.Build.Prim(PrimitiveType.Cube, "Curb", shell, new Vector3(0, 0.02f, 4.9f), new Vector3(30f, 0.12f, 0.2f), new Color(0.7f, 0.68f, 0.66f));

            // Counter
            var red = new Color(0.62f, 0.13f, 0.1f);
            DonerMaster.Build.Prim(PrimitiveType.Cube, "CounterBody", shell, new Vector3(0, 0.48f, 0f), new Vector3(5.4f, 0.96f, 3.2f), red, false, 0.3f);
            DonerMaster.Build.Prim(PrimitiveType.Cube, "CounterTop", shell, new Vector3(0, 0.99f, 0f), new Vector3(5.5f, 0.06f, 3.3f), new Color(0.78f, 0.8f, 0.83f), true, 0.7f, 0.85f);
            // tiled front facing the street
            for (int i = 0; i < 9; i++)
                DonerMaster.Build.Prim(PrimitiveType.Cube, "Tile", shell, new Vector3(-2.4f + i * 0.6f, 0.5f, 1.61f), new Vector3(0.56f, 0.8f, 0.02f),
                    i % 2 == 0 ? new Color(0.95f, 0.9f, 0.8f) : new Color(0.2f, 0.45f, 0.65f), false, 0.6f);
            // side walls framing the shop window
            DonerMaster.Build.Prim(PrimitiveType.Cube, "WallL", shell, new Vector3(-3.25f, 2f, 0.8f), new Vector3(0.3f, 4f, 2f), new Color(0.85f, 0.75f, 0.6f));
            DonerMaster.Build.Prim(PrimitiveType.Cube, "WallR", shell, new Vector3(3.25f, 2f, 0.8f), new Vector3(0.3f, 4f, 2f), new Color(0.85f, 0.75f, 0.6f));

            // Awning with stripes
            var awning = DonerMaster.Build.Empty("Awning", shell, new Vector3(0, 3.75f, 1.9f)).transform;
            awning.localRotation = Quaternion.Euler(-18f, 0, 0);
            for (int i = 0; i < 12; i++)
                DonerMaster.Build.Prim(PrimitiveType.Cube, "Stripe", awning, new Vector3(-3.025f + i * 0.55f, 0, 0), new Vector3(0.55f, 0.04f, 1.2f),
                    i % 2 == 0 ? new Color(0.85f, 0.15f, 0.12f) : new Color(0.98f, 0.95f, 0.88f), false, 0.2f);

            // Buildings across the street with warm windows
            var rng = new System.Random(7);
            float x = -14f;
            while (x < 14f)
            {
                float w = 2.5f + (float)rng.NextDouble() * 2.5f;
                float h = 4f + (float)rng.NextDouble() * 5f;
                var c = Color.HSVToRGB((float)rng.NextDouble() * 0.12f + 0.02f, 0.35f, 0.55f + (float)rng.NextDouble() * 0.25f);
                DonerMaster.Build.Prim(PrimitiveType.Cube, "Building", shell, new Vector3(x + w / 2f, h / 2f, 15f), new Vector3(w - 0.2f, h, 2f), c);
                for (float wy = 1.2f; wy < h - 0.6f; wy += 1.3f)
                for (float wx = x + 0.6f; wx < x + w - 0.6f; wx += 0.9f)
                {
                    bool lit = rng.NextDouble() < 0.6;
                    var win = DonerMaster.Build.Prim(PrimitiveType.Cube, "Window", shell, new Vector3(wx + 0.2f, wy, 13.98f), new Vector3(0.45f, 0.65f, 0.02f), Color.black);
                    win.GetComponent<Renderer>().sharedMaterial = lit ? WindowLit : WindowDark;
                }
                x += w;
            }

            // Street lamps
            for (int i = -1; i <= 1; i += 2)
            {
                var lamp = DonerMaster.Build.Empty("Lamp", shell, new Vector3(i * 4.2f, 0, 4.6f)).transform;
                DonerMaster.Build.Prim(PrimitiveType.Cylinder, "Pole", lamp, new Vector3(0, 1.7f, 0), new Vector3(0.08f, 1.7f, 0.08f), new Color(0.15f, 0.15f, 0.17f), false, 0.5f, 0.6f);
                var bulb = DonerMaster.Build.Prim(PrimitiveType.Sphere, "Bulb", lamp, new Vector3(0, 3.45f, 0), new Vector3(0.3f, 0.3f, 0.3f), Color.black);
                bulb.GetComponent<Renderer>().sharedMaterial = Mats.Emissive(new Color(1f, 0.85f, 0.55f), 2f);
                var l = DonerMaster.Build.Empty("LampLight", lamp, new Vector3(0, 3.3f, 0)).AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 6f;
                l.intensity = 1.2f;
                l.color = new Color(1f, 0.8f, 0.5f);
            }
        }

        static Material _winLit, _winDark;
        static Material WindowLit => _winLit != null ? _winLit : (_winLit = Mats.Emissive(new Color(1f, 0.78f, 0.45f), 1.2f));
        static Material WindowDark => _winDark != null ? _winDark : (_winDark = Mats.Make(new Color(0.12f, 0.14f, 0.2f), 0.8f));
    }
}
