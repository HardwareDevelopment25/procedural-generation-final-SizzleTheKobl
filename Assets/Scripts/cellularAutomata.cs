using System;
using System.Collections;
using UnityEngine;

public class cellularAutomata : MonoBehaviour
{

    int[,] m_grid;
    int[,] m_marchGrid;

    [SerializeField] int m_gridSize;
    [SerializeField] float m_percFill;
    [SerializeField] int m_seed;
    [SerializeField] int m_iterations;
    [SerializeField] Material m_material;
    [SerializeField] GameObject m_grassPrefab;
    [SerializeField] GameObject m_stonePrefab;
    [SerializeField] GameObject m_spikePrefab;
    [SerializeField] GameObject m_marchPrefab;

    [SerializeField] Sprite[] m_sprites;
    [SerializeField] Material m_marchMaterial;

    Texture2D m_texture;
    Color m_color;
    System.Random m_random;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_texture = new Texture2D(m_gridSize, m_gridSize);
        m_random = new System.Random(m_seed);
        m_grid = new int[m_gridSize, m_gridSize];
        for (int x = 0; x < m_gridSize; x++)
        {
            for (int y = 0; y < m_gridSize; y++)
            {

                float randomNo = (float)m_random.NextDouble();
                if (x == 0 || x == m_gridSize - 1) { randomNo = 0; }
                if (y == 0 || y == m_gridSize - 1) { randomNo = 0; }
                if (randomNo > m_percFill) { m_grid[x, y] = 1; }
                else { m_grid[x, y] = 0; }


            }
        }
        Draw();
        for (int i = 0; i < (m_iterations + 1); i++)
        {
            CaveGame();
        }

        Draw();

        //CreateWorld();
        MarchingSquares();
        //StartCoroutine(CellularAutomata());


    }

    // Update is called once per frame
    void Update()
    {
    }

    private void Draw()
    {
        for (int x = 0; x < m_gridSize; x++)
        {
            for (int y = 0; y < m_gridSize; y++)
            {

                if (m_grid[x, y] == 0) { m_color = Color.black; }
                else { m_color = Color.white; }

                m_texture.SetPixel(x, y, m_color);


            }
        }

        m_texture.filterMode = FilterMode.Point;
        m_texture.wrapMode = TextureWrapMode.Clamp;
        m_texture.Apply();
        m_material.mainTexture = m_texture;
    }

    int CheckNeighbour(int x, int y)
    {
        int neighbours = 0;
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if ((x + i > 0) && ((x + i) < (m_gridSize)))
                {
                    if ((y + j > 0) && ((y + j) < (m_gridSize)))
                    {
                        if (m_grid[(x + i), (y + j)] == 1) { neighbours++; }
                    }

                }
            }
        }
        if (m_grid[x, y] == 1) { neighbours--; }
        ;

        return neighbours;
    }

    void WolframGameOfLife()
    {
        int currentNeighbours = 0;
        for (int x = 1; x < m_gridSize - 1; x++)
        {
            for (int y = 1; y < m_gridSize - 1; y++)
            {

                currentNeighbours = CheckNeighbour(x, y);
                if (currentNeighbours < 2) { m_grid[x, y] = 1; } // Less than 2 neighbours dies.
                else if (currentNeighbours > 3) { m_grid[x, y] = 1; } // 4 or more dies.
                else if (currentNeighbours == 3) { m_grid[x, y] = 0; } // 2 or 3 lives/creates





            }
        }
        Draw();
    }

    void CaveGame()
    {
        int currentNeighbours = 0;
        for (int x = 0; x < m_gridSize; x++)
        {
            for (int y = 0; y < m_gridSize; y++)
            {

                currentNeighbours = CheckNeighbour(x, y);
                if (currentNeighbours > 4) { m_grid[x, y] = 1; }
                else if (currentNeighbours < 4) { m_grid[x, y] = 0; } // 





            }

        }
        Draw();
    }

    IEnumerator CellularAutomata()
    {
        while (true)
        {

            WolframGameOfLife();
            yield return new WaitForSeconds(0.1f);

        }
    }

    void TextureGrid()
    {
        for (int x = 0; x < m_gridSize; x++)
        {
            for (int y = 0; y < m_gridSize; y++)
            {
                if (m_grid[x, y] == 1)
                {
                    if (m_grid[x, (y - 1)] == 0) { m_grid[x, y] = 2; }
                    else if (m_grid[x, (y + 1)] == 0) { m_grid[x, y] = 3; }
                }
            }
        }

    }

    void InstantiateGrid()
    {
        Vector3 gridPos = new Vector3(0, 0, 0);
        for (int i = 0; i < m_gridSize; i++)
        {
            for (int j = 0; j < m_gridSize; j++)
            {
                gridPos.Set(i, 0, j);
                int type = m_grid[i, j];
                switch (type)
                {
                    case 0:
                        break;
                    case 1:
                        Instantiate(m_stonePrefab, gridPos, Quaternion.identity);
                        break;
                    case 2:
                        Instantiate(m_grassPrefab, gridPos, Quaternion.identity);
                        break;
                    case 3:
                        Instantiate(m_spikePrefab, gridPos, Quaternion.identity);
                        break;
                    default:
                        break;
                }
            }
        }
    }

    void CreateWorld()
    {
        TextureGrid();
        InstantiateGrid();
    }

    void MarchingSquares()
    {
        for (int x = 0; x < m_gridSize - 1; x++)
        {
            for (int y = 0; y < m_gridSize - 1; y++)
            {
                PlaceCell(x, y, GetMarch(x, y));
            }
        }
    }

    int GetMarch(int x, int y)
    {
        int marchVal = 0;
        if (m_grid[x, y] > 0) { marchVal |= 4; }
        if (m_grid[x + 1, y] >0) { marchVal |= 8; }
        if (m_grid[x, y + 1] > 0) { marchVal |= 2; }
        if (m_grid[x + 1, y + 1] > 0) { marchVal |= 1; }


        return marchVal;
    }

    void PlaceCell(int x, int y, int marchVal)
    {
        Vector3 gridPos = new Vector3(x, 0, y);
        
        Texture2D texture = new Texture2D(32, 32);
        var pixels = m_sprites[marchVal].texture.GetPixels((int)m_sprites[marchVal].textureRect.x, (int)m_sprites[marchVal].textureRect.y, 32, 32);
        texture.SetPixels(pixels);
        texture.Apply();
        GameObject instance = Instantiate(m_marchPrefab, gridPos, Quaternion.identity);
        MeshRenderer renderer = instance.GetComponent<MeshRenderer>();
        renderer.material.mainTexture = texture;
    }
}
