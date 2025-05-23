using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    // References to necessary components and prefabs
    [SerializeField] private GridManager gridManager;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private GameObject startPos;
    [SerializeField] private GameObject endPos;
    [SerializeField] private float searchDelay = 0.1f; // Delay between steps (seconds)

    // Internal state for tracking the path and instantiated markers
    private List<GridNode> pathNodes = new();
    private GameObject startInstance;
    private GameObject endInstance;

    private GridNode startNode;
    private GridNode endNode;


    // Set up the LineRenderer on start
    private void Awake()
    {
        SetupLineRenderer();
    }

    // Clears previous visual elements and starts a new pathfinding sequence
    public void ResetFeild()
    {
        if (startInstance != null) Destroy(startInstance);
        if (endInstance != null) Destroy(endInstance);

        lineRenderer.positionCount = 0;

        // Clean up any leftover direct line objects
        foreach (var line in GameObject.FindGameObjectsWithTag("Untagged"))
        {
            if (line.name.Contains("DirectLine"))
                Destroy(line);
        }

        StartCoroutine(GeneratePath());
    }

    // Pathfinding coroutine: selects start/end, simulates searching, and draws the result
    private IEnumerator GeneratePath()
    {
        List<GridNode> allNodes = gridManager.GetAllNodes();
        if (allNodes == null || allNodes.Count < 2) yield break;

        startNode = GetRandomWalkableNode();
        endNode = GetRandomWalkableNode();
        while (endNode == startNode)
            endNode = GetRandomWalkableNode();

        startInstance = Instantiate(startPos, startNode.WorldPosition, Quaternion.identity);
        endInstance = Instantiate(endPos, endNode.WorldPosition, Quaternion.identity);

        Debug.Log($"Start: {startNode.Name}, End: {endNode.Name}");

        pathNodes = FindPath(startNode, endNode);

        // Step through visualization
        foreach (GridNode node in pathNodes)
        {
            yield return new WaitForSeconds(searchDelay);
        }

        if (pathNodes.Count > 0)
        {
            DrawPath(pathNodes); 
        }
        else
        {
            Debug.Log("Path could not be found.");
        }
    
}

    // Draws a connected path line through the path node list
    private void DrawPath(List<GridNode> path)
    {
        if (lineRenderer == null) return;

        lineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            lineRenderer.SetPosition(i, path[i].WorldPosition + Vector3.up * 0.2f);
        }
    }
    private List<GridNode> FindPath(GridNode startNode, GridNode endNode)
    {
        List<GridNode> path = new List<GridNode>();
        HashSet<GridNode> visited = new HashSet<GridNode>();

        GridNode current = startNode;
        path.Add(current);
        visited.Add(current);

        while (current != endNode)
        {
            List<GridNode> neighbors = gridManager.GetNeighborNodes(current);
            GridNode bestNeighbor = null;
            float bestDistance = float.MaxValue;

            foreach (GridNode neighbor in neighbors)
            {
                if (!neighbor.walkable || visited.Contains(neighbor))
                    continue;

                float distance = Vector3.Distance(neighbor.WorldPosition, endNode.WorldPosition);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestNeighbor = neighbor;
                }
            }

            if (bestNeighbor == null)
            {
                Debug.Log("No path found (dead end).");
                return new List<GridNode>();
            }

            current = bestNeighbor;
            path.Add(current);
            visited.Add(current);
        }

        return path;
    }
    // Draws a straight line from start to end marker using a new LineRenderer
   

    // Used to pick a random walkable node from the grid
    private GridNode GetRandomWalkableNode()
    {
        var nodes = gridManager.GetAllNodes();
        GridNode node;
        do node = nodes[Random.Range(0, nodes.Count)];
        while (!node.walkable);
        return node;
    }

    // Creates and configures the LineRenderer if it isn't already assigned
    private void SetupLineRenderer()
    {
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();

        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.blue;
        lineRenderer.endColor = Color.black;
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.useWorldSpace = true;
        lineRenderer.positionCount = 0;
    }
}