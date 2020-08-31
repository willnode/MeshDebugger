using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Mesh Analytics Gatherer

public class MeshInfo
{
    public Mesh m_Mesh;
    public int m_lastMeshId = -1;
    public bool m_MeshReadable;
    public Bounds m_MeshBounds;
    public int m_VertCount;
    public int m_IndiceCount;
    public int m_IndiceCountNormalized;
    public int m_MeshSubmeshCount;

    public List<Vector3> m_Verts;
    public List<int> m_VertUsedCounts;
    public Dictionary<Vector3, int> m_VertSimilars;
    public List<List<int>> m_VertToIndicesDir;

    public List<int>[] m_Indices;
    public int[] m_IndiceOffsets;
    public MeshTopology[] m_IndiceTypes;
    public List<Vector3>[] m_IndiceMedians;
    public List<Vector3>[] m_IndiceNormals;
    public List<float>[] m_IndiceAreas;

    public int m_VertSimilarsMax;
    public int m_VertUsedCountMax;
    public int m_VertOrphan;
    public int m_VertDuplicates;
    public float m_IndiceAreaMax;
    public float m_IndiceAreaTotal;
    public int m_IndiceInvalidArea;

    public List<Color> m_Colors;
    public List<Vector4>[] m_UVs;
    public List<BoneWeight> m_BoneWeights;

    // Normal = 0, Tangent = 1, Bitangent = 2
    public List<Vector3>[] m_Normals;

    /// <summary>
    /// 3 = Normal + Tangents, 1 = Normal, 0 = None,
    /// </summary>
    public int m_NormalChannels = 0;

    public List<float> m_NormalFlips;

