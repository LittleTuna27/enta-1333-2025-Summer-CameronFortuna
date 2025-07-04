using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{

    [SerializeField] private GridSettings gridSettings;
    [SerializeField] private TerrainType defaultTerrainType;
    [SerializeField] private List<TerrainType> terrainTypes = new();

    public GridSettings GridSettings => gridSettings;

    public float nodeSize => gridSettings.NodeSize;

    private GridNode[,] gridNodes;
    [SerializeField] private List<GridNode> allNodes = new();
    public bool IsInitialized { get; private set; } = false;

    void Awake()
    {
        if (!IsInitialized)
        {
            InitializeGrid();
        }
    }


    public void InitializeGrid()
    {
        gridNodes = new GridNode[gridSettings.GridSizeX, gridSettings.GridSizeY];
        allNodes.Clear();

        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                Vector3 worldPos = gridSettings.UseXZPlane
                    ? new Vector3(x, 0, y) * nodeSize
                    : new Vector3(x, y, 0) * nodeSize;

                TerrainType chosenTerrain = terrainTypes.Count > 0
                    ? terrainTypes[Random.Range(0, terrainTypes.Count)]
                    : defaultTerrainType;

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

        for (int x = 0; x < gridSettings.GridSizeX; x++)
        {
            for (int y = 0; y < gridSettings.GridSizeY; y++)
            {
                GridNode node = gridNodes[x, y];
                if (node == null) continue;

                Gizmos.color = node.walkable ? node.terrainType.GizmoColour : Color.red;
                Gizmos.DrawWireCube(node.WorldPosition, Vector3.one * nodeSize * 0.9f);
            }
        }
    }

    public GridNode GetNode(int x, int y)
    {
        if (x >= 0 && x < gridSettings.GridSizeX && y >= 0 && y < gridSettings.GridSizeY)
            return gridNodes[x, y];
        return null;
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
            Vector3 checkPos = node.WorldPosition + new Vector3(dir.x, 0, dir.z) *  nodeSize;
            GridNode neighbor = GetNodeFromWorldPosition(checkPos);
            if (neighbor != null) neighbors.Add(neighbor);
        }

        return neighbors;
    }

    public GridNode GetNodeFromWorldPosition(Vector3 position)
    {
        int x = gridSettings.UseXZPlane
            ? Mathf.RoundToInt(position.x / nodeSize)
            : Mathf.RoundToInt(position.z / nodeSize);

        int y = gridSettings.UseXZPlane
            ? Mathf.RoundToInt(position.z / nodeSize)
            : Mathf.RoundToInt(position.y / nodeSize);

        if (x < 0 || x >= gridSettings.GridSizeX || y < 0 || y >= gridSettings.GridSizeY)
            return null;

        return GetNode(x, y);
    }
    public Vector2Int GetGridPositionFromWorld(Vector3 worldPosition)
    {
        int x = gridSettings.UseXZPlane
            ? Mathf.RoundToInt(worldPosition.x / nodeSize)
            : Mathf.RoundToInt(worldPosition.z / nodeSize);

        int y = gridSettings.UseXZPlane
            ? Mathf.RoundToInt(worldPosition.z / nodeSize)
            : Mathf.RoundToInt(worldPosition.y / nodeSize);

        return new Vector2Int(x, y);
    }
    public GridNode GetNearestWalkableNode(Vector3 position)
    {
        GridNode originNode = GetNodeFromWorldPosition(position);
        if (originNode != null && originNode.walkable)
            return originNode;

        // BFS to find nearest walkable node
        Queue<GridNode> open = new Queue<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        if (originNode != null)
            open.Enqueue(originNode);
        else
            return null;

        visited.Add(originNode);

        while (open.Count > 0)
        {
            GridNode current = open.Dequeue();

            foreach (GridNode neighbor in GetNeighborNodes(current))
            {
                if (!visited.Contains(neighbor))
                {
                    if (neighbor.walkable)
                        return neighbor;

                    visited.Add(neighbor);
                    open.Enqueue(neighbor);
                }
            }
        }

        return null; // No walkable node found
    }
}
