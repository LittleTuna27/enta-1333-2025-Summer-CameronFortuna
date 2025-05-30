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

    public List<GridNode> FindPath(GridManager gridManager, GridNode start, GridNode end, out List<GridNode> searchedNodes)
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

            //once the path hits the end node, stop searching
            if (current.Equals(end))
                break;

            //remove the current node from open set list and add it to the closed set list
            openSet.Remove(current);
            closedSet.Add(current);

            //check neighboring nodes to see if they are walkable and which has the lowest cost
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

        //clear visualization once path is found
        searchedNodes = new List<GridNode>(closedSet);

        //reconstruct path
        List<GridNode> path = new List<GridNode>();
        //if there isnt a path to the end node, return the path
        if (!cameFrom.ContainsKey(end)) return path; 

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
            //find the next node with lowest estimated total cost
            GridNode current = openSet[0];
            foreach (var node in openSet)
            {
                if (estematedTotalCost[node] < estematedTotalCost[current])
                    current = node;
            }

            //once we've reached the end node, stop searching
            if (current.Equals(end))
                break;

            //remove the current node from open set and add to closed set
            openSet.Remove(current);
            closedSet.Add(current);

            //update gizmo visualization data to add the next nodes to be search and change the old nodes to searched ones
            if (showSearchGizmos)
            {
                searchedNodesForGizmos.Add(current);
                openNodesForGizmos.Clear();
                openNodesForGizmos.AddRange(openSet);
                yield return new WaitForSeconds(searchVisualizationDelay);
            }

            //check neighboring nodes to see if they are walkable and which has the lowest cost
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

        //clear visualization after showing final result briefly
        if (showSearchGizmos)
        {
            yield return new WaitForSeconds(pathFoundDelay);
            ClearVisualization();
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

    private bool IsNodeWalkable(GridNode node)
    {
        if (node == null) return false;

        // Use the TerrainType's walkable property if available, otherwise fallback to node's walkable
        if (node.terrainType != null)
            return node.terrainType.Walkable;

        // Fallback to the node's walkable property if no terrain type is assigned
        return node.walkable;
    }

    private List<GridNode> GetNeighbors(GridManager gridManager, GridNode node)
    {
        return gridManager.GetNeighborNodes(node);
    }

    private int Heuristic(GridNode a, GridNode b)
    {
        // Convert world positions back to grid coordinates
        Vector2Int gridPosA = WorldToGridPosition(a.WorldPosition);
        Vector2Int gridPosB = WorldToGridPosition(b.WorldPosition);

        // Manhattan distance in grid steps
        int distanceX = Mathf.Abs(gridPosA.x - gridPosB.x);
        int distanceY = Mathf.Abs(gridPosA.y - gridPosB.y);

        // Return grid steps (this matchaes the scale of movement costs)
        return distanceX + distanceY;
    }

    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.z);
        return new Vector2Int(x, y);
    }
}