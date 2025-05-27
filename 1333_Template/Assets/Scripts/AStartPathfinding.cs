using System.Collections.Generic;
using UnityEngine;

public class AStartPathfinding : Pathfinding_Class
{
    public List<GridNode> FindPath(GridManager gridManager, GridNode start, GridNode end, int unitWidth, int unitHeight)
    {
        List<GridNode> openSet = new List<GridNode>();
        Dictionary<GridNode, int> gCost = new Dictionary<GridNode, int>();
        Dictionary<GridNode, int> hCost = new Dictionary<GridNode, int>();
        Dictionary<GridNode, int> fCost = new Dictionary<GridNode, int>();
        Dictionary<GridNode, GridNode> cameFrom = new Dictionary<GridNode, GridNode>();

        openSet.Add(start);
        gCost[start] = 0;
        hCost[start] = Heuristic(start, end);
        fCost[start] = gCost[start] + hCost[start];
        cameFrom[start] = null;

        while (openSet.Count > 0)
        {
            GridNode current = openSet[0];
            foreach (var node in openSet)
            {
                if (fCost.ContainsKey(node) && fCost[node] < fCost[current])
                    current = node;
            }

            if (current == end)
                return ReconstructPath(cameFrom, end);

            openSet.Remove(current);

            foreach (GridNode neighbor in GetNeighbors(gridManager, current))
            {
                if (!neighbor.walkable) continue;

                int tentativeG = gCost[current] + neighbor.MovementCost;

                if (!gCost.ContainsKey(neighbor) || tentativeG < gCost[neighbor])
                {
                    gCost[neighbor] = tentativeG;
                    hCost[neighbor] = Heuristic(neighbor, end);
                    fCost[neighbor] = gCost[neighbor] + hCost[neighbor];
                    cameFrom[neighbor] = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return new List<GridNode>(); // No path found
    }

    private List<GridNode> ReconstructPath(Dictionary<GridNode, GridNode> cameFrom, GridNode endNode)
    {
        List<GridNode> path = new();
        GridNode current = endNode;

        while (current != null)
        {
            path.Add(current);
            current = cameFrom.ContainsKey(current) ? cameFrom[current] : null;
        }

        path.Reverse();
        return path;
    }

    private List<GridNode> GetNeighbors(GridManager gridManager, GridNode node)
    {
        return gridManager.GetNeighborNodes(node);
    }

    private int Heuristic(GridNode a, GridNode b)
    {
        float dx = Mathf.Abs(a.WorldPosition.x - b.WorldPosition.x);
        float dz = Mathf.Abs(a.WorldPosition.z - b.WorldPosition.z); // Assuming XZ plane
        return Mathf.RoundToInt(dx + dz);
    }
}
