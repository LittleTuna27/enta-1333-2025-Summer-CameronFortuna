using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStartPathfinding : Pathfinding_Class
{
    [SerializeField] private float searchVisualizationDelay = 0.1f;
    [SerializeField] private float pathFoundDelay = 1.0f;
    [SerializeField] private bool showSearchGizmos = true;
    [SerializeField] private Color searchedNodeColor = Color.cyan;
    [SerializeField] private float gizmoSize = 0.4f;

    private List<GridNode> searchedNodesForGizmos = new List<GridNode>();

    public List<GridNode> FindPath(GridManager gridManager, GridNode start, GridNode end, out List<GridNode> searchedNodes)
    {
        ClearVisualization();

        List<GridNode> openSet = new List<GridNode>();
        List<GridNode> closedSet = new List<GridNode>();
        Dictionary<GridNode, int> costSoFar = new Dictionary<GridNode, int>();
        Dictionary<GridNode, int> estimatedTotalCost = new Dictionary<GridNode, int>();
        Dictionary<GridNode, GridNode> cameFrom = new Dictionary<GridNode, GridNode>();

        openSet.Add(start);
        costSoFar[start] = 0;
        estimatedTotalCost[start] = Heuristic(start, end);
        cameFrom[start] = start;

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

        List<GridNode> path = new List<GridNode>();
        if (!cameFrom.ContainsKey(end)) return path;

        GridNode currentPathNode = end;
        while (currentPathNode != start)
        {
            path.Add(currentPathNode);
            currentPathNode = cameFrom[currentPathNode];
        }

        path.Add(start);
        path.Reverse();
        return path;
    }

    public IEnumerator FindPathWithVisualization(GridManager gridManager, GridNode start, GridNode end, System.Action<List<GridNode>, List<GridNode>> onComplete)
    {
        ClearVisualization();

        List<GridNode> openSet = new List<GridNode>();
        List<GridNode> closedSet = new List<GridNode>();
        Dictionary<GridNode, int> costSoFar = new Dictionary<GridNode, int>();
        Dictionary<GridNode, int> estimatedTotalCost = new Dictionary<GridNode, int>();
        Dictionary<GridNode, GridNode> cameFrom = new Dictionary<GridNode, GridNode>();

        openSet.Add(start);
        costSoFar[start] = 0;
        estimatedTotalCost[start] = Heuristic(start, end);
        cameFrom[start] = start;

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
            {
                searchedNodesForGizmos.Add(current);
                yield return new WaitForSeconds(searchVisualizationDelay);
            }

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

        List<GridNode> path = new List<GridNode>();
        if (cameFrom.ContainsKey(end))
        {
            GridNode current = end;
            while (current != start)
            {
                path.Add(current);
                current = cameFrom[current];
            }
            path.Add(start);
            path.Reverse();
        }

        if (showSearchGizmos)
        {
            yield return new WaitForSeconds(pathFoundDelay);
            ClearVisualization();
        }

        onComplete?.Invoke(path, new List<GridNode>(closedSet));
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
        int y = Mathf.RoundToInt(worldPos.z); // Assuming XZ plane
        return new Vector2Int(x, y);
    }
}
