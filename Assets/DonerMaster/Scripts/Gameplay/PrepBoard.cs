using System.Collections.Generic;
using UnityEngine;

namespace DonerMaster
{
    /// <summary>The cutting board where an order is assembled: bread → meat → toppings → wrap.</summary>
    public class PrepBoard : MonoBehaviour, ITappable
    {
        public Product Product { get; private set; }
        public readonly List<MeatSlice> Meat = new List<MeatSlice>();
        public readonly HashSet<Ingredient> Ingredients = new HashSet<Ingredient>();
        public bool Ayran { get; private set; }
        public bool Wrapped { get; private set; }

        public bool IsEmpty => Product == Product.None && !Ayran && Meat.Count == 0;
        public bool CanWrap => Product != Product.None && !Wrapped && Meat.Count > 0;
        public bool ReadyToServe => (Wrapped) || (Product == Product.None && Ayran);

        Transform _contents;
        Transform _ayranSpot;
        GameObject _base;
        GameObject _ayranCup;
        GameObject _wrappedItem;
        readonly List<GameObject> _toppingObjects = new List<GameObject>();

        static readonly Color Lavash = new Color(0.93f, 0.84f, 0.62f);
        static readonly Color Bread = new Color(0.87f, 0.64f, 0.32f);

        public static PrepBoard Create(Transform parent, Vector3 localPos)
        {
            var root = Build.Empty("PrepBoard", parent, localPos);
            var board = root.AddComponent<PrepBoard>();
            Build.Prim(PrimitiveType.Cube, "Board", root.transform, new Vector3(0, 0.02f, 0), new Vector3(1.15f, 0.04f, 0.78f),
                new Color(0.72f, 0.5f, 0.3f), true, 0.2f);
            // wood grain stripes
            for (int i = 0; i < 4; i++)
                Build.Prim(PrimitiveType.Cube, "Grain", root.transform, new Vector3(0, 0.041f, -0.27f + i * 0.18f), new Vector3(1.05f, 0.002f, 0.012f),
                    new Color(0.62f, 0.42f, 0.25f));
            board._contents = Build.Empty("Contents", root.transform, new Vector3(0, 0.045f, 0)).transform;
            board._ayranSpot = Build.Empty("AyranSpot", root.transform, new Vector3(0.72f, 0f, 0.1f)).transform;
            return board;
        }

        public bool SetProduct(Product p, Vector3 fromWorld)
        {
            if (Product != Product.None) return false;
            Product = p;
            if (p == Product.Wrap)
            {
                _base = Build.Prim(PrimitiveType.Cylinder, "Lavash", _contents, Vector3.zero, new Vector3(0.8f, 0.006f, 0.7f), Lavash, false, 0.1f);
            }
            else
            {
                _base = Build.Prim(PrimitiveType.Capsule, "BreadBottom", _contents, new Vector3(0, 0.05f, 0), new Vector3(0.24f, 0.36f, 0.26f), Bread, false, 0.15f);
                _base.transform.localRotation = Quaternion.Euler(0, 0, 90);
                _base.transform.localScale = new Vector3(0.18f, 0.36f, 0.26f);
            }
            var target = _base.transform.localPosition;
            _base.transform.position = fromWorld;
            Juice.ArcToLocal(_base.transform, _contents, target, 0.4f, 0.25f, () =>
            {
                Juice.Punch(transform, 0.05f, 0.2f);
                AudioManager.Play(Sfx.Pop, 0.7f, 0.8f);
                FX.Crumbs(transform.position + Vector3.up * 0.06f, p == Product.Wrap ? Lavash : Bread, 5);
            });
            return true;
        }

        public void AddMeat(List<MeatSlice> slices)
        {
            int startIndex = Meat.Count;
            for (int i = 0; i < slices.Count; i++)
            {
                var s = slices[i];
                Meat.Add(s);
                s.Freeze();
                int idx = startIndex + i;
                float x = Mathf.Lerp(-0.28f, 0.28f, (idx % 6) / 5f) + Random.Range(-0.04f, 0.04f);
                float z = Random.Range(-0.08f, 0.08f);
                float y = (Product == Product.Sandwich ? 0.12f : 0.015f) + 0.008f * (idx / 6);
                var local = new Vector3(x, y, z);
                var t = s.transform;
                float delay = i * 0.05f;
                Juice.Delay(delay, () =>
                {
                    if (t == null) return;
                    Juice.ArcToLocal(t, _contents, local, 0.35f, 0.22f, () =>
                    {
                        if (t == null) return;
                        t.localRotation = Quaternion.Euler(90f, Random.Range(-30f, 30f), Random.Range(-15f, 15f));
                        AudioManager.Play(Sfx.Pop, 0.35f, 1.3f, 0.15f);
                    });
                });
            }
        }

