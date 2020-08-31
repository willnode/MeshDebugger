using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[ExecuteInEditMode]
public partial class MeshDebugger : EditorWindow, IHasCustomMenu
{
    public Mesh m_Mesh;
    public Transform m_Transform;
    public IMGizmos m_Gizmo;

    public bool m_Static = true;
    public bool m_DepthCulling;
    public bool m_EqualizeGizmoSize;
    public bool m_PartialDebug;
    public float m_PartialDebugStart = 0;
    public float m_PartialDebugEnd = 1;

    [Space]
    public bool m_DebugNormalVerts;
    public bool m_DebugTangentVerts;
    public bool m_DebugBinormalVerts;
    public bool m_DebugVertsToIndice;
    public bool m_DebugTrisNormal;
    public float m_RaySize = .2f;

    public enum DebugTriangle { None, Index, Area, Submesh }
    public enum DebugVertice { None, Index, Shared, Duplicates }
    public enum DebugSurface { None, Color, Facing, UV, Tangents }
    public enum DebugSurfaceUV { UV = 1, UV2 = 2, UV3 = 3, UV4 = 4 }
    public enum DebugSurfaceTangents { Normal = 1, Tangent = 2, Bitangent = 3, WorldNormal = 4, WorldTangent = 5, WorldBitangent = 6 }

    [Space]
    public DebugTriangle m_DebugTris;
    public DebugVertice m_DebugVert;
    public DebugSurface m_DebugSurface;
    public DebugSurfaceUV m_DebugSurfaceUV = DebugSurfaceUV.UV;
    public DebugSurfaceTangents m_DebugSurfaceTangents = DebugSurfaceTangents.Normal;
    public bool m_UseHeatmap;
    public float m_HeatSize = .1f;

    [Space]
    private Transform m_sceneCam;
    private Vector3 m_sceneCamPos;
    private Matrix4x4 m_matrix;
    private MeshInfo m_cpu = new MeshInfo();
    private Mesh m_tempMesh;
    private Material m_tempMat;

    private bool m_hasUpdated = false;

    [MenuItem("Window/Mesh Debugger")]
    public static void ShowUp()
    {
        var g = GetWindow<MeshDebugger>();
        g.titleContent = new GUIContent("Mesh Debugger");
        g.Show();
    }

    void OnEnable()
    {
#if UNITY_2018 || UNITY_2017 || UNITY_5
        SceneView.onSceneGUIDelegate += OnSceneGUI;
#else
        SceneView.duringSceneGui += OnSceneGUI;
#endif
        Selection.selectionChanged += OnSelectionChange;
        m_hasUpdated = false;

        if (!m_Gizmo)
            m_Gizmo = CreateInstance<IMGizmos>();
    }

    void OnDisable()
    {
#if UNITY_2018 || UNITY_2017 || UNITY_5
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
#else
        SceneView.duringSceneGui -= OnSceneGUI;
#endif
        Selection.selectionChanged -= OnSelectionChange;

        foreach (var item in m_Gizmo.m_Gizmos)
        {
            item.Clear();
        }
    }

    void OnDestroy()
    {
        m_Gizmo.Dispose();
        if (m_tempMesh)
            DestroyImmediate(m_tempMesh);
        if (m_tempMat)
            DestroyImmediate(m_tempMat);
    }

    Material[] m_backupMats;
    bool m_matModificationBreaksPrefab = false;

    void ChangeMaterial(Material mat)
    {
        MeshRenderer r;
        if (m_Transform && (r = m_Transform.GetComponent<MeshRenderer>()))
        {
            if (mat)
            {
                if (m_backupMats == null || m_backupMats.Length == 0)
                {
                    m_backupMats = r.sharedMaterials;
                    m_matModificationBreaksPrefab = (PrefabUtility.GetPrefabType(r) > PrefabType.ModelPrefab
                         && PrefabUtility.GetPropertyModifications(r).FirstOrDefault(x => x.propertyPath.StartsWith("m_Materials")) == null);
                    r.sharedMaterials = Enumerable.Repeat(mat, m_backupMats.Length).ToArray();
                }
                mat.SetInt(Styles.UV_Mode, (int)m_DebugSurfaceUV);
                mat.SetInt(Styles.Tan_Mode, (int)m_DebugSurfaceTangents);
            }
            else if (m_backupMats != null && m_backupMats.Length > 0)
            {
                if (m_matModificationBreaksPrefab)
                {
                    var modifs = PrefabUtility.GetPropertyModifications(r);
                    modifs = modifs.Where(x => !x.propertyPath.StartsWith("m_Materials")).ToArray();
                    PrefabUtility.SetPropertyModifications(r, modifs);
                }
                else
                    r.sharedMaterials = m_backupMats;
                m_backupMats = null;
            }
        }
    }

