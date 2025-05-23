using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridNode
{
    public string Name;
    public bool walkable;
    public Vector3 WorldPosition;
    public TerrainType terrainType;
    
    public int Horizontal_Weight;
    public int Diagonal_Weight;

    public float GCost;
    public float HCost;
    public float FCost;
    public GridNode CameFromNode;

    public void calculateFCost()
    {
        FCost = GCost + HCost;
    }
}