        public bool AddIngredient(Ingredient ing, Vector3 fromWorld)
        {
            if (Product == Product.None || Wrapped) return false;
            if (!Ingredients.Add(ing))
            {
                Juice.Punch(transform, 0.04f, 0.15f);
                return true;
            }
            var color = GameData.IngredientColors[(int)ing];
            int pieces = 5;
            for (int i = 0; i < pieces; i++)
            {
                GameObject go;
                switch (ing)
                {
                    case Ingredient.Tomato:
                        go = Build.Prim(PrimitiveType.Cylinder, "Tomato", null, fromWorld, new Vector3(0.09f, 0.012f, 0.09f), color, false, 0.6f);
                        break;
                    case Ingredient.Onion:
                        go = Build.Prim(PrimitiveType.Cylinder, "Onion", null, fromWorld, new Vector3(0.08f, 0.008f, 0.08f), color, false, 0.5f);
                        break;
                    case Ingredient.Lettuce:
                        go = Build.Prim(PrimitiveType.Cube, "Lettuce", null, fromWorld, new Vector3(0.11f, 0.008f, 0.07f), color, false, 0.4f);
                        break;
                    case Ingredient.Pickle:
                        go = Build.Prim(PrimitiveType.Cylinder, "Pickle", null, fromWorld, new Vector3(0.06f, 0.01f, 0.06f), color, false, 0.5f);
                        break;
                    default:
                        go = Build.Prim(PrimitiveType.Capsule, "Chili", null, fromWorld, new Vector3(0.03f, 0.05f, 0.03f), color, false, 0.6f);
                        break;
                }
                _toppingObjects.Add(go);
                int ingIndex = Ingredients.Count;
                float y = (Product == Product.Sandwich ? 0.14f : 0.03f) + 0.006f * ingIndex;
                var local = new Vector3(Random.Range(-0.3f, 0.3f), y, Random.Range(-0.12f, 0.12f));
                var t = go.transform;
                float delay = i * 0.035f;
                Juice.Delay(delay, () =>
                {
                    if (t == null) return;
                    Juice.ArcToLocal(t, _contents, local, 0.45f, 0.24f, () =>
                    {
                        if (t == null) return;
                        t.localRotation = ing == Ingredient.Chili ? Quaternion.Euler(90f, Random.Range(0f, 360f), 0f) : Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                        Juice.Punch(t, 0.3f, 0.2f);
                    });
                });
            }
            Juice.Delay(0.25f, () =>
            {
                AudioManager.Play(Sfx.Pop, 0.6f, 1.1f + 0.08f * (int)ing);
                Haptics.Light();
            });
            return true;
        }

        public bool AddAyran(Vector3 fromWorld)
        {
            if (Ayran) return false;
            Ayran = true;
            _ayranCup = Build.Empty("Ayran", null, fromWorld);
            Build.Prim(PrimitiveType.Cylinder, "Cup", _ayranCup.transform, new Vector3(0, 0.1f, 0), new Vector3(0.14f, 0.1f, 0.14f), new Color(0.97f, 0.97f, 0.95f), false, 0.6f);
            Build.Prim(PrimitiveType.Cylinder, "Band", _ayranCup.transform, new Vector3(0, 0.12f, 0), new Vector3(0.145f, 0.03f, 0.145f), new Color(0.2f, 0.5f, 0.85f), false, 0.4f);
            Build.Prim(PrimitiveType.Cylinder, "Lid", _ayranCup.transform, new Vector3(0, 0.205f, 0), new Vector3(0.15f, 0.006f, 0.15f), new Color(0.75f, 0.78f, 0.82f), false, 0.8f, 0.8f);
            Juice.ArcToLocal(_ayranCup.transform, _ayranSpot, Vector3.zero, 0.4f, 0.25f, () =>
            {
                Juice.Punch(_ayranCup != null ? _ayranCup.transform : null, 0.2f, 0.25f);
                AudioManager.Play(Sfx.Pop, 0.6f, 0.6f);
            });
            return true;
        }

