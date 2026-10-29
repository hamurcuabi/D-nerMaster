using System.Collections.Generic;
using UnityEngine;

namespace DonerMaster
{
    /// <summary>Catch pan under the spit. Sliced meat lands here; tapping it moves meat onto the board.</summary>
    public class MeatTray : MonoBehaviour, ITappable
    {
        public const int Capacity = 22;
        readonly List<MeatSlice> _slices = new List<MeatSlice>();

        public int Count
        {
            get { Prune(); return _slices.Count; }
        }

        public int GoodCount
        {
            get
            {
                Prune();
                int n = 0;
                foreach (var s in _slices) if (!s.IsBurnt) n++;
                return n;
            }
        }

        public int BurntCount => Count - GoodCount;

        public static MeatTray Create(Transform parent, Vector3 localPos, Vector2 size)
        {
            var root = Build.Empty("MeatTray", parent, localPos);
            var tray = root.AddComponent<MeatTray>();
            var steel = new Color(0.72f, 0.74f, 0.78f);
            float w = size.x, d = size.y, rimH = 0.07f, rimT = 0.03f;

            Build.Prim(PrimitiveType.Cube, "Pan", root.transform, new Vector3(0, 0.01f, 0), new Vector3(w, 0.02f, d), steel, true, 0.75f, 0.8f);
            Build.Prim(PrimitiveType.Cube, "RimL", root.transform, new Vector3(-w / 2, rimH / 2, 0), new Vector3(rimT, rimH, d), steel, true, 0.75f, 0.8f);
            Build.Prim(PrimitiveType.Cube, "RimR", root.transform, new Vector3(w / 2, rimH / 2, 0), new Vector3(rimT, rimH, d), steel, true, 0.75f, 0.8f);
            Build.Prim(PrimitiveType.Cube, "RimF", root.transform, new Vector3(0, rimH / 2, -d / 2), new Vector3(w, rimH, rimT), steel, true, 0.75f, 0.8f);
            Build.Prim(PrimitiveType.Cube, "RimB", root.transform, new Vector3(0, rimH / 2, d / 2), new Vector3(w, rimH, rimT), steel, true, 0.75f, 0.8f);
            return tray;
        }

        public void Add(MeatSlice s)
        {
            _slices.Add(s);
            Prune();
            while (_slices.Count > Capacity)
            {
                var old = _slices[0];
                _slices.RemoveAt(0);
                if (old != null) Juice.ShrinkAndDestroy(old.gameObject, 0.2f);
            }
        }

        /// <summary>Takes up to <paramref name="n"/> slices — oldest first, skipping burnt ones.</summary>
        public List<MeatSlice> Take(int n)
        {
            Prune();
            var result = new List<MeatSlice>();
            for (int i = 0; i < _slices.Count && result.Count < n; i++)
            {
                if (_slices[i].IsBurnt) continue;
                result.Add(_slices[i]);
            }
            foreach (var s in result) _slices.Remove(s);
            return result;
        }

        public int DiscardBurnt()
        {
            Prune();
            int n = 0;
            for (int i = _slices.Count - 1; i >= 0; i--)
            {
                if (!_slices[i].IsBurnt) continue;
                var s = _slices[i];
                _slices.RemoveAt(i);
                FX.Smoke(s.transform.position, 2);
                Juice.ShrinkAndDestroy(s.gameObject, 0.15f, n * 0.03f);
                n++;
            }
            return n;
        }

        public void ClearAll()
        {
            foreach (var s in _slices) if (s != null) Destroy(s.gameObject);
            _slices.Clear();
        }

        void Prune() => _slices.RemoveAll(s => s == null);

        public void OnTap() => GameManager.I?.OnTrayTapped();
    }
}
