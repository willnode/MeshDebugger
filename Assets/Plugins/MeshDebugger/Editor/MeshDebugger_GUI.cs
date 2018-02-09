using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public partial class MeshDebugger : EditorWindow
{
    public static class UI
    {
        public static GUIContent Target = new GUIContent("Target", "Currently inspected object");

        public static GUIContent Configuration = new GUIContent("Configuration", "Toggleable control");
        public static GUIContent Static = new GUIContent("Static", "Turn on to assume that the mesh won't change internally (ie. not procedural)");
        public static GUIContent DepthCulling = new GUIContent("Depth Culling", "Turn on to cull cues that behind the object");
        public static GUIContent Equalize = new GUIContent("Equalize", "Turn on to keep cues on scale no matter far it is\n(NOTE: does not work correctly if static is on)");
        public static GUIContent PartialDebug = new GUIContent("Partial Debug", "");

        public static GUIContent RaySize = new GUIContent("Ray Size", "The length of ray cues");
        public static GUIContent VertexRays = new GUIContent("Vertex Rays", "The length of ray cues");
        public static GUIContent Normal = new GUIContent("Normal", "Normal vector of vertices");
        public static GUIContent Tangent = new GUIContent("Tangent", "Tangent vector of vertices");
        public static GUIContent Bitangent = new GUIContent("Bitangent", "Bitangent (cross of normal and tangent) vector of vertices");

        public static GUIContent AdditionalRays = new GUIContent("Additional Rays", "");
        public static GUIContent VertsToIndice = new GUIContent("Vertex to Indice", "Incomplete");
        public static GUIContent TriangleNormal = new GUIContent("Triangle Normal", "");

        public static GUIContent UseHeatmap = new GUIContent("Use Heatmap", "");
        public static GUIContent DebugVertices = new GUIContent("Debug Vertices", "");
        public static GUIContent DebugTriangles = new GUIContent("Debug Triangles", "");

        public static GUIContent None = new GUIContent("None", "");
        public static GUIContent Index = new GUIContent("Index", "");
        public static GUIContent Shared = new GUIContent("Shared", "");
        public static GUIContent Duplicates = new GUIContent("Duplicates", "");
        public static GUIContent Area = new GUIContent("Area", "");
        public static GUIContent Submesh = new GUIContent("Submesh", "");

    }

    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(UI.Target);
            m_Transform = (Transform)EditorGUILayout.ObjectField(m_Transform, typeof(Transform), true);
            m_Mesh = (Mesh)EditorGUILayout.ObjectField(m_Mesh, typeof(Mesh), true);
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(UI.Configuration);
            m_Static = GUILayout.Toggle(m_Static, UI.Static, EditorStyles.miniButtonLeft);
            m_DepthCulling = GUILayout.Toggle(m_DepthCulling, UI.DepthCulling, EditorStyles.miniButtonMid);
            if (m_Static)
            {
                EditorGUI.BeginDisabledGroup(true);
                GUILayout.Toggle(false, UI.Equalize, EditorStyles.miniButtonRight);
                EditorGUI.EndDisabledGroup();
            }
            else
                m_EqualizeGizmoSize = GUILayout.Toggle(m_EqualizeGizmoSize, UI.Equalize, EditorStyles.miniButtonRight);
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(UI.PartialDebug);
            m_PartialDebug = GUI.Toggle(EditorGUILayout.GetControlRect(GUILayout.Width(16)), m_PartialDebug, "");
            EditorGUI.BeginDisabledGroup(!m_PartialDebug);
            EditorGUILayout.MinMaxSlider(ref m_PartialDebugStart, ref m_PartialDebugEnd, 0, 1);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
        m_RaySize = EditorGUILayout.Slider(UI.RaySize, m_RaySize, 0, 2);
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(UI.VertexRays);
            m_DebugNormalVerts = GUILayout.Toggle(m_DebugNormalVerts, UI.Normal, EditorStyles.miniButtonLeft);
            m_DebugTangentVerts = GUILayout.Toggle(m_DebugTangentVerts, UI.Tangent, EditorStyles.miniButtonMid);
            m_DebugBinormalVerts = GUILayout.Toggle(m_DebugBinormalVerts, UI.Bitangent, EditorStyles.miniButtonRight);
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(UI.AdditionalRays);
            m_DebugVertsToIndice = GUILayout.Toggle(m_DebugVertsToIndice, UI.VertsToIndice, EditorStyles.miniButtonLeft);
            m_DebugTrisNormal = GUILayout.Toggle(m_DebugTrisNormal, UI.TriangleNormal, EditorStyles.miniButtonRight);
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(UI.UseHeatmap);
            m_UseHeatmap = GUI.Toggle(EditorGUILayout.GetControlRect(GUILayout.Width(16)), m_UseHeatmap, "");
            EditorGUI.BeginDisabledGroup(!m_UseHeatmap);
            m_HeatSize = EditorGUILayout.Slider(m_HeatSize, 0, 0.5f);
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(UI.DebugVertices);
            if (GUILayout.Toggle(m_DebugVert == DebugVertice.None, UI.None, EditorStyles.miniButtonLeft)) m_DebugVert = DebugVertice.None;
            if (GUILayout.Toggle(m_DebugVert == DebugVertice.Index, UI.Index, EditorStyles.miniButtonMid)) m_DebugVert = DebugVertice.Index;
            if (GUILayout.Toggle(m_DebugVert == DebugVertice.Shared, UI.Shared, EditorStyles.miniButtonMid)) m_DebugVert = DebugVertice.Shared;
            if (GUILayout.Toggle(m_DebugVert == DebugVertice.Duplicates, UI.Duplicates, EditorStyles.miniButtonRight)) m_DebugVert = DebugVertice.Duplicates;
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(UI.DebugTriangles);
            if (GUILayout.Toggle(m_DebugTris == DebugTriangle.None, UI.None, EditorStyles.miniButtonLeft)) m_DebugTris = DebugTriangle.None;
            if (GUILayout.Toggle(m_DebugTris == DebugTriangle.Index, UI.Index, EditorStyles.miniButtonMid)) m_DebugTris = DebugTriangle.Index;
            if (GUILayout.Toggle(m_DebugTris == DebugTriangle.Area, UI.Area, EditorStyles.miniButtonMid)) m_DebugTris = DebugTriangle.Area;
            if (GUILayout.Toggle(m_DebugTris == DebugTriangle.Submesh, UI.Submesh, EditorStyles.miniButtonRight)) m_DebugTris = DebugTriangle.Submesh;
            EditorGUILayout.EndHorizontal();
        }
        {
            EditorGUILayout.Space();
            if (m_Mesh)
                EditorGUILayout.HelpBox( "Mesh Features:\n" + m_cpu.m_Features, MessageType.Info);
            if (!m_UseHeatmap && (m_DebugVert != DebugVertice.None || m_DebugTris != DebugTriangle.None) && !IsSafeToDrawGUI())
                EditorGUILayout.HelpBox("Verts / Triangle count are too large to be displayed with GUI index rendering.\nConsider set smaller section or enable Heatmap instead.", MessageType.Warning);
        }
        if (EditorGUI.EndChangeCheck())
        {
            m_hasUpdated = false;
            SceneView.RepaintAll();
        }
    }
}