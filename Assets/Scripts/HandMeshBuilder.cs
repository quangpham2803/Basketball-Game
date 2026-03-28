using UnityEngine;
using System.Collections.Generic;

public static class HandMeshBuilder
{
    private const int RingVerts = 10;

    public static Mesh BuildHandMesh(bool isRight)
    {
        float m = isRight ? 1f : -1f;
        var verts = new List<Vector3>();
        var tris = new List<int>();

        AddTube(verts, tris,
            new Vector3(0f, -0.01f, -0.22f), new Vector3(0f, -0.005f, -0.04f),
            0.028f, 0.032f, 6, RingVerts);

        AddFlatTube(verts, tris,
            new Vector3(0f, 0f, -0.04f), new Vector3(0f, 0f, 0.045f),
            0.04f * m, 0.012f, 0.038f * m, 0.01f, 5, RingVerts);

        float[] fingerX = { -0.027f * m, -0.009f * m, 0.009f * m, 0.024f * m };
        float[] fingerLen = { 0.062f, 0.073f, 0.068f, 0.052f };
        float[] fingerRad = { 0.0085f, 0.009f, 0.0085f, 0.0075f };

        for (int fi = 0; fi < 4; fi++)
        {
            Vector3 basePos = new Vector3(fingerX[fi], 0.003f, 0.045f);
            float len = fingerLen[fi];
            float rad = fingerRad[fi];

            // 3 phalanges: proximal 45%, middle 30%, distal 25%
            float[] segLen = { len * 0.45f, len * 0.3f, len * 0.25f };
            float[] segRad = { rad, rad * 0.92f, rad * 0.82f, rad * 0.55f };

            float spread = (fi - 1.5f) * 0.03f * m;
            Vector3 dir = new Vector3(spread, 0.01f, 1f).normalized;
            Vector3 pos = basePos;

            for (int seg = 0; seg < 3; seg++)
            {
                Vector3 endPos = pos + dir * segLen[seg];
                endPos.y -= seg * 0.003f;
                AddTube(verts, tris, pos, endPos, segRad[seg], segRad[seg + 1], 4, RingVerts);

                if (seg < 2)
                    AddSphere(verts, tris, endPos, segRad[seg + 1] * 1.08f, 4, 6);

                pos = endPos;
            }

            AddHemisphere(verts, tris, pos, dir, segRad[3], RingVerts);

            Vector3 nailCenter = pos - dir * segLen[2] * 0.35f + Vector3.up * segRad[3] * 0.85f;
            AddNail(verts, tris, nailCenter, dir, segRad[3] * 0.65f, segRad[3] * 1.0f);
        }

        {
            Vector3 thumbBase = new Vector3(0.035f * m, -0.003f, -0.01f);
            Vector3 thumbDir = new Vector3(0.55f * m, 0.12f, 0.83f).normalized;

            float[] tLen = { 0.028f, 0.024f, 0.019f };
            float[] tRad = { 0.0105f, 0.0095f, 0.0085f, 0.006f };
            Vector3 pos = thumbBase;

            for (int seg = 0; seg < 3; seg++)
            {
                Vector3 endPos = pos + thumbDir * tLen[seg];
                AddTube(verts, tris, pos, endPos, tRad[seg], tRad[seg + 1], 4, RingVerts);
                if (seg < 2)
                    AddSphere(verts, tris, endPos, tRad[seg + 1] * 1.06f, 3, 6);
                pos = endPos;
                thumbDir = Vector3.Slerp(thumbDir, new Vector3(0.35f * m, 0.08f, 0.93f).normalized, 0.25f);
            }

            AddHemisphere(verts, tris, pos, thumbDir, tRad[3], RingVerts);
            Vector3 tnail = pos - thumbDir * tLen[2] * 0.3f + Vector3.up * tRad[3] * 0.8f;
            AddNail(verts, tris, tnail, thumbDir, tRad[3] * 0.6f, tRad[3] * 0.9f);
        }

        var mesh = new Mesh();
        mesh.name = isRight ? "RightHand" : "LeftHand";
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static void AddTube(List<Vector3> v, List<int> t,
        Vector3 from, Vector3 to, float r0, float r1, int segments, int res)
    {
        Vector3 axis = (to - from).normalized;
        Vector3 perp = Perp(axis);
        Vector3 perp2 = Vector3.Cross(axis, perp);

        int baseIdx = v.Count;

        for (int ring = 0; ring <= segments; ring++)
        {
            float frac = (float)ring / segments;
            Vector3 center = Vector3.Lerp(from, to, frac);
            float radius = Mathf.Lerp(r0, r1, frac);

            for (int i = 0; i < res; i++)
            {
                float angle = i * Mathf.PI * 2f / res;
                Vector3 offset = (perp * Mathf.Cos(angle) + perp2 * Mathf.Sin(angle)) * radius;
                v.Add(center + offset);
            }
        }

        for (int ring = 0; ring < segments; ring++)
            StitchRings(t, baseIdx + ring * res, baseIdx + (ring + 1) * res, res);
    }

    private static void AddFlatTube(List<Vector3> v, List<int> t,
        Vector3 from, Vector3 to, float w0, float h0, float w1, float h1, int segments, int res)
    {
        Vector3 axis = (to - from).normalized;
        Vector3 right = Vector3.Cross(axis, Vector3.up).normalized;
        if (right.sqrMagnitude < 0.01f) right = Vector3.right;
        Vector3 up = Vector3.Cross(right, axis);

        int baseIdx = v.Count;

        for (int ring = 0; ring <= segments; ring++)
        {
            float frac = (float)ring / segments;
            Vector3 center = Vector3.Lerp(from, to, frac);
            float w = Mathf.Lerp(Mathf.Abs(w0), Mathf.Abs(w1), frac) * Mathf.Sign(w0);
            float h = Mathf.Lerp(h0, h1, frac);

            for (int i = 0; i < res; i++)
            {
                float angle = i * Mathf.PI * 2f / res;
                Vector3 offset = right * (Mathf.Cos(angle) * w) + up * (Mathf.Sin(angle) * h);
                v.Add(center + offset);
            }
        }

        for (int ring = 0; ring < segments; ring++)
            StitchRings(t, baseIdx + ring * res, baseIdx + (ring + 1) * res, res);
    }

    private static void AddSphere(List<Vector3> v, List<int> t, Vector3 center, float radius, int stacks, int res)
    {
        int baseIdx = v.Count;

        for (int stack = 0; stack <= stacks; stack++)
        {
            float phi = (float)stack / stacks * Mathf.PI;
            float r = Mathf.Sin(phi) * radius;
            float y = Mathf.Cos(phi) * radius;

            for (int i = 0; i < res; i++)
            {
                float angle = i * Mathf.PI * 2f / res;
                v.Add(center + new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r));
            }
        }

        for (int stack = 0; stack < stacks; stack++)
            StitchRings(t, baseIdx + stack * res, baseIdx + (stack + 1) * res, res);
    }

