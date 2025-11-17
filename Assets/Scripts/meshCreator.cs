using System.Collections;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;

public class meshCreator : MonoBehaviour
{
    
    Vector3[] vertices;
    MeshFilter m_meshFilter;
    MeshRenderer m_mRender;

    private void Start()
    {
        m_meshFilter = this.AddComponent<MeshFilter>();
        m_mRender = this.AddComponent<MeshRenderer>();
        vertices = new Vector3[3]
        {
            new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0)
        };
        TriCreator(vertices);


        
    }

    void TriCreator(Vector3[] verts) 
    {
        m_meshFilter.mesh.vertices = vertices;
        m_meshFilter.mesh.triangles = new int[3]
        {
        0, 1, 2
        };
    }
}
