using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinderVisulization : MonoBehaviour
{
    [SerializeField] private GridManager gridManager; // grid reference used for node access
    [SerializeField] private AStartPathfinding pathfindingLogic; // A* pathfinding logic
    [SerializeField] private GameObject endPosPrefab; // prefab to mark the goal node
    [SerializeField] private bool useVisualization = true; // toggle for search visual feedback

    [Header("Auto Loop Testing")]
    [SerializeField] private bool autoLoop = false; // toggle for looping search visualization
    [SerializeField] private float loopDelay = 2.0f; // time between looped searches
    [SerializeField] private UnitInstance defaultUnit; // unit used for automatic visual testing

    private LineRenderer lineRenderer;
    private List<GridNode> pathNodes = new(); // holds the current path
    private GameObject endInstance;
    private GridNode startNode;
    private GridNode endNode;
    private Coroutine loopRoutine;

    private void Awake()
    {
        // set up the line renderer used to draw path lines
        SetupLineRenderer();
    }

    private void Start()
    {
        // if auto loop is on, begin testing paths automatically
        if (autoLoop && defaultUnit != null)
        {
            loopRoutine = StartCoroutine(LoopVisualization());
        }
    }

    private void Update()
    {
        // press space to run a one-time visualized path search
        if (Input.GetKeyDown(KeyCode.Space) && defaultUnit != null)
        {
            StopAllCoroutines();
            StartCoroutine(GeneratePath(defaultUnit));
        }

        // press R to reset visualizer
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetFeild();
        }

        // press L to toggle auto path loop mode
        if (Input.GetKeyDown(KeyCode.L) && defaultUnit != null)
        {
            autoLoop = !autoLoop;

            if (autoLoop)
                loopRoutine = StartCoroutine(LoopVisualization());
            else if (loopRoutine != null)
                StopCoroutine(loopRoutine);
        }
    }

    private IEnumerator LoopVisualization()
    {
        // run continuous path visualizations between random points
        while (autoLoop && defaultUnit != null)
        {
            yield return StartCoroutine(GeneratePath(defaultUnit));
            yield return new WaitForSeconds(loopDelay);
            ResetFeild();
        }
    }

    public void ResetFeild()
    {
        // cleanup any leftover visuals or state from previous path
        CleanupPreviousSearch();
    }

    private void CleanupPreviousSearch()
    {
        // destroy any previous goal marker
        if (endInstance != null) Destroy(endInstance);

        // clear the path line
        lineRenderer.positionCount = 0;

        // clear any pathfinding gizmos
        pathfindingLogic.ClearVisualization();

        // destroy debug lines in scene (optional extra cleanup)
        foreach (var line in GameObject.FindGameObjectsWithTag("Untagged"))
        {
            if (line.name.Contains("DirectLine"))
                Destroy(line);
        }
    }

    public IEnumerator GeneratePath(UnitInstance unit)
    {
        // make sure unit is valid before continuing
        if (unit == null)
        {
            Debug.LogWarning("Unit is null in GeneratePath.");
            yield break;
        }

        // get the node under the unit as the starting point
        startNode = gridManager.GetNodeFromWorldPosition(unit.transform.position);
        endNode = GetRandomWalkableNode();

        // ensure end and start are not the same node
        while (endNode == startNode)
            endNode = GetRandomWalkableNode();

        // create marker to visualize the destination node
        if (endInstance != null) Destroy(endInstance);
        endInstance = Instantiate(endPosPrefab, endNode.WorldPosition, Quaternion.identity);

        Debug.Log($"Start: {startNode.Name}, End: {endNode.Name}");

        // run either animated or immediate pathfinding
        if (useVisualization)
        {
            yield return StartCoroutine(pathfindingLogic.FindPathWithVisualization(
                gridManager, startNode, endNode, OnPathFound));
        }
        else
        {
            List<GridNode> searchedNodes;
            pathNodes = pathfindingLogic.FindPath(gridManager, startNode, endNode, out searchedNodes);
            ProcessFoundPath(unit);
        }
    }

    private void OnPathFound(List<GridNode> foundPath, List<GridNode> searchedNodes)
    {
        // store path and pass it to the unit for movement
        pathNodes = foundPath;

        if (defaultUnit != null)
            ProcessFoundPath(defaultUnit);
    }

    private void ProcessFoundPath(UnitInstance unit)
    {
        // if a path was found, draw it and send it to the mover
        if (pathNodes.Count > 0)
        {
            DrawPath(pathNodes);

            if (unit.TryGetComponent(out PathAgentMover mover))
            {
                mover.StartMoving(pathNodes);
            }
        }
        else
        {
            Debug.Log("Path could not be found.");
            ResetFeild(); // clean up if no path was found
        }
    }

    private GridNode GetRandomWalkableNode()
    {
        // randomly choose a node on the grid that is marked as walkable
        var nodes = gridManager.GetAllNodes();
        GridNode node;
        do node = nodes[Random.Range(0, nodes.Count)];
        while (!node.walkable);
        return node;
    }

    public void DrawPath(List<GridNode> path)
    {
        // draw a line through each node on the path
        if (lineRenderer == null) return;
        lineRenderer.positionCount = path.Count;

        for (int i = 0; i < path.Count; i++)
        {
            lineRenderer.SetPosition(i, path[i].WorldPosition + Vector3.up * 0.2f);
        }
    }

    private void SetupLineRenderer()
    {
        // set up the LineRenderer for path drawing
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
