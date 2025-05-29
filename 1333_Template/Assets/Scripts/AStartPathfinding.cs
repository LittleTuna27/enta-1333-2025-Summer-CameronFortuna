using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AStartPathfinding : Pathfinding_Class
{
    [SerializeField] private float searchVisualizationDelay = 0.1f;
    [SerializeField] private bool showSearchGizmos = true;
    [SerializeField] private Color searchedNodeColor = Color.cyan;
    [SerializeField] private Color openNodeColor = Color.yellow;
    [SerializeField] private float gizmoSize = 0.4f;

    // Store nodes for gizmo visualization
    private List<GridNode> searchedNodesForGizmos = new List<GridNode>();
    private List<GridNode> openNodesForGizmos = new List<GridNode>();

    public List<GridNode> FindPath(GridManager gridManager, GridNode start, GridNode end, int unitWidth, int unitHeight, out List<GridNode> searchedNodes)
    {
        List<GridNode> openSet = new List<GridNode>();
        List<GridNode> closedSet = new List<GridNode>(); // Track searched nodes
        Dictionary<GridNode, int> costSoFar = new Dictionary<GridNode, int>(); // Cost from start
        Dictionary<GridNode, int> estematedTotalCost = new Dictionary<GridNode, int>(); // Total estimated cost (g + h)
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

            // Check neighbors
            foreach (GridNode neighbor in GetNeighbors(gridManager, current))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor)) continue;

                // Check if this area is walkable for a unit of given size
                if (!IsAreaWalkable(gridManager, neighbor, unitWidth, unitHeight)) continue;

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

        // Set the searched nodes for visualization
        searchedNodes = new List<GridNode>(closedSet);

        // Reconstruct path
        List<GridNode> path = new List<GridNode>();
        if (!cameFrom.ContainsKey(end)) return path; // No path found

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

    public IEnumerator FindPathWithVisualization(GridManager gridManager, GridNode start, GridNode end, int unitWidth, int unitHeight, System.Action<List<GridNode>, List<GridNode>> onComplete)
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

            // Remove the current node from open set and add to closed set
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

            // Check neighbors
            foreach (GridNode neighbor in GetNeighbors(gridManager, current))
            {
                if (!neighbor.walkable || closedSet.Contains(neighbor)) continue;

                // Check if this area is walkable for a unit of given size
                if (!IsAreaWalkable(gridManager, neighbor, unitWidth, unitHeight)) continue;

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

        // Return results via callback
        onComplete?.Invoke(path, new List<GridNode>(closedSet));
    }

    private void OnDrawGizmos()
    {
        if (!showSearchGizmos) return;

        // Draw searched nodes (closed set)
        Gizmos.color = searchedNodeColor;
        foreach (GridNode node in searchedNodesForGizmos)
        {
            if (node != null)
            {
                Gizmos.DrawCube(node.WorldPosition + Vector3.up * 0.1f, Vector3.one * gizmoSize);
            }
        }

        // Draw open nodes (nodes being considered)
        Gizmos.color = openNodeColor;
        foreach (GridNode node in openNodesForGizmos)
        {
            if (node != null)
            {
                Gizmos.DrawWireCube(node.WorldPosition + Vector3.up * 0.15f, Vector3.one * gizmoSize * 0.8f);
            }
        }
    }

    public void ClearVisualization()
    {
        searchedNodesForGizmos.Clear();
        openNodesForGizmos.Clear();
    }

    private bool IsAreaWalkable(GridManager gridManager, GridNode centerNode, int unitWidth, int unitHeight)
    {
        // For a 1x1 unit, just check the center node
        if (unitWidth <= 1 && unitHeight <= 1)
            return centerNode.walkable;

        // For larger units, check all nodes in the area
        int halfWidth = unitWidth / 2;
        int halfHeight = unitHeight / 2;

        // Convert world position to grid coordinates
        Vector3 worldPos = centerNode.WorldPosition;
        int centerX = gridManager.GridSettings.UseXZPlane
            ? Mathf.RoundToInt(worldPos.x / gridManager.GridSettings.NodeSize)
            : Mathf.RoundToInt(worldPos.z / gridManager.GridSettings.NodeSize);
        int centerY = gridManager.GridSettings.UseXZPlane
            ? Mathf.RoundToInt(worldPos.z / gridManager.GridSettings.NodeSize)
            : Mathf.RoundToInt(worldPos.y / gridManager.GridSettings.NodeSize);

        // Check all nodes in the unit's footprint
        for (int x = centerX - halfWidth; x <= centerX + halfWidth; x++)
        {
            for (int y = centerY - halfHeight; y <= centerY + halfHeight; y++)
            {
                GridNode node = gridManager.GetNode(x, y);
                if (node == null || !node.walkable)
                    return false;
            }
        }

        return true;
    }

    private List<GridNode> GetNeighbors(GridManager gridManager, GridNode node)
    {
        return gridManager.GetNeighborNodes(node);
    }

    private GridNode FindClosestNode(GridManager gridManager, Vector3 position)
    {
        int x = Mathf.RoundToInt(position.x / gridManager.GridSettings.NodeSize);
        int y = Mathf.RoundToInt(position.z / gridManager.GridSettings.NodeSize);
        return gridManager.GetNode(x, y);
    }

    private int Heuristic(GridNode a, GridNode b)
    {
        // Manhattan distance heuristic
        float dx = Mathf.Abs(a.WorldPosition.x - b.WorldPosition.x);
        float dz = Mathf.Abs(a.WorldPosition.z - b.WorldPosition.z);
        return Mathf.RoundToInt(dx + dz);
    }
}