    void RestoreDefault()
    {
        if (m_Transform.GetComponent<MeshDebuggerProxyUI>())
        {
            DestroyImmediate(m_Transform.GetComponent<MeshDebuggerProxyUI>());
        }
        if (m_backupMats != null && m_backupMats.Length > 0)
        {
            ChangeMaterial(null);
        }
    }

    void OnSelectionChange()
    {
        if (m_Transform)
            RestoreDefault();

        m_Transform = Selection.activeTransform;
        if (m_Transform)
        {
            var m = m_Transform.GetComponent<MeshFilter>();
            var m2 = m_Transform.GetComponent<Graphic>();
            var m3 = m_Transform.GetComponent<SkinnedMeshRenderer>();
            if (m)
            {
                m_Mesh = m.sharedMesh;
                if (m_DebugSurface != DebugSurface.None)
                    ChangeMaterial(m_tempMat);
            }
            else if (m2)
            {
                var m4 = m_Transform.gameObject.AddComponent<MeshDebuggerProxyUI>();
                m_Mesh = null;
                m4.callback += () =>
                {
                    m_Mesh = m4.mesh;
                    m_hasUpdated = false;
                    Repaint();
                };
            }
            else if (m3 && m3.sharedMesh)
            {
                if (!m_tempMesh)
                {
                    m_tempMesh = new Mesh();
                    m_tempMesh.hideFlags = HideFlags.HideAndDontSave;
                }
                else
                {
                    m_tempMesh.Clear();
                }

                m3.BakeMesh(m_Mesh = m_tempMesh);
                m_Mesh.name = m3.sharedMesh.name + " (Snapshot)";
            }
            else
                m_Mesh = null;
        }
        else
        {
            m_Mesh = null;
        }
        Repaint();
    }

    [ContextMenu("Show Help")]
    void ShowHelp()
    {
        Application.OpenURL("https://github.com/willnode/MeshDebugger/blob/master/INSTRUCTIONS.md");
    }


    public virtual void AddItemsToMenu(GenericMenu menu)
    {
        GUIContent content = new GUIContent("Show Help");
        menu.AddItem(content, false, this.ShowHelp);
    }

