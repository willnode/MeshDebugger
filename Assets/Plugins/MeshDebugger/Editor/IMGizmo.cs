using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Lightweight, Fast immediate mesh drawing. Works in runtime actually!
public class IMGizmo : ScriptableObject
{
    private void OnEnable()
    {
        hideFlags = HideFlags.DontSave;
    }

    private void OnDisable()
    {
        Clear();
    }

    private void OnDestroy()
    {
        if (m_Mesh)
        {
            DestroyImmediate(m_Mesh);
            DestroyImmediate(m_LineMaterial);
            DestroyImmediate(m_QuadMaterial);
        }
    }

    public Transform m_Camera;
    public Mesh m_Mesh;
    public Material m_LineMaterial;
    public Material m_QuadMaterial;
    public Matrix4x4 m_Matrix;
    public bool m_EqualSize;
    public bool m_Active = true;

    public List<Vector3> m_Vertices = new List<Vector3>(16);
    public List<Color> m_Color = new List<Color>(16);
    public List<int> m_Lines = new List<int>(16);
    public List<int> m_Quads = new List<int>(16);
    public List<Vector2> m_UV = new List<Vector2>(16);

    public void AddLine(Vector3 start, Vector3 end, Color color)
    {
        m_Vertices.Add(start);
        m_Vertices.Add(end);
        var m = m_Vertices.Count;
        m_Lines.Add(m - 2);
        m_Lines.Add(m - 1);
        m_Color.Add(color);
        m_Color.Add(color);
        m_UV.Add(default(Vector2));
        m_UV.Add(default(Vector2));
    }

    public void AddRay(Vector3 pos, Vector3 dir, Color color)
    {
        if (m_EqualSize)
            dir *= GetHandleSize(pos);
        AddLine(pos, pos + dir, color);
    }

    public void AddQuad(Vector3 pos, Vector2 size, Color color)
    {
        m_Vertices.Add(pos);
        m_Vertices.Add(pos);
        m_Vertices.Add(pos);
        m_Vertices.Add(pos);
        var m = m_Vertices.Count;
        m_Quads.Add(m - 4);
        m_Quads.Add(m - 3);
        m_Quads.Add(m - 2);
        m_Quads.Add(m - 1);
        m_Color.Add(color);
        m_Color.Add(color);
        m_Color.Add(color);
        m_Color.Add(color);
        m_UV.Add(new Vector2(-size.x, -size.y));
        m_UV.Add(new Vector2(size.x, -size.y));
        m_UV.Add(new Vector2(size.x, size.y));
        m_UV.Add(new Vector2(-size.x, size.y));
    }

    public void AddQuad(Vector3 pos, float size, Color color)
    {
        if (m_EqualSize)
            size *= GetHandleSize(pos);
        AddQuad(pos, Vector2.one * size, color);
    }

    public void AddQuad(Vector3 pos, float size, float colorFactor)
    {
        if (m_EqualSize)
            size *= GetHandleSize(pos);
        AddQuad(pos, Vector2.one * size, HSVToRGB(colorFactor));
    }

