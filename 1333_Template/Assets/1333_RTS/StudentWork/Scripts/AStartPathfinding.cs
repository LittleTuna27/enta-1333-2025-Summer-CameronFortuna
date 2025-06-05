using System.Collections.Generic;
using UnityEngine;

public class AStartPathfinding : Pathfinding_Class
{
    [Header("Optional Debug Settings")]
    [SerializeField] private bool showSearchGizmos = true;
    [SerializeField] private Color searchedNodeColor = Color.cyan;
    [SerializeField] private float gizmoSize = 0.4f;

    private List<GridNode> searchedNodesForGizmos = new();

    public List<GridNode> FindPath(GridManager gridManager, GridNode start, GridNode end, out List<GridNode> searchedNodes)
    {
        ClearVisualization();

        // initialize A* data structures
        List<GridNode> openSet = new() { start };
        HashSet<GridNode> closedSet = new();
        Dictionary<GridNode, int> costSoFar = new() { [start] = 0 };
        Dictionary<GridNode, int> estimatedTotalCost = new() { [start] = Heuristic(start, end) };
        Dictionary<GridNode, GridNode> cameFrom = new() { [start] = start };

        // A* algorithm
        while (openSet.Count > 0)
        {
            GridNode current = openSet[0];
            foreach (var node in openSet)
            {
                if (estimatedTotalCost[node] < estimatedTotalCost[current])
                    current = node;
            }

            if (current == end)
                break;

            openSet.Remove(current);
            closedSet.Add(current);

            if (showSearchGizmos)
                searchedNodesForGizmos.Add(current);

            foreach (GridNode neighbor in gridManager.GetNeighborNodes(current))
            {
                if (!IsNodeWalkable(neighbor) || closedSet.Contains(neighbor)) continue;

                int newCost = costSoFar[current] + neighbor.terrainType.MovementCost;

                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    estimatedTotalCost[neighbor] = newCost + Heuristic(neighbor, end);
                    cameFrom[neighbor] = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        searchedNodes = new List<GridNode>(closedSet);
        return ReconstructPath(cameFrom, start, end);
    }

    private List<GridNode> ReconstructPath(Dictionary<GridNode, GridNode> cameFrom, GridNode start, GridNode end)
    {
        List<GridNode> path = new();

        if (!cameFrom.ContainsKey(end)) return path;

        GridNode current = end;
        while (current != start)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Add(start);
        path.Reverse();
        return path;
    }

    private void OnDrawGizmos()
    {
        if (!showSearchGizmos || searchedNodesForGizmos == null) return;

        Gizmos.color = searchedNodeColor;
        foreach (GridNode node in searchedNodesForGizmos)
        {
            if (node != null)
                Gizmos.DrawCube(node.WorldPosition + Vector3.up * 0.1f, Vector3.one * gizmoSize);
        }
    }

    public void ClearVisualization()
    {
        searchedNodesForGizmos?.Clear();
    }

    private bool IsNodeWalkable(GridNode node)
    {
        return node != null && (node.terrainType?.Walkable ?? node.walkable);
    }

    private int Heuristic(GridNode a, GridNode b)
    {
        Vector2Int posA = WorldToGridPosition(a.WorldPosition);
        Vector2Int posB = WorldToGridPosition(b.WorldPosition);
        return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y);
    }

    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.z); // assuming XZ plane
        return new Vector2Int(x, y);
    }
}