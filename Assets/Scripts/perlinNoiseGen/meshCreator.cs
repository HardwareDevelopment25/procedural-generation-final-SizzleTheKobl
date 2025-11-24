using System.Collections;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.LowLevelPhysics;

public class meshCreator : MonoBehaviour
{
    [SerializeField] int scale = 1;    
    
    MeshFilter m_meshFilter;
    MeshRenderer m_mRender;

    private void Start()
    {
        
        Vector3[] vertices;
        m_meshFilter = this.AddComponent<MeshFilter>();
        m_mRender = this.AddComponent<MeshRenderer>();
        vertices = new Vector3[3]
        {
            new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(1, 0, 0)
        };
        m_meshFilter.mesh = TriCreator(vertices);
        vertices = new Vector3[4]
        {
            new Vector3(-scale, -scale, 0), new Vector3(-scale, scale, 0), new Vector3(scale, scale, 0), new Vector3(scale, -scale, 0)
        };
        m_meshFilter.mesh = SquareCreator(vertices);


    }

    Mesh TriCreator(Vector3[] verts) 
    {
        Mesh triangle = new Mesh();
        triangle.vertices = verts;
        triangle.triangles = new int[3]
        {
        0, 1, 2
        };
        return triangle;
    }

    Mesh SquareCreator(Vector3[] verts)
    {
        Mesh square = new Mesh();
        
        square.vertices = verts;
        square.uv = new Vector2[] { new Vector2(-scale, -scale), new Vector2(-scale, scale), new Vector2(scale, scale), new Vector2(-scale, scale) };
        square.triangles = new int[]
        { 0,1,2,2,3,0 };
        return square;
    }
}