    private float GetHandleSize(Vector3 position)
    {
        Camera current = Camera.current;
        float result;
        if (current)
        {
            position = m_Matrix.MultiplyPoint3x4(position);
            Transform transform = m_Camera;
            Vector3 position2 = transform.position;
            float z = Vector3.Dot(position - position2, transform.TransformDirection(new Vector3(0f, 0f, 1f)));
            Vector3 a = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(0f, 0f, z)));
            Vector3 b = current.WorldToScreenPoint(position2 + transform.TransformDirection(new Vector3(1f, 0f, z)));
            float magnitude = (a - b).magnitude;
            result = 80f / Mathf.Max(magnitude, 0.0001f) * UnityEditor.EditorGUIUtility.pixelsPerPoint;
        }
        else
        {
            result = 20f;
        }
        return result;
    }

    // Modified HSVToRGB with SV always 1 and H ranges from 0-1 to 0-0.83 (Red to Magenta)
    public static Color HSVToRGB(float H)
    {
        Color white = Color.white;

        float num = H * 5f;
        float num2 = Mathf.Floor(num);
        float num3 = num - num2;
        float num5 = 1f - num3;
        float num6 = num3;
        switch ((int)num2 + 1)
        {
            case 0:
                white.r = 1;
                white.g = 0;
                white.b = num5;
                break;

            case 1:
                white.r = 1;
                white.g = num6;
                white.b = 0;
                break;

            case 2:
                white.r = num5;
                white.g = 1;
                white.b = 0;
                break;

            case 3:
                white.r = 0;
                white.g = 1;
                white.b = num6;
                break;

            case 4:
                white.r = 0;
                white.g = num5;
                white.b = 1;
                break;

            case 5:
                white.r = num6;
                white.g = 0;
                white.b = 1;
                break;

            case 6:
                white.r = 1;
                white.g = 0;
                white.b = num5;
                break;

            case 7:
                white.r = 1;
                white.g = num6;
                white.b = 0;
                break;
        }
        return white;
    }

    public void Init(Transform transform, Transform camera, bool depth, bool equalSize)
    {
        if (!m_Mesh)
        {
            m_Mesh = new Mesh();
            m_Mesh.hideFlags = HideFlags.HideAndDontSave;
        }

        InitMaterial(depth);

        m_Vertices.Clear();
        m_Color.Clear();
        m_Lines.Clear();
        m_Quads.Clear();
        m_UV.Clear();

        m_Matrix = transform.localToWorldMatrix;
        m_Camera = camera;
        m_EqualSize = equalSize;
    }

    public void UpdateGO(Transform transform)
    {
        m_Matrix = transform.localToWorldMatrix;
    }

    private void InitMaterial(bool writeDepth)
    {
        if (!m_LineMaterial)
        {
            var shader = Shader.Find("Hidden/InternalLineColorful");
            m_LineMaterial = new Material(shader);
            m_LineMaterial.hideFlags = HideFlags.DontSave;
            var shader2 = Shader.Find("Hidden/InternalQuadColorful");
            m_QuadMaterial = new Material(shader2);
            m_QuadMaterial.hideFlags = HideFlags.DontSave;
        }
        // Set external depth on/off
        m_LineMaterial.SetInt("_ZTest", writeDepth ? 4 : 0);
        m_QuadMaterial.SetInt("_ZTest", writeDepth ? 4 : 0);
    }

    public void End()
    {
        m_Mesh.Clear();

        m_Mesh.SetVertices(m_Vertices);
        m_Mesh.SetColors(m_Color);
        m_Mesh.SetUVs(0, m_UV);
        m_Mesh.subMeshCount = 2;
#if UNITY_2019_2 || UNITY_2019_1 || UNITY_2018 || UNITY_2017 || UNITY_5
        InternalMeshUtil.SetIndices(m_Mesh, m_Lines, MeshTopology.Lines, 0, false);
        InternalMeshUtil.SetIndices(m_Mesh, m_Quads, MeshTopology.Quads, 1, false);
#else
        m_Mesh.SetIndices(m_Lines, MeshTopology.Lines, 0, false);
        m_Mesh.SetIndices(m_Quads, MeshTopology.Quads, 1, false);
#endif
        m_Mesh.RecalculateBounds();
    }

    public void Clear()
    {
        m_Mesh.Clear();
        m_Vertices.Clear();
        m_Color.Clear();
        m_Lines.Clear();
        m_Quads.Clear();
        m_UV.Clear();
    }

    public void Render()
    {
        m_LineMaterial.SetPass(0);
        Graphics.DrawMeshNow(m_Mesh, m_Matrix, 0);
        m_QuadMaterial.SetPass(0);
        Graphics.DrawMeshNow(m_Mesh, m_Matrix, 1);
    }
}