using System;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace MeshDebuggerLib.Proxy
{
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


        [ContextMenu("Force Read from Graphic")]
        public void ForceReadFromGraphic()
        {
            var g = GetComponent<Graphic>();
            g.SetVerticesDirty();
            g.Rebuild(CanvasUpdate.PreRender);
            FieldInfo meshField = typeof(Graphic).GetField("s_Mesh", BindingFlags.NonPublic | BindingFlags.Static);
            var m = meshField.GetValue(null) as Mesh;
            if (m)
            {
                var vh = new VertexHelper(m);
                vh.FillMesh(mesh);
                if (callback != null) callback();
            }
            else
            {
                Debug.LogError("Failed to read mesh from Graphic");
            }
        }
    }
}
