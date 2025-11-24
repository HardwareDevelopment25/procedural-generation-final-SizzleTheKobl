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

    //Private Variables
    //System
    System.Random m_random = new System.Random();
    int[,] m_tileGrid;


    //Lists
    List<Node> m_leaves = new List<Node>();
    List<RectInt> m_rooms = new List<RectInt>();
    List<RectInt> m_corridors = new List<RectInt>();

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
      
        Node startNode = new Node(root);
        SplitRecursive(startNode, 0);

        GetLeaves(startNode);

        foreach (Node leaf in m_leaves)
        {
            RectInt roomRoot = leaf.GetRect();
            RectInt room = CreateRoom(roomRoot);
            leaf.SetRoom(room);
            m_rooms.Add(room);
        }

        ConnectTree(startNode);
        RasterizeGrid();
        InstantiateGrid(m_tileGrid);
        
    }

    void ClearTiles() 
    {
        foreach (GameObject g in this.transform)
        {
            Destroy(g);
        }
        m_tileGrid = new int[m_mapSize.x, m_mapSize.y];
    }

    void RasterizeGrid() 
    {
        foreach (RectInt room in m_rooms)
        {
            for (int x = room.xMin; x < room.xMax; x++)
            {
                for (int y = room.yMin; y < room.yMax; y++)
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
                    Instantiate(m_floorPrefab, position, Quaternion.identity, this.transform);
                }
                    
            }
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
        if (nodeRect.height <= (2 * m_minLeafSize) || nodeRect.width <= (2 * m_minLeafSize)) 
        {
            return;
        }
        if (nodeRect.height > nodeRect.width)
        {
            int minY = nodeRect.yMin + m_minLeafSize;
            int maxY = nodeRect.yMax - m_minLeafSize;
            splitLine = m_random.Next(minY, maxY); //Guarantees that leaves will be at least 10 on each side.
            orientation = 'y'; //To allow to split properly.
        }
        else 
        {
            int minX = nodeRect.xMin + m_minLeafSize;
            int maxX = nodeRect.xMax - m_minLeafSize;
            splitLine = m_random.Next(minX, maxX); //^^^^
            orientation = 'x';
        }
        Debug.Log(splitLine);
        RectInt[] childRects = new RectInt[2];
        switch (orientation) 
        {
            case 'x': //Width;
                childRects[0] = new RectInt(nodeRect.x, nodeRect.y, (splitLine - nodeRect.x), nodeRect.height);
                childRects[1] = new RectInt(splitLine, nodeRect.y, (nodeRect.xMax-splitLine), nodeRect.height);
                break;
            case 'y': //Height
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
        node.SetChildren(left, right);

        depth++;
        SplitRecursive(left, depth);
        SplitRecursive(right, depth);

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
        roomX = m_random.Next((leaf.x+1), (leaf.xMax - roomWidth-1));
        roomY = m_random.Next((leaf.y+1), (leaf.yMax - roomHeight-1));
        return new RectInt(roomX, roomY, roomWidth, roomHeight);
    }

    void CreateCorridor(Vector2Int from, Vector2Int to) 
    { 
        if (from.y == to.y) 
        {
            int x = Mathf.Min(from.x, to.x);
            int w = Mathf.Abs(from.x - to.x);
            var rect = new RectInt(x - 1 / 2, from.y - 1 / 2, w, 1);
            m_corridors.Add(rect);
            return;
        }
        if (from.x == to.x)
        {
            int y = Mathf.Min(from.y, to.y);
            int w = Mathf.Abs(from.y - to.y);
            var rect = new RectInt(from.x - 1 / 2, y - 1 / 2, 1, w);
            m_corridors.Add(rect);
            return;
        }

    }

    void ConnectTree (Node node) 
    {
        if (node == null || node.IsLeaf()) 
        {
            return;
        }

        ConnectTree(node.GetLeft());
        ConnectTree(node.GetRight());

        RectInt leftRoom = (node.GetLeft()).GetRoom();
        RectInt rightRoom = (node.GetRight()).GetRoom();

        Vector2Int midPoint = new Vector2Int(FindCenter(rightRoom).x, FindCenter(leftRoom).y);

        CreateCorridor(FindCenter(rightRoom), midPoint);
        CreateCorridor(FindCenter(leftRoom), midPoint);

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
    RectInt m_room;
    Node m_left, m_right;

    public Node(RectInt rect)
    { 
        m_rect = rect;
    }

    
    //GETTERS
    public RectInt GetRect() { return m_rect; }
    public Node GetLeft() { return m_left; }
    public Node GetRight() { return m_right; }
    public RectInt GetRoom() { return m_room; }
    //SETTERS
    public void SetChildren(Node left, Node right)
    {
        m_left = left;
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