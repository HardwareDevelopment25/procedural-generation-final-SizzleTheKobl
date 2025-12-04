using JetBrains.Annotations;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapGeneration : MonoBehaviour
{
    //Ints

    int m_maxDepth = 4;
    int m_minLeafSize = 10;
    //CellularAutomata
    bool m_generateCave;
    int[,] automataGrid;
    float m_percFill = 0.6f;
    int m_iterations = 2;
    //BSP
    bool m_generateDungeon;
    Vector2Int m_borderSize = new Vector2Int(3, 3);
    Vector2Int m_dungeonSize = new Vector2Int(50, 50);
    Vector2Int m_minRoomSize = new Vector2Int(5, 5);
    Vector2Int m_maxRoomSize = new Vector2Int(20, 20); //used for centralizing the dungeon.
    Vector2Int m_caveBorder = new Vector2Int(0, 0);
    //Requirements.
    [SerializeField] TileType[] m_tileTypes; //should allow for editor work


    //Private Variables
    //System
    public UnityEvent Generating;
    public UnityEvent DoneGenerating;
    //Grids
    Tile[,] m_bspGrid;
    Tile[,] m_combinedGrid;


    //Lists
    [SerializeField] BSPNode rootNode;
    List<BSPNode> m_leaves = new List<BSPNode>();
    List<Room> m_rooms = new List<Room>();
    List<Wall> m_corridors = new List<Wall>();
    List<Wall> m_horizWalls = new List<Wall>();
    List<Wall> m_vertWalls = new List<Wall>();

    private void Start()
    {

    }

    //SET
    #region public functions for var setting
    public void SetMapX(Slider xSlider)
    {
        m_dungeonSize.x = (int)xSlider.value;
        if (m_dungeonSize.x > GameManager.m_mapX)
        {
            m_dungeonSize.x = GameManager.m_mapX;
            xSlider.value = m_dungeonSize.x;
        }
    }
    public void SetMapY(Slider ySlider)
    {
        m_dungeonSize.y = (int)ySlider.value;
        if (m_dungeonSize.y > GameManager.m_mapX)
        {
            m_dungeonSize.y = GameManager.m_mapY;
            ySlider.value = m_dungeonSize.y;
        }
    }
    public void SetMinLeaf(Slider leafSlider)
    {
        m_minLeafSize = (int)leafSlider.value;
    }
    public void SetMinX(Slider xSlider)
    {
        int x = (int)xSlider.value;
        m_minRoomSize.x = x;
    }
    public void SetMaxX(Slider xSlider)
    {
        int x = (int)xSlider.value;
        m_maxRoomSize.x = x;
    }
    public void SetMinY(Slider ySlider)
    {
        int y = (int)ySlider.value;
        m_minRoomSize.y = y;
    }
    public void SetMaxY(Slider ySlider)
    {
        int y = (int)ySlider.value;
        m_maxRoomSize.y = y;
    }
    public void SetMaxDepth(Slider depthSlider)
    {
        m_maxDepth = (int)depthSlider.value;
    }

    public void SetDungeon(bool dungeon) 
    {
        m_generateDungeon = dungeon;
    }

    public void SetCave(bool cave) 
    {
        m_generateCave = cave;
    }
    #endregion
    public void Generate()
    {
        StartCoroutine(GenerateMap());
    }

    IEnumerator GenerateMap() 
    {
        Generating.Invoke();
        GameManager.SetSeed(GameManager.m_currentSeed); //This ensures every single generation of a certain seed will remain the same.
        ClearTiles();
        yield return new WaitForSeconds(0.1f);
        if (m_generateCave)
        { GenerateCave(); }
        yield return new WaitForSeconds(0.1f);
        if (m_generateDungeon) 
        { GenerateBSP(); }
        yield return new WaitForSeconds(0.1f);
        CombineGrids();
        yield return new WaitForSeconds(0.1f);
        InstantiateGrid(m_combinedGrid);
        yield return new WaitForSeconds(0.1f);
        MergeMeshes();
        DoneGenerating.Invoke();

    }
    #region Cellular Automata
    void GenerateCave()
    {
        for (int x = 0; x < GameManager.m_mapX; x++)
        {
            for (int y = 0; y < GameManager.m_mapY; y++)
            {
                float randomNo = (float)GameManager.m_random.NextDouble();
                if (randomNo > m_percFill) { automataGrid[x, y] = 0; }
                else { automataGrid[x, y] = 1; }
            }
        }
        for (int i = 0; i < (m_iterations + 1); i++)
        {
            CaveGame();
        }
    }

    int CANeighbours(int x, int y)
    {
        int neighbours = 0;
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                if ((x + i > 0) && ((x + i) < (GameManager.m_mapX)))
                {
                    if ((y + j > 0) && ((y + j) < (GameManager.m_mapY)))
                    {
                        if (automataGrid[(x + i), (y + j)] == 1) { neighbours++; }
                    }

                }
            }
        }
        if (automataGrid[x, y] == 1) { neighbours--; };

        return neighbours;
    }
    void CaveGame()
    {
        int currentNeighbours = 0;
        for (int x = 0; x < GameManager.m_mapX; x++)
        {
            for (int y = 0; y < GameManager.m_mapY; y++)
            {
                currentNeighbours = CANeighbours(x, y);
                if (currentNeighbours > 4) { automataGrid[x, y] = 1; }
                else if (currentNeighbours < 4) { automataGrid[x, y] = 0; }
            }
        }
    }
    #endregion
    void GenerateBSP() 
    {
        m_caveBorder.x = (GameManager.m_mapX - m_dungeonSize.x) / 2;
        m_caveBorder.y = (GameManager.m_mapY - m_dungeonSize.y) / 2;
         //Clear Previous Tiles
        RectInt root = new RectInt (m_borderSize.x, m_borderSize.y, Mathf.Max(1, (m_dungeonSize.x - (m_borderSize.x * 2))), Mathf.Max(1, (m_dungeonSize.y - (m_borderSize.y * 2))));
      
        rootNode = new BSPNode(root);
        SplitRecursive(rootNode, 0);

        GetLeaves(rootNode);

        foreach (BSPNode leaf in m_leaves)
        {
            RectInt roomRoot = leaf.GetRect();
            RectInt room = CreateRoom(roomRoot);
            int roomID = 0; //This may be used to determine a room ID after MVP
            leaf.SetRoom(room, roomID);
            leaf.GetRoom().SetTiles(m_tileTypes[3]);
            m_rooms.Add(leaf.GetRoom());

        }
        
        ConnectTree(rootNode);
        RasterizeBSPGrid();
        DefineWalls();
        RasterizeWalls();
    }

    //All work to do with Instantiating/Rasterising the grid.
    #region GridWork
    public bool ClearTiles() 
    {
        m_leaves.Clear();
        m_rooms.Clear();
        m_corridors.Clear();
        m_horizWalls.Clear();
        m_vertWalls.Clear();
        for (int i = 0; i < m_tileTypes.Length; i++)
        {

            GameObject parent = m_tileTypes[i].parent;

            for (int j = parent.transform.childCount - 1; j >= 0; j--)
            {
                GameObject toDelete = parent.transform.GetChild(j).gameObject;
                DestroyImmediate(toDelete);
            }
            parent.GetComponent<MeshFilter>().sharedMesh = new Mesh();
        }
        m_bspGrid = new Tile[m_dungeonSize.x, m_dungeonSize.y];
        for (int x = 0;  x < m_dungeonSize.x; x++) 
        {
            for (int y = 0; y < m_dungeonSize.y; y++)
            {
                m_bspGrid[x, y] = new Tile(m_tileTypes[0], Vector3.zero);
            }
        }
        m_combinedGrid = new Tile[GameManager.m_mapX, GameManager.m_mapY];
        for (int x = 0; x < GameManager.m_mapX; x++)
        {
            for (int y = 0; y < GameManager.m_mapY; y++)
            {
                m_combinedGrid[x, y] = new Tile(m_tileTypes[0], Vector3.zero);
            }
        }

        automataGrid = new int[GameManager.m_mapX, GameManager.m_mapY];
        for (int x = 0; x < GameManager.m_mapX; x++)
        {
            for (int y = 0; y < GameManager.m_mapY; y++)
            {
                automataGrid[x, y] = 0;
            }
        }
        return true;
    }
    
    void RasterizeBSPGrid() 
    {
        foreach (Room room in m_rooms)
        {
            RectInt rect = room.GetRect();
            Tile[,] tiles = room.GetTiles();
            for (int x = rect.xMin; x < rect.xMax; x++)
            {
                for (int y = rect.yMin; y < rect.yMax; y++)
                {
                    m_bspGrid[x, y] = tiles[(x - rect.xMin), (y - rect.yMin)];
                }
            }
        }

        foreach (Wall corridor in m_corridors)
        {
            if (corridor.start.y == corridor.end.y) 
            {
                for (int x = (int)corridor.start.x; x <= (int)corridor.end.x; x++)
                {
                    m_bspGrid[x, (int)corridor.start.y] = new Tile(m_tileTypes[3], new Vector3((x), 0, (corridor.start.y)));
                }
            }
            else 
            {
                for (int y = (int)corridor.start.y; y <= (int)corridor.end.y; y++)
                {
                    m_bspGrid[(int)corridor.start.x, y] = new Tile(m_tileTypes[3], new Vector3((corridor.start.x), 0, (y)));
                }
            }
        }
    }

    void RasterizeWalls() 
    {
        foreach (Wall wall in m_horizWalls)
        {
            if (wall.isCorridor)
            {
                for (int x = (int)wall.start.x; x <= (int)wall.end.x; x++)
                {
                    m_bspGrid[x, (int)(wall.start.y - 1)] = new Tile(m_tileTypes[4], new Vector3((x), 1, (wall.start.y - 1)));
                }
                for (int x = (int)wall.start.x; x <= (int)wall.end.x; x++)
                {
                    m_bspGrid[x, (int)(wall.start.y + 1)] = new Tile(m_tileTypes[4], new Vector3((x), 1, (wall.start.y + 1)));
                }
            }
            else
            {
                if (CheckNeighbours(new Vector2Int((int)wall.start.x, (int)wall.start.y), "Dungeon Floor")[2] == true)
                {
                    m_bspGrid[(int)wall.start.x - 1, (int)(wall.start.y + wall.axis)] = new Tile(m_tileTypes[4], new Vector3(wall.start.x - 1, 1, (wall.start.y + wall.axis)));

                }
                if (CheckNeighbours(new Vector2Int((int)wall.end.x, (int)wall.end.y), "Dungeon Floor")[3] == true)
                {
                    m_bspGrid[(int)wall.end.x + 1, (int)(wall.end.y + wall.axis)] = new Tile(m_tileTypes[4], new Vector3(wall.end.x + 1, 1, (wall.start.y + wall.axis)));
                    Debug.Log(m_bspGrid[(int)wall.end.x + 1, (int)(wall.end.y + wall.axis)].CheckFor("Dungeon Wall"));
                }
                for (int x = (int)wall.start.x; x <= (int)wall.end.x; x++)
                {
                    m_bspGrid[x, (int)(wall.start.y + wall.axis)] = new Tile(m_tileTypes[4], new Vector3((x), 1, (wall.start.y + wall.axis)));
                }
            }
        }
        foreach (Wall wall in m_vertWalls) 
        {
            if (wall.isCorridor)
            {
                for (int y = (int)wall.start.y; y <= wall.end.y; y++)
                {
                    m_bspGrid[(int)(wall.start.x + 1), y] = new Tile(m_tileTypes[4], new Vector3((wall.start.x + 1), 1, (y)));
                }
                for (int y = (int)wall.start.y; y <= wall.end.y; y++)
                {
                    m_bspGrid[(int)(wall.start.x - 1), y] = new Tile(m_tileTypes[4], new Vector3((wall.start.x - 1), 1, (y)));
                }
            }
            else 
            {
                for (int y = (int)wall.start.y; y <= wall.end.y; y++)
                {
                    m_bspGrid[(int)(wall.start.x + wall.axis), y] = new Tile(m_tileTypes[4], new Vector3((wall.start.x + wall.axis), 1, (y)));
                }
            }
        }
    }

    void CombineGrids()
    {
        m_combinedGrid = new Tile[GameManager.m_mapX, GameManager.m_mapY];
        if (m_generateCave)
        {
            for (int x = 0; x < GameManager.m_mapX; x++) //Cellular Automata
            {
                for (int y = 0; y < GameManager.m_mapY; y++)
                {
                    if (automataGrid[x, y] == 0)
                    {
                        m_combinedGrid[x, y] = new Tile(m_tileTypes[1], new Vector3(x, 0, y));
                    }
                    else
                    {
                        m_combinedGrid[x, y] = new Tile(m_tileTypes[2], new Vector3(x, 1, y));
                    }
                }
            }
        }
        else 
        {
            for (int x = 0; x < GameManager.m_mapX; x++) //Cellular Automata
            {
                for (int y = 0; y < GameManager.m_mapY; y++)
                {
                    m_combinedGrid[x, y] = new Tile(m_tileTypes[0], Vector3.zero;
                }
            }
        }
        if (m_generateDungeon)
        {
            for (int x = 0; x < m_dungeonSize.x; x++)
            {
                for (int y = 0; y < m_dungeonSize.y; y++)
                {
                    if (m_bspGrid[x, y].GetID() != 0)
                    {
                        if (!(automataGrid[x + m_caveBorder.x, y + m_caveBorder.y] == 0 && m_bspGrid[x, y].CheckFor("Dungeon Wall")))
                        {
                            Vector3 location = m_bspGrid[x, y].GetPosition();
                            location.x = location.x + (m_caveBorder.x);
                            location.z = location.z + (m_caveBorder.y);
                            m_bspGrid[x, y].SetLocation(location);
                            m_combinedGrid[(x + m_caveBorder.x), (y + m_caveBorder.y)] = m_bspGrid[x, y];
                        }
                    }

                }
            }
        }
    }
    void InstantiateGrid(Tile[,] grid) 
    {
        for (int x = 0; x < grid.GetLength(0); x++) 
        { 
            for (int y = 0; y < grid.GetLength(1); y++) 
            {
                if (grid[x, y].GetID() != 0) 
                {
                    Tile tile = grid[x, y];
                    Material material = tile.GetMaterial();
                    GameObject newTile = Instantiate(tile.GetPrefab(), tile.GetPosition(), Quaternion.identity, tile.GetParent());
                    newTile.GetComponent<Renderer>().material = material;
                }
            }
        }
    }

    void MergeMeshes() 
    { 
        foreach (TileType tileType in m_tileTypes)
        {
            if (tileType.parent.transform.childCount > 0)
            {
                tileType.parent.GetComponent<MeshFilter>().sharedMesh = new Mesh();
                CombineInstance[] instances = new CombineInstance[tileType.parent.transform.childCount];
                for (int i = 0; i < tileType.parent.transform.childCount; i++)
                {
                    Transform child = tileType.parent.transform.GetChild(i);
                    MeshFilter meshFilter = child.GetComponent<MeshFilter>();
                    instances[i].mesh = meshFilter.sharedMesh;
                    instances[i].transform = child.localToWorldMatrix;
                    meshFilter.gameObject.SetActive(false);
                }

                Mesh combinedMesh = new Mesh();
                combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                combinedMesh.CombineMeshes(instances);
                tileType.parent.GetComponent<MeshFilter>().sharedMesh = combinedMesh;
                tileType.parent.GetComponent<MeshRenderer>().material = tileType.material;
                tileType.parent.SetActive(true);
            }
            
        }
    }

    #endregion
    //General BSP algorithm, including getting leaves, creating rooms and connecting the tree.
    #region BSPLeaves and Rooms
    void SplitRecursive(BSPNode node, int depth) 
    {
        int splitLine;
        char orientation;
        RectInt nodeRect = node.GetRect();

        if (depth >= m_maxDepth) 
        {
            return;
        }
        if (nodeRect.height <= (2 * m_minLeafSize) && nodeRect.width <= (2 * m_minLeafSize)) 
        {
            return;
        }
        if (nodeRect.height > nodeRect.width)
        { //Guarantees that leaves will be at least 10 on each side.
            orientation = 'y'; //To allow to split properly.
        }
        else 
        { //^^^^
            orientation = 'x';
        }
        RectInt[] childRects = new RectInt[2];
        switch (orientation) 
        {
            case 'x': //Width;

                int minX = nodeRect.xMin + m_minLeafSize;
                int maxX = nodeRect.xMax - m_minLeafSize;
                splitLine = GameManager.m_random.Next(minX, maxX);
                childRects[0] = new RectInt(nodeRect.x, nodeRect.y, (splitLine - nodeRect.x), nodeRect.height);
                childRects[1] = new RectInt(splitLine, nodeRect.y, (nodeRect.xMax-splitLine), nodeRect.height);
                break;
            case 'y': //Height
                int minY = nodeRect.yMin + m_minLeafSize;
                int maxY = nodeRect.yMax - m_minLeafSize;
                splitLine = GameManager.m_random.Next(minY, maxY);
                childRects[0] = new RectInt(nodeRect.x, nodeRect.y, nodeRect.width, (splitLine - nodeRect.y));
                childRects[1] = new RectInt(nodeRect.x, splitLine, nodeRect.width, (nodeRect.yMax - splitLine));
                break;
            default:
                //Shouldn't occur, but debug.
                Debug.Log("ERROR, NO ORIENTATION");
                break;
        }

        BSPNode left = new BSPNode(childRects[0]);
        BSPNode right = new BSPNode(childRects[1]);
        node.SetLeft(left);
        node.SetRight(right);

        SplitRecursive(left, depth+1);
        SplitRecursive(right, depth+1);

    }

    void GetLeaves(BSPNode node) 
    {
        if (node.IsLeaf()) 
        {
            m_leaves.Add(node);
            return; 
        }
        else 
        { 
            if (node.GetLeft() != null) { GetLeaves(node.GetLeft()); }
            if (node.GetRight() != null) { GetLeaves(node.GetRight()); }
        }
    }

    RectInt CreateRoom(RectInt leaf) 
    {
        int roomWidth, roomHeight, roomX, roomY;
        roomWidth = GameManager.m_random.Next(m_minRoomSize.x, m_maxRoomSize.x); 
        roomHeight = GameManager.m_random.Next(m_minRoomSize.y, m_maxRoomSize.y);
        roomWidth = Mathf.Min(roomWidth, (leaf.width - 4));
        roomHeight = Mathf.Min(roomHeight, (leaf.height - 4));
        roomX = GameManager.m_random.Next((leaf.x+2), (leaf.xMax - roomWidth));
        roomY = GameManager.m_random.Next((leaf.y+2), (leaf.yMax - roomHeight));
        return new RectInt(roomX, roomY, roomWidth, roomHeight);
    }

    Vector2Int FindCenter(RectInt Room)
    {
        return new Vector2Int(Mathf.RoundToInt(Room.center.x), Mathf.RoundToInt(Room.center.y));
    }

    void ConnectTree(BSPNode node)
    {
        if (node == null || node.IsLeaf())
        {
            return;
        }

        BSPNode leftNode = node.GetLeft();
        BSPNode rightNode = node.GetRight();
        ConnectTree(leftNode);
        ConnectTree(rightNode);
        
        var leftRoom = leftNode.GetRoom();
        var rightRoom = rightNode.GetRoom();
        if (leftRoom != null && rightRoom != null) 
        {
            Vector2Int a = FindCenter(leftRoom.GetRect());
            Vector2Int b = FindCenter(rightRoom.GetRect());

            Vector2Int midPoint = new Vector2Int(b.x, a.y);

            CreateCorridor(a, midPoint);
            CreateCorridor(midPoint, b);
        }
        
    }

    void CreateCorridor(Vector2Int from, Vector2Int to) 
    {

        Vector2 start;
        Vector2 end;
        Wall newCorridor = new Wall();
        int length;
        if (from.y == to.y) 
        {
            if (from.x < to.x) 
            {
                start = new Vector2(from.x, from.y);
                end = new Vector2(to.x, to.y);
                length = (from.x - to.x);
            }
            else 
            {
                start = new Vector2(to.x, to.y);
                end = new Vector2(from.x, from.y);
                length = (to.x - from.x);
            }
            newCorridor.length = length;
            newCorridor.start = start;
            newCorridor.end = end;
            m_corridors.Add(newCorridor);
            return;
        }
        if (from.x == to.x)
        {
            if (from.y < to.y)
            {
                start = new Vector2(from.x, from.y);
                end = new Vector2(to.x, to.y);
                length = (from.y - to.y);
            }
            else
            {
                start = new Vector2(to.x, to.y);
                end = new Vector2(from.x, from.y);
                length = (to.y - from.y);
            }
            newCorridor.length = length;
            newCorridor.start = start;
            newCorridor.end = end;
            m_corridors.Add(newCorridor);
            return;
        }
    }
    #endregion 

    #region Walls
    void DefineWalls() 
    {
        Wall newEdge;
        bool[] neighbours = new bool[4];
        bool newWall = false;
        Vector2Int currentLocation = Vector2Int.zero;
        for (int x = 0; x < m_dungeonSize.x; x++)
        {
            for (int y = 0; y < m_dungeonSize.y; y++)
            {
                newEdge = new Wall();
                currentLocation.x = x;
                currentLocation.y = y;
                neighbours = CheckNeighbours(currentLocation, "Dungeon Floor");
                if (m_bspGrid[x, y].CheckFor("Dungeon Floor") && ((neighbours[0] ^ neighbours[1]) || (neighbours[0] && neighbours[1]))) //IF grid is 1, and either only 1 or both are empty.
                {
                    if (m_horizWalls.Count < 1) 
                    {
                        m_horizWalls.Add(CreateWall(newEdge, currentLocation, neighbours, false));
                    } //If no walls, add wall. This will be added to
                    else
                    {
                        for (int i = 0; i < m_horizWalls.Count; i++)
                        {
                            if (currentLocation.y == m_horizWalls[i].start.y && currentLocation.x == (m_horizWalls[i].end.x + 1))
                            {
                                if ((!m_horizWalls[i].isCorridor) & (neighbours[0] & neighbours[1])) //If not a corridor and goes into a corridor, break to make a corridor instead.
                                {
                                    newWall = true; 
                                    break;
                                }
                                else if ((m_horizWalls[i].isCorridor) & (neighbours[0] ^ neighbours[1])) //If a corridor, and goes out of a corridor, make a new wall instead.
                                {
                                    newWall = true;
                                    break;
                                }
                                else
                                {
                                    newEdge = m_horizWalls[i];
                                    newEdge.length += 1;
                                    newEdge.end = currentLocation;
                                    m_horizWalls[i] = newEdge;
                                    newWall = false;
                                    break;
                                }
                            }
                            else 
                            {
                                newWall = true;
                            }
                        }
                        if (newWall) 
                        {
                            m_horizWalls.Add(CreateWall(newEdge, currentLocation, neighbours, false));
                        }

                    }
                }
                if (m_bspGrid[x, y].CheckFor("Dungeon Floor") && ((neighbours[2] ^ neighbours[3]) || (neighbours[2] && neighbours[3]))) 
                {
                    if (m_vertWalls.Count < 1)
                    {
                        m_vertWalls.Add(CreateWall(newEdge, currentLocation, neighbours, true));
                    } //If no walls, add wall. This will be added to
                    else
                    {
                        for (int i = 0; i < m_vertWalls.Count; i++)
                        {
                            if (currentLocation.x == m_vertWalls[i].start.x && currentLocation.y == (m_vertWalls[i].end.y + 1))
                            {
                                if ((!m_vertWalls[i].isCorridor) & (neighbours[2] & neighbours[3])) //If not a corridor and goes into a corridor, break to make a corridor instead.
                                { 
                                    newWall = true; break; 
                                }
                                else if ((m_vertWalls[i].isCorridor) & (neighbours[2] ^ neighbours[3]))
                                {
                                    newWall = true;
                                    break;
                                }
                                else
                                {
                                    newEdge = m_vertWalls[i];
                                    newEdge.length += 1;
                                    newEdge.end = currentLocation;
                                    m_vertWalls[i] = newEdge;
                                    newWall = false;
                                    break;
                                }
                            }
                            else
                            {
                                newWall = true;
                            }
                        }
                        if (newWall)
                        {
                            m_vertWalls.Add(CreateWall(newEdge, currentLocation, neighbours, true));
                        }

                    }
                }
            }
        }
    }
    Wall CreateWall(Wall newEdge, Vector2Int currentLocation, bool[] neighbours, bool isVert) //Common, repeated code.
    {
        
        int index = 0;
        if (isVert) { index = 2; }
        newEdge.start = currentLocation;
        newEdge.end = currentLocation;
        newEdge.length = 0;
        if ((neighbours[index] & !neighbours[index + 1]))
        {
            newEdge.axis = -1f;
            newEdge.isCorridor = false;
        }
        else if ((!neighbours[index] & neighbours[index + 1]))
        {
            newEdge.axis = 1f;
            newEdge.isCorridor = false;
        }
        else if ((neighbours[index] & neighbours[index + 1]))
        {
            newEdge.axis = 0f;
            newEdge.isCorridor = true;
        }
        return newEdge;
    } 
    bool[] CheckNeighbours(Vector2Int Location, string checkFor) 
        //String checkfor lets the script check for different kinds of neighbours. For example, if i want to check for floors, i check for "Dungeon Floor"
        //For this reason, future dungeon floor textures must have "Dungeon Floor" at the start of their name.
    {
        bool northEmpty = false, southEmpty = false, westEmpty = false, eastEmpty = false;
        if (m_dungeonSize.y > Location.y && Location.y >= 0 && m_dungeonSize.x > Location.x && Location.x >= 0) 
        {
            if (Location.y - 1 < 0) { northEmpty = true; }
            else if (!m_bspGrid[Location.x, Location.y - 1].CheckFor(checkFor)) { northEmpty = true; }
            if (Location.y + 1 >= m_dungeonSize.y) { southEmpty = true; }
            else if (!m_bspGrid[Location.x, Location.y + 1].CheckFor(checkFor)) { southEmpty = true; }
            if (Location.x - 1 < 0) { westEmpty = true; }
            else if (!m_bspGrid[Location.x - 1, Location.y].CheckFor(checkFor)) { westEmpty = true; }
            if (Location.x + 1 >= m_dungeonSize.x) { eastEmpty = true; }
            else if (!m_bspGrid[Location.x + 1, Location.y].CheckFor(checkFor)) { eastEmpty = true; }
        }
        return new bool[4] { northEmpty, southEmpty, westEmpty, eastEmpty };
    }
    #endregion
}

