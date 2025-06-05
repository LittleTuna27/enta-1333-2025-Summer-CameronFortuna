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

        // initialize data structures for A* algorithm
        List<GridNode> openSet = new List<GridNode>();
        List<GridNode> closedSet = new List<GridNode>();
        Dictionary<GridNode, int> costSoFar = new Dictionary<GridNode, int>();
        Dictionary<GridNode, int> estimatedTotalCost = new Dictionary<GridNode, int>();
        Dictionary<GridNode, GridNode> cameFrom = new Dictionary<GridNode, GridNode>();

        // add starting node to the open set with initial values
        openSet.Add(start);
        costSoFar[start] = 0;
        estimatedTotalCost[start] = Heuristic(start, end);
        cameFrom[start] = start;

        // continue searching until we've explored all possible paths
        while (openSet.Count > 0)
        {
            // find the node with the lowest estimated total cost
            GridNode current = openSet[0];
            foreach (var node in openSet)
            {
                if (estimatedTotalCost[node] < estimatedTotalCost[current])
                    current = node;
            }

            // if we reached the destination, we're done
            if (current == end)
                break;

            // move current node from open to closed set
            openSet.Remove(current);
            closedSet.Add(current);

            // check all neighboring nodes
            foreach (GridNode neighbor in gridManager.GetNeighborNodes(current))
            {
                // skip unwalkable nodes or nodes we've already processed
                if (!IsNodeWalkable(neighbor) || closedSet.Contains(neighbor)) continue;

                // calculate the cost to reach this neighbor through current node
                int newCost = costSoFar[current] + neighbor.terrainType.MovementCost;

                // if this path to neighbor is better than any previous one
                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    estimatedTotalCost[neighbor] = newCost + Heuristic(neighbor, end);
                    cameFrom[neighbor] = current;

                    // add to open set if not already there
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // return the nodes we searched for debugging purposes
        searchedNodes = new List<GridNode>(closedSet);

        // reconstruct the path by working backwards from end to start
        List<GridNode> path = new List<GridNode>();
        if (!cameFrom.ContainsKey(end)) return path; // no path found

        GridNode currentPathNode = end;
        while (currentPathNode != start)
        {
            path.Add(currentPathNode);
            currentPathNode = cameFrom[currentPathNode];
        }

        path.Add(start);
        path.Reverse(); // reverse to get start-to-end order
        return path;
    }

    public IEnumerator FindPathWithVisualization(GridManager gridManager, GridNode start, GridNode end, System.Action<List<GridNode>, List<GridNode>> onComplete)
    {
        ClearVisualization();

        // initialize data structures for A* algorithm (same as above)
        List<GridNode> openSet = new List<GridNode>();
        List<GridNode> closedSet = new List<GridNode>();
        Dictionary<GridNode, int> costSoFar = new Dictionary<GridNode, int>();
        Dictionary<GridNode, int> estimatedTotalCost = new Dictionary<GridNode, int>();
        Dictionary<GridNode, GridNode> cameFrom = new Dictionary<GridNode, GridNode>();

        // add starting node to the open set with initial values
        openSet.Add(start);
        costSoFar[start] = 0;
        estimatedTotalCost[start] = Heuristic(start, end);
        cameFrom[start] = start;

        // continue searching until we've explored all possible paths
        while (openSet.Count > 0)
        {
            // find the node with the lowest estimated total cost
            GridNode current = openSet[0];
            foreach (var node in openSet)
            {
                if (estimatedTotalCost[node] < estimatedTotalCost[current])
                    current = node;
            }

            // if we reached the destination, we're done
            if (current == end)
                break;

            // move current node from open to closed set
            openSet.Remove(current);
            closedSet.Add(current);

            // add to visualization and wait for delay to show search progress
            if (showSearchGizmos)
            {
                searchedNodesForGizmos.Add(current);
                yield return new WaitForSeconds(searchVisualizationDelay);
            }

            // check all neighboring nodes
            foreach (GridNode neighbor in gridManager.GetNeighborNodes(current))
            {
                // skip unwalkable nodes or nodes we've already processed
                if (!IsNodeWalkable(neighbor) || closedSet.Contains(neighbor)) continue;

                // calculate the cost to reach this neighbor through current node
                int newCost = costSoFar[current] + neighbor.terrainType.MovementCost;

                // if this path to neighbor is better than any previous one
                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    estimatedTotalCost[neighbor] = newCost + Heuristic(neighbor, end);
                    cameFrom[neighbor] = current;

                    // add to open set if not already there
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        // reconstruct the path by working backwards from end to start
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
            path.Reverse(); // reverse to get start-to-end order
        }

        // wait a moment to show the final path before clearing visualization
        if (showSearchGizmos)
        {
            yield return new WaitForSeconds(pathFoundDelay);
            ClearVisualization();
        }

        // call the completion callback with the found path and searched nodes
        onComplete?.Invoke(path, new List<GridNode>(closedSet));
    }

    private void OnDrawGizmos()
    {
        // draw gizmos for all nodes that have been searched
        if (!showSearchGizmos || searchedNodesForGizmos == null) return;

        Gizmos.color = searchedNodeColor;
        foreach (GridNode node in searchedNodesForGizmos)
        {
            if (node != null)
                // draw cube slightly above the node position to avoid z-fighting
                Gizmos.DrawCube(node.WorldPosition + Vector3.up * 0.1f, Vector3.one * gizmoSize);
        }
    }

    public void ClearVisualization()
    {
        // clear all visualization data for a fresh search
        searchedNodesForGizmos?.Clear();
    }

    private bool IsNodeWalkable(GridNode node)
    {
        // check if node exists and is walkable based on terrain type or fallback property
        return node != null && (node.terrainType?.Walkable ?? node.walkable);
    }

    private int Heuristic(GridNode a, GridNode b)
    {
        // calculate Manhattan distance between two nodes for A* heuristic
        Vector2Int posA = WorldToGridPosition(a.WorldPosition);
        Vector2Int posB = WorldToGridPosition(b.WorldPosition);
        return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y);
    }

    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        // convert world position to grid coordinates
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.z); // assuming XZ plane for grid
        return new Vector2Int(x, y);
    }
}