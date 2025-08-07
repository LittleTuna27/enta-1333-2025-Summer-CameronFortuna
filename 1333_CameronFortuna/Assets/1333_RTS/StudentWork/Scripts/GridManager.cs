using System.Collections;
using System.Collections.Generic;
using System.Threading;
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

    // inittializes grid at the start
    void Awake()
    {
        if (!IsInitialized)
        {
            InitializeGrid();
        }
    }

    //builds the grid of nodes based on settings and terrain
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

    //draws the grid in the editor using Gizmos
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

    //gives the node at the given grid coordinates
    public GridNode GetNode(int x, int y)
    {
        if (x >= 0 && x < gridSettings.GridSizeX && y >= 0 && y < gridSettings.GridSizeY)
            return gridNodes[x, y];
        return null;
    }

    //returns a list of all nodes in the grid
    public List<GridNode> GetAllNodes() => allNodes;

    //returns the 4 neighboring nodes (up, down, left, right)
    public List<GridNode> GetNeighborNodes(GridNode node)
    {
        List<GridNode> neighbors = new();
        Vector3Int[] directions = {
            new(1, 0, 0), new(-1, 0, 0),
            new(0, 0, 1), new(0, 0, -1),
        };

        foreach (var dir in directions)
        {
            Vector3 checkPos = node.WorldPosition + new Vector3(dir.x, 0, dir.z) * nodeSize;
            GridNode neighbor = GetNodeFromWorldPosition(checkPos);
            if (neighbor != null) neighbors.Add(neighbor);
        }

        return neighbors;
    }

    //finds the node at a given world position
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
    //converts a world position into grid coordinates
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
    //finds the nearest walkable node from a given position
    public GridNode GetNearestWalkableNode(Vector3 position, int maxSearchRadius = 10)
    {
        GridNode originNode = GetNodeFromWorldPosition(position);

        // If no node found at position, search nearby
        if (originNode == null)
        {
            Debug.LogWarning($"GetNearestWalkableNode: No node found at position {position}");
            return FindWalkableNodeInArea(position, maxSearchRadius);
        }

        // If node is already good, return it
        if (originNode.walkable && !originNode.IsOccupied)
            return originNode;

        // Breadth-first search to expand outward
        Queue<GridNode> open = new Queue<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        open.Enqueue(originNode);
        visited.Add(originNode);

        int searchRadius = 0;
        while (open.Count > 0 && searchRadius < maxSearchRadius)
        {
            int currentLevelSize = open.Count;
            searchRadius++;

            for (int i = 0; i < currentLevelSize; i++)
            {
                GridNode current = open.Dequeue();

                foreach (GridNode neighbor in GetNeighborNodes(current))
                {
                    if (!visited.Contains(neighbor))
                    {
                        visited.Add(neighbor);

                        if (neighbor.walkable && !neighbor.IsOccupied)
                        {
                            Debug.Log($"Found walkable node at distance {searchRadius} from {position}");
                            return neighbor;
                        }

                        open.Enqueue(neighbor);
                    }
                }
            }
        }

        Debug.LogWarning($"GetNearestWalkableNode: No walkable node found within {maxSearchRadius} radius of {position}");
        return null;
    }
    //expands out in a square search to find a walkable node
    private GridNode FindWalkableNodeInArea(Vector3 centerPosition, int maxRadius)
    {
        for (int radius = 1; radius <= maxRadius; radius++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    if (Mathf.Abs(x) < radius && Mathf.Abs(z) < radius)
                        continue;

                    Vector3 checkPosition = centerPosition + new Vector3(x * nodeSize, 0, z * nodeSize);
                    GridNode node = GetNodeFromWorldPosition(checkPosition);

                    if (node != null && node.walkable && !node.IsOccupied)
                    {
                        Debug.Log($"Found walkable node at {checkPosition} (radius {radius})");
                        return node;
                    }
                }
            }
        }

        return null;
    }
    //returns all nodes that a building would cover at a given position
    public List<GridNode> GetNodesInArea(Vector3 centerPosition, int width, int height)
    {
        List<GridNode> nodes = new List<GridNode>();
        Vector2Int gridPos = GetGridPositionFromWorld(centerPosition);

        int halfWidth = width / 2;
        int halfHeight = height / 2;

        for (int x = gridPos.x - halfWidth; x <= gridPos.x + halfWidth; x++)
        {
            for (int y = gridPos.y - halfHeight; y <= gridPos.y + halfHeight; y++)
            {
                GridNode node = GetNode(x, y);
                if (node != null)
                {
                    nodes.Add(node);
                }
            }
        }

        return nodes;
    }
}