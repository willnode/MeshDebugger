using System;
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

    [Space]
    public DebugTriangle m_DebugTris;
    public DebugVertice m_DebugVert;
    public bool m_UseHeatmap;
    public float m_HeatSize = .1f;

    [Space]
    private Transform m_sceneCam;
    private Vector3 m_sceneCamPos;
    private Matrix4x4 m_matrix;
    private MeshInfo m_cpu = new MeshInfo();

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
        SceneView.onSceneGUIDelegate += OnSceneGUI;
        Selection.selectionChanged += OnSelectionChange;
        m_hasUpdated = false;

        if (!m_Gizmo)
            m_Gizmo = CreateInstance<IMGizmos>();
    }

    void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= OnSceneGUI;
        Selection.selectionChanged -= OnSelectionChange;

        foreach (var item in m_Gizmo.m_Gizmos)
        {
            item.Clear();
        }
    }

    void OnDestroy() { m_Gizmo.Dispose(); }

    void OnSelectionChange()
    {
        if (m_Transform && m_Transform.GetComponent<MeshDebuggerProxyUI>())
        {
            DestroyImmediate(m_Transform.GetComponent<MeshDebuggerProxyUI>());
        }

        m_Transform = Selection.activeTransform;
        if (m_Transform)
        {
            var m = m_Transform.GetComponent<MeshFilter>();
            var m2 = m_Transform.GetComponent<Graphic>();
            if (m) m_Mesh = m.sharedMesh;
            else if (m2)
            {
                var m3 = m_Transform.gameObject.AddComponent<MeshDebuggerProxyUI>();
                m_Mesh = null;
                m3.callback += () =>
                {
                    m_Mesh = m3.mesh;
                    m_hasUpdated = false;
                };
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

        if (!m_Mesh || !m_Mesh.isReadable)
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
                if (m_DebugNormalVerts)
                    m_Gizmo.AddRay(vert, m_cpu.m_Normals[0][i] * m_RaySize, blue);
                if (m_DebugTangentVerts)
                    m_Gizmo.AddRay(vert, m_cpu.m_Normals[1][i] * m_RaySize, green);
                if (m_DebugBinormalVerts)
                    m_Gizmo.AddRay(vert, m_cpu.m_Normals[2][i] * m_RaySize, red);
                if (m_DebugVertsToIndice)
                    m_Gizmo.AddLine(vert, vert + m_cpu.m_VertToIndicesDir[i], cyan);
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
            (m_PartialDebug ? (m_PartialDebugEnd - m_PartialDebugStart) : 1) < 2500;
    }

    private void DrawGUILabels()
    {
        Handles.matrix = m_matrix = m_Transform.localToWorldMatrix;
        Handles.BeginGUI();
        switch (m_DebugTris)
        {
            case DebugTriangle.Index:
                EachIndice((i, j, vert) =>
                    DrawLabel(vert, m_cpu.m_IndiceNormals[i][j], (j + m_cpu.m_IndiceOffsets[i]).ToString())
                );
                break;
            case DebugTriangle.Area:
                EachIndice((i, j, vert) =>
                     DrawLabel(vert, m_cpu.m_IndiceNormals[i][j], m_cpu.m_IndiceAreas[i][j].ToString("0.0"))
                );
                break;
            case DebugTriangle.Submesh:
                EachIndice((i, j, vert) =>
                        DrawLabel(vert, m_cpu.m_IndiceNormals[i][j], i.ToString("0.0"))
                );
                break;
        }

        switch (m_DebugVert)
        {
            case DebugVertice.Index:
                EachVert((i, vert) =>
                    DrawLabel(vert, m_cpu.m_Normals[0][i], i.ToString())
                );
                break;
            case DebugVertice.Shared:
                EachVert((i, vert) =>
                    DrawLabel(vert, m_cpu.m_Normals[0][i], m_cpu.m_VertUsedCounts[i].ToString())
                );
                break;
            case DebugVertice.Duplicates:
                foreach (var item in m_cpu.m_VertSimilars)
                    DrawLabel(item.Key, item.Key, item.Value.ToString());
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

    private void DrawLabel(Vector3 pos, Vector3 normal, string text)
    {
        if (!m_DepthCulling || (IsFacingCamera(pos, normal)))
        {
            m_gui.text = text;
            var GUIPos = HandleUtility.WorldPointToSizedRect(pos, m_gui, Styles.blockLabel);
            GUIPos.y -= 7f;
            GUI.Label(GUIPos, m_gui, Styles.blockLabel);
        }
    }

    public Material lineMaterial;

    static public class Styles
    {
        static public GUIStyle blockLabel = new GUIStyle(EditorStyles.boldLabel);

        static Styles()
        {
            blockLabel.normal.background = EditorGUIUtility.whiteTexture;
            blockLabel.margin = new RectOffset();//2, 2, 1, 1);
            blockLabel.padding = new RectOffset();
            blockLabel.alignment = TextAnchor.MiddleCenter;
        }
    }


}
