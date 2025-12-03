using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MapGeneration : MonoBehaviour
{
    //Ints

    int m_maxDepth = 4;
    int m_minLeafSize = 10;
    const int m_borderSize = 3;
    //Vectors
    [Header("BSP ,Sizes")]
    Vector2Int m_dungeonSize = new Vector2Int(100,100);
    Vector2Int m_minRoomSize = new Vector2Int(5, 5);
    Vector2Int m_maxRoomSize = new Vector2Int(20, 20);
    Vector2Int m_caveBorder = new Vector2Int(); //used for centralizing the dungeon.
    //Materials
    [Header("Prefab Requirements")]
    //Prefabs
    [SerializeField] TileType[] m_tileTypes; //should allow for editor work


    //Private Variables
    //System
    
    Tile[,] m_tileGrid;


    //Lists
    [SerializeField] BSPNode rootNode;
    List<BSPNode> m_leaves = new List<BSPNode>();
    List<Room> m_rooms = new List<Room>();
    List<RectInt> m_corridors = new List<RectInt>();
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
        m_caveBorder.x = (GameManager.m_mapX - m_dungeonSize.x);
    }
    public void SetMapY(Slider ySlider) 
    {
        m_dungeonSize.y = (int)ySlider.value;
        m_caveBorder.y = (GameManager.m_mapY - m_dungeonSize.y);
    }
    public void SetMinLeaf(int leaf)
    {
        m_minLeafSize = leaf;
    }
    public void SetMinX(int x) 
    { 
        m_minRoomSize.x = x;
    }
    public void SetMaxX(int x) 
    {
        m_maxRoomSize.x = x;
    }
    public void SetMinY(int y) 
    {
        m_minRoomSize.y = y;
    }
    public void SetMaxY(int y) 
    {
        m_maxRoomSize.y = y;
    }
    public void SetMaxDepth(int depth) 
    {
        m_maxDepth = depth;
    }
    #endregion

    public void GenerateMap() 
    {
        GenerateCave();
        GenerateBSP();
        InstantiateGrid(m_tileGrid);
    }

    void GenerateCave() 
    { 
        
    }
    void GenerateBSP() 
    {

        ClearTiles(); //Clear Previous Tiles
        RectInt root = new RectInt (m_borderSize, m_borderSize, Mathf.Max(1, (m_dungeonSize.x - (m_borderSize * 2))), Mathf.Max(1, (m_dungeonSize.y - (m_borderSize * 2))));
      
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
        
    }

    //All work to do with Instantiating/Rasterising the grid.
    #region GridWork
    void ClearTiles() 
    {
        foreach (TileType tileType in m_tileTypes) 
        {
            Transform parent = tileType.parent.transform;
            foreach (GameObject g in parent) 
            {
                Destroy(g);
            }
        }
        m_tileGrid = new Tile[m_dungeonSize.x, m_dungeonSize.y];
        for (int x = 0;  x < m_dungeonSize.x; x++) 
        {
            for (int y = 0; y < m_dungeonSize.y; y++)
            {
                m_tileGrid[x, y] = new Tile(m_tileTypes[0], Vector3.zero);
            }
        }

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
                    m_tileGrid[x, y] = tiles[(x - rect.xMin), (y - rect.yMin)];
                }
            }
        }

        foreach (RectInt corridor in m_corridors)
        {
            for (int x = corridor.xMin; x < corridor.xMax; x++)
            {
                for (int y = corridor.yMin; y < corridor.yMax; y++)
                {
                    m_tileGrid[x, y] = new Tile(m_tileTypes[2], new Vector3 (x, 0, y));
                }
            }
        }
    }

    void RasterizeWalls() 
    { 
    
    }

    void InstantiateGrid(Tile[,] grid) 
    {
        for (int x = 0; x < grid.GetLength(0); x++) 
        { 
            for (int y = 0; y < grid.GetLength(1); y++) 
            {
                if (grid[x, y].GetID() != 0 ) 
                {
                    Tile tile = m_tileGrid[x, y];
                    Instantiate(tile.GetPrefab(), tile.GetPosition(), Quaternion.identity, tile.GetParent());
                }
            }
        }
    }
    
    //void InstantiateWalls()
    //{
    //    Vector3 placePos = Vector3.zero;
    //    GameObject newWall = m_wallPrefab;
    //    foreach (Wall wall in m_horizWalls) 
    //    {

    //        placePos = new Vector3((wall.start.x + (wall.length / 2)), 1, (wall.start.y));
    //        newWall.transform.localScale = new Vector3(wall.length + 1, 1, 1);
    //        if (wall.isCorridor)
    //        {
    //            placePos.z -= 1;
    //            Instantiate(newWall, placePos, Quaternion.identity, m_wallParent.transform);

    //            placePos.z += 2;
    //            Instantiate(newWall, placePos, Quaternion.identity, m_wallParent.transform);
    //        }
    //        else 
    //        {
    //            placePos.z += wall.axis;
    //            Instantiate(newWall, placePos, Quaternion.identity, m_wallParent.transform);
    //        }
    //    }
    //    foreach (Wall wall in m_vertWalls)
    //    {

    //        placePos = new Vector3((wall.start.x), 1, (wall.start.y + (wall.length / 2)));
    //        newWall.transform.localScale = new Vector3(1, 1, wall.length + 1);
    //        if (wall.isCorridor)
    //        {
    //            placePos.x -= 1;
    //            Instantiate(newWall, placePos, Quaternion.identity, m_wallParent.transform);

    //            placePos.x += 2;
    //            Instantiate(newWall, placePos, Quaternion.identity, m_wallParent.transform);
    //        }
    //        else
    //        {

    //            placePos.x += wall.axis;
    //            Instantiate(newWall, placePos, Quaternion.identity, m_wallParent.transform);
    //        }
    //    }
    //}

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
                if (m_tileGrid[x, y].CheckFor("Dungeon Floor") && ((neighbours[0] ^ neighbours[1]) || (neighbours[0] && neighbours[1]))) //IF grid is 1, and either only 1 or both are empty.
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
                if (m_tileGrid[x, y].CheckFor("Dungeon Floor") && ((neighbours[2] ^ neighbours[3]) || (neighbours[2] && neighbours[3]))) 
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
        Debug.Log($"Wall at {currentLocation} has {neighbours[index]} on North/West, and {neighbours[index]} on South/East");
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
            Debug.Log($"Wall at {currentLocation} is a corridor.");
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
            else if (m_tileGrid[Location.x, Location.y - 1].CheckFor(checkFor)) { northEmpty = true; }
            if (Location.y + 1 >= m_dungeonSize.y) { southEmpty = true; }
            else if (m_tileGrid[Location.x, Location.y + 1].CheckFor(checkFor)) { southEmpty = true; }
            if (Location.x - 1 < 0) { westEmpty = true; }
            else if (m_tileGrid[Location.x - 1, Location.y].CheckFor(checkFor)) { westEmpty = true; }
            if (Location.x + 1 >= m_dungeonSize.x) { eastEmpty = true; }
            else if (m_tileGrid[Location.x + 1, Location.y].CheckFor(checkFor)) { eastEmpty = true; }
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