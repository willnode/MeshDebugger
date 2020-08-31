using System;
using System.Collections.Generic;
using UnityEngine;

// High management from IM Gizmos, with 65K mesh split!
[Serializable]
public class IMGizmos : ScriptableObject, IDisposable
{
    public List<IMGizmo> m_Gizmos = new List<IMGizmo>();

    public int m_TotalVert;
    public int m_CurIter;

    public void AddLine(Vector3 start, Vector3 end, Color color)
    {
        ValidateCurrentIter(2);
        m_Gizmos[m_CurIter].AddLine(start, end, color);
    }

    public void AddRay(Vector3 pos, Vector3 dir, Color color)
    {
        ValidateCurrentIter(2);
        m_Gizmos[m_CurIter].AddRay(pos, dir, color);
    }

    public void AddQuad(Vector3 pos, Vector2 size, Color color)
    {
        ValidateCurrentIter(4);
        m_Gizmos[m_CurIter].AddQuad(pos, size, color);
    }

    public void AddQuad(Vector3 pos, float size, Color color)
    {
        ValidateCurrentIter(4);
        m_Gizmos[m_CurIter].AddQuad(pos, Vector2.one * size, color);
    }

    public void AddQuad(Vector3 pos, float size, float colorFactor)
    {
        ValidateCurrentIter(4);
        m_Gizmos[m_CurIter].AddQuad(pos, size, colorFactor);
    }

    private void ValidateCurrentIter(int additional)
    {
        m_TotalVert += additional;
        if (m_TotalVert < 65000 * (m_CurIter + 1))
            return;
        m_CurIter++;
        if (m_Gizmos.Count == m_CurIter)
            CreateNewIter();
        else
            m_Gizmos[m_CurIter].m_Active = true;
    }

    private Transform _transform;
    private Transform _camera;
    private bool _depth;
    private bool _equalSize;

    private void CreateNewIter()
    {
        var giz = ScriptableObject.CreateInstance<IMGizmo>();
        giz.Init(_transform, _camera, _depth, _equalSize);
        m_Gizmos.Add(giz);
    }

    public void Init(Transform transform, Transform camera, bool depth, bool equalSize)
    {
        _transform = transform;
        _camera = camera;
        _depth = depth;
        _equalSize = equalSize;

        m_TotalVert = 0;
        m_CurIter = 0;
        if (m_Gizmos.Count == 0)
            CreateNewIter();
        for (int i = 0; i < m_Gizmos.Count; i++)
        {
            m_Gizmos[i].m_Active = i == 0;
            m_Gizmos[i].Init(transform, camera, depth, equalSize);
        }

    }

    public void End()
    {
        for (int i = 0; i < m_Gizmos.Count; i++)
        {
            if (m_Gizmos[i].m_Active)
                m_Gizmos[i].End();
        }
    }

    public void Render()
    {
        for (int i = 0; i < m_Gizmos.Count; i++)
        {
            if (m_Gizmos[i].m_Active)
                m_Gizmos[i].Render();
        }
    }

    public void Dispose()
    {
        foreach (var item in m_Gizmos)
        {
            UnityEngine.Object.DestroyImmediate(item);
        }
    }

    public void Clear()
    {
        foreach (var item in m_Gizmos)
        {
            item.Clear();
        }
    }

    internal void UpdateGO(Transform transform)
    {
        for (int i = 0; i < m_Gizmos.Count; i++)
        {
            if (m_Gizmos[i].m_Active)
                m_Gizmos[i].UpdateGO(transform);
        }
    }
}