    void OnSceneGUI(SceneView view)
    {
        if (Event.current.type != EventType.Repaint)
            return;

        if (!m_Transform || !m_Mesh || !m_Mesh.isReadable)
        {
            if (m_Gizmo != null)
                m_Gizmo.Clear();
            m_cpu.m_lastMeshId = -1;
            return;
        }

        m_sceneCam = view.camera.transform;
        m_sceneCamPos = m_sceneCam.position;

        m_cpu.m_Mesh = m_Mesh;

        if (!m_Static || !m_cpu.hasUpdated)
            m_cpu.Update();
        else if (m_hasUpdated)
        {
            m_Gizmo.UpdateGO(m_Transform);
            m_Gizmo.Render();
            if (!m_UseHeatmap && IsSafeToDrawGUI())
                DrawGUILabels();
            return;
        }

        m_Gizmo.Init(m_Transform, m_sceneCam, m_DepthCulling, m_EqualizeGizmoSize && !m_Static);

        if (m_DebugNormalVerts || m_DebugTangentVerts || m_DebugBinormalVerts || m_DebugVertsToIndice)
        {
            Color blue = Color.blue, green = Color.green, red = Color.red, cyan = Color.cyan;
            EachVert((i, vert) =>
            {
                if (m_DebugNormalVerts && m_cpu.m_NormalChannels >= 1)
                    m_Gizmo.AddRay(vert, m_cpu.m_Normals[0][i] * m_RaySize, blue);
                if (m_DebugTangentVerts && m_cpu.m_NormalChannels >= 2)
                    m_Gizmo.AddRay(vert, m_cpu.m_Normals[1][i] * m_RaySize, green);
                if (m_DebugBinormalVerts && m_cpu.m_NormalChannels >= 3)
                    m_Gizmo.AddRay(vert, m_cpu.m_Normals[2][i] * m_RaySize, red);
                if (m_DebugVertsToIndice)
                {
                    var refs = m_cpu.m_VertToIndicesDir[i];
                    if (refs != null)
                        for (int j = 0; j < refs.Count; j++)
                        {
                            int submesh, localidx;
                            m_cpu.UnpackTriangleIdx(refs[j], out submesh, out localidx);
                            localidx /= MeshInfo.m_TopologyDivision[m_cpu.m_IndiceTypes[submesh]];
                            m_Gizmo.AddLine(vert, m_cpu.m_IndiceMedians[submesh][localidx], cyan);
                        }
                }
            });
        }

        if (m_DebugTrisNormal)
        {
            var color = Color.yellow;
            var norms = m_cpu.m_IndiceNormals;
            EachIndice((i, j, median) =>
                m_Gizmo.AddRay(median, norms[i][j] * m_RaySize, color)
            );
        }

        if (m_UseHeatmap)
        {
            float factor;
            switch (m_DebugTris)
            {
                case DebugTriangle.Index:
                    factor = 1f / m_cpu.m_IndiceCountNormalized;
                    EachIndice((i, j, median) =>
                        m_Gizmo.AddQuad(median, m_HeatSize, (j + m_cpu.m_IndiceOffsets[i]) * factor)
                    );
                    break;
                case DebugTriangle.Area:
                    factor = 1f / m_cpu.m_IndiceAreaMax;
                    EachIndice((i, j, median) =>
                        m_Gizmo.AddQuad(median, m_HeatSize, m_cpu.m_IndiceAreas[i][j] * factor)
                    );
                    break;
                case DebugTriangle.Submesh:
                    factor = 1f / m_cpu.m_MeshSubmeshCount;
                    EachIndice((i, j, median) =>
                         m_Gizmo.AddQuad(median, m_HeatSize, i * factor)
                    );
                    break;
            }

            switch (m_DebugVert)
            {
                case DebugVertice.Index:
                    factor = 1f / m_cpu.m_VertCount;
                    EachVert((i, vert) =>
                        m_Gizmo.AddQuad(vert, m_HeatSize, i * factor)
                    );
                    break;
                case DebugVertice.Shared:
                    factor = 1f / m_cpu.m_VertUsedCountMax;
                    EachVert((i, vert) =>
                        m_Gizmo.AddQuad(vert, m_HeatSize, m_cpu.m_VertUsedCounts[i] * factor)
                    );
                    break;
                case DebugVertice.Duplicates:
                    factor = 1f / m_cpu.m_VertSimilarsMax;
                    foreach (var item in m_cpu.m_VertSimilars)
                        m_Gizmo.AddQuad(item.Key, m_HeatSize, item.Value * factor);
                    break;
            }
        }
        else if (IsSafeToDrawGUI())
        {
            // IMGUI is always slow. Better safe than sorry
            DrawGUILabels();
        }

        m_Gizmo.End();

        m_Gizmo.Render();

        m_hasUpdated = true;
    }

    private bool IsSafeToDrawGUI()
    {
        return ((m_DebugTris == DebugTriangle.None ? 0 : m_cpu.m_IndiceCountNormalized) +
            (m_DebugVert == DebugVertice.None ? 0 : m_cpu.m_VertCount)) *
            (m_PartialDebug ? (m_PartialDebugEnd - m_PartialDebugStart) : 1) < Styles.GUILimit;
    }

