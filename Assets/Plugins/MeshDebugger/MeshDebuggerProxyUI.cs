using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class MeshDebuggerProxyUI : BaseMeshEffect
{
    public Mesh mesh;

    public Action callback;

    protected override void OnEnable()
    {
        if (!mesh)
        {
            mesh = new Mesh();
            mesh.hideFlags = HideFlags.HideAndDontSave;
            mesh.name = gameObject.name + " (Snapshot)";
        }
        base.OnEnable();
    }

    protected override void OnDestroy()
    {
        if (mesh)
        {
            DestroyImmediate(mesh);
        }
        base.OnDestroy();
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        vh.FillMesh(mesh);
        if (callback != null) callback();     
    }
}
