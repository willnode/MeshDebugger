using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//[Serializable]
public class MeshInfo
{
    public Mesh m_Mesh;
    private int m_lastMeshId = -1;
    public bool m_MeshReadable;
    public Bounds m_MeshBounds;
    public int m_VertCount;
    public int m_IndiceCount;
    public int m_MeshSubmeshCount;

    public List<Vector3> m_Verts;
    public List<int> m_VertUsedCounts;
    public Dictionary<Vector3, int> m_VertSimilars;
    public List<Vector3> m_VertToIndicesDir;

    public List<int>[] m_Indices;
    public int[] m_IndiceOffsets;
    public MeshTopology[] m_IndiceTypes;
    public List<Vector3>[] m_IndiceMedians;
    public List<Vector3>[] m_IndiceNormals;
    public List<float>[] m_IndiceAreas;

    public int m_VertSimilarsMax;
    public int m_VertUsedCountMax;
    public float m_IndiceAreaMax;

    public List<Color> m_Colors;
    public List<Vector4>[] m_UVs;
    public List<BoneWeight> m_BoneWeights;

    // Normal = 0, Tangent = 1, Bitangent = 2
    public List<Vector3>[] m_Normals;

    public List<float> m_NormalFlips;

    public bool hasUpdated
    {
        get
        {
            return m_Mesh && (m_Mesh.GetInstanceID() == m_lastMeshId);
        }
    }

    public void Update()
    {
        if (!m_Mesh)
            return;
        if (!(m_MeshReadable = m_Mesh.isReadable))
            return;
        {
            m_MeshBounds = m_Mesh.bounds;
            m_MeshSubmeshCount = m_Mesh.subMeshCount;
        }
        {
            Set(ref m_Verts, m_Mesh.vertices);
            Set(ref m_Colors, m_Mesh.colors);
            Set(ref m_BoneWeights, m_Mesh.boneWeights);
            Resize(ref m_UVs, 4);
            for (int i = 0; i < m_MeshSubmeshCount; i++)
            {
                m_Mesh.GetUVs(i, m_UVs[i]);
            }
            m_VertCount = m_Verts.Count;
        }
        {
            Resize(ref m_Normals, 3);
            Set(ref m_Normals[0], m_Mesh.normals);
            Reset(ref m_NormalFlips);
            var tan = m_Mesh.tangents;
            for (int i = 0; i < m_Normals[0].Count; i++)
            {
                m_Normals[1].Add(tan[i]);
                m_NormalFlips.Add(tan[i].w);
                m_Normals[2].Add(Vector3.Cross(m_Normals[0][i], m_Normals[1][i]) * m_NormalFlips[i]);
            }
        }
        {
            Resize(ref m_Indices, m_MeshSubmeshCount);
            Resize(ref m_IndiceAreas, m_MeshSubmeshCount);
            Resize(ref m_IndiceMedians, m_MeshSubmeshCount);
            Resize(ref m_IndiceNormals, m_MeshSubmeshCount);
            Array.Resize(ref m_IndiceOffsets, m_MeshSubmeshCount);
            Array.Resize(ref m_IndiceTypes, m_MeshSubmeshCount);
            var iter = 0;
            for (int i = 0; i < m_MeshSubmeshCount; i++)
            {
                Set(ref m_Indices[i], m_Mesh.GetIndices(i));
                m_IndiceTypes[i] = m_Mesh.GetTopology(i);
                m_IndiceOffsets[i] = iter;
                var steps = m_TopologyDivision[m_IndiceTypes[i]];
                var indice = m_Indices[i];
                var normal = m_Normals[0];
                iter += indice.Count;
                for (int m = 0; m < indice.Count; m += steps)
                {
                    switch (steps)
                    {
                        case 1:
                            m_IndiceMedians[i].Add(m_Verts[indice[m]]);
                            m_IndiceNormals[i].Add(normal[indice[m]]);
                            m_IndiceAreas[i].Add(0); break;
                        case 2:
                            m_IndiceMedians[i].Add((m_Verts[indice[m]] + m_Verts[indice[m + 1]]) / 2);
                            m_IndiceNormals[i].Add((normal[indice[m]] + normal[indice[m + 1]]).normalized);
                            m_IndiceAreas[i].Add(0); break;
                        case 3:
                            m_IndiceMedians[i].Add((m_Verts[indice[m]] + m_Verts[indice[m + 1]] + m_Verts[indice[m + 2]]) / 3);
                            m_IndiceNormals[i].Add((normal[indice[m]] + normal[indice[m + 1]] + normal[indice[m + 2]]).normalized);
                            m_IndiceAreas[i].Add(GetTriArea(m_Verts[indice[m]], m_Verts[indice[m + 1]], m_Verts[indice[m + 2]])); break;
                        case 4:
                            m_IndiceMedians[i].Add((m_Verts[indice[m]] + m_Verts[indice[m + 1]] + m_Verts[indice[m + 2]] + m_Verts[indice[m + 3]]) / 4);
                            m_IndiceNormals[i].Add((normal[indice[m]] + normal[indice[m + 1]] + normal[indice[m + 2]] + normal[indice[m + 3]]).normalized);
                            m_IndiceAreas[i].Add(GetTriArea(m_Verts[indice[m]], m_Verts[indice[m + 1]], m_Verts[indice[m + 2]]) +
                             GetTriArea(m_Verts[indice[m + 3]], m_Verts[indice[m + 1]], m_Verts[indice[m + 2]])); break;
                    }
                    m_IndiceAreaMax = Mathf.Max(m_IndiceAreaMax, m_IndiceAreas[i][m_IndiceAreas[i].Count - 1]);
                }
            }
            m_IndiceCount = iter;
        }
        {
            m_VertSimilarsMax = 0;
            m_VertUsedCountMax = 0;
            Resize(ref m_VertUsedCounts, m_Verts.Count);
            Resize(ref m_VertToIndicesDir, m_Verts.Count);
            Reset(ref m_VertSimilars);
            for (int i = 0; i < m_Verts.Count; i++)
            {
                var v = m_Verts[i];
                if (m_VertSimilars.ContainsKey(v))
                    m_VertSimilars[v]++;
                else
                    m_VertSimilars[v] = 1;
                m_VertSimilarsMax = Mathf.Max(m_VertSimilarsMax, m_VertSimilars[v]);
            }
            for (int i = 0; i < m_Indices.Length; i++)
            {
                var indice = m_Indices[i];
                for (int j = 0; j < indice.Count; j++)
                {
                    var idx = indice[j];
                    m_VertToIndicesDir[idx] = (m_Verts[idx] - m_IndiceMedians[i][j / m_TopologyDivision[m_IndiceTypes[i]]]).normalized;
                    m_VertUsedCountMax = Mathf.Max(m_VertUsedCountMax, m_VertUsedCounts[idx]++);
                }
            }
        }
        m_lastMeshId = m_Mesh.GetInstanceID();
    }

