using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyPathPlanner : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private AStartPathfinding pathfindingLogic;
    [SerializeField] private Transform targetTransform; // e.g., your Town Hall or Player Base

    public void SetTarget(Transform newTarget)
    {
        targetTransform = newTarget;
    }

    public List<GridNode> GetPathToTarget(Vector3 fromPosition)
    {
        if (gridManager == null || pathfindingLogic == null || targetTransform == null)
        {
            Debug.LogWarning("PathPlanner is missing references.");
            return null;
        }

        GridNode startNode = gridManager.GetNodeFromWorldPosition(fromPosition);
        GridNode endNode = gridManager.GetNodeFromWorldPosition(targetTransform.position);

        if (startNode == null || endNode == null || !endNode.walkable)
        {
            Debug.LogWarning("Invalid path endpoints.");
            return null;
        }

        List<GridNode> searched;
        return pathfindingLogic.FindPath(gridManager, startNode, endNode, out searched);
    }
}