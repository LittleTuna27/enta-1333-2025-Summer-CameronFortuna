//using System.Collections.Generic;
//using UnityEngine;

//public class PathFinderVisualization : MonoBehaviour
//{
//    [SerializeField] private AStartPathfinding pathfindingLogic; // reference to the A* logic (used only for ClearVisualization)

//    private LineRenderer lineRenderer;

//    private void Awake()
//    {
//        // set up the line renderer used to draw path lines
//        SetupLineRenderer();
//    }

//    public void ResetFeild()
//    {
//        // clear any line and gizmos previously drawn
//        lineRenderer.positionCount = 0;
//        pathfindingLogic?.ClearVisualization();
//    }

//    public void DrawPath(List<GridNode> path)
//    {
//        // validate the path before drawing
//        if (lineRenderer == null || path == null || path.Count == 0) return;

//        lineRenderer.positionCount = path.Count;

//        for (int i = 0; i < path.Count; i++)
//        {
//            // draw slightly above ground
//            lineRenderer.SetPosition(i, path[i].WorldPosition + Vector3.up * 0.2f);
//        }
//    }

//    private void SetupLineRenderer()
//    {
//        // configure line renderer with basic settings
//        if (lineRenderer == null)
//            lineRenderer = gameObject.AddComponent<LineRenderer>();

//        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
//        lineRenderer.startColor = Color.blue;
//        lineRenderer.endColor = Color.black;
//        lineRenderer.startWidth = 0.2f;
//        lineRenderer.endWidth = 0.2f;
//        lineRenderer.useWorldSpace = true;
//        lineRenderer.positionCount = 0;
//    }
//}