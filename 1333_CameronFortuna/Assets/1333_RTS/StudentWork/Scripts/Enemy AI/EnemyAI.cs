using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private UnitInstance unitInstance;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float castleSearchRadius = 2f; // how close to get to castle before attacking
    [SerializeField] private float playerUnitDetectionRange = 3f;
    [SerializeField] private float retargetInterval = 1f; // how often to check for new targets

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private GameObject targetCastle;
    private GridNode targetNodeNearCastle;
    private Coroutine aiCoroutine;
    private AIState currentAIState = AIState.Idle;

    //ai states
    public enum AIState
    {
        Idle,
        MovingToCastle,
        AttackingCastle,
        AttackingPlayer,
        Searching
    }

    //gets building radius using BuildingHealth or fallback estimate
    private int GetActualBuildingRadius(Vector3 buildingCenter)
    {
        // Try to get the BuildingHealth component to get actual size
        Collider[] colliders = Physics.OverlapSphere(buildingCenter, 0.1f);
        foreach (var collider in colliders)
        {
            BuildingHealth building = collider.GetComponent<BuildingHealth>();
            if (building != null)
            {
                return building.GetBuildingRadius();
            }
        }

        // Fallback to estimation if BuildingHealth not found
        return EstimateBuildingSize(unitInstance.gridManager.GetGridPositionFromWorld(buildingCenter));
    }

    //estimates building size by scanning grid around it
    private int EstimateBuildingSize(Vector2Int centerGrid)
    {
        GridManager grid = unitInstance.gridManager;

        // Check in expanding squares to find the building's approximate size
        for (int radius = 0; radius <= 6; radius++) // Increased from 4 to 6 for larger buildings
        {
            bool foundWalkableSpace = false;

            // Check all positions at this radius from center
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Skip interior points, only check the perimeter
                    if (radius > 0 && Mathf.Abs(x) < radius && Mathf.Abs(y) < radius) continue;

                    GridNode node = grid.GetNode(centerGrid.x + x, centerGrid.y + y);
                    if (node != null && node.walkable && !node.IsOccupied)
                    {
                        foundWalkableSpace = true;
                        break;
                    }
                }
                if (foundWalkableSpace) break;
            }

            // If we found walkable space at this radius, the building extends to radius-1
            if (foundWalkableSpace)
            {
                return Mathf.Max(0, radius - 1); // Changed from Max(1, radius-1) to allow for 1x1 buildings
            }
        }

        return 1; // Default for 1x1 building instead of 2
    }

    //gets all grid nodes in a ring around a position
    private List<GridNode> GetNodesInRing(Vector2Int center, int radius)
    {
        List<GridNode> ringNodes = new List<GridNode>();
        GridManager grid = unitInstance.gridManager;

        // Get all nodes in a ring/perimeter at the given radius
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                // Only include perimeter nodes (ring edge)
                if (Mathf.Abs(x) == radius || Mathf.Abs(y) == radius)
                {
                    GridNode node = grid.GetNode(center.x + x, center.y + y);
                    if (node != null)
                    {
                        ringNodes.Add(node);
                    }
                }
            }
        }

        return ringNodes;
    }

    //checks if enemy is in range to attack a building
    private bool IsInAttackRangeOfBuilding(Vector3 buildingCenter)
    {
        GridManager grid = unitInstance.gridManager;
        if (grid == null) return false;

        Vector2Int ourGrid = grid.GetGridPositionFromWorld(transform.position);
        Vector2Int buildingGrid = grid.GetGridPositionFromWorld(buildingCenter);

        // Get the actual building size more accurately
        int buildingRadius = GetActualBuildingRadius(buildingCenter);
        int attackRange = unitInstance.AttackRange;

        // Calculate the minimum distance from our position to any edge of the building
        int deltaX = Mathf.Max(0, Mathf.Abs(ourGrid.x - buildingGrid.x) - buildingRadius);
        int deltaY = Mathf.Max(0, Mathf.Abs(ourGrid.y - buildingGrid.y) - buildingRadius);

        // Use Manhattan distance for grid-based attack range
        int distanceToEdge = deltaX + deltaY;

        Debug.Log($"{name}: Position ({ourGrid.x}, {ourGrid.y}), Building Center ({buildingGrid.x}, {buildingGrid.y}), " +
                  $"Building Radius: {buildingRadius}, Distance to Edge: {distanceToEdge}, Attack Range: {attackRange}");

        return distanceToEdge <= attackRange;
    }

    public AIState CurrentAIState => currentAIState;
    public GameObject TargetCastle => targetCastle;

    //sets up ai at start
    private void Start()
    {
        // Get UnitInstance if not assigned
        if (unitInstance == null)
        {
            unitInstance = GetComponent<UnitInstance>();
        }

        if (unitInstance == null)
        {
            Debug.LogError($"{name}: No UnitInstance found! EnemyAI requires a UnitInstance component.");
            enabled = false;
            return;
        }

        // Start AI behavior
        StartAI();
    }

    //starts ai when enabled
    private void OnEnable()
    {
        if (unitInstance != null)
        {
            StartAI();
        }
    }

    //stops ai when disabled
    private void OnDisable()
    {
        StopAI();
    }

    //starts ai behavior loop coroutine
    public void StartAI()
    {
        if (aiCoroutine != null)
        {
            StopCoroutine(aiCoroutine);
        }
        aiCoroutine = StartCoroutine(AIBehaviorLoop());
    }

    //stops ai behavior loop
    public void StopAI()
    {
        if (aiCoroutine != null)
        {
            StopCoroutine(aiCoroutine);
            aiCoroutine = null;
        }
    }

    //sets target castle reference
    public void SetTargetCastle(GameObject castle)
    {
        targetCastle = castle;
        Debug.Log($"{name}: Target castle set to {castle?.name}");
    }

    //main ai behavior loop
    private IEnumerator AIBehaviorLoop()
    {
        while (unitInstance != null && unitInstance.IsAlive)
        {
            UpdateAIBehavior();
            yield return new WaitForSeconds(retargetInterval);
        }
    }

    //updates ai behavior based on priorities
    private void UpdateAIBehavior()
    {
        if (!unitInstance.IsAlive) return;

        // Priority 1: Check for nearby player units to attack
        UnitInstance nearbyPlayerUnit = FindNearestPlayerUnit();
        if (nearbyPlayerUnit != null)
        {
            SetAIState(AIState.AttackingPlayer);
            AttackTarget(nearbyPlayerUnit);
            return;
        }

        // Priority 2: Check if we're in range of the castle
        if (targetCastle != null)
        {
            // Check if we're close enough to attack the castle
            if (IsInAttackRangeOfBuilding(targetCastle.transform.position))
            {
                BuildingHealth castleHealth = targetCastle.GetComponent<BuildingHealth>();
                if (castleHealth != null && castleHealth.IsAlive)
                {
                    SetAIState(AIState.AttackingCastle);
                    AttackTarget(castleHealth);

                    if (unitInstance.state == UnitState.Moving)
                    {
                        unitInstance.StopMovement();
                        Debug.Log($"{name}: FORCED STOP - Now attacking castle from valid position");
                    }
                    return;
                }
            }

            // Priority 2.5: ALWAYS check for blocking buildings (even when idle)
            BuildingHealth blockingWall = FindNearestBlockingBuilding();
            if (blockingWall != null)
            {
                SetAIState(AIState.AttackingCastle);
                AttackTarget(blockingWall);
                Debug.Log($"{name}: Found blocking wall {blockingWall.name} at distance {Vector3.Distance(transform.position, blockingWall.transform.position):F2}");
                return;
            }

            // Priority 3: Move toward castle only if we're not attacking
            if (currentAIState != AIState.AttackingCastle)
            {
                SetAIState(AIState.MovingToCastle);
                MoveTowardsCastle();
            }
        }
        else
        {
            if (currentAIState != AIState.Searching)
            {
                SetAIState(AIState.Searching);
                SearchForCastle();
            }
        }
    }

    //sets new ai state
    private void SetAIState(AIState newState)
    {
        if (currentAIState != newState)
        {
            Debug.Log($"{name}: AI State changed from {currentAIState} to {newState}");
            currentAIState = newState;
        }
    }

    //finds the nearest player unit in range
    private UnitInstance FindNearestPlayerUnit()
    {
        UnitInstance[] allUnits = FindObjectsOfType<UnitInstance>();
        UnitInstance nearestPlayerUnit = null;
        float nearestDistance = playerUnitDetectionRange;

        foreach (var unit in allUnits)
        {
            // Skip if it's the same army (enemy units shouldn't attack each other)
            if (unit.ArmyID == unitInstance.ArmyID || !unit.IsAlive)
                continue;

            float distance = Vector3.Distance(transform.position, unit.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlayerUnit = unit;
            }
        }

        return nearestPlayerUnit;
    }

    //moves enemy towards castle
    private void MoveTowardsCastle()
    {
        if (targetCastle == null || unitInstance.gridManager == null)
        {
            Debug.LogWarning($"{name}: Cannot move to castle - missing references");
            return;
        }

        // Don't move if we're already attacking something
        if (currentAIState == AIState.AttackingCastle)
        {
            Debug.Log($"{name}: Already attacking, not moving");
            return;
        }

        Vector3 castlePosition = targetCastle.transform.position;
        Vector3 ourPosition = transform.position;

        // Instead of trying to pathfind to the exact castle, just move in the general direction
        Vector3 directionToCastle = (castlePosition - ourPosition).normalized;

        // Move 3-5 grid squares in the direction of the castle
        Vector3 intermediateTarget = ourPosition + directionToCastle * (3f * unitInstance.gridManager.nodeSize);

        GridNode targetNode = unitInstance.gridManager.GetNearestWalkableNode(intermediateTarget);

        if (targetNode != null)
        {
            unitInstance.MoveTo(targetNode);
            Debug.Log($"{name}: Moving towards castle direction");
        }
        else
        {
            // If that fails, just try to move slightly forward
            for (int distance = 1; distance <= 3; distance++)
            {
                Vector3 closeTarget = ourPosition + directionToCastle * (distance * unitInstance.gridManager.nodeSize);
                targetNode = unitInstance.gridManager.GetNearestWalkableNode(closeTarget);

                if (targetNode != null)
                {
                    unitInstance.MoveTo(targetNode);
                    Debug.Log($"{name}: Moving forward step by step");
                    break;
                }
            }
        }
    }

    //handles attacking a target (building or unit)
    private void AttackTarget(IDamageable target)
    {
        if (target == null || !target.IsAlive)
        {
            unitInstance.ClearAttackTarget();
            return;
        }

        // FORCE STOP MOVEMENT when we start attacking
        if (unitInstance.state == UnitState.Moving)
        {
            unitInstance.StopMovement();
            Debug.Log($"{name}: Stopping movement to attack target");
        }

        // Set the attack target on the unit instance
        unitInstance.SetAttackTarget(target);
        Debug.Log($"{name}: Attacking {GetTargetName(target)}");
    }

    //searches for a castle or fallback building if none found
    private void SearchForCastle()
    {
        // Look for player castles (buildings with ArmyID different from this unit's ArmyID)
        BuildingHealth[] buildings = FindObjectsOfType<BuildingHealth>();

        foreach (var building in buildings)
        {
            // Look for enemy buildings (different army ID)
            if (building.ArmyID != unitInstance.ArmyID && building.IsAlive)
            {
                // Check if it's a castle by name
                if (IsCastle(building.BuildingName) || IsCastle(building.name))
                {
                    SetTargetCastle(building.gameObject);
                    return;
                }
            }
        }

        // If no castle found, look for any enemy building
        foreach (var building in buildings)
        {
            if (building.ArmyID != unitInstance.ArmyID && building.IsAlive)
            {
                SetTargetCastle(building.gameObject);
                Debug.Log($"{name}: No castle found, targeting building: {building.BuildingName}");
                return;
            }
        }

        Debug.LogWarning($"{name}: No enemy buildings found to target");
    }

    //checks if a building is a castle by name
    private bool IsCastle(string buildingName)
    {
        if (string.IsNullOrEmpty(buildingName)) return false;

        string lowerName = buildingName.ToLower();
        return lowerName.Contains("castle") ||
               lowerName.Contains("keep") ||
               lowerName.Contains("fortress") ||
               lowerName.Contains("citadel");
    }

    //gets name of the current target for logs
    private string GetTargetName(IDamageable target)
    {
        if (target is MonoBehaviour monoBehaviour)
        {
            return monoBehaviour.name;
        }
        else if (target is Component component)
        {
            return component.name;
        }
        return "Unknown Target";
    }

    //forces ai to update behavior
    public void ForceRetarget()
    {
        if (unitInstance != null && unitInstance.IsAlive)
        {
            UpdateAIBehavior();
        }
    }

    //returns if the unit is currently attacking
    public bool IsAttacking()
    {
        return unitInstance != null && unitInstance.AttackTarget != null;
    }

    //returns the current target's position
    public Vector3? GetCurrentTargetPosition()
    {
        if (targetCastle != null)
        {
            return targetCastle.transform.position;
        }
        return unitInstance?.GetAttackTargetPosition();
    }

    //finds the nearest enemy building blocking path
    private BuildingHealth FindNearestBlockingBuilding()
    {
        BuildingHealth[] buildings = FindObjectsOfType<BuildingHealth>();
        BuildingHealth nearestBuilding = null;
        float nearestDistance = 6f; // Increased from 4f to 6f - should catch walls 2 squares away

        Debug.Log($"{name}: Checking {buildings.Length} buildings for blocking");

        foreach (var building in buildings)
        {
            if (building.ArmyID != unitInstance.ArmyID && building.IsAlive)
            {
                float distance = Vector3.Distance(transform.position, building.transform.position);
                Debug.Log($"{name}: Building {building.name} at distance {distance:F2}, ArmyID: {building.ArmyID} vs Mine: {unitInstance.ArmyID}");

                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestBuilding = building;
                    Debug.Log($"{name}: New nearest building: {building.name} at {distance:F2}");
                }
            }
        }

        return nearestBuilding;
    }
}