    public string m_Features;

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
            if (tan.Length > 0)
            {
                for (int i = 0; i < m_Normals[0].Count; i++)
                {
                    m_Normals[1].Add(tan[i]);
                    m_NormalFlips.Add(tan[i].w);
                    m_Normals[2].Add(Vector3.Cross(m_Normals[0][i], m_Normals[1][i]) * m_NormalFlips[i]);
                }
                m_NormalChannels = 3;
            }
            else if (m_Normals[0].Count > 0)
                m_NormalChannels = 1;
            else
                m_NormalChannels = 0;
        }
        {
            m_IndiceAreaMax = 0;
            m_IndiceAreaTotal = 0;
            m_IndiceInvalidArea = 0;
            Resize(ref m_Indices, m_MeshSubmeshCount);
            Resize(ref m_IndiceAreas, m_MeshSubmeshCount);
            Resize(ref m_IndiceMedians, m_MeshSubmeshCount);
            Resize(ref m_IndiceNormals, m_MeshSubmeshCount);
            Array.Resize(ref m_IndiceOffsets, m_MeshSubmeshCount);
            Array.Resize(ref m_IndiceTypes, m_MeshSubmeshCount);
            int iter = 0, iterNormaled = 0;
            for (int i = 0; i < m_MeshSubmeshCount; i++)
            {
                Set(ref m_Indices[i], m_Mesh.GetIndices(i));
                m_IndiceTypes[i] = m_Mesh.GetTopology(i);
                m_IndiceOffsets[i] = iter;
                var steps = m_TopologyDivision[m_IndiceTypes[i]];
                var indice = m_Indices[i];
                var normal = m_Normals[0];
                iter += indice.Count;
                iterNormaled += indice.Count / steps;
                for (int m = 0; m < indice.Count; m += steps)
                {
                    int a, b, c, d;
                    switch (steps)
                    {
                        case 1:
                            a = indice[m];
                            m_IndiceMedians[i].Add(m_Verts[a]);
                            if (m_NormalChannels > 0)
                                m_IndiceNormals[i].Add(normal[a]);
                            m_IndiceAreas[i].Add(0); break;
                        case 2:
                            a = indice[m]; b = indice[m + 1];
                            m_IndiceMedians[i].Add((m_Verts[a] + m_Verts[b]) / 2);
                            if (m_NormalChannels > 0)
                                m_IndiceNormals[i].Add((normal[a] + normal[b]).normalized);
                            m_IndiceAreas[i].Add((m_Verts[a] + m_Verts[b]).magnitude); break;
                        case 3:
                            a = indice[m]; b = indice[m + 1]; c = indice[m + 2];
                            m_IndiceMedians[i].Add((m_Verts[a] + m_Verts[b] + m_Verts[c]) / 3);
                            if (m_NormalChannels > 0)
                                m_IndiceNormals[i].Add((normal[a] + normal[b] + normal[c]).normalized);
                            m_IndiceAreas[i].Add(GetTriArea(m_Verts[a], m_Verts[b], m_Verts[c])); break;
                        case 4:
                            a = indice[m]; b = indice[m + 1]; c = indice[m + 2]; d = indice[m + 3];
                            m_IndiceMedians[i].Add((m_Verts[a] + m_Verts[b] + m_Verts[c] + m_Verts[d]) / 4);
                            if (m_NormalChannels > 0)
                                m_IndiceNormals[i].Add((normal[a] + normal[b] + normal[c] + normal[d]).normalized);
                            m_IndiceAreas[i].Add(GetTriArea(m_Verts[a], m_Verts[b], m_Verts[c]) +
                             GetTriArea(m_Verts[d], m_Verts[b], m_Verts[c])); break;
                    }
                    var area = m_IndiceAreas[i][m_IndiceAreas[i].Count - 1];
                    m_IndiceAreaMax = Mathf.Max(m_IndiceAreaMax, area);
                    m_IndiceAreaTotal += area;
                    if (area < 0e-10f)
                        m_IndiceInvalidArea++;
                }
            }
            m_IndiceCount = iter;
            m_IndiceCountNormalized = iterNormaled;
        }
        {
            m_VertSimilarsMax = 0;
            m_VertUsedCountMax = 0;
            m_VertDuplicates = 0;
            m_VertOrphan = m_Verts.Count;
            Resize(ref m_VertUsedCounts, m_Verts.Count);
            Resize(ref m_VertToIndicesDir, m_Verts.Count);
            Reset(ref m_VertSimilars);
            for (int i = 0; i < m_Verts.Count; i++)
            {
                var v = m_Verts[i];
                if (m_VertSimilars.ContainsKey(v))
                {
                    m_VertSimilars[v]++;
                    m_VertDuplicates++;
                }
                else
                    m_VertSimilars[v] = 1;
                m_VertSimilarsMax = Mathf.Max(m_VertSimilarsMax, m_VertSimilars[v]);
            }
            for (int i = 0; i < m_Indices.Length; i++)
            {
                var indice = m_Indices[i];
                for (int j = 0; j < indice.Count; j++)
                {
                    var idx = indice[j]; // this is index of vertex, btw
                    if (m_VertUsedCounts[idx] == 0)
                    {
                        m_VertOrphan--;
                        m_VertToIndicesDir[idx] = new List<int>();
                    }
                    m_VertToIndicesDir[idx].Add(m_IndiceOffsets[i] + j);
                    m_VertUsedCountMax = Mathf.Max(m_VertUsedCountMax, ++m_VertUsedCounts[idx]);
                }
            }
        }
        {
            m_Features = "Mesh Features:" +
                "\nVertices: " + m_VertCount + " total, " + (m_VertOrphan > 0 ? m_VertOrphan + " orphan, " : "") + (m_VertDuplicates > 0 ? m_VertDuplicates + " duplicates, " : "") +
                "\nIndices: " + m_IndiceCountNormalized + " total, " + m_IndiceCount + " buffer capacity, " + m_IndiceAreaTotal.ToString("0.##") + " unit surface area, " +
                (m_MeshSubmeshCount > 1 ? m_MeshSubmeshCount + " submeshes, " : "") + (m_IndiceInvalidArea > 0 ? m_IndiceInvalidArea + " invalid, " : "") +
                "\nChannels: position, " + (m_NormalChannels >= 1 ? "normals, " + ( m_NormalChannels >= 3 ? "tangents, " : "") : "") + InternalMeshUtil.GetVertexFormat(m_Mesh).Replace(",", ", ") +
                "\nSize: " + m_MeshBounds.size.ToString("0.00");
        }
        m_lastMeshId = m_Mesh.GetInstanceID();
    }

    static public Dictionary<MeshTopology, int> m_TopologyDivision = new Dictionary<MeshTopology, int>(new MeshTopologyComparer())
        {
            {MeshTopology.Lines, 2},
            {MeshTopology.LineStrip, 2},
            {MeshTopology.Points, 1},
            {MeshTopology.Quads, 4},
            {MeshTopology.Triangles, 3},
        };

    // for performance godsake
    public class MeshTopologyComparer : IEqualityComparer<MeshTopology>
    {
        public bool Equals(MeshTopology x, MeshTopology y)
        {
            return x == y;
        }

        public int GetHashCode(MeshTopology obj)
        {
            return (int)obj;
        }
    }

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

    public void UnpackTriangleIdx(int src, out int submesh, out int localidx)
    {
        for (int i = m_MeshSubmeshCount; i-- > 0;)
        {
            if (i > 0 && m_IndiceOffsets[i] > src)
                continue;
            submesh = i;
            localidx = src - m_IndiceOffsets[i];
            return;
        }
        submesh = 0;
        localidx = src;
    }
}

