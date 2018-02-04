using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

[ExecuteInEditMode]
public class MeshDebugger : EditorWindow
{
    public Mesh m_Mesh;
    public Transform m_Transform;
    public IMGizmos m_Gizmo = new IMGizmos();

    public bool m_Static;
    public bool m_DepthCulling;
    public bool m_EqualizeGizmoSize;

    [Space]
    public bool m_DebugNormalVerts;
    public bool m_DebugTangentVerts;
    public bool m_DebugBinormalVerts;
    public bool m_DebugVertsToIndice;
    public bool m_DebugTrisNormal;
    public float m_RaySize = .4f;

    public enum DebugTriangle { None, Index, Area, Submesh }
    public enum DebugVertice { None, Index, Shared, Duplicates }

    [Space]
    public DebugTriangle m_DebugTris;
    public DebugVertice m_DebugVert;
    public bool m_UseHeatmap;
    public float m_HeatSize = .2f;

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
        m_Transform = Selection.activeTransform;
        if (m_Transform)
        {
            var m = m_Transform.GetComponent<MeshFilter>();
            if (m) m_Mesh = m.sharedMesh;
            else
            {
                var m2 = m_Transform.GetComponent<Graphic>();

            }
        }
        else
        {
            foreach (var item in m_Gizmo.m_Gizmos)
            {
                item.Clear();
            }
            m_Mesh = null;
        }
        Repaint();
    }

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        m_Transform = (Transform)EditorGUILayout.ObjectField("Target", m_Transform, typeof(Transform), true);
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Configuration");
            m_Static = GUILayout.Toggle(m_Static, "Static", EditorStyles.miniButtonLeft);
            m_DepthCulling = GUILayout.Toggle(m_DepthCulling, "Depth Culling", EditorStyles.miniButtonMid);
            m_EqualizeGizmoSize = GUILayout.Toggle(m_EqualizeGizmoSize, "Equalize", EditorStyles.miniButtonRight);
            EditorGUILayout.EndHorizontal();
        }
        m_RaySize = EditorGUILayout.Slider("Ray Size", m_RaySize, 0, 2);
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Vertex Rays");
            m_DebugNormalVerts = GUILayout.Toggle(m_DebugNormalVerts, "Normal", EditorStyles.miniButtonLeft);
            m_DebugTangentVerts = GUILayout.Toggle(m_DebugTangentVerts, "Tangent", EditorStyles.miniButtonMid);
            m_DebugBinormalVerts = GUILayout.Toggle(m_DebugBinormalVerts, "Bitangent", EditorStyles.miniButtonRight);
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Additional Rays");
            m_DebugVertsToIndice = GUILayout.Toggle(m_DebugVertsToIndice, "Verts to Indice", EditorStyles.miniButtonLeft);
            m_DebugTrisNormal = GUILayout.Toggle(m_DebugTrisNormal, "Triangle Normal", EditorStyles.miniButtonRight);
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Use Heatmap");
            m_UseHeatmap = EditorGUILayout.Toggle(m_UseHeatmap);
            EditorGUI.BeginDisabledGroup(!m_UseHeatmap);
            m_HeatSize = EditorGUILayout.Slider(m_HeatSize, 0, 1);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Debug Vertices");
            if (GUILayout.Toggle(m_DebugVert == DebugVertice.None, "None", EditorStyles.miniButtonLeft)) m_DebugVert = DebugVertice.None;
            if (GUILayout.Toggle(m_DebugVert == DebugVertice.Index, "Index", EditorStyles.miniButtonMid)) m_DebugVert = DebugVertice.Index;
            if (GUILayout.Toggle(m_DebugVert == DebugVertice.Shared, "Shared", EditorStyles.miniButtonMid)) m_DebugVert = DebugVertice.Shared;
            if (GUILayout.Toggle(m_DebugVert == DebugVertice.Duplicates, "Duplicates", EditorStyles.miniButtonRight)) m_DebugVert = DebugVertice.Duplicates;
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Debug Triangles");
            if (GUILayout.Toggle(m_DebugTris == DebugTriangle.None, "None", EditorStyles.miniButtonLeft)) m_DebugTris = DebugTriangle.None;
            if (GUILayout.Toggle(m_DebugTris == DebugTriangle.Index, "Index", EditorStyles.miniButtonMid)) m_DebugTris = DebugTriangle.Index;
            if (GUILayout.Toggle(m_DebugTris == DebugTriangle.Area, "Area", EditorStyles.miniButtonMid)) m_DebugTris = DebugTriangle.Area;
            if (GUILayout.Toggle(m_DebugTris == DebugTriangle.Submesh, "Submesh", EditorStyles.miniButtonRight)) m_DebugTris = DebugTriangle.Submesh;
            EditorGUILayout.EndHorizontal();
        }
        if (EditorGUI.EndChangeCheck())
        {
            m_hasUpdated = false;
            SceneView.RepaintAll();
        }
    }

    void OnSceneGUI(SceneView view)
    {
        if (Event.current.type != EventType.Repaint)
            return;


        if (!m_Mesh || !m_Mesh.isReadable)
        {
            if (m_Gizmo != null)
                m_Gizmo.Clear();
            return;
        }

        m_sceneCam = view.camera.transform;
        m_sceneCamPos = m_sceneCam.position;

        m_cpu.m_Mesh = m_Mesh;

        if (!m_Static || !m_cpu.hasUpdated)
            m_cpu.Update();
        else if (m_hasUpdated)
        {
            m_Gizmo.Render();
            return;
        }

        m_Gizmo.Init(m_Transform, m_sceneCam, m_DepthCulling, m_EqualizeGizmoSize);

        Handles.matrix = m_matrix = m_Transform.localToWorldMatrix;

        if (m_DebugNormalVerts || m_DebugTangentVerts || m_DebugBinormalVerts || m_DebugVertsToIndice)
        {
            Color blue = Color.blue, green = Color.green, red = Color.red, yellow = Color.yellow;
            for (int i = 0; i < m_cpu.m_VertCount; i++)
            {
                var vert = m_cpu.m_Verts[i];
                if (m_DebugNormalVerts)
                    m_Gizmo.AddRay(vert, m_cpu.m_Normals[0][i] * m_RaySize, blue);
                if (m_DebugTangentVerts)
                    m_Gizmo.AddRay(vert, m_cpu.m_Normals[1][i] * m_RaySize, green);
                if (m_DebugBinormalVerts)
                    m_Gizmo.AddRay(vert, m_cpu.m_Normals[2][i] * m_RaySize, red);
                if (m_DebugVertsToIndice)
                    m_Gizmo.AddLine(vert, vert + m_cpu.m_VertToIndicesDir[i], yellow);
            }
        }

        if (m_DebugTrisNormal)
        {
            for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
            {
                var norms = m_cpu.m_IndiceNormals[i];
                var medians = m_cpu.m_IndiceMedians[i];
                for (int j = 0; j < medians.Count; j++)
                {
                    var vert = medians[j];
                    m_Gizmo.AddRay(vert, norms[j] * m_RaySize, Color.yellow);
                }
            }
        }

        if (m_UseHeatmap)
        {
            float factor;
            switch (m_DebugTris)
            {
                case DebugTriangle.Index:
                    factor = 1f / m_cpu.m_IndiceCountNormalized;
                    for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
                    {
                        var medians = m_cpu.m_IndiceMedians[i];
                        var offset = m_cpu.m_IndiceOffsets[i];
                        for (int j = 0; j < medians.Count; j++)
                            m_Gizmo.AddQuad(medians[j], m_HeatSize, (j + offset) * factor);
                    }
                    break;
                case DebugTriangle.Area:
                    factor = 1f / m_cpu.m_IndiceAreaMax;
                    for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
                    {
                        var medians = m_cpu.m_IndiceMedians[i];
                        var area = m_cpu.m_IndiceAreas[i];
                        for (int j = 0; j < medians.Count; j++)
                            m_Gizmo.AddQuad(medians[j], m_HeatSize, area[j] * factor);
                    }
                    break;
                case DebugTriangle.Submesh:
                    factor = 1f / m_cpu.m_MeshSubmeshCount;
                    for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
                    {
                        var medians = m_cpu.m_IndiceMedians[i];
                        for (int j = 0; j < medians.Count; j++)
                            m_Gizmo.AddQuad(medians[j], m_HeatSize, (i) * factor);
                    }
                    break;
            }

            switch (m_DebugVert)
            {
                case DebugVertice.Index:
                    factor = 1f / m_cpu.m_VertCount;
                    for (int i = 0; i < m_cpu.m_VertCount; i++)
                        m_Gizmo.AddQuad(m_cpu.m_Verts[i], m_HeatSize, (i) * factor);
                    break;
                case DebugVertice.Shared:
                    factor = 1f / m_cpu.m_VertUsedCountMax;
                    for (int i = 0; i < m_cpu.m_VertCount; i++)
                        m_Gizmo.AddQuad(m_cpu.m_Verts[i], m_HeatSize, m_cpu.m_VertUsedCounts[i] * factor);
                    break;
                case DebugVertice.Duplicates:
                    factor = 1f / m_cpu.m_VertSimilarsMax;
                    foreach (var item in m_cpu.m_VertSimilars)
                        m_Gizmo.AddQuad(item.Key, m_HeatSize, item.Value * factor);
                    break;
            }
        }
        else if (m_cpu.m_IndiceCount < 10000 && m_cpu.m_VertCount < 5000)
        {
           // IMGUI is always slow. Better safe than sorry
             
            Handles.BeginGUI();
            switch (m_DebugTris)
            {
                case DebugTriangle.Index:
                    for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
                    {
                        var norms = m_cpu.m_IndiceNormals[i];
                        var medians = m_cpu.m_IndiceMedians[i];
                        var offset = m_cpu.m_IndiceOffsets[i];
                        for (int j = 0; j < medians.Count; j++)
                            DrawLabel(medians[j], norms[j], (j + offset).ToString());
                    }
                    break;
                case DebugTriangle.Area:
                    for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
                    {
                        var medians = m_cpu.m_IndiceMedians[i];
                        var area = m_cpu.m_IndiceAreas[i];
                        var norms = m_cpu.m_IndiceNormals[i];
                        for (int j = 0; j < medians.Count; j++)
                            DrawLabel(medians[j], norms[j], area[j].ToString("0.0"));
                    }
                    break;
                case DebugTriangle.Submesh:
                    for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
                    {
                        var medians = m_cpu.m_IndiceMedians[i];
                        var norms = m_cpu.m_IndiceNormals[i];
                        for (int j = 0; j < medians.Count; j++)
                            DrawLabel(medians[j], norms[j], i.ToString());
                    }
                    break;
            }

            switch (m_DebugVert)
            {
                case DebugVertice.Index:
                    for (int i = 0; i < m_cpu.m_VertCount; i++)
                        DrawLabel(m_cpu.m_Verts[i], m_cpu.m_Normals[0][i], i.ToString());
                    break;
                case DebugVertice.Shared:
                    for (int i = 0; i < m_cpu.m_VertCount; i++)
                        DrawLabel(m_cpu.m_Verts[i], m_cpu.m_Normals[0][i], m_cpu.m_VertUsedCounts[i].ToString());
                    break;
                case DebugVertice.Duplicates:
                    foreach (var item in m_cpu.m_VertSimilars)
                        DrawLabel(item.Key, item.Key, item.Value.ToString());
                    break;
                default:
                    break;
            }
            Handles.EndGUI();
        }

        m_Gizmo.End();

        m_Gizmo.Render();

        Handles.matrix = Matrix4x4.identity;

        m_hasUpdated = true;
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