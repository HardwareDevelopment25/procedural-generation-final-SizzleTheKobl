using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class MikeTheMiner : MonoBehaviour
{

    [SerializeField] int m_sips;
    [SerializeField] float m_gridXSize;
    [SerializeField] float m_gridZSize;
    [SerializeField] int seed;

    [SerializeField] GameObject m_mikeTheMiner;
    [SerializeField] GameObject m_drawTile;
    [SerializeField] GameObject m_drawWall;

    System.Random m_random;


    public List<Vector3> FloorPositions = new List<Vector3>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_random = new System.Random();
        int gridXBoundary = (int)(m_gridXSize / 2);
        int gridZBoundary = (int)(m_gridZSize / 2);
        int movedir = 0;

        Vector3 mikePos = m_mikeTheMiner.transform.position;
        Quaternion quaternion = Quaternion.identity;

        bool moved = false;
        bool isX = false;
        int safetyCounter = 0;
        int direction = 0; // Similar to the Axis on controllers or input actions. -1 is left, +1 is right.
        FloorPositions.Add(mikePos);
        while (m_sips > 0)
        {
            movedir = m_random.Next(0, 4);
            switch (movedir) 
            {
                case 0:
                    direction = 1;
                    if ((mikePos.x + (2 * direction)) <= gridXBoundary) 
                    {
                        isX = true;
                        mikePos.x += direction;
                        if (CheckValid(mikePos, isX, direction))
                        { m_sips -= 1; moved = true; }
                        else { mikePos.x -= direction; moved = false; }
                    }
                    break;
                case 1:
                    direction = -1;
                    if ((mikePos.x + (2 * direction)) >= -gridXBoundary) 
                    {
                        isX = true;
                        mikePos.x += direction;
                        if (CheckValid(mikePos, isX, direction))
                        { m_sips -= 1; moved = true; }
                        else { mikePos.x -= direction; moved = false; }
                    }
                    break;
                case 2:
                    direction = 1;
                    if ((mikePos.z + (2 * direction)) <= gridZBoundary) 
                    {
                        
                        isX = false;
                        mikePos.z += direction;
                        if (CheckValid(mikePos, isX, direction))
                        { m_sips -= 1; moved = true; }
                        else 
                        { mikePos.z -= direction; moved = false; }
                    }
                    break;
                case 3:
                    direction = -1;
                    if ((mikePos.z + (2 * direction)) >= -gridZBoundary) 
                    {
                        isX = false;
                        mikePos.z += direction;
                        if (CheckValid(mikePos, isX, direction))
                        { m_sips -= 1; moved = true; }
                        else { mikePos.z -= direction; moved = false; ; }
                    }
                    break;
                default:
                    break;
            }
            if (moved)
            {
                FloorPositions.Add(mikePos);
                if (isX) { mikePos.x += direction; }
                else { mikePos.z += direction; }
                FloorPositions.Add(mikePos);
                m_mikeTheMiner.transform.position = mikePos;
                safetyCounter = 0;
            }
            else
            {
                safetyCounter++;
                if (safetyCounter >= 1000000) { break; }
            }

        }
        Draw(gridXBoundary, gridZBoundary, quaternion);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    bool CheckValid(Vector3 currentPos, bool isX, int direction) 
    {
        if (isX) { currentPos.x += direction; }
        else {  currentPos.z += direction; }
            Vector3 LeftNeighbour = currentPos;
        Vector3 RightNeighbour = currentPos;
        Vector3 ForwardRightNeighbour = currentPos;
        Vector3 ForwardLeftNeighbour = currentPos;
        Vector3 LookAhead = currentPos;
        if (isX)
        {
            LeftNeighbour.z -= 1;
            RightNeighbour.z += 1;
            LookAhead.x += direction;
            if (!FloorPositions.Contains(LeftNeighbour) && !FloorPositions.Contains(RightNeighbour) && !FloorPositions.Contains(LookAhead) && !FloorPositions.Contains(currentPos)) { return true; }
            else { return false; }
        }
        else 
        {
            LeftNeighbour.x -= 1;
            RightNeighbour.x += 1;
            LookAhead.z += direction;
            if (!FloorPositions.Contains(LeftNeighbour) && !FloorPositions.Contains(RightNeighbour) && !FloorPositions.Contains(LookAhead) && !FloorPositions.Contains(currentPos)) { return true; }
            else { return false; }
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