    private void DrawGUILabels()
    {
        Handles.matrix = m_matrix = m_Transform.localToWorldMatrix;
        Handles.BeginGUI();
        switch (m_DebugTris)
        {
            case DebugTriangle.Index:
                EachIndice((i, j, vert) =>
                    DrawLabel(vert, m_cpu.m_IndiceNormals[i][j], (j + m_cpu.m_IndiceOffsets[i]))
                );
                break;
            case DebugTriangle.Area:
                EachIndice((i, j, vert) =>
                {
                    var area = m_cpu.m_IndiceAreas[i][j];
                    DrawLabel(vert, m_cpu.m_IndiceNormals[i][j], area.ToString(area < 1 ? "0.00" : "0.0"));
                }
                );
                break;
            case DebugTriangle.Submesh:
                EachIndice((i, j, vert) =>
                        DrawLabel(vert, m_cpu.m_IndiceNormals[i][j], i)
                );
                break;
        }

        switch (m_DebugVert)
        {
            case DebugVertice.Index:
                EachVert((i, vert) =>
                    DrawLabel(vert, m_cpu.m_Normals[0][i], i)
                );
                break;
            case DebugVertice.Shared:
                EachVert((i, vert) =>
                    DrawLabel(vert, m_cpu.m_Normals[0][i], m_cpu.m_VertUsedCounts[i])
                );
                break;
            case DebugVertice.Duplicates:
                foreach (var item in m_cpu.m_VertSimilars)
                    DrawLabel(item.Key, item.Key, item.Value);
                break;
            default:
                break;
        }
        Handles.EndGUI();
        Handles.matrix = Matrix4x4.identity;
    }

    private void EachVert(Action<int, Vector3> gui)
    {
        var count = m_cpu.m_VertCount;
        var start = m_PartialDebug ? (int)(count * m_PartialDebugStart) : 0;
        var end = m_PartialDebug ? (int)(count * m_PartialDebugEnd) : count;
        var verts = m_cpu.m_Verts;
        for (int i = start; i < end; i++)
            gui(i, verts[i]);
    }

    private void EachIndice(Action<int, int, Vector3> gui)
    {
        if (m_PartialDebug)
        {
            var count = m_cpu.m_IndiceCountNormalized;
            var start = (int)(count * m_PartialDebugStart);
            var end = (int)(count * m_PartialDebugEnd);
            for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
            {
                var tris = m_cpu.m_IndiceMedians[i];
                var offset = m_cpu.m_IndiceOffsets[i];
                var offset2 = offset + tris.Count;
                if (start > offset2 || end < offset) continue;
                for (int j = 0; j < tris.Count; j++)
                {
                    var k = offset + j;
                    if (k > start && k < end)
                        gui(i, j, tris[j]);
                }
            }
        }
        else
        {
            for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
            {
                var tris = m_cpu.m_IndiceMedians[i];
                for (int j = 0; j < tris.Count; j++)
                    gui(i, j, tris[j]);
            }

        }
    }

    private bool IsFacingCamera(Vector3 pos, Vector3 normal)
    {
        return new Plane(m_matrix.MultiplyVector(normal), m_matrix.MultiplyPoint3x4(pos)).GetSide(m_sceneCamPos);
    }

    private static GUIContent m_gui = new GUIContent();

    private void DrawLabel(Vector3 pos, Vector3 normal, int number)
    {
        DrawLabel(pos, normal, number < Styles.GUILimit ? Styles.numbers[number] : number.ToString());
    }

    private void DrawLabel(Vector3 pos, Vector3 normal, string text)
    {
        if (!m_DepthCulling || (IsFacingCamera(pos, normal)))
        {
            m_gui.text = text;
            var GUIPos = HandleUtility.WorldPointToSizedRect(pos, m_gui, Styles.blockLabel);
            GUIPos.y -= 7f;
            GUIPos.x -= GUIPos.width / 2;
            GUI.Label(GUIPos, m_gui, Styles.blockLabel);
        }
    }

    public Material lineMaterial;

    static public class Styles
    {
        static public GUIStyle blockLabel = new GUIStyle(EditorStyles.boldLabel);

        static public string[] numbers;

        public const int GUILimit = 2500;

        public static int UV_Mode = Shader.PropertyToID("UV_Mode");

        public static int Tan_Mode = Shader.PropertyToID("Tan_Mode");

        static Styles()
        {
            numbers = new string[GUILimit];
            for (int i = 0; i < numbers.Length; i++)
                numbers[i] = i.ToString();

            blockLabel.normal.textColor = Color.black;
            blockLabel.normal.background = EditorGUIUtility.whiteTexture;
            blockLabel.margin = new RectOffset();//2, 2, 1, 1);
            blockLabel.padding = new RectOffset();
            blockLabel.alignment = TextAnchor.MiddleCenter;
        }
    }


}
