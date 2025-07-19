using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("AI Settings")]
    [SerializeField] private UnitInstance unitInstance;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float castleSearchRadius = 2f; // How close to get to castle before attacking
    [SerializeField] private float playerUnitDetectionRange = 3f;
    [SerializeField] private float retargetInterval = 1f; // How often to check for new targets

    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;

    private GameObject targetCastle;
    private GridNode targetNodeNearCastle;
    private Coroutine aiCoroutine;
    private AIState currentAIState = AIState.Idle;

    // AI States
    public enum AIState
    {
        Idle,
        MovingToCastle,
        AttackingCastle,
        AttackingPlayer,
        Searching
    }

    // NEW METHOD: Find the best approach node around a multi-tile building
    private GridNode FindBestApproachNodeAroundBuilding(Vector3 buildingCenter)
    {
        GridManager grid = unitInstance.gridManager;
        if (grid == null) return null;

        // Get the building's estimated size by checking occupied nodes around the center
        Vector2Int centerGrid = grid.GetGridPositionFromWorld(buildingCenter);
        int buildingRadius = EstimateBuildingSize(centerGrid);

        // Search in expanding rings around the building
        List<GridNode> candidateNodes = new List<GridNode>();

        // Search from building edge + 1 to building edge + 3 for good approach positions
        for (int searchRadius = buildingRadius + 1; searchRadius <= buildingRadius + 3; searchRadius++)
        {
            candidateNodes.AddRange(GetNodesInRing(centerGrid, searchRadius));
        }

        // Filter for walkable nodes and sort by distance to our current position
        Vector3 ourPosition = transform.position;
        candidateNodes.RemoveAll(node => node == null || !node.walkable || node.IsOccupied);

        if (candidateNodes.Count == 0)
        {
            Debug.LogWarning($"{name}: No walkable nodes found around building");
            return grid.GetNearestWalkableNode(buildingCenter); // Fallback
        }

        // Sort by distance to our current position (closest first)
        candidateNodes.Sort((a, b) =>
        {
            float distA = Vector3.Distance(ourPosition, a.WorldPosition);
            float distB = Vector3.Distance(ourPosition, b.WorldPosition);
            return distA.CompareTo(distB);
        });

        // Return the closest walkable node
        return candidateNodes[0];
    }

    private int EstimateBuildingSize(Vector2Int centerGrid)
    {
        GridManager grid = unitInstance.gridManager;

        // Check in expanding squares to find the building's approximate size
        for (int radius = 1; radius <= 4; radius++) // Max check up to 4x4 + some buffer
        {
            bool foundWalkableInRing = false;

            // Check the perimeter of the current radius
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Only check perimeter nodes (not interior)
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius) continue;

                    GridNode node = grid.GetNode(centerGrid.x + x, centerGrid.y + y);
                    if (node != null && node.walkable && !node.IsOccupied)
                    {
                        foundWalkableInRing = true;
                        break;
                    }
                }
                if (foundWalkableInRing) break;
            }

            // If we found walkable nodes at this radius, the building is smaller than this
            if (foundWalkableInRing)
            {
                return radius - 1; // Building radius is one less than where we found walkable space
            }
        }

        return 2; // Default fallback for unknown building size
    }

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

    // Check if unit is close enough to attack a multi-tile building
    private bool IsInAttackRangeOfBuilding(Vector3 buildingCenter)
    {
        GridManager grid = unitInstance.gridManager;
        if (grid == null) return false;

        Vector2Int ourGrid = grid.GetGridPositionFromWorld(transform.position);
        Vector2Int buildingGrid = grid.GetGridPositionFromWorld(buildingCenter);

        // Estimate building size
        int buildingRadius = EstimateBuildingSize(buildingGrid);

        // Check if we're within attack range of any edge of the building
        int attackRange = unitInstance.AttackRange;

        // Calculate minimum distance to building edge
        int deltaX = Mathf.Max(0, Mathf.Abs(ourGrid.x - buildingGrid.x) - buildingRadius);
        int deltaY = Mathf.Max(0, Mathf.Abs(ourGrid.y - buildingGrid.y) - buildingRadius);
        int distanceToEdge = Mathf.Max(deltaX, deltaY); // Use Chebyshev distance for grid-based games

        return distanceToEdge <= attackRange;
    }

    public AIState CurrentAIState => currentAIState;
    public GameObject TargetCastle => targetCastle;

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

    private void OnEnable()
    {
        if (unitInstance != null)
        {
            StartAI();
        }
    }

    private void OnDisable()
    {
        StopAI();
    }

    public void StartAI()
    {
        if (aiCoroutine != null)
        {
            StopCoroutine(aiCoroutine);
        }
        aiCoroutine = StartCoroutine(AIBehaviorLoop());
    }

    public void StopAI()
    {
        if (aiCoroutine != null)
        {
            StopCoroutine(aiCoroutine);
            aiCoroutine = null;
        }
    }

    // Method to set the target castle (called by spawners or managers)
    public void SetTargetCastle(GameObject castle)
    {
        targetCastle = castle;
        Debug.Log($"{name}: Target castle set to {castle?.name}");
    }

    private IEnumerator AIBehaviorLoop()
    {
        while (unitInstance != null && unitInstance.IsAlive)
        {
            UpdateAIBehavior();
            yield return new WaitForSeconds(retargetInterval);
        }
    }

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
            // Use a more intelligent range check for multi-tile buildings
            if (IsInAttackRangeOfBuilding(targetCastle.transform.position))
            {
                BuildingHealth castleHealth = targetCastle.GetComponent<BuildingHealth>();
                if (castleHealth != null && castleHealth.IsAlive)
                {
                    SetAIState(AIState.AttackingCastle);
                    AttackTarget(castleHealth);

                    // STOP MOVEMENT - Force the unit to stop moving and attack
                    if (unitInstance.state == UnitState.Moving)
                    {
                        unitInstance.StopMovement();
                        Debug.Log($"{name}: Stopped movement to attack castle");
                    }
                    return;
                }
            }

            // Priority 3: Move toward castle if not already doing so OR if we reached destination but aren't attacking
            if (currentAIState != AIState.MovingToCastle ||
                (unitInstance.state == UnitState.Idle && currentAIState == AIState.MovingToCastle))
            {
                // Only move if we're not already trying to attack the castle
                if (currentAIState != AIState.AttackingCastle)
                {
                    SetAIState(AIState.MovingToCastle);
                    MoveTowardsCastle();
                }
            }
        }
        else
        {
            // Priority 4: Search for a castle if we don't have one
            if (currentAIState != AIState.Searching)
            {
                SetAIState(AIState.Searching);
                SearchForCastle();
            }
        }
    }

    private void SetAIState(AIState newState)
    {
        if (currentAIState != newState)
        {
            Debug.Log($"{name}: AI State changed from {currentAIState} to {newState}");
            currentAIState = newState;
        }
    }

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

    private void MoveTowardsCastle()
    {
        if (targetCastle == null || unitInstance.gridManager == null)
        {
            Debug.LogWarning($"{name}: Cannot move to castle - missing references");
            return;
        }

        // Don't move if we're already attacking the castle
        if (currentAIState == AIState.AttackingCastle)
        {
            Debug.Log($"{name}: Already attacking castle, not moving");
            return;
        }

        // Don't move if we're close enough to attack
        if (IsInAttackRangeOfBuilding(targetCastle.transform.position))
        {
            Debug.Log($"{name}: Already in attack range, switching to attack mode");
            BuildingHealth castleHealth = targetCastle.GetComponent<BuildingHealth>();
            if (castleHealth != null && castleHealth.IsAlive)
            {
                SetAIState(AIState.AttackingCastle);
                AttackTarget(castleHealth);
            }
            return;
        }

        // Find a walkable node around the castle perimeter
        Vector3 castlePosition = targetCastle.transform.position;
        GridNode targetNode = FindBestApproachNodeAroundBuilding(castlePosition);

        if (targetNode != null)
        {
            targetNodeNearCastle = targetNode;
            unitInstance.MoveTo(targetNode);
            Debug.Log($"{name}: Moving towards castle perimeter node at {targetNode.WorldPosition}");
        }
        else
        {
            Debug.LogWarning($"{name}: Could not find walkable path around castle - trying direct approach");
            // Fallback: try to get as close as possible
            GridNode fallbackNode = unitInstance.gridManager.GetNearestWalkableNode(castlePosition);
            if (fallbackNode != null)
            {
                unitInstance.MoveTo(fallbackNode);
            }
        }
    }

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

    private bool IsCastle(string buildingName)
    {
        if (string.IsNullOrEmpty(buildingName)) return false;

        string lowerName = buildingName.ToLower();
        return lowerName.Contains("castle") ||
               lowerName.Contains("keep") ||
               lowerName.Contains("fortress") ||
               lowerName.Contains("citadel");
    }

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

    // Public method to force retarget (useful for external calls)
    public void ForceRetarget()
    {
        if (unitInstance != null && unitInstance.IsAlive)
        {
            UpdateAIBehavior();
        }
    }

    // Method to check if unit is currently attacking something
    public bool IsAttacking()
    {
        return unitInstance != null && unitInstance.AttackTarget != null;
    }

    // Method to get current target position for external reference
    public Vector3? GetCurrentTargetPosition()
    {
        if (targetCastle != null)
        {
            return targetCastle.transform.position;
        }
        return unitInstance?.GetAttackTargetPosition();
    }
}