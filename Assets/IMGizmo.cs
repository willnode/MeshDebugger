
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

[Serializable]
public class IMUnit {
    
    public XList<Vector3> m_Vertices = new XList<Vector3>();
    public XList<int> m_Indices = new XList<int>();

    public XList<Color> m_Color;
    public XList<Vector4> m_UV;
    public XList<Vector4> m_UV2;
    public XList<Vector4> m_UV3;
    public XList<Vector4> m_UV4;

    public bool m_useColor;
    public int m_useUV;

    public void Init (bool color, int uvs) {
        m_useColor = color;
        m_useUV = uvs;

        m_Vertices.Clear();
        m_Indices.Clear();
        if (color)
            ClearOrNew(ref m_Color);
        if (uvs >= 1)
            ClearOrNew(ref m_UV);
        if (uvs >= 2)
            ClearOrNew(ref m_UV2);
        if (uvs >= 3)
            ClearOrNew(ref m_UV3);
        if (uvs >= 4)
            ClearOrNew(ref m_UV4);
    }

    void ClearOrNew<T>(ref XList<T> l) {
        if (l == null)
            l = new XList<T>();
         else
            l.Clear();
    }


    public void Vertex3(Vector3 pos) {
        m_Vertices.Add(pos);
    }


    public void Vertex3(Vector3 pos, Color color) {
        m_Vertices.Add(pos);
        m_Color.Add(color);  
    }


    public void Vertex3(Vector3 pos, Color color, Vector4 uv) {
        m_Vertices.Add(pos);
        m_Color.Add(color);
        m_UV.Add(uv);     
    }


    public void Vertex3(Vector3 pos, Color color, Vector4 uv, Vector4 uv2) {
        m_Vertices.Add(pos);
        m_Color.Add(color);
        m_UV.Add(uv);     
        m_UV2.Add(uv2);
    }

    public void Vertex3(Vector3 pos, Color color, Vector4 uv, Vector4 uv2, Vector4 uv3) {
        m_Vertices.Add(pos);
        m_Color.Add(color);
        m_UV.Add(uv);     
        m_UV2.Add(uv2);
        m_UV3.Add(uv3); 
    }

    public void Vertex3(Vector3 pos, Color color, Vector4 uv, Vector4 uv2, Vector4 uv3, Vector4 uv4) {
        m_Vertices.Add(pos);
        m_Color.Add(color);
        m_UV.Add(uv);     
        m_UV2.Add(uv2);
        m_UV3.Add(uv3);
        m_UV4.Add(uv4);  
    }

    public void MakeQuad()
    {
        var size = m_Vertices.Count;
        MakeQuad(size--, size--, size--, size--);
    }

    public void MakeQuad(int a, int b, int c, int d)
    {
        var size = m_Indices.Count;
        m_Indices.AddEmpty(6);
        var buff = m_Indices.buffer;
        buff[size++] = a;
        buff[size++] = b;
        buff[size++] = c;
        buff[size++] = b;
        buff[size++] = c;
        buff[size++] = d;
    }

    public void FillMesh(Mesh m) {
        m.Clear();
        var del = GetInternalMeshUploader(m);

        del(0, 0, 3, m_Vertices.buffer, m_Vertices.Count);
        if (m_useColor)
            del(2, 2, 3, m_Color.buffer, m_Color.Count);
        if (m_useUV >= 1)
            del(3, 0, 3, m_UV.buffer, m_UV.Count);
        if (m_useUV >= 2)
            del(4, 0, 3, m_UV2.buffer, m_UV2.Count);
        if (m_useUV >= 3)
            del(5, 0, 3, m_UV3.buffer, m_UV3.Count);
        if (m_useUV >= 4)
            del(6, 0, 3, m_UV4.buffer, m_UV4.Count);

        var tris = GetInternalTriangleUploader(m);
        tris(m_Indices.buffer, 0, true); 
        m.RecalculateBounds();
    }


    static InternalUploaderDelegate GetInternalMeshUploader(Mesh m) {
        var method = typeof(Mesh).GetMethod("SetSizedArrayForChannel", BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance);
        return (InternalUploaderDelegate)Delegate.CreateDelegate(typeof(InternalUploaderDelegate), m, method);
    }

    static TriangleUploaderDelegate GetInternalTriangleUploader(Mesh m) {
        var method = typeof(Mesh).GetMethod("SetTrianglesImpl", BindingFlags.NonPublic | BindingFlags.InvokeMethod | BindingFlags.Instance);
        return (TriangleUploaderDelegate)Delegate.CreateDelegate(typeof(TriangleUploaderDelegate), m, method);
    }

    delegate void TriangleUploaderDelegate(int[] triangles, int submesh, bool calculateBounds);

    delegate void InternalUploaderDelegate(object channel, object format, object dim, object values, object count);


    internal enum MeshChannel
    {
        Vertex,
        Normal,
        Color,
        TexCoord0,
        TexCoord1,
        TexCoord2,
        TexCoord3,
        Tangent
    }

    internal enum EmpEnum
    {
    }

    internal enum MeshFormat
    {
        Vertex,
        Color = 2,
    }
}