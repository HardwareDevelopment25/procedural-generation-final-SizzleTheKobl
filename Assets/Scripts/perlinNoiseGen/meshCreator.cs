using System.Collections;
using Unity.VisualScripting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.LowLevelPhysics;
using static creatingTextures;

public class meshCreator : MonoBehaviour
{
    [SerializeField] TerrainType[] terrainTypes;
    
    //Components
    MeshFilter m_meshFilter;
    MeshRenderer m_meshRender;
    MeshData m_meshData;
    [SerializeField] Material m_material;
    
    //PerlinNoise
    [SerializeField] int m_gridSize = 64;
    [SerializeField] int m_seed = 0;
    [SerializeField] float m_scale = 10f;
    [SerializeField] int m_octave = 5;
    [SerializeField] float m_lacunarity = 2;
    [SerializeField] float m_persistance = 1;
    [SerializeField] Vector2 m_offset;
    //Falloff
    [SerializeField] AnimationCurve m_animCurve;

    private void Start()
    {
        m_meshFilter = this.AddComponent<MeshFilter>();
        m_meshRender = this.AddComponent<MeshRenderer>();
        float[,] noiseMap = perlinNoise.CreatePerlinNoise(m_gridSize, m_gridSize, m_octave, m_scale, m_lacunarity, m_persistance, m_seed, m_offset);
        float[,] fallOffMap = perlinNoise.FalloffMap(m_gridSize, m_animCurve);
        float[,] combinedMap = new float[m_gridSize, m_gridSize];
        for (int x = 0; x < noiseMap.GetLength(0); x++) 
        { 
            for (int y = 0; y < noiseMap.GetLength(1); y++) 
            {
                combinedMap[x, y] = noiseMap[x, y] - fallOffMap[x,y];
            }
        }
        m_meshData = GenerateTerrain(combinedMap, m_scale);
        m_meshFilter.mesh = m_meshData.CreateMesh();
        m_meshRender.material = m_material;
        ColourMap(combinedMap);

    }
    public void ColourMap(float[,] noiseMap)
    {
        Color[] pixels = new Color[m_gridSize * m_gridSize];
        Texture2D texture = new Texture2D(m_gridSize, m_gridSize);
        Color terrainColour = new Color(0, 0, 0);
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float indPixel = Mathf.Clamp(noiseMap[x,y], 0, 1);
                for (int type = 0; type < terrainTypes.Length; type++)
                {
                    if (indPixel >= terrainTypes[type].height)
                    {
                        terrainColour = terrainTypes[type].colour;
                    }
                }
                pixels[y * texture.width + x] = terrainColour;
            }
        }

        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels(pixels);
        texture.Apply();
        m_material.mainTexture = texture;

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
        square.uv = new Vector2[] { new Vector2(-m_scale, -m_scale), new Vector2(-m_scale, m_scale), new Vector2(m_scale, m_scale), new Vector2(-m_scale, m_scale) };
        square.triangles = new int[]
        { 0,1,2,2,3,0 };
        return square;
    }

    public MeshData GenerateTerrain(float[,] heightMap, float hightMulti)
    {
        int height = heightMap.GetLength(0);
        int width = heightMap.GetLength(1);

        float topLeftX = (width - 1) / -2f;
        float topLeftZ = (height - 1) / 2f;

        MeshData meshData = new MeshData(width, height);
        int vertexIndex = 0;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                meshData.SetVertex(vertexIndex, new Vector3((topLeftX + x), (heightMap[x, y] * hightMulti), (topLeftZ - y)));

                meshData.SetUV(vertexIndex, new Vector2(x / (float)width, y / (float)height));


                //create triangle faces

                if ((x < width - 1) && (y < height - 1))//making square of 2 triangles
                {
                    meshData.AddTriangle(new int[3] { vertexIndex, vertexIndex + width + 1, vertexIndex + width });
                    meshData.AddTriangle(new int[3] { vertexIndex + width + 1, vertexIndex, vertexIndex + 1 });


                }
                vertexIndex++;
            }
        }

        return meshData;
    }
}

[System.Serializable]
public struct TerrainType
{
    public string name;
    public float height;
    public Color colour;
}
public class MeshData
{
    Vector3[] m_vertices;
    int[] m_tris;

    int m_triIndex = 0;
    Vector2[] m_uvs;

    public MeshData(int meshWidth, int meshHeight)
    {
        m_vertices = new Vector3[meshWidth * meshHeight];
        m_tris = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
        m_uvs = new Vector2[meshWidth * meshHeight];
    }

    //GETTERS
    public Vector3[] GetVertices() 
    {
        return m_vertices;
    }
    //SETTERS

    public void SetVertex(int vertIndex, Vector3 vertex) 
    {
        m_vertices[vertIndex] = vertex;
    
    }

    public void SetUV(int UVIndex, Vector2 uv) 
    {
        m_uvs[UVIndex] = uv;
    }
    //FUNCTIONALITY
    

    public void AddTriangle(int[] triVert)
    {
        for (int i = 0; i < 3; i++)
        { 
            m_tris[m_triIndex] = triVert[i];
            m_triIndex++;
        }
    }

    public Mesh CreateMesh() // make mesh
    {
        Mesh mesh = new Mesh();
        mesh.vertices = m_vertices;
        mesh.triangles = m_tris;
        mesh.uv = m_uvs;
        mesh.RecalculateNormals();

        return mesh;
    }
}
