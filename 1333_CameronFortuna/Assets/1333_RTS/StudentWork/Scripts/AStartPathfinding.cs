using System.Collections.Generic;
using UnityEngine;

public class AStartPathfinding : Pathfinding_Class
{
    [SerializeField] private bool showSearchGizmos = true;
    [SerializeField] private Color searchedNodeColor = Color.cyan;
    [SerializeField] private float gizmoSize = 0.4f;

    private List<GridNode> searchedNodesForGizmos = new();

    //main A* pathfinding software 
    public List<GridNode> FindPath(GridManager gridManager, GridNode start, GridNode end, out List<GridNode> searchedNodes)
    {
        ClearVisualization(); // Clear any previous visualization data

        // Validate start and end nodes
        if (!IsNodeWalkable(start))
        {
            Debug.LogWarning("Start node is not walkable!");
            searchedNodes = new List<GridNode>();
            return new List<GridNode>();
        }

        if (!IsNodeWalkable(end))
        {
            Debug.LogWarning("End node is not walkable!");
            searchedNodes = new List<GridNode>();
            return new List<GridNode>();
        }

        // A* algorithm initialization
        List<GridNode> openSet = new() { start }; // Open set starts with the start node
        HashSet<GridNode> closedSet = new(); // Set for processed nodes
        Dictionary<GridNode, int> costSoFar = new() { [start] = 0 }; // Costs for reaching each node
        Dictionary<GridNode, int> estimatedTotalCost = new() { [start] = Heuristic(start, end) }; // Estimated total cost (f = g + h)
        Dictionary<GridNode, GridNode> cameFrom = new() { [start] = start }; // Path reconstruction map

        // A* algorithm loop
        while (openSet.Count > 0)
        {
            GridNode current = openSet[0]; // Start with the first node in openSet
            foreach (var node in openSet)
            {
                if (estimatedTotalCost[node] < estimatedTotalCost[current])
                    current = node; //choose the node with the lowest f cost
            }

            if (current == end) break;

            openSet.Remove(current);
            closedSet.Add(current);

            if (showSearchGizmos)
                searchedNodesForGizmos.Add(current);

            //check neighbors of the current node
            foreach (GridNode neighbor in gridManager.GetNeighborNodes(current))
            {
                if (!IsNodeWalkable(neighbor) || closedSet.Contains(neighbor)) continue;

                //calculate cost to move to this neighbor using the neighbor's movement cost
                int newCost = costSoFar[current] + neighbor.MovementCost;

                // If a better path is found, update the cost and add the neighbor to the open set
                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    estimatedTotalCost[neighbor] = newCost + Heuristic(neighbor, end);
                    cameFrom[neighbor] = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor); // Add neighbor to open set if not already there
                }
            }
        }
        //return the nodes visited during the search and reconstruct and return the path from start to end
        searchedNodes = new List<GridNode>(closedSet);
        return ReconstructPath(cameFrom, start, end);
    }

    //reconstructs the path from start to end using the 'cameFrom' dictionary
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

    //unity Gizmos function for visualizing the search process in the editor
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

    //clears the list of nodes used for visualization
    public void ClearVisualization()
    {
        searchedNodesForGizmos?.Clear();
    }

    //check if a node is walkable based on its terrain type - now uses TerrainType as authority
    private bool IsNodeWalkable(GridNode node)
    {
        if (node == null || node.terrainType == null)
        {
            Debug.LogWarning($"Node at {node?.WorldPosition} has no terrain type assigned!");
            return false;
        }

        return node.terrainType.Walkable && !node.IsOccupied; //  Check for buildings too
    }

    //heuristic function to calculate the Manhattan distance between two nodes
    private int Heuristic(GridNode a, GridNode b)
    {
        Vector2Int posA = WorldToGridPosition(a.WorldPosition);
        Vector2Int posB = WorldToGridPosition(b.WorldPosition);
        return Mathf.Abs(posA.x - posB.x) + Mathf.Abs(posA.y - posB.y); // Manhattan distance
    }

    //converts a world position to grid position (2D grid)
    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x);
        int y = Mathf.RoundToInt(worldPos.z); // Assuming XZ plane for 2D grid
        return new Vector2Int(x, y);
    }
}
