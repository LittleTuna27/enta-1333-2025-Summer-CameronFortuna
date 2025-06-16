using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridNode
{
    public string Name;
    public bool walkable;
    public Vector3 WorldPosition;
    public TerrainType terrainType;

    public bool IsOccupied = false; // Add this

    public GridNode CameFromNode;

    public int MovementCost => terrainType != null ? terrainType.MovementCost : 1;
    public int GCost;
    public int HCost;
    public int FCost => GCost + HCost;
}
