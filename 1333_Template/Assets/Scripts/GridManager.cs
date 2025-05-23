using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] private GridSettings gridSettings;
    [SerializeField] private TerrainType defaultTerrainType;

    [SerializeField] private List<TerrainType> terrainTypes = new();
    public GridSettings GridSettings => gridSettings;

    private GridNode[,] gridNodes;
    [SerializeField] private List<GridNode> allNodes = new();
    public bool IsInitialized { get; private set; } = false;

    public void InitializeGrid()
    {
        gridNodes = new GridNode[gridSettings.GridSizeX, gridSettings.GridSizeY];
        allNodes.Clear();

        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                Vector3 worldPos = gridSettings.UseXZPlane
                    ? new Vector3(x, 0, y) * gridSettings.NodeSize
                    : new Vector3(x, y, 0) * gridSettings.NodeSize;

                TerrainType chosenTerrain = terrainTypes[Random.Range(0, terrainTypes.Count)];

                GridNode node = new GridNode
                {
                    Name = $"Cell_{x}_{y}",
                    WorldPosition = worldPos,
                    terrainType = chosenTerrain,
                    walkable = chosenTerrain.Walkable
                };

                gridNodes[x, y] = node;
                allNodes.Add(node);
            }
        }

        IsInitialized = true;
    }
    private void OnDrawGizmos()
    {
        if (gridNodes == null || gridSettings == null) return;

        Gizmos.color = Color.white;

        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                GridNode node = gridNodes[x, y];
                if (node == null) continue;

                Gizmos.color = node.walkable ? node.terrainType.GizmoColour : Color.red;
                Gizmos.DrawWireCube(node.WorldPosition, Vector3.one * gridSettings.NodeSize * 0.9f);
            }
        }
    }
    public GridNode GetNode(int x, int y)
    {
        if (x >= 0 && x < gridSettings.GridSizeX && y >= 0 && y < gridSettings.GridSizeY)
            return gridNodes[x, y];
        return null;
    }

    public GridNode GetNodeFromWorld(Vector3 worldPosition)
    {
        int x = Mathf.RoundToInt(worldPosition.x / gridSettings.NodeSize);
        int y = Mathf.RoundToInt(worldPosition.z / gridSettings.NodeSize);
        return GetNode(x, y);
    }

    public List<GridNode> GetAllNodes() => allNodes;

    public List<GridNode> GetNeighborNodes(GridNode node)
    {
        List<GridNode> neighbors = new();
        Vector3Int[] directions = {
            new(1, 0, 0), new(-1, 0, 0),
            new(0, 0, 1), new(0, 0, -1),
        };

        foreach (var dir in directions)
        {
            Vector3 checkPos = node.WorldPosition + new Vector3(dir.x, 0, dir.z) * gridSettings.NodeSize;
            GridNode neighbor = GetNodeFromWorld(checkPos);
            if (neighbor != null) neighbors.Add(neighbor);
        }

        return neighbors;
    }
    public GridNode GetNode(Vector2Int position)
    {
        if (position.x >= 0 && position.x < gridSettings.GridSizeX && position.y >= 0 && position.y < gridSettings.GridSizeY)
        {
            return gridNodes[position.x, position.y];
        }
        return null;
    }

}
