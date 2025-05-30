using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinderVisulization : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private AStartPathfinding pathfindingLogic;
    [SerializeField] private GameObject endPosPrefab;
    [SerializeField] private GameObject movingAgent;
    [SerializeField] private bool useVisualization = true;

    private LineRenderer lineRenderer;
    private List<GridNode> pathNodes = new();
    private GameObject endInstance;
    private GridNode startNode;
    private GridNode endNode;

    private void Awake()
    {
        SetupLineRenderer();
    }

    public void ResetFeild()
    {
        CleanupPreviousSearch();
        StartCoroutine(GeneratePath());
    }

    private void CleanupPreviousSearch()
    {
        if (endInstance != null) Destroy(endInstance);
        lineRenderer.positionCount = 0;

        //clean the grid of any gizmos used to to visulaize data
        pathfindingLogic.ClearVisualization();

        //clean  the grid of any direct lines
        foreach (var line in GameObject.FindGameObjectsWithTag("Untagged"))
        {
            if (line.name.Contains("DirectLine"))
                Destroy(line);
        }
    }

    private IEnumerator GeneratePath()
    {
        //get all nodes on the grid and add them to a list
        List<GridNode> allNodes = gridManager.GetAllNodes();
        if (allNodes == null || allNodes.Count < 2) yield break;

       //set the start node to the character and randomly select a end node
        startNode = gridManager.GetNodeFromWorldPosition(movingAgent.transform.position);
        endNode = GetRandomWalkableNode();

        //restart if the nodes are the same
        while (endNode == startNode)
            endNode = GetRandomWalkableNode();

        endInstance = Instantiate(endPosPrefab, endNode.WorldPosition, Quaternion.identity);
        Debug.Log($"Start: {startNode.Name}, End: {endNode.Name}");

        if (useVisualization)
        {
            //start runing the visulaizer for the pathfinding
            yield return StartCoroutine(pathfindingLogic.FindPathWithVisualization(
                gridManager, startNode, endNode, OnPathFound));
        }
        else
        {
            //use the immediate version
            List<GridNode> searchedNodes;
            pathNodes = pathfindingLogic.FindPath(gridManager, startNode, endNode, out searchedNodes);
            ProcessFoundPath();
        }
    }

    private void OnPathFound(List<GridNode> foundPath, List<GridNode> searchedNodes)
    {
        //add the nodes found for the path to a new list the proceed throught the path
        pathNodes = foundPath;
        ProcessFoundPath();
    }

    private void ProcessFoundPath()
    {
        //so long as there is nodes in the path move to the next node
        if (pathNodes.Count > 0)
        {
            DrawPath(pathNodes);
            if (movingAgent.TryGetComponent(out PathAgentMover mover))
            {
                mover.StartMoving(pathNodes);
            }
        }
        else
        {
            //ResetFeild feild if there is no path
            Debug.Log("Path could not be found.");
            ResetFeild(); // retry
        }
    }

    private GridNode GetRandomWalkableNode()
    {
        //randomly sellect one node on the grid so long as it is walkable
        var nodes = gridManager.GetAllNodes();
        GridNode node;
        do node = nodes[Random.Range(0, nodes.Count)];
        while (!node.walkable);
        return node;
    }

    private void DrawPath(List<GridNode> path)
    {
        //draw a path form one path node to the next in the list
        if (lineRenderer == null) return;
        lineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            lineRenderer.SetPosition(i, path[i].WorldPosition + Vector3.up * 0.2f);
        }
    }

    private void SetupLineRenderer()
    {
        // set up a line render to use as the pathfinding code
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