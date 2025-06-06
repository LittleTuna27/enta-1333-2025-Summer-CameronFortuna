using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TerrainType", menuName = "Game/TerrainType")]
public class TerrainType : ScriptableObject
{
    [SerializeField] private string terrainName = "Default";
    [SerializeField] private Color gizmoColour = Color.green;
    [SerializeField] private bool walkable = true;
    public int movementCost = 1;

    public string TerrainName => terrainName;
    public Color GizmoColour => gizmoColour;
    public bool Walkable => walkable;
    public int MovementCost => movementCost;
}
