using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class IMGizmos
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

    public void AddQuad(Vector3 pos, Vector3 up, Vector3 right, Color color)
    {
        ValidateCurrentIter(4);
        m_Gizmos[m_CurIter].AddQuad(pos, up, right, color);
    }

    public void AddQuad(Vector3 pos, float size, Color color)
    {
        ValidateCurrentIter(4);
        m_Gizmos[m_CurIter].AddQuad(pos, size, color);
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

    private void CreateNewIter()
    {
        var giz = ScriptableObject.CreateInstance<IMGizmo>();
        m_Gizmos.Add(giz);
    }

    public void Init(Transform transform, bool depth, bool equalSize)
    {
        m_TotalVert = 0;
        m_CurIter = 0;
        if (m_Gizmos.Count == 0)
            CreateNewIter();
        for (int i = 0; i < m_Gizmos.Count; i++)
        {
            m_Gizmos[i].m_Active = i == 0;
            m_Gizmos[i].Init(transform, depth, equalSize);
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
}