    static public Dictionary<MeshTopology, int> m_TopologyDivision = new Dictionary<MeshTopology, int>()
        {
            {MeshTopology.Lines, 2},
            {MeshTopology.LineStrip, 2},
            {MeshTopology.Points, 1},
            {MeshTopology.Quads, 4},
            {MeshTopology.Triangles, 3},
        };

    private static float GetTriArea(Vector3 A, Vector3 B, Vector3 C)
    {
        var a = Vector3.Distance(B, C);
        var b = Vector3.Distance(A, C);
        var c = Vector3.Distance(A, B);
        var p = (a + b + c) / 2;
        return Mathf.Sqrt(p * (p - a) * (p - b) * (p - c));
    }

    private static void Reset<T>(ref List<T> list)
    {
        if (list == null)
            list = new List<T>();
        else
            list.Clear();
    }

    private static void Reset<K, V>(ref Dictionary<K, V> dict)
    {
        if (dict == null)
            dict = new Dictionary<K, V>();
        else
            dict.Clear();
    }

    private static void Resize<T>(ref List<T>[] list, int size, bool alsoClear = true)
    {
        if (list == null)
            list = new List<T>[size];
        Array.Resize(ref list, size);
        for (int i = 0; i < size; i++)
        {
            if (list[i] == null)
                list[i] = new List<T>();
            else if (alsoClear)
                list[i].Clear();
        }
    }

    public static void Resize<T>(ref List<T> list, int size, bool reset = true, T c = default(T))
    {
        if (list == null)
            list = new List<T>(size);
        if (!reset)
        {
            int cur = list.Count;
            if (size < cur)
                list.RemoveRange(size, cur - size);
            else if (size > cur)
            {
                if (size > list.Capacity)
                    list.Capacity = size;
                list.AddRange(Enumerable.Repeat(c, size - cur));
            }
        }
        else
        {
            list.Clear();
            if (size > list.Capacity)
                list.Capacity = size;
            list.AddRange(Enumerable.Repeat(c, size));
        }
    }

    private static void Set<T>(ref List<T> list, T[] array)
    {
        Reset<T>(ref list);
        list.AddRange(array);
    }
}