    private static void AddHemisphere(List<Vector3> v, List<int> t, Vector3 center, Vector3 dir, float radius, int res)
    {
        Vector3 perp = Perp(dir);
        Vector3 perp2 = Vector3.Cross(dir, perp);
        int rings = 4;
        int baseIdx = v.Count;

        for (int ring = 0; ring <= rings; ring++)
        {
            float phi = (float)ring / rings * Mathf.PI * 0.5f;
            float r = Mathf.Cos(phi) * radius;
            float h = Mathf.Sin(phi) * radius;

            for (int i = 0; i < res; i++)
            {
                float angle = i * Mathf.PI * 2f / res;
                v.Add(center + (perp * Mathf.Cos(angle) + perp2 * Mathf.Sin(angle)) * r + dir * h);
            }
        }

        int tipIdx = v.Count;
        v.Add(center + dir * radius);

        for (int ring = 0; ring < rings - 1; ring++)
            StitchRings(t, baseIdx + ring * res, baseIdx + (ring + 1) * res, res);

        int lastRing = baseIdx + (rings - 1) * res;
        for (int i = 0; i < res; i++)
        {
            t.Add(lastRing + i);
            t.Add(tipIdx);
            t.Add(lastRing + (i + 1) % res);
        }
    }

    private static void AddNail(List<Vector3> v, List<int> t, Vector3 center, Vector3 dir, float width, float length)
    {
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(dir, up).normalized;
        int baseIdx = v.Count;
        int rx = 3, rz = 4;

        for (int z = 0; z <= rz; z++)
        {
            for (int x = 0; x <= rx; x++)
            {
                float u = (float)x / rx - 0.5f;
                float vv = (float)z / rz;
                // Slight convex curve
                float lift = (1f - u * u * 4f) * 0.0015f;
                v.Add(center + right * u * width + dir * vv * length + up * lift);
            }
        }

        for (int z = 0; z < rz; z++)
        {
            for (int x = 0; x < rx; x++)
            {
                int i = baseIdx + z * (rx + 1) + x;
                t.Add(i); t.Add(i + rx + 1); t.Add(i + 1);
                t.Add(i + 1); t.Add(i + rx + 1); t.Add(i + rx + 2);
            }
        }
    }

    private static void StitchRings(List<int> t, int ring0, int ring1, int res)
    {
        for (int i = 0; i < res; i++)
        {
            int a = ring0 + i;
            int b = ring0 + (i + 1) % res;
            int c = ring1 + i;
            int d = ring1 + (i + 1) % res;
            t.Add(a); t.Add(c); t.Add(b);
            t.Add(b); t.Add(c); t.Add(d);
        }
    }

    private static Vector3 Perp(Vector3 dir)
    {
        Vector3 cross = Vector3.Cross(dir, Vector3.up);
        if (cross.sqrMagnitude < 0.001f)
            cross = Vector3.Cross(dir, Vector3.forward);
        return cross.normalized;
    }
}
