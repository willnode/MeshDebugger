
using System;
using UnityEngine;
using System.Collections.Generic;

[Serializable]
public class IMGizmos {

    public List<IMUnit> m_Unit = new List<IMUnit>();
    
    public int m_CurIter;
    public bool m_Color;
    public int m_UVs;

    public IMUnit current {
        get {
            if (m_Unit.Count == m_CurIter) {
                var n = new IMUnit();
                m_Unit.Add(n);
                return n;
            }
            return m_Unit[m_CurIter];
        }
    }

    void ValidateCurrentIter (int additional) {
        if (current.m_Vertices.Count + additional < 65000)
            return;
        m_CurIter++; 
    }
    
   
    public void Init () {
        m_CurIter = 0;
        current.Init(m_Color, m_UVs);
        for (int i = 0; i < m_Unit.Count; i++)
        {
            m_Unit[i].Init(m_Color, m_UVs);
        }
    }

}