        public bool Wrap()
        {
            if (!CanWrap) return false;
            Wrapped = true;

            // Hide the loose contents and show a finished item
            foreach (Transform child in _contents) child.gameObject.SetActive(false);
            foreach (var m in Meat) if (m != null) m.gameObject.SetActive(false);          // some may still be in flight
            foreach (var go in _toppingObjects) if (go != null) go.SetActive(false);
            _wrappedItem = Build.Empty("WrappedItem", _contents, new Vector3(0, 0.1f, 0));
            if (Product == Product.Wrap)
            {
                var roll = Build.Prim(PrimitiveType.Cylinder, "Roll", _wrappedItem.transform, Vector3.zero, new Vector3(0.2f, 0.32f, 0.2f), Lavash, false, 0.15f);
                roll.transform.localRotation = Quaternion.Euler(0, 0, 90);
                var paper = Build.Prim(PrimitiveType.Cylinder, "Paper", _wrappedItem.transform, new Vector3(-0.1f, 0, 0), new Vector3(0.215f, 0.2f, 0.215f), new Color(0.96f, 0.95f, 0.9f), false, 0.1f);
                paper.transform.localRotation = Quaternion.Euler(0, 0, 90);
                // a peek of meat and toppings at the open end
                var peek = Build.Prim(PrimitiveType.Cylinder, "Filling", _wrappedItem.transform, new Vector3(0.315f, 0, 0), new Vector3(0.16f, 0.005f, 0.16f), MeatSlice.CookColor(AvgCook()), false, 0.4f);
                peek.transform.localRotation = Quaternion.Euler(0, 0, 90);
            }
            else
            {
                var bottom = Build.Prim(PrimitiveType.Capsule, "Bun", _wrappedItem.transform, new Vector3(0, -0.02f, 0), new Vector3(0.2f, 0.36f, 0.26f), Bread, false, 0.15f);
                bottom.transform.localRotation = Quaternion.Euler(0, 0, 90);
                var filling = Build.Prim(PrimitiveType.Cube, "Filling", _wrappedItem.transform, new Vector3(0, 0.04f, -0.02f), new Vector3(0.6f, 0.05f, 0.2f), MeatSlice.CookColor(AvgCook()), false, 0.4f);
                var paper = Build.Prim(PrimitiveType.Cube, "Paper", _wrappedItem.transform, new Vector3(-0.12f, 0.0f, 0), new Vector3(0.4f, 0.22f, 0.28f), new Color(0.96f, 0.95f, 0.9f), false, 0.1f);
                filling.transform.localScale = new Vector3(0.6f, 0.05f, 0.2f);
                paper.transform.localScale = new Vector3(0.4f, 0.22f, 0.28f);
            }
            Juice.PopIn(_wrappedItem.transform, 0.3f);
            AudioManager.Play(Sfx.Paper, 0.9f);
            Haptics.Light();
            return true;
        }

        float AvgCook()
        {
            if (Meat.Count == 0) return 50f;
            float c = 0f;
            foreach (var m in Meat) if (m != null) c += m.Cook;
            return c / Meat.Count;
        }

        /// <summary>Throw the finished item to the customer and reset the board.</summary>
        public void SendTo(Vector3 worldTarget)
        {
            var parcel = Build.Empty("Parcel", null, transform.position + Vector3.up * 0.15f);
            if (_wrappedItem != null)
            {
                _wrappedItem.transform.SetParent(parcel.transform, true);
            }
            if (_ayranCup != null)
            {
                _ayranCup.transform.SetParent(parcel.transform, true);
                _ayranCup = null;
            }
            _wrappedItem = null;
            Juice.Arc(parcel.transform, worldTarget, 0.8f, 0.35f, () => Juice.ShrinkAndDestroy(parcel, 0.15f, 0.25f));
            ResetBoard(false);
        }

        /// <summary>Clear everything (trash) — with a little animation.</summary>
        public void Clear()
        {
            ResetBoard(true);
        }

        void ResetBoard(bool animate)
        {
            for (int i = _contents.childCount - 1; i >= 0; i--)
            {
                var go = _contents.GetChild(i).gameObject;
                if (animate) Juice.ShrinkAndDestroy(go, 0.15f, i * 0.01f);
                else Destroy(go);
            }
            foreach (var go in _toppingObjects)
                if (go != null) { if (animate) Juice.ShrinkAndDestroy(go, 0.15f); else Destroy(go); }
            _toppingObjects.Clear();
            foreach (var m in Meat)
                if (m != null) { if (animate) Juice.ShrinkAndDestroy(m.gameObject, 0.15f); else Destroy(m.gameObject); }
            if (_ayranCup != null) { if (animate) Juice.ShrinkAndDestroy(_ayranCup, 0.15f); else Destroy(_ayranCup); }
            _ayranCup = null;
            _wrappedItem = null;
            _base = null;
            Meat.Clear();
            Ingredients.Clear();
            Product = Product.None;
            Ayran = false;
            Wrapped = false;
        }

        public void OnTap() => GameManager.I?.OnBoardTapped();
    }
}