[System.Serializable]
class BSPNode
{
    RectInt m_rect;
    Room m_room;
    BSPNode m_left, m_right;

    public BSPNode(RectInt rect)
    { 
        m_rect = rect;
    }

    #region GETTERS
    public RectInt GetRect() { return m_rect; }
    public BSPNode GetLeft() { return m_left; }
    public BSPNode GetRight() { return m_right; }
    public Room GetRoom() 
    {
        if (m_room != null)
        { return m_room; }
        else
        {
            Room leftResult = m_left.GetRoom();
            if (leftResult != null) { return leftResult; }

            Room rightResult = m_right.GetRoom();
            return rightResult;

        }
    }
    #endregion
    #region SETTERS
    public void SetLeft(BSPNode left) 
    {
        m_left = left;
    }

    public void SetRight(BSPNode right) 
    { 
        m_right = right;
    }
    public void SetRoom(RectInt room, int roomID) 
    {
        m_room = new Room(room, roomID);
    }
    #endregion
    //FUNCTIONALITY
    public bool IsLeaf()
    {
        if (m_left != null && m_right != null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

}

[System.Serializable]
class Room
{
    int m_roomID; //This is used to determine type of room. May be important later once MVP is done.
    RectInt m_rect;
    Tile[,] m_tiles;

    public Room(RectInt rect, int roomID) 
    {
        m_rect = rect;
        m_roomID = roomID;
    }
    #region GETTERS
    public RectInt GetRect() 
    {
        return m_rect;
    }
    public int GetID() 
    {
        return m_roomID;
    }
    public Tile[,] GetTiles() 
    {
        return m_tiles;
    }
    #endregion

    #region SETTERS
    public void SetRect(RectInt rect)
    {
        m_rect = rect;
    }

    public void SetID(int roomID) 
    {
        m_roomID = roomID;
    }
    #endregion
    #region FUNCTIONALITY
    public void SetTiles(TileType tileType) 
    {
        m_tiles = new Tile[m_rect.width, m_rect.height];
        for (int x = m_rect.xMin; x < m_rect.xMax; x++)
        {
            for (int y = m_rect.yMin; y < m_rect.yMax; y++)
            {
                Tile tile = new(tileType, new Vector3(x, 0, y));
                m_tiles[(x-m_rect.xMin), (y-m_rect.yMin)] = tile;
                
            }
        }
    }
    #endregion
}
class Tile //replacing the grid with a tile.
{
    TileType m_tileType;
    Vector3 m_location;

    public Tile(TileType tileType, Vector3 location) 
    { 
        m_tileType = tileType;
        m_location = location;
    }
    #region GETTERS
    public Transform GetParent() 
    {
        return m_tileType.parent.transform;
    }

    public Material GetMaterial() 
    {
        return m_tileType.material;
    }

    public GameObject GetPrefab() 
    {
        return m_tileType.prefab;
    }

    public int GetID() 
    {
        return m_tileType.tileID;
    }

    public Vector3 GetPosition() 
    {
        return m_location;
    }
    #endregion
    #region SETTERS
    public void SetLocation(Vector3 location) 
    {
        m_location = location;
    }
    #endregion
    public bool CheckFor(string name) 
    {
        if (m_tileType.tileName.Contains(name)) { return true; }
        else { return false; }
    }
}

#region STRUCTS
[System.Serializable]
public struct Wall
{
    public Vector2 start;
    public Vector2 end;
    public float length;
    public bool isCorridor;
    public float axis;
}

[System.Serializable]
public struct TileType 
{
    public string tileName;
    public GameObject prefab;
    public GameObject parent;
    public Material material;
    public int tileID;
}
#endregion