internal static class InternalMeshUtil
{
    public static Func<Mesh, string> GetVertexFormat;

    static InternalMeshUtil()
    {
        var type = typeof(Editor).Assembly.GetTypes().First((x) => x.Name == "InternalMeshUtil");
        GetVertexFormat = (Func<Mesh, string>)Delegate.CreateDelegate(typeof(Func<Mesh, string>), null,
            type.GetMethod("GetVertexFormat", BindingFlags.Static | BindingFlags.Public));
    }

    // https://gist.github.com/willnode/032eb0c73733ffc862bdfec0a8e8af0e

    static Func<object, Array> _ExtractArrayFromList;

    static void CreateExtractDelegate()
    {

#if UNITY_2017_2 || UNITY_2017_1 || UNITY_5 || UNITY_4
        var type = typeof(Mesh);
#else
        var type = typeof(Mesh).Assembly.GetTypes().First(x => x.Name == "NoAllocHelpers");
#endif

        var m = type.GetMethod("ExtractArrayFromList", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
        _ExtractArrayFromList = (Func<object, Array>)Delegate.CreateDelegate(typeof(Func<object, Array>), m);
    }

    /// <summary>
    /// Extract array from list
    /// </summary>
    public static Array ExtractArrayFromList<T>(List<T> list)
    {
        if (_ExtractArrayFromList == null)
            CreateExtractDelegate();
        return _ExtractArrayFromList(list);
    }

    // -----------------------------------------------------

    private delegate void Action<T1, T2, T3, T4, T5, T6>(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f);
    private delegate void Action<T1, T2, T3, T4, T5, T6, T7>(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g);

#if UNITY_2017_2 || UNITY_2017_1 || UNITY_5 || UNITY_4

    static Action<Mesh, int, MeshTopology, Array, int, bool> _SetIndices;

    static void CreateSetIndicesDelegate()
    {
        // See ILSpy for this hidden feature
        var m = typeof(Mesh).GetMethod("SetIndicesImpl", BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(int), typeof(MeshTopology), typeof(Array), typeof(int), typeof(bool) }, null);
        _SetIndices = (Action<Mesh, int, MeshTopology, Array, int, bool>)Delegate.CreateDelegate(typeof(Action<Mesh, int, MeshTopology, Array, int, bool>), null, m);
    }

    /// <summary>
    /// Mesh.SetIndices with generic list variant
    /// </summary>
    public static void SetIndices(Mesh m, List<int> buffer, MeshTopology topology, int submesh, bool recalculate)
    {
        if (_SetIndices == null)
            CreateSetIndicesDelegate();
        _SetIndices(m, submesh, topology, ExtractArrayFromList(buffer), buffer.Count, recalculate);
    }

#elif UNITY_2017 || UNITY_2018 || UNITY_2019_1 || UNITY_2019_2

    static Action<Mesh, int, MeshTopology, Array, int, bool, int> _SetIndices;

    static void CreateSetIndicesDelegate()
    {
        // See ILSpy for this hidden feature
        var m = typeof(Mesh).GetMethod("SetIndicesImpl", BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(int), typeof(MeshTopology), typeof(Array), typeof(int), typeof(bool), typeof(int) }, null);
        _SetIndices = (Action<Mesh, int, MeshTopology, Array, int, bool, int>)Delegate.CreateDelegate(typeof(Action<Mesh, int, MeshTopology, Array, int, bool, int>), null, m);
    }

    /// <summary>
    /// Mesh.SetIndices with generic list variant
    /// </summary>
    public static void SetIndices(Mesh m, List<int> buffer, MeshTopology topology, int submesh, bool recalculate)
    {
        if (_SetIndices == null)
            CreateSetIndicesDelegate();
        _SetIndices(m, submesh, topology, ExtractArrayFromList(buffer), buffer.Count, recalculate, 0);
    }


#endif

}