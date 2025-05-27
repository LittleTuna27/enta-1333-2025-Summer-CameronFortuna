using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinder : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private AStartPathfinding pathfindingLogic;
    [SerializeField] private GameObject startPosPrefab;
    [SerializeField] private GameObject endPosPrefab;
    [SerializeField] private GameObject movingAgent;
    [SerializeField] private float searchDelay = 0.1f;

    private LineRenderer lineRenderer;
    private List<GridNode> pathNodes = new();
    private GameObject startInstance;
    private GameObject endInstance;
    private GridNode startNode;
    private GridNode endNode;

    private void Awake()
    {
        SetupLineRenderer();
    }

    public void ResetFeild()
    {
        if (startInstance != null) Destroy(startInstance);
        if (endInstance != null) Destroy(endInstance);
        lineRenderer.positionCount = 0;

        foreach (var line in GameObject.FindGameObjectsWithTag("Untagged"))
        {
            if (line.name.Contains("DirectLine"))
                Destroy(line);
        }

        StartCoroutine(GeneratePath());
    }

    private IEnumerator GeneratePath()
    {
        List<GridNode> allNodes = gridManager.GetAllNodes();
        if (allNodes == null || allNodes.Count < 2) yield break;

        startNode = GetRandomWalkableNode();
        endNode = GetRandomWalkableNode();
        while (endNode == startNode)
            endNode = GetRandomWalkableNode();

        startInstance = Instantiate(startPosPrefab, startNode.WorldPosition, Quaternion.identity);
        endInstance = Instantiate(endPosPrefab, endNode.WorldPosition, Quaternion.identity);

        Debug.Log($"Start: {startNode.Name}, End: {endNode.Name}");

        pathNodes = FindPath(startNode, endNode);

        foreach (GridNode node in pathNodes)
        {
            yield return new WaitForSeconds(searchDelay);
        }

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
            Debug.Log("Path could not be found.");
            ResetFeild(); // retry
        }
    }

    private List<GridNode> FindPath(GridNode startNode, GridNode endNode)
    {
        if (pathfindingLogic == null)
        {
            Debug.LogError("AStartPathfinding reference not set in inspector!");
            return new List<GridNode>();
        }
        Debug.Log("Using A* pathfinding");
        return pathfindingLogic.FindPath(gridManager, startNode, endNode, 1, 1);
    }


    private GridNode GetRandomWalkableNode()
    {
        var nodes = gridManager.GetAllNodes();
        GridNode node;
        do node = nodes[Random.Range(0, nodes.Count)];
        while (!node.walkable);
        return node;
    }

    private void DrawPath(List<GridNode> path)
    {
        if (lineRenderer == null) return;

        lineRenderer.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            lineRenderer.SetPosition(i, path[i].WorldPosition + Vector3.up * 0.2f);
        }
    }

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
