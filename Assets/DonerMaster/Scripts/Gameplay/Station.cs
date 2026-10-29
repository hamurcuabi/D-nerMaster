using UnityEngine;

namespace DonerMaster
{
    public enum StationKind { Lavash, Bread, Ingredient, Ayran, Trash, HeatKnob }

    /// <summary>Anything on the counter you tap: bread, toppings, ayran fridge, trash, burner knob.</summary>
    public class Station : MonoBehaviour, ITappable
    {
        public StationKind Kind;
        public Ingredient Ingredient;
        public string Label;
        public Transform Knob;

        /// <summary>Where flying items start from.</summary>
        public Vector3 SpawnPoint => transform.position + Vector3.up * 0.25f;

        public static Station Create(Transform parent, Vector3 localPos, StationKind kind, Ingredient ing = Ingredient.Tomato)
        {
            var root = Build.Empty("Station_" + kind + (kind == StationKind.Ingredient ? "_" + ing : ""), parent, localPos);
            var st = root.AddComponent<Station>();
            st.Kind = kind;
            st.Ingredient = ing;
            var steel = new Color(0.74f, 0.76f, 0.8f);
            var t = root.transform;
            var box = root.AddComponent<BoxCollider>();

            switch (kind)
            {
                case StationKind.Lavash:
                    st.Label = "LAVASH";
                    Build.Prim(PrimitiveType.Cube, "Tray", t, new Vector3(0, 0.02f, 0), new Vector3(0.5f, 0.04f, 0.42f), steel, false, 0.7f, 0.7f);
                    for (int i = 0; i < 6; i++)
                        Build.Prim(PrimitiveType.Cylinder, "Lavash", t, new Vector3(Random.Range(-0.01f, 0.01f), 0.05f + i * 0.012f, Random.Range(-0.01f, 0.01f)),
                            new Vector3(0.4f, 0.005f, 0.36f), new Color(0.93f, 0.84f, 0.62f) * (i % 2 == 0 ? 1f : 0.96f), false, 0.1f);
                    box.center = new Vector3(0, 0.08f, 0); box.size = new Vector3(0.52f, 0.2f, 0.45f);
                    break;

                case StationKind.Bread:
                    st.Label = "BREAD";
                    Build.Prim(PrimitiveType.Cube, "Tray", t, new Vector3(0, 0.02f, 0), new Vector3(0.5f, 0.04f, 0.42f), steel, false, 0.7f, 0.7f);
                    for (int i = 0; i < 2; i++)
                    {
                        var loaf = Build.Prim(PrimitiveType.Capsule, "Loaf", t, new Vector3(0, 0.1f, -0.08f + i * 0.16f), new Vector3(0.13f, 0.2f, 0.14f), new Color(0.87f, 0.64f, 0.32f), false, 0.15f);
                        loaf.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    }
                    box.center = new Vector3(0, 0.1f, 0); box.size = new Vector3(0.52f, 0.24f, 0.45f);
                    break;

                case StationKind.Ingredient:
                    st.Label = GameData.IngredientNames[(int)ing].ToUpper();
                    Build.Prim(PrimitiveType.Cube, "Bin", t, new Vector3(0, 0.07f, 0), new Vector3(0.44f, 0.14f, 0.36f), steel, false, 0.7f, 0.7f);
                    Build.Prim(PrimitiveType.Cube, "Inside", t, new Vector3(0, 0.141f, 0), new Vector3(0.4f, 0.002f, 0.32f), new Color(0.3f, 0.3f, 0.32f));
                    var c = GameData.IngredientColors[(int)ing];
                    for (int i = 0; i < 14; i++)
                    {
                        var p = new Vector3(Random.Range(-0.16f, 0.16f), 0.15f + Random.Range(0f, 0.025f), Random.Range(-0.12f, 0.12f));
                        var piece = ing == Ingredient.Lettuce
                            ? Build.Prim(PrimitiveType.Cube, "Piece", t, p, new Vector3(0.09f, 0.02f, 0.06f), c, false, 0.4f)
                            : ing == Ingredient.Chili
                                ? Build.Prim(PrimitiveType.Capsule, "Piece", t, p, new Vector3(0.03f, 0.05f, 0.03f), c, false, 0.6f)
                                : Build.Prim(PrimitiveType.Cylinder, "Piece", t, p, new Vector3(0.075f, 0.012f, 0.075f), c * Random.Range(0.92f, 1.05f), false, 0.55f);
                        piece.transform.localRotation = Quaternion.Euler(Random.Range(-25f, 25f) + (ing == Ingredient.Chili ? 90f : 0f), Random.Range(0f, 360f), Random.Range(-25f, 25f));
                    }
                    box.center = new Vector3(0, 0.09f, 0); box.size = new Vector3(0.46f, 0.2f, 0.38f);
                    break;

                case StationKind.Ayran:
                    st.Label = "AYRAN";
                    Build.Prim(PrimitiveType.Cube, "Fridge", t, new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.5f, 0.42f), new Color(0.92f, 0.94f, 0.97f), false, 0.6f);
                    var glass = Build.Prim(PrimitiveType.Cube, "Glass", t, new Vector3(0, 0.26f, -0.212f), new Vector3(0.4f, 0.36f, 0.005f), Color.black);
                    glass.GetComponent<Renderer>().sharedMaterial = Mats.Emissive(new Color(0.55f, 0.8f, 1f), 0.5f);
                    for (int i = 0; i < 3; i++)
                    for (int row = 0; row < 2; row++)
                    {
                        Build.Prim(PrimitiveType.Cylinder, "Cup", t, new Vector3(-0.12f + i * 0.12f, 0.14f + row * 0.17f, -0.17f), new Vector3(0.08f, 0.055f, 0.08f), Color.white, false, 0.5f);
                    }
                    Build.Prim(PrimitiveType.Cube, "Brand", t, new Vector3(0, 0.46f, -0.212f), new Vector3(0.42f, 0.05f, 0.006f), new Color(0.2f, 0.5f, 0.85f));
                    box.center = new Vector3(0, 0.25f, 0); box.size = new Vector3(0.52f, 0.52f, 0.44f);
                    break;

                case StationKind.Trash:
                    st.Label = "TRASH";
                    Build.Prim(PrimitiveType.Cylinder, "Bin", t, new Vector3(0, 0.17f, 0), new Vector3(0.34f, 0.17f, 0.34f), new Color(0.3f, 0.36f, 0.32f), false, 0.4f, 0.3f);
                    Build.Prim(PrimitiveType.Cylinder, "Lid", t, new Vector3(0, 0.35f, 0), new Vector3(0.37f, 0.015f, 0.37f), new Color(0.22f, 0.26f, 0.24f), false, 0.5f, 0.3f);
                    box.center = new Vector3(0, 0.18f, 0); box.size = new Vector3(0.38f, 0.38f, 0.38f);
                    break;

                case StationKind.HeatKnob:
                    st.Label = "HEAT";
                    Build.Prim(PrimitiveType.Cylinder, "Plate", t, new Vector3(0, 0.01f, 0), new Vector3(0.3f, 0.01f, 0.3f), new Color(0.2f, 0.2f, 0.22f), false, 0.6f, 0.6f);
                    var knob = Build.Empty("Knob", t, new Vector3(0, 0.05f, 0));
                    Build.Prim(PrimitiveType.Cylinder, "Cap", knob.transform, Vector3.zero, new Vector3(0.18f, 0.04f, 0.18f), new Color(0.08f, 0.08f, 0.08f), false, 0.5f);
                    Build.Prim(PrimitiveType.Cube, "Mark", knob.transform, new Vector3(0, 0.045f, -0.05f), new Vector3(0.02f, 0.01f, 0.08f), new Color(0.95f, 0.25f, 0.15f));
                    st.Knob = knob.transform;
                    box.center = new Vector3(0, 0.05f, 0); box.size = new Vector3(0.32f, 0.14f, 0.32f);
                    break;
            }
            return st;
        }

        public void SetKnob(int heat)
        {
            if (Knob == null) return;
            Knob.localRotation = Quaternion.Euler(0f, -60f + heat * 60f, 0f);
        }

        public void OnTap()
        {
            Juice.Punch(transform, 0.12f, 0.22f);
            GameManager.I?.OnStationTapped(this);
        }
    }
}
