
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class IMGizmo : ScriptableObject {
    
    
    void OnEnable () {
        hideFlags = HideFlags.DontSave;
        SceneView.onSceneGUIDelegate += OnRenderScene;
    }
    
    void OnDisable () {
        SceneView.onSceneGUIDelegate -= OnRenderScene;
    }

    public Transform m_Camera;
    public Mesh m_Mesh;
    public Material m_Material;
    public Matrix4x4 m_Matrix;
    public bool m_EqualSize;
    public bool m_Active = true;
    public int m_Layer = 0;

    public List<Vector3> m_Vertices = new List<Vector3>(16);
    public List<Color> m_Color = new List<Color>(16);
    public List<int> m_Lines = new List<int>(16);
    public List<int> m_Quads = new List<int>(16);
    
    public void AddLine (Vector3 start, Vector3 end, Color color)
    {
        
        m_Vertices.Add(start);
        m_Vertices.Add(end);
        var m = m_Vertices.Count;
        m_Lines.Add(m - 2);
        m_Lines.Add(m - 1);
        m_Color.Add(color);
        m_Color.Add(color);
    }
    
    public void AddRay (Vector3 pos, Vector3 dir, Color color) {
        if (m_EqualSize)
            dir *= GetHandleSize(pos);
        AddLine(pos, pos + dir, color);
    }
    
    public void AddQuad (Vector3 pos, Vector3 up, Vector3 right, Color color)
    {
        m_Vertices.Add(pos + up - right);
        m_Vertices.Add(pos - up - right);
        m_Vertices.Add(pos - up + right);
        m_Vertices.Add(pos + up + right);
        var m = m_Vertices.Count;
        m_Quads.Add(m - 4);
        m_Quads.Add(m - 3);
        m_Quads.Add(m - 2);
        m_Quads.Add(m - 1);
        m_Color.Add(color);
        m_Color.Add(color);
        m_Color.Add(color);
        m_Color.Add(color);
    }
    
    
    public void AddQuad (Vector3 pos, float size, Color color)
    {
        if (m_EqualSize)
            size *= GetHandleSize(pos);
        AddQuad(pos, m_Camera.up * size, m_Camera.right * size, color);
    }
    
    public void AddQuad (Vector3 pos, float size, float colorFactor)
    {
        if (m_EqualSize)
            size *= GetHandleSize(pos);
        AddQuad(pos + m_Camera.forward * -size, m_Camera.up * size, m_Camera.right * size, HSVToRGB(colorFactor));
    }
    
    float GetHandleSize(Vector3 position)
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
            result = 80f / Mathf.Max(magnitude, 0.0001f) * EditorGUIUtility.pixelsPerPoint;
        }
        else
        {
            result = 20f;
        }
        return result;
    }
    
    // Simplified HSVToRGB with S, V always 1
    public static Color HSVToRGB(float H)
        {
            Color white = Color.white;
  
            float num = H * 6f;
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

    public void Init (Transform transform, bool depth, bool equalSize) {
        
        if (!m_Mesh) {
            m_Mesh = new Mesh();
            m_Mesh.hideFlags = HideFlags.DontSave;
        }
        
        InitMaterial(depth);

        m_Vertices.Clear();
        m_Color.Clear();
        m_Lines.Clear();
        m_Quads.Clear();

        m_Matrix = transform.localToWorldMatrix;
        m_Camera = Camera.current.transform;
        m_EqualSize = equalSize;
        m_Layer = transform.gameObject.layer;
    }
    
    void InitMaterial (bool writeDepth)
	{
		if (!m_Material)
		{
			// Unity has a built-in shader that is useful for drawing
			// simple colored things.
			var shader = Shader.Find ("Hidden/Internal-Colored");
			m_Material = new Material (shader);
			m_Material.hideFlags = HideFlags.DontSave;
			// Turn on alpha blending
			m_Material.SetInt ("_SrcBlend", (int)BlendMode.SrcAlpha);
            m_Material.SetInt ("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
			// Turn backface culling off
			m_Material.SetInt ("_Cull", (int)CullMode.Off);
			// Turn off depth writes
			m_Material.SetInt ("_ZWrite", 0);
		}
        // Set external depth on/off
        m_Material.SetInt ("_ZTest", writeDepth ? 4 : 0);
	}
    
    public void End () {
        m_Mesh.Clear();
        
        m_Mesh.SetVertices(m_Vertices);
        m_Mesh.SetColors(m_Color);
        m_Mesh.subMeshCount = 2;
        m_Mesh.SetIndices(m_Lines.ToArray(), MeshTopology.Lines, 0);
        m_Mesh.SetIndices(m_Quads.ToArray(), MeshTopology.Quads, 1);
        //m_Mesh.RecalculateNormals();
        m_Mesh.RecalculateBounds();
    }
    
    void OnRenderScene (SceneView view) {
        
         {
            m_Material.SetPass(0);
            
            Graphics.DrawMeshNow(m_Mesh, m_Matrix, 0);
        }    Graphics.DrawMeshNow(m_Mesh, m_Matrix, 1);
        
    }
    
}