using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class MikeTheMiner : MonoBehaviour
{

    [SerializeField] int m_steps;
    [SerializeField] float m_gridXSize;
    [SerializeField] float m_gridZSize;
    [SerializeField] int seed;

    [SerializeField] GameObject m_mikeTheMiner;
    [SerializeField] GameObject m_drawTile;
    [SerializeField] GameObject m_drawWall;

    System.Random m_random;

    public List<Vector3> FloorPositions = new List<Vector3>();

    public Stack<Vector3> IntersectPositions = new Stack<Vector3>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_random = new System.Random();
        int gridXBoundary = (int)(m_gridXSize / 2);
        int gridZBoundary = (int)(m_gridZSize / 2);
        // Similar to the Axis on controllers or input actions. -1 is left, +1 is right.

        Vector3 mikeStart = m_mikeTheMiner.transform.position;
        FloorPositions.Add(mikeStart);
        for (int i = 0; i < 4; i++) { IntersectPositions.Push(mikeStart); } //Places 4 positions into the stack to pull, meaning at the very least it will create a path for each direction.
        
        Quaternion quaternion = Quaternion.identity;
        while (IntersectPositions.Count > 0)
        {
            mikeStart = IntersectPositions.Pop();
            m_mikeTheMiner.transform.position = mikeStart;
            Path(m_steps, gridXBoundary, gridZBoundary);
        }
        Draw((gridXBoundary+1), (gridZBoundary+1), quaternion);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    bool CheckValid(Vector3 currentPos, bool isX, int axis) 
    {
        if (isX) { currentPos.x += axis; }
        else {  currentPos.z += axis; }
        Vector3 LeftNeighbour = currentPos;
        Vector3 RightNeighbour = currentPos;
        Vector3 LookAhead = currentPos;
        if (isX)
        {
            LeftNeighbour.z -= 1;
            RightNeighbour.z += 1;
            LookAhead.x += axis;
            if (!FloorPositions.Contains(LeftNeighbour) && !FloorPositions.Contains(RightNeighbour) && !FloorPositions.Contains(LookAhead) && !FloorPositions.Contains(currentPos)) { return true; }
            else { return false; }
        }
        else 
        {
            LeftNeighbour.x -= 1;
            RightNeighbour.x += 1;
            LookAhead.z += axis;
            if (!FloorPositions.Contains(LeftNeighbour) && !FloorPositions.Contains(RightNeighbour) && !FloorPositions.Contains(LookAhead) && !FloorPositions.Contains(currentPos)) { return true; }
            else { return false; }
        }
    
    }

    void Path(int steps, int gridXBoundary, int gridZBoundary) 
    {
       
        //VECTORS
        Vector3 mikePos = m_mikeTheMiner.transform.position;
        Vector3 chosenMikePos = mikePos;
        //BOOLS
        bool moved = false;
        bool isX = false;
        //INTS
        int movedir = 0;
        int safetyCounter = 0;
        int axis = 0;

        while (steps > 0)
        {
            movedir = m_random.Next(0, 4);

            switch (movedir)
            {
                case 0:
                    axis = 1;
                    if (Mathf.Abs(mikePos.x) + 2 <= gridXBoundary)
                    {
                        isX = true;
                        mikePos.x += axis;
                        if (CheckValid(mikePos, isX, axis))
                        { steps -= 1; moved = true; }
                        else { mikePos.x -= axis; moved = false; }
                    }
                    break;
                case 1:
                    axis = -1;
                    if (Mathf.Abs(mikePos.x) + 2 <= gridXBoundary)
                    {
                        isX = true;
                        mikePos.x += axis;
                        if (CheckValid(mikePos, isX, axis))
                        { steps -= 1; moved = true; }
                        else { mikePos.x -= axis; moved = false; }
                    }
                    break;
                case 2:
                    axis = 1;
                    if (Mathf.Abs(mikePos.z) + 2 <= gridZBoundary)
                    {

                        isX = false;
                        mikePos.z += axis;
                        if (CheckValid(mikePos, isX, axis))
                        { steps -= 1; moved = true; }
                        else
                        { mikePos.z -= axis; moved = false; }
                    }
                    break;
                case 3:
                    axis = -1;
                    if (Mathf.Abs(mikePos.z) + 2 <= gridZBoundary)
                    {
                        isX = false;
                        mikePos.z += axis;
                        if (CheckValid(mikePos, isX, axis))
                        { steps -= 1; moved = true; }
                        else { mikePos.z -= axis; moved = false; ; }
                    }
                    break;
                default:
                    break;
            }
            if (moved)
            {
                FloorPositions.Add(mikePos);
                if (isX) { mikePos.x += axis; }
                else { mikePos.z += axis; }
                FloorPositions.Add(mikePos);
                m_mikeTheMiner.transform.position = mikePos;
                for (int i = 0; i < 3; i++) { IntersectPositions.Push(mikePos); }
                safetyCounter = 0;
            }
            else
            {
                safetyCounter++;
                if (safetyCounter >= 100) { break; }
            }

        }
    }
    void Draw(int gridXBoundary, int gridZBoundary, Quaternion quaternion) 
    {
        Vector3 wallPos = new Vector3(0, 0, 0);
        for (int i = -gridXBoundary; i <= gridXBoundary; i++)
        {
            for (int j = -gridZBoundary; j <= gridZBoundary; j++)
            {
                wallPos.Set(i, 0, j);
                if (!FloorPositions.Contains(wallPos))
                {
                    Instantiate(m_drawWall, wallPos, quaternion);
                }
                else
                {
                    wallPos.y = -1;
                    Instantiate(m_drawTile, wallPos, quaternion);
                }
            }
        }
    }
}
