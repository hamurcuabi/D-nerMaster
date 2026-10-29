using System.Collections.Generic;
using UnityEngine;

namespace DonerMaster
{
    public static class MeshFactory
    {
        /// <summary>
        /// A closed, tapered cylinder sector (a "slice of cake" seen from above), pivot at the bottom centre.
        /// Angles are in degrees, measured from +X towards +Z.
        /// </summary>
        public static Mesh Sector(float a0, float a1, float height, float rBottom, float rTop, int segments)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            var up = Vector3.up;

            // Outer curved surface
            float slope = (rBottom - rTop) / Mathf.Max(0.0001f, height);
            int baseIdx = v.Count;
            for (int i = 0; i <= segments; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)segments) * Mathf.Deg2Rad;
                var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var normal = (dir + up * slope).normalized;
                v.Add(dir * rBottom); n.Add(normal);
                v.Add(dir * rTop + up * height); n.Add(normal);
            }
            for (int i = 0; i < segments; i++)
            {
                int b = baseIdx + i * 2;
                float am = Mathf.Lerp(a0, a1, (i + 0.5f) / segments) * Mathf.Deg2Rad;
                var outward = new Vector3(Mathf.Cos(am), 0f, Mathf.Sin(am));
                AddQuad(v, t, b, b + 1, b + 3, b + 2, outward);
            }

            // Caps
            AddCap(v, n, t, a0, a1, rBottom, 0f, Vector3.down, segments);
            AddCap(v, n, t, a0, a1, rTop, height, Vector3.up, segments);

            // Radial walls
            AddWall(v, n, t, a0, height, rBottom, rTop, false);
            AddWall(v, n, t, a1, height, rBottom, rTop, true);

            var mesh = new Mesh { name = "DonerSector" };
            mesh.SetVertices(v);
            mesh.SetNormals(n);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        static void AddCap(List<Vector3> v, List<Vector3> n, List<int> t, float a0, float a1, float r, float y, Vector3 normal, int seg)
        {
            int c = v.Count;
            v.Add(new Vector3(0, y, 0)); n.Add(normal);
            for (int i = 0; i <= seg; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)seg) * Mathf.Deg2Rad;
                v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r)); n.Add(normal);
            }
            for (int i = 0; i < seg; i++) AddTri(v, t, c, c + 1 + i, c + 2 + i, normal);
        }

        static void AddWall(List<Vector3> v, List<Vector3> n, List<int> t, float angle, float h, float rb, float rt, bool positiveSide)
        {
            float a = angle * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a));
            var tangent = new Vector3(-Mathf.Sin(a), 0, Mathf.Cos(a));
            var normal = positiveSide ? tangent : -tangent;
            int b = v.Count;
            v.Add(Vector3.zero); v.Add(new Vector3(0, h, 0)); v.Add(dir * rt + Vector3.up * h); v.Add(dir * rb);
            for (int i = 0; i < 4; i++) n.Add(normal);
            AddQuad(v, t, b, b + 1, b + 2, b + 3, normal);
        }

        static void AddQuad(List<Vector3> v, List<int> t, int a, int b, int c, int d, Vector3 outward)
        {
            AddTri(v, t, a, b, c, outward);
            AddTri(v, t, a, c, d, outward);
        }

        /// <summary>Adds a triangle, flipping winding if needed so its face points along <paramref name="outward"/>.</summary>
        static void AddTri(List<Vector3> v, List<int> t, int a, int b, int c, Vector3 outward)
        {
            var cross = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
            if (Vector3.Dot(cross, outward) < 0) { int tmp = b; b = c; c = tmp; }
            t.Add(a); t.Add(b); t.Add(c);
        }
    }

    /// <summary>Small helpers for building grey-box geometry from primitives.</summary>
    public static class Build
    {
        public static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, Color color,
            bool collider = false, float smoothness = 0.25f, float metallic = 0f)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = Mats.Get(color, smoothness, metallic);
            if (!collider)
            {
                var c = go.GetComponent<Collider>();
                if (c != null) Object.Destroy(c);
            }
            return go;
        }

        public static GameObject Empty(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        public static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var c);
            return c;
        }
    }
}
