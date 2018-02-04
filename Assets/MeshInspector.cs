using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEngine.Rendering;

public class MeshInspector : MonoBehaviour {

    public Mesh m_Mesh;
    public IMGizmos m_Gizmo;

    public bool m_static;
    public bool m_DepthCulling;
    public float m_VertsOffset;
    public float m_RaySize = .4f;
    public float m_HeatSize = .2f;
    public bool m_UseHeatmap;
    public bool m_EqualizeGizmoSize;
    [Space]
    public bool m_DebugNormalVerts;
    public bool m_DebugTangentVerts;
    public bool m_DebugBinormalVerts;
    public bool m_DebugTriangleNormal;
    [Space]
    public bool m_DebugTriangleOrder;
    public bool m_DebugTriangleHotness;
    public bool m_DebugTriangleSubmesh;
    [Space]
    public bool m_DebugVertIndex;
    public bool m_DebugVertUsed;
    public bool m_DebugVertDuplis;
    [Space]
    
    Transform m_sceneCam;
    Vector3 m_sceneCamPos;

    Matrix4x4 m_matrix;
    MeshInfo m_cpu = new MeshInfo();

    bool m_hasUpdated = false;

    // Update is called once per frame
    void OnDrawGizmosSelected () {
		if (!enabled)
            return;
        m_sceneCam = SceneView.lastActiveSceneView.camera.transform;
        m_sceneCamPos = m_sceneCam.position;
        
        if (!m_Mesh) {
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

        if (m_DebugNormalVerts || m_DebugTangentVerts || m_DebugBinormalVerts) {
            Color blue = Color.blue, green = Color.green, red = Color.red;
            for (int i = 0; i < m_cpu.m_VertCount; i++)
			{
				var vert = m_cpu.m_Verts[i];
                if (m_DebugNormalVerts) 
                    m_Gizmo.AddRay(vert, m_cpu.m_Normals[0][i] * m_RaySize, blue);                
                if (m_DebugTangentVerts) 
                    m_Gizmo.AddRay(vert, m_cpu.m_Normals[1][i] * m_RaySize, green);
                if (m_DebugBinormalVerts) 
                    m_Gizmo.AddRay(vert, m_cpu.m_Normals[2][i] * m_RaySize, red);                
            }
		}
        
        if (m_DebugTriangleNormal) {
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
                var factor = 1f / m_cpu.m_VertCount;
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
            Handles.EndGUI();
        }

        m_Gizmo.End();

        Handles.matrix = Matrix4x4.identity;

        m_hasUpdated = true;
    }

    void OnValidate () {
        m_hasUpdated = false;
    }
    /*string m_infoTris;
    string GetTrisInfo (int iter, int submesh, int index) {
        return string.Format(m_infoTris, 
    }*/

    bool IsFacingCamera (Vector3 pos, Vector3 normal) {
        return new Plane(m_matrix.MultiplyVector(normal), m_matrix.MultiplyPoint3x4(pos)).GetSide(m_sceneCamPos);
	}
    

    void InitGL (int mode, bool keepWorldSpace) {
    	// Use GL for replacement of Handles
        CreateLineMaterial(m_DepthCulling);
        lineMaterial.SetPass(0);

        GL.PushMatrix();

        if (!keepWorldSpace)
            GL.MultMatrix(Handles.matrix);

        // Draw lines
        GL.Begin(mode);
	}
    
    void EndGL () {
	    GL.End();
        GL.PopMatrix();
    }
    
    void SetColorGL (Color color)
    {
        GL.Color(color);
    }
    
    void DrawRay(Vector3 pos, Vector3 dir, Color color)
    {
        SetColorGL(color);
        DrawLine(pos, pos + dir);
    }
    
    void DrawLine(Vector3 start, Vector3 end)
    {
        // One vertex at transform position
        GL.Vertex(start);
        // Another vertex at edge of circle
        GL.Vertex(end);
    }

    void DrawRect(Vector3 pos, float size)
    {
        pos = m_matrix.MultiplyPoint3x4(pos);
        Vector3 b = m_sceneCam.right * size;
		Vector3 b2 = m_sceneCam.up * size;
		GL.Vertex(pos + b + b2);
		GL.Vertex(pos + b - b2);
		GL.Vertex(pos - b - b2);
		GL.Vertex(pos - b + b2);
    }



    static GUIContent m_gui = new GUIContent();
    
    void DrawLabel (Vector3 pos, Vector3 normal, string text)
	{
		
        if (m_DepthCulling && (!IsFacingCamera(pos, normal))) 
			return;
		
        m_gui.text = text;
        var GUIPos = HandleUtility.WorldPointToSizedRect (pos, m_gui, Styles.blockLabel);
		GUIPos.y -= 7f;
		GUI.Label (GUIPos, m_gui, Styles.blockLabel);	
	}

    


    
    public Material lineMaterial;
	void CreateLineMaterial (bool writeDepth)
	{
		if (!lineMaterial)
		{
			// Unity has a built-in shader that is useful for drawing
			// simple colored things.
			var shader = Shader.Find ("Hidden/Internal-Colored");
			lineMaterial = new Material (shader);
			lineMaterial.hideFlags = HideFlags.DontSave;
			// Turn on alpha blending
			lineMaterial.SetInt ("_SrcBlend", (int)BlendMode.SrcAlpha);
            lineMaterial.SetInt ("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
			// Turn backface culling off
			lineMaterial.SetInt ("_Cull", (int)CullMode.Off);
			// Turn off depth writes
			lineMaterial.SetInt ("_ZWrite", 0);
		}
        // Set external depth on/off
        lineMaterial.SetInt ("_ZTest", writeDepth ? 4 : 0);
	}

    static public class Styles
    {
		static public GUIStyle blockLabel = new GUIStyle (EditorStyles.boldLabel);
		
		static Styles ()
		{
			blockLabel.normal.background = EditorGUIUtility.whiteTexture;
			blockLabel.margin = new RectOffset ();//2, 2, 1, 1);
			blockLabel.padding = new	RectOffset ();
			blockLabel.alignment = TextAnchor.MiddleCenter;
			//blockLabel.contentOffset = new Vector2(0, -10f);
		}
    }
    
    
 
}
