// --- AStartPathfinding.cs ---

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStartPathfinding : Pathfinding_Class
{
    [SerializeField] private float searchVisualizationDelay = 0.1f;
    [SerializeField] private float pathFoundDelay = 1.0f;
    [SerializeField] private bool showSearchGizmos = true;
    [SerializeField] private Color searchedNodeColor = Color.cyan;
    [SerializeField] private Color openNodeColor = Color.yellow;
    [SerializeField] private float gizmoSize = 0.4f;

    // Store nodes for gizmo visualization
    private List<GridNode> searchedNodesForGizmos = new List<GridNode>();
    private List<GridNode> openNodesForGizmos = new List<GridNode>();

    // Returns path and also outputs all searched nodes
    public List<GridNode> FindPath(GridManager gridManager, GridNode start, GridNode end, out List<GridNode> searchedNodes)
    {
        List<GridNode> openSet = new List<GridNode>();
        List<GridNode> closedSet = new List<GridNode>();
        Dictionary<GridNode, int> costSoFar = new Dictionary<GridNode, int>();
        Dictionary<GridNode, int> estematedTotalCost = new Dictionary<GridNode, int>();
        Dictionary<GridNode, GridNode> cameFrom = new Dictionary<GridNode, GridNode>();

        openSet.Add(start);
        costSoFar[start] = 0;
        estematedTotalCost[start] = Heuristic(start, end);
        cameFrom[start] = start;

        while (openSet.Count > 0)
        {
            // Find node with lowest estimated total cost
            GridNode current = openSet[0];
            foreach (var node in openSet)
            {
                if (estematedTotalCost[node] < estematedTotalCost[current])
                    current = node;
            }

            // If we've reached the end node, stop searching
            if (current.Equals(end))
                break;

            // Remove the current node from open set and add to closed set
            openSet.Remove(current);
            closedSet.Add(current);

            // Check neighboring nodes to see if they are walkable and which has the lowest cost
            foreach (GridNode neighbor in GetNeighbors(gridManager, current))
            {
                if (!IsNodeWalkable(neighbor) || closedSet.Contains(neighbor)) continue;

                int tentativeCostSoFar = costSoFar[current] + neighbor.terrainType.movementCost;

                if (!costSoFar.ContainsKey(neighbor) || tentativeCostSoFar < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = tentativeCostSoFar;
                    estematedTotalCost[neighbor] = tentativeCostSoFar + Heuristic(neighbor, end);
                    cameFrom[neighbor] = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // Clear visualization once path is found
        searchedNodes = new List<GridNode>(closedSet);

        // Reconstruct path
        List<GridNode> path = new List<GridNode>();
        if (!cameFrom.ContainsKey(end)) return path; // if there isn't a path to the end node, return empty path

        GridNode currentNode = end;
        while (currentNode != start)
        {
            path.Add(currentNode);
            currentNode = cameFrom[currentNode];
        }
        path.Add(start);
        path.Reverse();

        return path;
    }

    // Coroutine version of pathfinding that visualizes the search process step-by-step
    public IEnumerator FindPathWithVisualization(GridManager gridManager, GridNode start, GridNode end,
        System.Action<List<GridNode>, List<GridNode>> onComplete)
    {
        // Clear previous visualization data
        searchedNodesForGizmos.Clear();
        openNodesForGizmos.Clear();

        List<GridNode> openSet = new List<GridNode>();
        List<GridNode> closedSet = new List<GridNode>();
        Dictionary<GridNode, int> costSoFar = new Dictionary<GridNode, int>();
        Dictionary<GridNode, int> estematedTotalCost = new Dictionary<GridNode, int>();
        Dictionary<GridNode, GridNode> cameFrom = new Dictionary<GridNode, GridNode>();

        openSet.Add(start);
        costSoFar[start] = 0;
        estematedTotalCost[start] = Heuristic(start, end);
        cameFrom[start] = start;

        while (openSet.Count > 0)
        {
            // Find node with lowest estimated total cost
            GridNode current = openSet[0];
            foreach (var node in openSet)
            {
                if (estematedTotalCost[node] < estematedTotalCost[current])
                    current = node;
            }

            // If we've reached the end node, stop searching
            if (current.Equals(end))
                break;

            openSet.Remove(current);
            closedSet.Add(current);

            // Update gizmo visualization data
            if (showSearchGizmos)
            {
                searchedNodesForGizmos.Add(current);
                openNodesForGizmos.Clear();
                openNodesForGizmos.AddRange(openSet);
                yield return new WaitForSeconds(searchVisualizationDelay);
            }

            foreach (GridNode neighbor in GetNeighbors(gridManager, current))
            {
                if (!IsNodeWalkable(neighbor) || closedSet.Contains(neighbor)) continue;

                int tentativeCostSoFar = costSoFar[current] + neighbor.terrainType.movementCost;

                if (!costSoFar.ContainsKey(neighbor) || tentativeCostSoFar < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = tentativeCostSoFar;
                    estematedTotalCost[neighbor] = tentativeCostSoFar + Heuristic(neighbor, end);
                    cameFrom[neighbor] = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // Reconstruct path
        List<GridNode> path = new List<GridNode>();
        if (cameFrom.ContainsKey(end))
        {
            GridNode currentNode = end;
            while (currentNode != start)
            {
                path.Add(currentNode);
                currentNode = cameFrom[currentNode];
            }
            path.Add(start);
            path.Reverse();
        }

        // Clear visualization after showing final result briefly
        if (showSearchGizmos)
        {
            yield return new WaitForSeconds(pathFoundDelay);
            ClearVisualization();
        }

        // Return results via callback
        onComplete?.Invoke(path, new List<GridNode>(closedSet));
    }

    // Draw Gizmo cubes and outlines for visited and open nodes
    private void OnDrawGizmos()
    {
        if (!showSearchGizmos) return;

        // Draw searched nodes (closed set)
        Gizmos.color = searchedNodeColor;
        foreach (GridNode node in searchedNodesForGizmos)
        {
            if (node != null)
                Gizmos.DrawCube(node.WorldPosition + Vector3.up * 0.1f, Vector3.one * gizmoSize);
        }

        // Draw open nodes (nodes being considered)
        Gizmos.color = openNodeColor;
        foreach (GridNode node in openNodesForGizmos)
        {
            if (node != null)
                Gizmos.DrawWireCube(node.WorldPosition + Vector3.up * 0.15f, Vector3.one * gizmoSize * 0.8f);
        }
    }

    // Utility: clears visualized data
    public void ClearVisualization()
    {
        searchedNodesForGizmos.Clear();
        openNodesForGizmos.Clear();
    }

    private bool IsNodeWalkable(GridNode node)
    {
        if (node == null) return false;
        if (node.terrainType != null)
            return node.terrainType.Walkable;
        return node.walkable;
    }

    private List<GridNode> GetNeighbors(GridManager gridManager, GridNode node)
    {
        return gridManager.GetNeighborNodes(node);
    }

    private int Heuristic(GridNode a, GridNode b)
    {
        float dx = Mathf.Abs(a.WorldPosition.x - b.WorldPosition.x);
        float dz = Mathf.Abs(a.WorldPosition.z - b.WorldPosition.z);
        return Mathf.RoundToInt(dx + dz);
    }

    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.z);
        return new Vector2Int(x, y);
    }
}
