using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteInEditMode]
public class MeshInspector : MonoBehaviour
{
    public Mesh m_Mesh;
    public IMGizmos m_Gizmo;

    public bool m_static;
    public bool m_DepthCulling;
    public float m_VertsOffset;
    public bool m_EqualizeGizmoSize;

    [Space]
    public bool m_DebugNormalVerts;
    public bool m_DebugTangentVerts;
    public bool m_DebugBinormalVerts;
    public bool m_DebugVertsToIndice;
    public bool m_DebugTriangleNormal;
    public float m_RaySize = .4f;

    [Space]
    public bool m_DebugTriangleOrder;
    public bool m_DebugTriangleArea;
    public bool m_DebugTriangleSubmesh;
    public bool m_UseHeatmap;
    public float m_HeatSize = .2f;

    [Space]
    public bool m_DebugVertIndex;
    public bool m_DebugVertUsed;
    public bool m_DebugVertDuplis;

    [Space]
    private Transform m_sceneCam;
    private Vector3 m_sceneCamPos;
    private Matrix4x4 m_matrix;
    private MeshInfo m_cpu = new MeshInfo();

    private bool m_hasUpdated = false;

    void OnEnable() { m_hasUpdated = false; }

    void OnDisable()
    {
        foreach (var item in m_Gizmo.m_Gizmos)
        {
            item.Clear();
        }
    }

    void OnDestroy() { m_Gizmo.Dispose(); }

    // Update is called once per frame
    private void OnDrawGizmosSelected()
    {
        if (!enabled)
            return;

        m_sceneCam = SceneView.lastActiveSceneView.camera.transform;
        m_sceneCamPos = m_sceneCam.position;

        if (!m_Mesh)
        {
            m_Mesh = GetComponent<MeshFilter>().sharedMesh;
            if (!m_Mesh)
                return;
        }
        if (m_Gizmo == null)
            m_Gizmo = new IMGizmos();

        m_cpu.m_Mesh = m_Mesh;
        if (!m_cpu.hasUpdated || !m_static)
            m_cpu.Update();
        else if (m_hasUpdated)
            return;

        m_Gizmo.Init(transform, m_DepthCulling, m_EqualizeGizmoSize);

        Handles.matrix = m_matrix = transform.localToWorldMatrix;

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

        if (m_DebugTriangleNormal)
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
            if (m_DebugTriangleOrder)
            {
                var factor = 1f / m_cpu.m_IndiceCountNormalized;
                for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
                {
                    var medians = m_cpu.m_IndiceMedians[i];
                    var offset = m_cpu.m_IndiceOffsets[i];
                    for (int j = 0; j < medians.Count; j++)
                    {
                        m_Gizmo.AddQuad(medians[j], m_HeatSize, (j + offset) * factor);
                    }
                }
            }
            else if (m_DebugTriangleArea)
            {
                var factor = 1f / m_cpu.m_IndiceAreaMax;
                for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
                {
                    var medians = m_cpu.m_IndiceMedians[i];
                    var area = m_cpu.m_IndiceAreas[i];
                    for (int j = 0; j < medians.Count; j++)
                    {
                        m_Gizmo.AddQuad(medians[j], m_HeatSize, area[j] * factor);
                    }
                }
            }
            else if (m_DebugTriangleSubmesh)
            {
                var factor = 1f / m_cpu.m_MeshSubmeshCount;
                for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
                {
                    var medians = m_cpu.m_IndiceMedians[i];
                    for (int j = 0; j < medians.Count; j++)
                    {
                        m_Gizmo.AddQuad(medians[j], m_HeatSize, (i) * factor);
                    }
                }
            }

            if (m_DebugVertIndex)
            {
                var factor = 1f / m_cpu.m_VertCount;
                for (int i = 0; i < m_cpu.m_VertCount; i++)
                {
                    m_Gizmo.AddQuad(m_cpu.m_Verts[i], m_HeatSize, (i) * factor);
                }
            }
            else if (m_DebugVertUsed)
            {
                var factor = 1f / m_cpu.m_VertUsedCountMax;
                for (int i = 0; i < m_cpu.m_VertCount; i++)
                {
                    m_Gizmo.AddQuad(m_cpu.m_Verts[i], m_HeatSize, m_cpu.m_VertUsedCounts[i] * factor);
                }
            }
            else if (m_DebugVertDuplis)
            {
                var factor = 1f / m_cpu.m_VertSimilarsMax;
                foreach (var item in m_cpu.m_VertSimilars)
                {
                    m_Gizmo.AddQuad(item.Key, m_HeatSize, item.Value * factor);
                }
            }
        }
        else
        {
            Handles.BeginGUI();
            if (m_DebugTriangleOrder)
            {
                for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
                {
                    var norms = m_cpu.m_IndiceNormals[i];
                    var medians = m_cpu.m_IndiceMedians[i];
                    var offset = m_cpu.m_IndiceOffsets[i];
                    for (int j = 0; j < medians.Count; j++)
                    {
                        DrawLabel(medians[j], norms[j], (j + offset).ToString());
                    }
                }
            }
            else if (m_DebugTriangleArea)
            {
                for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
                {
                    var medians = m_cpu.m_IndiceMedians[i];
                    var area = m_cpu.m_IndiceAreas[i];
                    var norms = m_cpu.m_IndiceNormals[i];
                    for (int j = 0; j < medians.Count; j++)
                    {
                        DrawLabel(medians[j], norms[j], area[j].ToString("0.0"));
                    }
                }
            }
            else if (m_DebugTriangleSubmesh)
            {
                for (int i = 0; i < m_cpu.m_MeshSubmeshCount; i++)
                {
                    var medians = m_cpu.m_IndiceMedians[i];
                    var norms = m_cpu.m_IndiceNormals[i];
                    for (int j = 0; j < medians.Count; j++)
                    {
                        DrawLabel(medians[j], norms[j], i.ToString());
                    }
                }
            }


            if (m_DebugVertIndex)
            {
                for (int i = 0; i < m_cpu.m_VertCount; i++)
                {
                    DrawLabel(m_cpu.m_Verts[i], m_cpu.m_Normals[0][i], i.ToString());
                }
            }
            else if (m_DebugVertUsed)
            {
                for (int i = 0; i < m_cpu.m_VertCount; i++)
                {
                    DrawLabel(m_cpu.m_Verts[i], m_cpu.m_Normals[0][i], m_cpu.m_VertUsedCounts[i].ToString());
                }
            }
            else if (m_DebugVertDuplis)
            {
                foreach (var item in m_cpu.m_VertSimilars)
                {
                    DrawLabel(item.Key, item.Key, item.Value.ToString());
                }
            }
            Handles.EndGUI();
        }

        m_Gizmo.End();

        Handles.matrix = Matrix4x4.identity;

        m_hasUpdated = true;
    }

    private void OnValidate()
    {
        m_hasUpdated = false;
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