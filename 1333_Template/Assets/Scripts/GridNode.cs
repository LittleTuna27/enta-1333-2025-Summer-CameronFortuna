using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridNode
{
    public string Name;
    public bool walkable;
    public Vector3 WorldPosition;
    public TerrainType terrainType;

    public GridNode CameFromNode;

    // Computed movement cost (from TerrainType)
    public int MovementCost => terrainType != null ? terrainType.MovementCost : 1;

    // Optional A* cost values (can be handled externally if preferred)
    public int GCost;
    public int HCost;
    public int FCost => GCost + HCost;
}
