using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnifiedEnemyAI;

public class UnifiedEnemyAI : MonoBehaviour
{
    public enum AIState
    {
        MovingToCastle,
        AttackingEnemy,
        AttackingCastle,
        AttackingBlockingObject,
        Idle
    }

    [Header("AI Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float castleAttackRange = 4f;  // Larger than castle size (4x4)

    [Header("Target References")]
    [SerializeField] private Transform playerCastle;

    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private UnitInstance unitInstance;
    private GridManager gridManager;
    private AIState currentState = AIState.MovingToCastle;
    private IDamageable currentTarget;
    private float lastUpdateTime;
    private bool isInitialized = false;

    void Start()
    {
        StartCoroutine(DelayedInitialize());
    }

    private IEnumerator DelayedInitialize()
    {
        // Wait a frame for other components to initialize
        yield return null;

        unitInstance = GetComponent<UnitInstance>();
        gridManager = FindObjectOfType<GridManager>();

        if (!unitInstance || !gridManager)
        {
            Debug.LogError($"UnifiedEnemyAI on {name} missing required components.");
            enabled = false;
            yield break;
        }

        // Get settings from AIManager if available
        if (AIManager.Instance != null)
        {
            playerCastle = AIManager.Instance.PlayerCastle;
            detectionRange = AIManager.Instance.GlobalDetectionRange;
            // DON'T override castleAttackRange - keep it at 2f
            updateInterval = AIManager.Instance.GlobalPathfindingInterval;
            AIManager.Instance.RegisterEnemyAI(this);
        }

        // Find castle if not assigned
        if (playerCastle == null)
        {
            playerCastle = FindPlayerCastle();
        }

        isInitialized = true;

        if (showDebugLogs)
            Debug.Log($"UnifiedEnemyAI initialized for {name}");
    }

    void Update()
    {
        if (!isInitialized || !unitInstance || !unitInstance.IsAlive) return;

        // Throttle updates for performance
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;

        UpdateAIBehavior();
    }

    private void UpdateAIBehavior()
    {
        // Priority 1: Look for nearby enemies to fight
        IDamageable nearbyEnemy = FindNearestEnemy();
        if (nearbyEnemy != null)
        {
            EngageTarget(nearbyEnemy, AIState.AttackingEnemy);
            return;
        }

        // Priority 2: Check if we're at the castle
        if (playerCastle != null)
        {
            float distanceToCastle = Vector3.Distance(transform.position, playerCastle.position);

            if (distanceToCastle <= castleAttackRange)
            {
                // We're at the castle - attack it!
                BuildingHealth castleHealth = playerCastle.GetComponent<BuildingHealth>();
                if (castleHealth != null && castleHealth.IsAlive)
                {
                    EngageTarget(castleHealth, AIState.AttackingCastle);
                    return;
                }
            }
        }

        // Priority 3: Look for blocking objects on our path
        IDamageable blocker = FindBlockingObject();
        if (blocker != null)
        {
            EngageTarget(blocker, AIState.AttackingBlockingObject);
            return;
        }

        // Priority 4: Move toward castle
        if (playerCastle != null)
        {
            MoveTowardsCastle();
        }
        else
        {
            currentState = AIState.Idle;
        }
    }

    private void EngageTarget(IDamageable target, AIState newState)
    {
        // Stop movement immediately
        if (unitInstance.state == UnitState.Moving)
        {
            unitInstance.StopMovement();
        }

        // Update state and target
        currentState = newState;
        currentTarget = target;

        // Use UnitInstance's built-in attack system
        unitInstance.SetAttackTarget(target);

        if (showDebugLogs)
            Debug.Log($"{name}: Engaging {GetTargetName(target)} - State: {newState} - Using UnitInstance attack system");
    }

    private void MoveTowardsCastle()
    {
        if (playerCastle == null) return;

        currentState = AIState.MovingToCastle;
        currentTarget = null;

        // Clear any attack target when moving
        unitInstance.ClearAttackTarget();

        // Only recalculate path if we're not moving or path is outdated
        if (unitInstance.state != UnitState.Moving || ShouldRecalculatePath())
        {
            // Use GridManager's method with larger search radius for castle
            GridNode targetNode = gridManager.GetNearestWalkableNode(playerCastle.position, 15);
            if (targetNode != null)
            {
                unitInstance.MoveTo(targetNode);

                if (showDebugLogs)
                    Debug.Log($"{name}: Moving toward castle - targeting node at {targetNode.WorldPosition}");
            }
            else if (showDebugLogs)
            {
                Debug.LogWarning($"{name}: Could not find any walkable node near castle!");
            }
        }
    }

    private GridNode FindWalkableNodeNearCastle()
    {
        if (playerCastle == null || gridManager == null) return null;

        Vector3 castlePos = playerCastle.position;

        // Try different distances from the castle to find a walkable node
        float[] searchDistances = { 2f, 3f, 4f, 5f, 6f }; // Search in expanding circles

        foreach (float distance in searchDistances)
        {
            // Try 8 directions around the castle
            Vector3[] directions = {
                Vector3.forward,    // North
                Vector3.back,       // South  
                Vector3.left,       // West
                Vector3.right,      // East
                (Vector3.forward + Vector3.right).normalized,   // NE
                (Vector3.forward + Vector3.left).normalized,    // NW
                (Vector3.back + Vector3.right).normalized,      // SE
                (Vector3.back + Vector3.left).normalized        // SW
            };

            foreach (Vector3 direction in directions)
            {
                Vector3 testPosition = castlePos + direction * distance;

                // Use GridManager's method that properly checks BOTH walkable AND not occupied
                GridNode testNode = gridManager.GetNearestWalkableNode(testPosition, 2);

                if (testNode != null)
                {
                    if (showDebugLogs)
                        Debug.Log($"{name}: Found walkable node near castle at distance {distance}, position {testNode.WorldPosition}");
                    return testNode;
                }
            }
        }

        // Fallback: use the original method with larger search radius
        if (showDebugLogs)
            Debug.LogWarning($"{name}: Could not find walkable node around castle, using fallback method");

        return gridManager.GetNearestWalkableNode(castlePos, 15);
    }
    {
        if (playerCastle == null || unitInstance.CurrentPath == null || unitInstance.CurrentPath.Count == 0)
            return true;

        Vector3 currentDest = unitInstance.CurrentPath[^1].WorldPosition;
    float distance = Vector3.Distance(currentDest, playerCastle.position);
        return distance > 3f;
    }

private IDamageable FindNearestEnemy()
{
    Collider[] colliders = Physics.OverlapSphere(transform.position, detectionRange);
    IDamageable closestEnemy = null;
    float closestDistance = float.MaxValue;

    foreach (var collider in colliders)
    {
        IDamageable target = collider.GetComponent<IDamageable>();
        if (target == null)
        {
            target = collider.GetComponentInParent<IDamageable>();
        }

        if (target == null || !target.IsAlive) continue;

        if (IsEnemy(target))
        {
            float distance = Vector3.Distance(transform.position, collider.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestEnemy = target;
            }
        }
    }

    return closestEnemy;
}

private IDamageable FindBlockingObject()
{
    if (unitInstance.CurrentPath == null) return null;

    foreach (var node in unitInstance.CurrentPath)
    {
        if (!node.walkable && node.OccupyingObject != null)
        {
            IDamageable blocker = node.OccupyingObject.GetComponent<IDamageable>();
            if (blocker != null && blocker.IsAlive && IsEnemy(blocker))
            {
                return blocker;
            }
        }
    }

    return null;
}

private bool IsEnemy(IDamageable target)
{
    if (target is UnitInstance unit)
        return unit.ArmyID != unitInstance.ArmyID;
    if (target is BuildingHealth building)
        return building.ArmyID != unitInstance.ArmyID;
    return true;
}

private Transform FindPlayerCastle()
{
    // Try tagged approach first
    GameObject taggedCastle = GameObject.FindWithTag("Castle");
    if (taggedCastle != null) return taggedCastle.transform;

    // Fallback: search for BuildingHealth components
    BuildingHealth[] buildings = FindObjectsOfType<BuildingHealth>();
    foreach (var building in buildings)
    {
        if (building.ArmyID == 0 && building.name.ToLower().Contains("castle"))
        {
            return building.transform;
        }
    }
    return null;
}

private string GetTargetName(IDamageable target)
{
    if (target is MonoBehaviour mono) return mono.name;
    if (target is Component comp) return comp.name;
    return "Unknown Target";
}

// Public methods for external control
public void SetCastleTarget(Transform castle)
{
    playerCastle = castle;
    if (showDebugLogs)
        Debug.Log($"{name}: Castle target set to {castle?.name}");
}

public void SetDebugLogs(bool enabled)
{
    showDebugLogs = enabled;
}

// Public getters for debugging/UI
public AIState GetCurrentState() => currentState;
public IDamageable GetCurrentTarget() => currentTarget;
public bool IsAtCastle() => currentState == AIState.AttackingCastle;
public Transform GetCastleTarget() => playerCastle;

void OnDestroy()
{
    if (AIManager.Instance != null)
    {
        AIManager.Instance.UnregisterEnemyAI(this);
    }
}

void OnDrawGizmosSelected()
{
    if (!isInitialized) return;

    // Draw detection range
    Gizmos.color = Color.yellow;
    Gizmos.DrawWireSphere(transform.position, detectionRange);

    // Draw castle attack range
    Gizmos.color = Color.red;
    Gizmos.DrawWireSphere(transform.position, castleAttackRange);

    // Draw line to castle
    if (playerCastle != null)
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, playerCastle.position);
    }
}
}