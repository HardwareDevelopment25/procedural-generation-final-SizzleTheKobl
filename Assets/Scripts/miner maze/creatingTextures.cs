using JetBrains.Annotations;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor.PackageManager.UI;
using UnityEngine;
using UnityEngine.UIElements;

public class creatingTextures : MonoBehaviour
{

    [SerializeField] int imageSize = 64;
    [SerializeField] Material m_material;
    [SerializeField] TerrainType[] terrainTypes;
    [SerializeField] AnimationCurve curve;
    [SerializeField] int seed = 0;
    Texture2D m_texture;
    Color[] m_pixels;
    System.Random m_random = new System.Random();
    public float m_scale = 1f;
    public float m_lacunarity, m_persistence;
    public int m_octaves = 5;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_random = new System.Random(seed);
        m_texture = new Texture2D(imageSize, imageSize);
        m_pixels = new Color[imageSize * imageSize];
        //createColourTexture();
        createLayerTexture();
    }

    private void Update()
    {

    }

    [System.Serializable]
    public struct TerrainType
    {
        public string name;
        public float height;
        public Color colour;
    }

    public void createPattern()
    {

        for (int y = 0; y < m_texture.height; y++)
        {
            for (int x = 0; x < m_texture.width; x++)
            {
                float xCoord = (float)x / m_texture.width * m_scale;
                float yCoord = (float)y / m_texture.height * m_scale;
                float sample = Mathf.PerlinNoise(xCoord, yCoord);
                m_pixels[y * m_texture.width + x] = new Color(sample, sample, sample);
                //Color pixelColour = new Color(sample, sample, sample);
                //m_texture.SetPixel(x, y, pixelColour);
            }
        }
        m_texture.SetPixels(m_pixels);
        m_texture.Apply();
        m_material.mainTexture = m_texture;

    }

    public void createLayerTexture()
    {
        float[,] perlinResults = perlinNoise.CreatePerlinNoise(imageSize, imageSize, m_octaves, m_scale, m_lacunarity, m_persistence, m_random.Next(), new Vector2(0, 0));
        for (int y = 0; y < m_texture.height; y++)
        {
            for (int x = 0; x < m_texture.width; x++)
            {
                float indPixel = perlinResults[x, y];
                m_pixels[y * m_texture.width + x] = new Color(indPixel, indPixel, indPixel);
            }
        }
        m_texture.SetPixels(m_pixels);
        m_texture.Apply();
        m_material.mainTexture = m_texture;
    }

    public void createColourTexture()
    {
        float[,] falloffMap = FalloffMap();
        Color terrainColour = new Color(0, 0, 0);
        float[,] perlinResults = perlinNoise.CreatePerlinNoise(imageSize, imageSize, m_octaves, m_scale, m_lacunarity, m_persistence, m_random.Next(), new Vector2(0, 0));
        for (int y = 0; y < imageSize; y++)
        {
            for (int x = 0; x < imageSize; x++)
            {
                float indPixel = Mathf.Clamp(perlinResults[x, y] - falloffMap[x, y], 0, 1);
                for (int type = 0; type < terrainTypes.Length; type++)
                {
                    if (indPixel >= terrainTypes[type].height)
                    {

                        terrainColour = terrainTypes[type].colour;
                    }
                }
                m_pixels[y * imageSize + x] = terrainColour;
            }
        }
        m_texture.SetPixels(m_pixels);
        //Debug.Log(m_pixels.Length);
        //Debug.Log(imageSize * imageSize);
        //Debug.Log($"{m_texture.width} {m_texture.height}");
        m_texture.Apply();
        m_material.mainTexture = m_texture;

    }

    public float[,] FalloffMap()
    {
        float[,] falloffMap = new float[imageSize, imageSize];
        for (int y = 0; y < (imageSize); y++)
        {
            for (int x = 0; x < (imageSize); x++)
            {
                float fallX = x / (float)imageSize * 2 - 1;
                float fallY = y / (float)imageSize * 2 - 1;
                float value = (Mathf.Max(Mathf.Abs(fallX), Mathf.Abs(fallY)));
                falloffMap[x, y] = curve.Evaluate(value);
            }
        }
        return falloffMap;
    }
}
