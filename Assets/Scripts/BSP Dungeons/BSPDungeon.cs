using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using TreeEditor;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class BSPDungeon : MonoBehaviour
{
    //Public, Serialized Variables
    //Ints
    [SerializeField] int m_seed; //Mostly for testing.
    [Header("BSP Algorithm Variables")]
    [SerializeField] int m_maxDepth = 4;
    [SerializeField] int m_minLeafSize = 10;
    [SerializeField] int m_borderSize = 5;
    //Vectors
    [Header("Sizes")]
    [SerializeField] Vector2Int m_mapSize = new Vector2Int(100,100);
    [SerializeField] Vector2Int m_minRoomSize = new Vector2Int(5, 5);
    [SerializeField] Vector2Int m_maxRoomSize = new Vector2Int(20, 20);
    //Materials
    [Header("Prefab Requirements")]
    [SerializeField] Material m_floorMaterial;
    [SerializeField] float m_tileSize = 1f;
    //Prefabs
    [SerializeField] GameObject m_floorPrefab;
    [SerializeField] GameObject m_floorParent;
    [SerializeField] GameObject m_wallParent;

    //Private Variables
    //System
    System.Random m_random = new System.Random();
    int[,] m_tileGrid;


    //Lists
    [SerializeField] Node rootNode;
    List<Node> m_leaves = new List<Node>();
    List<RectInt> m_rooms = new List<RectInt>();
    List<RectInt> m_corridors = new List<RectInt>();
    List<Vector2> m_edgeLocations = new List<Vector2>();
    List<Wall> m_northWalls = new List<Wall>();
    List<Wall> m_southWalls = new List<Wall>();
    List<Wall> m_westWalls = new List<Wall>();
    List<Wall> m_eastWalls = new List<Wall>();

    private void Start()
    {
        Generate();
    }

    void Generate() 
    {
        ClearTiles(); //Clear Previous Tiles
        if (m_seed != 0) //If seed specified, make random based on seed.
        { m_random = new System.Random(m_seed); }
        else { m_random = new System.Random(); } //If unspecified, create a new random.
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
        CreateWalls();
        InstantiateGrid(m_tileGrid);
        InstantiateWalls();
        
    }

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

    void InstantiateWalls() 
    { 
        Vector3 Position = Vector3.zero;
        foreach (Wall wall in m_northWalls) 
        {
            float xPos = (float)wall.m_length / 2 + wall.m_start.x;
            Position = new Vector3(xPos, 1, wall.m_start.y-1);
            GameObject newWall = m_floorPrefab;
            newWall.transform.localScale = new Vector3(wall.m_length, 1, 1);
            Instantiate(newWall, Position, Quaternion.identity, m_wallParent.transform);
        }

        foreach (Wall wall in m_eastWalls) 
        {

            float yPos = (float)wall.m_length / 2 + wall.m_start.y;
            Position = new Vector3(wall.m_start.x + 1, 1, yPos);
            GameObject newWall = m_floorPrefab;
            newWall.transform.localScale = new Vector3(1, 1, wall.m_length);
            Instantiate(newWall, Position, Quaternion.identity, m_wallParent.transform);
        }

        foreach (Wall wall in m_southWalls) 
        {

            float xPos = (float)wall.m_length / 2 + wall.m_start.x;
            Position = new Vector3(xPos, 1, wall.m_start.y + 1);
            GameObject newWall = m_floorPrefab;
            newWall.transform.localScale = new Vector3(wall.m_length, 1, 1);
            Instantiate(newWall, Position, Quaternion.identity, m_wallParent.transform);
        }

        foreach (Wall wall in m_westWalls) 
        {
            float yPos = (float)wall.m_length / 2 + wall.m_start.y;
            Position = new Vector3(wall.m_start.x - 1, 1, yPos);
            GameObject newWall = m_floorPrefab;
            newWall.transform.localScale = new Vector3(1, 1, wall.m_length);
            Instantiate(newWall, Position, Quaternion.identity, m_wallParent.transform);
        }
    }

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
                splitLine = m_random.Next(minX, maxX);
                childRects[0] = new RectInt(nodeRect.x, nodeRect.y, (splitLine - nodeRect.x), nodeRect.height);
                childRects[1] = new RectInt(splitLine, nodeRect.y, (nodeRect.xMax-splitLine), nodeRect.height);
                break;
            case 'y': //Height
                int minY = nodeRect.yMin + m_minLeafSize;
                int maxY = nodeRect.yMax - m_minLeafSize;
                splitLine = m_random.Next(minY, maxY);
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
        roomWidth = m_random.Next(m_minRoomSize.x, m_maxRoomSize.x); 
        roomHeight = m_random.Next(m_minRoomSize.y, m_maxRoomSize.y);
        roomWidth = Mathf.Min(roomWidth, (leaf.width - 2));
        roomHeight = Mathf.Min(roomHeight, (leaf.height - 2));
        roomX = m_random.Next((leaf.x+1), (leaf.xMax - roomWidth));
        roomY = m_random.Next((leaf.y+1), (leaf.yMax - roomHeight));
        return new RectInt(roomX, roomY, roomWidth, roomHeight);
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
        Debug.Log(from + " " + to);
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

    /*void CreateWalls() 
    {
        for (int x = 0; x < m_mapSize.x; x++)
        {
            for (int y = 0; y < m_mapSize.y; y++)
            {
                Vector2 edgeStart = new Vector2(x, y); //Shouldn't Change
                Vector2 edgeEnd = new Vector2(0, 0);
                Vector2 prevLoc = new Vector2(0, 0);
                Vector2 newLoc = new Vector2(x, y);
                if (!(m_edgeLocations.Contains(edgeStart)))
                {
                    bool[] neighbours = CheckNeighbours(edgeStart);
                    if (neighbours[0] == true)
                    {
                        newLoc = new Vector2(x, y);
                        while (m_tileGrid[(int)newLoc.x, (int)newLoc.y] == 1)
                        {
                            prevLoc = newLoc; //Previous Location
                            newLoc.x += 1;

                            neighbours = CheckNeighbours(newLoc);
                            if (neighbours[0] == false)
                            {
                                edgeEnd = prevLoc;
                                Wall newEdge = new Wall();
                                newEdge.SetParams(edgeStart, edgeEnd, 'H');
                                if (!m_northWalls.Contains(newEdge)) { m_northWalls.Add(newEdge); }
                                
                            }
                            else { m_edgeLocations.Add(prevLoc); }
                        } 
                    }
                    if (neighbours[1] == true)
                    {

                        newLoc = new Vector2(x, y);
                        while (m_tileGrid[(int)newLoc.x, (int)newLoc.y] == 1)
                        {
                            prevLoc = newLoc; //Previous Location
                            newLoc.x += 1;
                            neighbours = CheckNeighbours(newLoc);
                            if (neighbours[1] == false)
                            {
                                edgeEnd = prevLoc;
                                Wall newEdge = new Wall();
                                newEdge.SetParams(edgeStart, edgeEnd, 'H');
                                if (!m_southWalls.Contains(newEdge)) { m_southWalls.Add(newEdge); }
                            }
                            else 
                            {
                                m_edgeLocations.Add(newLoc); 
                            }
                        } 
                    }
                    if (neighbours[2] == true)
                    {

                        newLoc = new Vector2(x, y);
                        while (m_tileGrid[(int)newLoc.x, (int)newLoc.y] == 1)
                        {
                            prevLoc = newLoc; //Previous Location
                            newLoc.y += 1;
                            neighbours = CheckNeighbours(newLoc);
                            if (neighbours[2] == false) 
                            { 
                                edgeEnd = prevLoc; 
                                Wall newEdge = new Wall();
                                newEdge.SetParams(edgeStart, edgeEnd, 'V');
                                if (!m_westWalls.Contains(newEdge)) { m_westWalls.Add(newEdge); }
                            }
                            else { m_edgeLocations.Add(prevLoc); }
                        } 

                    }
                    if (neighbours[3] == true)
                    {

                        newLoc = new Vector2(x, y);
                        while (m_tileGrid[(int)newLoc.x, (int)newLoc.y] == 1)
                        {

                            prevLoc = newLoc; //Previous Location
                            newLoc.y += 1;
                            neighbours = CheckNeighbours(newLoc);
                            if (neighbours[3] == false)
                            {
                                edgeEnd = prevLoc;
                                Wall newEdge = new Wall();
                                newEdge.SetParams(edgeStart, edgeEnd, 'V');
                                if (!m_eastWalls.Contains(newEdge)) { m_eastWalls.Add(newEdge); }

                            }
                            else { m_edgeLocations.Add(prevLoc); }

                        } 

                    }
                }
            }
        }
    
    }*/

    bool[] CheckNeighbours(Vector2 Location) 
    {
        int x = (int)Location.x;
        int y = (int)Location.y;
        bool northEmpty = false, southEmpty = false, westEmpty = false, eastEmpty = false;
        if (m_mapSize.y-1 > y && y > 0 && m_mapSize.x-1 > x && x > 0 && m_tileGrid[x, y] == 1) 
        {
            if (m_tileGrid[x, y - 1] == 0) { northEmpty = true; }
            if (m_tileGrid[x, y + 1] == 0) { southEmpty = true; }
            if (m_tileGrid[x - 1, y] == 0) { westEmpty = true; }
            if (m_tileGrid[x + 1, y] == 0) { eastEmpty = true; }
        }
        return new bool[4] { northEmpty, southEmpty, westEmpty, eastEmpty };
    }

    



    Vector2Int FindCenter(RectInt Room) 
    { 
        return new Vector2Int(Mathf.RoundToInt(Room.center.x), Mathf.RoundToInt(Room.center.y));
    }
}

[System.Serializable]
class Node
{
    RectInt m_rect;
    RectInt? m_room;
    Node m_left, m_right;

    public Node(RectInt rect)
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
    Vector2 m_end;
    public char m_orientation;
    public int m_length;

    public void SetParams(Vector2 start, Vector2 end, char orientation) 
    { 
        m_start = start;
        m_end = end;
        m_orientation = orientation;
        switch (orientation) 
        {
            case 'V':
                m_length = (int)(end.y - start.y);
                break;
            case 'H':
                m_length = (int)(end.x - start.x);
                break;
            default:
                Debug.Log("Invalid Orientation.");
                break;
        }
    }
}