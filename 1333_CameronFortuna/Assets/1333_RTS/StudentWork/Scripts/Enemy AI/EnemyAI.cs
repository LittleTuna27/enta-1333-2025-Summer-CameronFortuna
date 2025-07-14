using System.Collections.Generic;
using System.Collections;
using UnityEngine;


public class EnemyAI : MonoBehaviour
{
    public enum AIState
    {
        SeekingCastle,
        AttackingTarget,
        AttackingBlockingBuilding,
        Idle
    }

    [Header("AI Settings")]
    [SerializeField] private float pathfindingInterval = 2f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float castleDetectionRange = 15f;

    [Header("Target Priority")]
    [SerializeField] private Transform playerCastle;
    [SerializeField] private LayerMask enemyLayerMask = -1;

    private UnitInstance unitInstance;
    private GridManager gridManager;
    private AStartPathfinding pathfinder;
    private Coroutine aiCoroutine;
    private IDamageable currentTarget;
    private bool isInitialized = false;
    private AIState currentAIState = AIState.SeekingCastle;

    void Start()
    {
        unitInstance = GetComponent<UnitInstance>();
        gridManager = FindObjectOfType<GridManager>();
        pathfinder = FindObjectOfType<AStartPathfinding>();

        if (!unitInstance || !gridManager || !pathfinder)
        {
            Debug.LogError("EnemyAI missing required components.");
            return;
        }

        StartCoroutine(DelayedInitialize());
    }

    private IEnumerator DelayedInitialize()
    {
        yield return null;

        if (AIManager.Instance != null)
        {
            playerCastle = AIManager.Instance.PlayerCastle;
            pathfindingInterval = AIManager.Instance.GlobalPathfindingInterval;
            detectionRange = AIManager.Instance.GlobalDetectionRange;
            castleDetectionRange = AIManager.Instance.GlobalCastleDetectionRange;
            AIManager.Instance.RegisterEnemyAI(this);
        }
        else if (playerCastle == null)
        {
            playerCastle = FindPlayerCastle();
        }

        if (playerCastle == null)
        {
            Debug.LogWarning("EnemyAI: No player castle found.");
        }

        isInitialized = true;
        aiCoroutine = StartCoroutine(AILoop());
    }

    private Transform FindPlayerCastle()
    {
        GameObject taggedCastle = GameObject.FindWithTag("Castle");
        return taggedCastle ? taggedCastle.transform : null;
    }

    private IEnumerator AILoop()
    {
        while (isInitialized && unitInstance != null && unitInstance.IsAlive)
        {
            UpdateAI();
            yield return new WaitForSeconds(pathfindingInterval);
        }
    }

    private void UpdateAI()
    {
        if (!isInitialized || unitInstance == null || !unitInstance.IsAlive)
            return;

        IDamageable nearbyEnemy = FindNearestEnemy();

        if (nearbyEnemy != null)
        {
            currentAIState = AIState.AttackingTarget;
            currentTarget = nearbyEnemy;
            unitInstance.SetAttackTarget(nearbyEnemy);
        }
        else if (playerCastle != null)
        {
            currentAIState = AIState.SeekingCastle;
            IDamageable blocking = FindBlockingBuilding();

            if (blocking != null)
            {
                currentAIState = AIState.AttackingBlockingBuilding;
                currentTarget = blocking;
                unitInstance.SetAttackTarget(blocking);
            }
            else
            {
                currentTarget = null;
                unitInstance.ClearAttackTarget();
                MoveTowardsCastle();
            }
        }
        else
        {
            currentAIState = AIState.Idle;
            currentTarget = null;
            unitInstance.ClearAttackTarget();
        }
    }

    private void MoveTowardsCastle()
    {
        if (playerCastle == null) return;

        if (unitInstance.state != UnitState.Moving || ShouldRecalculatePath())
        {
            GridNode targetNode = gridManager.GetNearestWalkableNode(playerCastle.position);
            if (targetNode != null)
                unitInstance.MoveTo(targetNode);
        }
    }

    private bool ShouldRecalculatePath()
    {
        if (playerCastle == null || unitInstance.CurrentPath == null || unitInstance.CurrentPath.Count == 0)
            return true;

        Vector3 currentDest = unitInstance.CurrentPath[^1].WorldPosition;
        float distance = Vector3.Distance(currentDest, playerCastle.position);
        return distance > 3f;
    }

    private IDamageable FindNearestEnemy() { /* ... */ return null; }
    private IDamageable FindBlockingBuilding() { /* ... */ return null; }

    private float CalculatePathDistance(List<GridNode> path)
    {
        float dist = 0f;
        for (int i = 1; i < path.Count; i++)
            dist += Vector3.Distance(path[i - 1].WorldPosition, path[i].WorldPosition);
        return dist;
    }

    public void SetCastleTarget(Transform castle)
    {
        playerCastle = castle;
    }

    public IDamageable GetCurrentTarget() => currentTarget;
    public AIState GetCurrentState() => currentAIState;

    public void GetBuildingEdge()
    {

    }
    private void OnDestroy()
    {
        if (aiCoroutine != null)
            StopCoroutine(aiCoroutine);

        if (AIManager.Instance != null)
            AIManager.Instance.UnregisterEnemyAI(this);
    }
}

