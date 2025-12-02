using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapGeneration : MonoBehaviour
{
    //Public, Serialized Variables
    //Ints
    [SerializeField] int m_seed;

    [Header("BSP Algorithm Variables")]
    [SerializeField] int m_maxDepth = 4;
    [SerializeField] int m_minLeafSize = 10;
    [SerializeField] int m_borderSize = 5;
    //Vectors
    [Header("BSP ,Sizes")]
    [SerializeField] Vector2Int m_mapSize = new Vector2Int(100,100);
    [SerializeField] Vector2Int m_minRoomSize = new Vector2Int(5, 5);
    [SerializeField] Vector2Int m_maxRoomSize = new Vector2Int(20, 20);
    //Materials
    [Header("Prefab Requirements")]
    [SerializeField] Material m_floorMaterial;
    //Prefabs
    [SerializeField] GameObject m_floorPrefab;
    [SerializeField] GameObject m_wallPrefab;
    [SerializeField] GameObject m_floorParent;
    [SerializeField] GameObject m_wallParent;

    //Private Variables
    //System
    int[,] m_tileGrid;


    //Lists
    [SerializeField] Node rootNode;
    List<Node> m_leaves = new List<Node>();
    List<RectInt> m_rooms = new List<RectInt>();
    List<RectInt> m_corridors = new List<RectInt>();
    List<Edge> m_horizWalls = new List<Edge>();
    List<Edge> m_vertWalls = new List<Edge>();

    private void Start()
    {
        GameManager.m_random = new System.Random();
    }

    void Generate() 
    {
        ClearTiles(); //Clear Previous Tiles
        RectInt root = new RectInt (m_borderSize, m_borderSize, Mathf.Max(1, (m_mapSize.x - (m_borderSize * 2))), Mathf.Max(1, (m_mapSize.y - (m_borderSize * 2))));
      
        rootNode = new Node(root);
        SplitRecursive(rootNode, 0);

        GetLeaves(rootNode);

        foreach (Node leaf in m_leaves)
        {
            RectInt roomRoot = leaf.GetRect();
            RectInt room = CreateRoom(roomRoot);
            leaf.SetRoom(room);
            m_rooms.Add(room);
        }
        
        ConnectTree(rootNode);
        RasterizeGrid();
        DefineWalls();
        InstantiateGrid(m_tileGrid);
        InstantiateWalls();
        
    }

    //All work to do with Instantiating/Rasterising the grid.
    #region GridWork
    void ClearTiles() 
    {
        foreach (GameObject g in m_floorParent.transform)
        {
            Destroy(g);
        }
        foreach (GameObject g in m_wallParent.transform)
        {
            Destroy(g);
        }
        m_tileGrid = new int[m_mapSize.x, m_mapSize.y];
    }
    
    void RasterizeGrid() 
    {
        foreach (RectInt room in m_rooms)
        {
            for (int x = room.xMin; x < room.xMax+1; x++)
            {
                for (int y = room.yMin; y < room.yMax+1; y++)
                {
                    m_tileGrid[x, y] = 1;
                }
            }
        }

        foreach (RectInt corridor in m_corridors)
        {
            for (int x = corridor.xMin; x < corridor.xMax; x++)
            {
                for (int y = corridor.yMin; y < corridor.yMax; y++)
                {
                    m_tileGrid[x, y] = 1;
                }
            }
        }
    }

    void InstantiateGrid(int[,] grid) 
    {
        Vector3 position = Vector3.zero;
        for (int x = 0; x < grid.GetLength(0); x++) 
        { 
            for (int y = 0; y < grid.GetLength(1); y++) 
            {
                if (grid[x, y] == 1) 
                {
                    position = new Vector3(x, 0, y);
                    GameObject newFloor = m_floorPrefab;
                    newFloor.transform.localScale = new Vector3(1, 1, 1);
                    Instantiate(m_floorPrefab, position, Quaternion.identity, m_floorParent.transform);
                }
            }
        }
    }
    #endregion
    void InstantiateWalls()
    {
        Vector3 placePos = Vector3.zero;
        GameObject newWall = m_wallPrefab;
        foreach (Edge wall in m_horizWalls) 
        {

            placePos = new Vector3((wall.m_start.x + (wall.m_length / 2)), 1, (wall.m_start.y));
            newWall.transform.localScale = new Vector3(wall.m_length + 1, 1, 1);
            if (wall.m_corridor)
            {
                placePos.z -= 1;
                Instantiate(newWall, placePos, Quaternion.identity, m_wallParent.transform);

                placePos.z += 2;
                Instantiate(newWall, placePos, Quaternion.identity, m_wallParent.transform);
            }
            else 
            {
                placePos.z += wall.m_axis;
                Instantiate(newWall, placePos, Quaternion.identity, m_wallParent.transform);
            }
        }
        foreach (Edge wall in m_vertWalls)
        {

            placePos = new Vector3((wall.m_start.x), 1, (wall.m_start.y + (wall.m_length / 2)));
            newWall.transform.localScale = new Vector3(1, 1, wall.m_length + 1);
            if (wall.m_corridor)
            {
                placePos.x -= 1;
                Instantiate(newWall, placePos, Quaternion.identity, m_wallParent.transform);

                placePos.x += 2;
                Instantiate(newWall, placePos, Quaternion.identity, m_wallParent.transform);
            }
            else
            {

                placePos.x += wall.m_axis;
                Instantiate(newWall, placePos, Quaternion.identity, m_wallParent.transform);
            }
        }
    }

    //General BSP algorithm, including getting leaves, creating rooms and connecting the tree.
    #region BSPLeaves and Rooms
    void SplitRecursive(Node node, int depth) 
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

        Node left = new Node(childRects[0]);
        Node right = new Node(childRects[1]);
        node.SetLeft(left);
        node.SetRight(right);

        SplitRecursive(left, depth+1);
        SplitRecursive(right, depth+1);

    }

    void GetLeaves(Node node) 
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

    void ConnectTree(Node node)
    {
        if (node == null || node.IsLeaf())
        {
            return;
        }

        Node leftNode = node.GetLeft();
        Node rightNode = node.GetRight();
        ConnectTree(leftNode);
        ConnectTree(rightNode);
        
        var leftRoom = leftNode.GetRoom();
        var rightRoom = rightNode.GetRoom();
        if (leftRoom != null && rightRoom != null) 
        {
            Vector2Int a = FindCenter(leftRoom.Value);
            Vector2Int b = FindCenter(rightRoom.Value);

            Vector2Int midPoint = new Vector2Int(b.x, a.y);

            CreateCorridor(a, midPoint);
            CreateCorridor(midPoint, b);
        }
        
    }

    void CreateCorridor(Vector2Int from, Vector2Int to) 
    {
        int corridorWidth = 1;
        if (from.y == to.y) 
        {
            int x = Mathf.Min(from.x, to.x);
            int w = Mathf.Abs(from.x - to.x) + 1;
            var rect = new RectInt(x - corridorWidth / 2, from.y - corridorWidth / 2, w, corridorWidth);
            m_corridors.Add(rect);
            return;
        }
        if (from.x == to.x)
        {
            int y = Mathf.Min(from.y, to.y);
            int w = Mathf.Abs(from.y - to.y) + 1;
            var rect = new RectInt(from.x - corridorWidth / 2, y - corridorWidth / 2, corridorWidth, w);
            m_corridors.Add(rect);
            return;
        }
    }
    #endregion 

    #region Walls
    void DefineWalls() 
    {
        Edge newEdge;
        bool[] neighbours = new bool[4];
        bool newWall = false;
        Vector2Int currentLocation = Vector2Int.zero;
        for (int x = 0; x < m_mapSize.x; x++)
        {
            for (int y = 0; y < m_mapSize.y; y++)
            {
                newEdge = new Edge();
                currentLocation.x = x;
                currentLocation.y = y;
                neighbours = CheckNeighbours(currentLocation);
                if (m_tileGrid[x, y] == 1 && ((neighbours[0] ^ neighbours[1]) || (neighbours[0] && neighbours[1]))) //IF grid is 1, and either only 1 or both are empty.
                {
                    if (m_horizWalls.Count < 1) 
                    {
                        m_horizWalls.Add(CreateWall(newEdge, currentLocation, neighbours, false));
                    } //If no walls, add wall. This will be added to
                    else
                    {
                        for (int i = 0; i < m_horizWalls.Count; i++)
                        {
                            if (currentLocation.y == m_horizWalls[i].m_start.y && currentLocation.x == (m_horizWalls[i].m_end.x + 1))
                            {
                                if ((!m_horizWalls[i].m_corridor) & (neighbours[0] & neighbours[1])) //If not a corridor and goes into a corridor, break to make a corridor instead.
                                {
                                    newWall = true; 
                                    break;
                                }
                                else if ((m_horizWalls[i].m_corridor) & (neighbours[0] ^ neighbours[1])) //If a corridor, and goes out of a corridor, make a new wall instead.
                                {
                                    newWall = true;
                                    break;
                                }
                                else
                                {
                                    newEdge = m_horizWalls[i];
                                    newEdge.m_length += 1;
                                    newEdge.m_end = currentLocation;
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
                if (m_tileGrid[x,y] == 1 && ((neighbours[2] ^ neighbours[3]) || (neighbours[2] && neighbours[3]))) 
                {
                    if (m_vertWalls.Count < 1)
                    {
                        m_vertWalls.Add(CreateWall(newEdge, currentLocation, neighbours, true));
                    } //If no walls, add wall. This will be added to
                    else
                    {
                        for (int i = 0; i < m_vertWalls.Count; i++)
                        {
                            if (currentLocation.x == m_vertWalls[i].m_start.x && currentLocation.y == (m_vertWalls[i].m_end.y + 1))
                            {
                                if ((!m_vertWalls[i].m_corridor) & (neighbours[2] & neighbours[3])) //If not a corridor and goes into a corridor, break to make a corridor instead.
                                { 
                                    newWall = true; break; 
                                }
                                else if ((m_vertWalls[i].m_corridor) & (neighbours[2] ^ neighbours[3]))
                                {
                                    newWall = true;
                                    break;
                                }
                                else
                                {
                                    newEdge = m_vertWalls[i];
                                    newEdge.m_length += 1;
                                    newEdge.m_end = currentLocation;
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
    Edge CreateWall(Edge newEdge, Vector2Int currentLocation, bool[] neighbours, bool isVert) //Common, repeated code.
    {
        
        int index = 0;
        if (isVert) { index = 2; }
        Debug.Log($"Wall at {currentLocation} has {neighbours[index]} on North/West, and {neighbours[index]} on South/East");
        newEdge.m_start = currentLocation;
        newEdge.m_end = currentLocation;
        newEdge.m_length = 0;
        if ((neighbours[index] & !neighbours[index + 1]))
        {
            newEdge.m_axis = -1f;
            newEdge.m_corridor = false;
        }
        else if ((!neighbours[index] & neighbours[index + 1]))
        {
            newEdge.m_axis = 1f;
            newEdge.m_corridor = false;
        }
        else if ((neighbours[index] & neighbours[index + 1]))
        {
            newEdge.m_axis = 0f;
            newEdge.m_corridor = true;
            Debug.Log($"Wall at {currentLocation} is a corridor.");
        }
        return newEdge;
    } 
    bool[] CheckNeighbours(Vector2Int Location) 
    {
        bool northEmpty = false, southEmpty = false, westEmpty = false, eastEmpty = false;
        if (m_mapSize.y > Location.y && Location.y >= 0 && m_mapSize.x > Location.x && Location.x >= 0) 
        {
            if (Location.y - 1 < 0) { northEmpty = true; }
            else if (m_tileGrid[Location.x, Location.y - 1] == 0) { northEmpty = true; }
            if (Location.y + 1 >= m_mapSize.y) { southEmpty = true; }
            else if (m_tileGrid[Location.x, Location.y + 1] == 0) { southEmpty = true; }
            if (Location.x - 1 < 0) { westEmpty = true; }
            else if (m_tileGrid[Location.x - 1, Location.y] == 0) { westEmpty = true; }
            if (Location.x + 1 >= m_mapSize.x) { eastEmpty = true; }
            else if (m_tileGrid[Location.x + 1, Location.y] == 0) { eastEmpty = true; }
        }
        return new bool[4] { northEmpty, southEmpty, westEmpty, eastEmpty };
    }
    #endregion
}

[System.Serializable]
class BSPNode
{
    RectInt m_rect;
    RectInt? m_room;
    Node m_left, m_right;

    public BSPNode(RectInt rect)
    { 
        m_rect = rect;
    }
    
    //GETTERS
    public RectInt GetRect() { return m_rect; }
    public Node GetLeft() { return m_left; }
    public Node GetRight() { return m_right; }
    public RectInt? GetRoom() 
    {
        if (m_room.HasValue)
        { return m_room.Value; }
        else
        {
            RectInt? leftResult = m_left.GetRoom();
            if (leftResult.HasValue) { return leftResult; }

            RectInt? rightResult = m_right.GetRoom();
            return rightResult;

        }
    }
    //SETTERS
    public void SetLeft( Node left) 
    {
        m_left = left;
    }

    public void SetRight( Node right) 
    { 
        m_right = right;
    }
    public void SetRoom(RectInt room) 
    {
        m_room = room;
    }

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
public struct Wall 
{
    public Vector2 m_start;
    public Vector2 m_end;
    public float m_length;
    public bool m_corridor;
    public float m_axis;
}