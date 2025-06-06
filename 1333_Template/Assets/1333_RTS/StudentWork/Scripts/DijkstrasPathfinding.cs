using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DijkstrasPathfinding : Pathfinding_Class
{
    public List<GridNode> FindPath(GridManager gridManager, GridNode start, GridNode end, int unitWidth, int unitHeight)
    { 
        List<GridNode> openSet = new List<GridNode>();

        Dictionary<GridNode, int> costSoFar = new Dictionary<GridNode, int>();

        Dictionary<GridNode, GridNode> cameFrom = new Dictionary<GridNode, GridNode>();

        openSet.Add(start);

        costSoFar[start] = 0;

        cameFrom[start] = start;

        while (openSet.Count > 0)
        {

            // Find node with lowest cost so far
            GridNode current = openSet[0];
            foreach (var node in openSet)
            {
                if (costSoFar[node] < costSoFar[current])
                    current = node;
            }

            // If we've reached the end node, stop searching
            if (current.Equals(end))
                break;

            // Remove the current node from open set
            openSet.Remove(current);

            // Check neighbors
            foreach (GridNode neighbor in GetNeighbors(gridManager, current))
            {
                if (!neighbor.walkable) continue;

                int newCost = costSoFar[current] + neighbor.MovementCost; // Uniform cost for Dijkstra (can replace with terrain weight)

                if (!costSoFar.ContainsKey(neighbor) || newCost < costSoFar[neighbor])
                {
                    costSoFar[neighbor] = newCost;
                    cameFrom[neighbor] = current;
                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

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

    private List<GridNode> GetNeighbors(GridManager gridManager, GridNode node)
    {
        //GridNode 
        return gridManager.GetNeighborNodes(node); // Assumes your GridManager has a GetNeighbors function
    }
}
        