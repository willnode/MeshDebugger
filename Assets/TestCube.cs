using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
public class TestCube : MonoBehaviour
{
    public IMGizmos giz = new IMGizmos();
    // Use this for initialization
    void Start()
    {
	
    }
	
    // Update is called once per frame
    void OnEnable()
    {
	    giz.Init();
        var g = giz.current;
        g.Vertex3(Vector3.zero);
        g.Vertex3(Vector3.up);
        g.Vertex3(Vector3.forward + Vector3.up);
        g.Vertex3(Vector3.forward);
        g.MakeQuad();

        var m = GetComponent<MeshFilter>().mesh;
        g.FillMesh(m);
    }
}

