using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UI;

public class UnitInstance : UnitBase, IDamageable
{
    [Header("Component References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SkinnedMeshRenderer unitSkin;
    [SerializeField] private ParticleSystem hurtParticles;
    [SerializeField] private PathFinderVisualization pathFinderVisulization;

    [Header("Army Settings")]
    [SerializeField] private int armyID = 0; // The army this unit belongs to
    private CurrentTeamArmyManager armyManager;

    public GridManager gridManager;
    protected AStartPathfinding pathfinder;

    protected List<GridNode> currentPath = new();
    protected int pathIndex = 0;

    private bool isMoving = false;
    private UnitState _state = UnitState.Idle;

    public bool IsMoving => isMoving;
    public List<GridNode> CurrentPath => currentPath;
    public int ArmyID => armyID;
    public CurrentTeamArmyManager ArmyManager => armyManager;
    public CurrentTeamArmyManager enemyArmyManager;
    public GridNode currentNodeUnitON;

    [Header("Combat Settings")]
    [SerializeField] private float attackCooldown = 2f; // Attack every 2 seconds
    private float lastAttackTime = 0f;

    // Properties to get values from UnitType
    public int AttackDamage => unitType?.Damage ?? 0;
    public int MaxHealth => unitType?.MaxHp ?? 100;
    public int Defense => unitType?.Defence ?? 0;
    public int AttackRange => unitType?.Range ?? 1;

    // REFACTORED: Use IDamageable interface for flexible targeting
    public IDamageable AttackTarget { get; private set; }
    public UnitBase CurrentTarget; // Keep for backward compatibility
    public int currentHealth;
    public int CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;

    //showing the state the unit is in
    public Vector3 GetPosition()
    {
        return transform.position;
    }

    public UnitState state
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                //Debug.Log($"{name} state changed from {_state} to {value}");
                _state = value;

                // Update animator based on state
                UpdateAnimator();
            }
        }
    }

    private void Start()
    {
        //register this unit to the selection manager if present
        if (UnitSelectionManager.Instance != null)
        {
            UnitSelectionManager.Instance.allUnitsList.Add(this);
            Debug.Log($"{name} registered to UnitSelectionManager.");
        }
        else
        {
            Debug.LogWarning("UnitSelectionManager.Instance is null!");
        }
        InitializeHealth();
    }

    private void InitializeHealth()
    {
        currentHealth = MaxHealth;
        InitializeHealthBar();
    }

    private void Update()
    {
        if (state == UnitState.Moving)
        {
            DoMove();
        }

        Attackmode();
    }

    public void Initialize(AStartPathfinding pathfinder, UnitType unitType, GridManager grid, PathFinderVisualization pathFinderVis, int armyID)
    {
        this.pathfinder = pathfinder;
        this.unitType = unitType ?? ScriptableObject.CreateInstance<UnitType>();
        gridManager = grid;
        this.pathFinderVisulization = pathFinderVis;
        this.armyID = armyID;

        // Initialize health based on UnitType
        currentHealth = MaxHealth;

        // Find the army manager for this army ID
        FindArmyManager();

        Debug.Log($"Unit {name} initialized - UnitType: {this.unitType?.name}, MoveSpeed: {this.unitType?.MoveSpeed}, " +
                  $"Health: {currentHealth}/{MaxHealth}, Damage: {AttackDamage}, Defense: {Defense}, ArmyID: {this.armyID}");

        //reference check
        if (this.pathfinder == null) Debug.LogError($"{name}: pathfinder is null!");
        if (this.unitType == null) Debug.LogError($"{name}: unitType is null!");
        if (gridManager == null) Debug.LogError($"{name}: gridManager is null!");
    }

    private void FindArmyManager()
    {
        // Find the army manager that matches this unit's army ID
        CurrentTeamArmyManager[] managers = FindObjectsOfType<CurrentTeamArmyManager>();
        foreach (var manager in managers)
        {
            if (manager.armyID == armyID)
            {
                armyManager = manager;
                break;
            }
        }

        if (armyManager == null)
        {
            Debug.LogWarning($"{name}: Could not find army manager for Army ID {armyID}");
        }
    }
    //telling the unit to move to a certain node
    //handling getting the nodes and call pathfinding and visualizer to get the math and draw the path
    public override void MoveTo(GridNode targetNode)
    {
        GridNode startNode = gridManager.GetNodeFromWorldPosition(transform.position);

        // If the target node is not walkable, find the nearest walkable one
        if (targetNode != null && (!targetNode.walkable || !targetNode.IsOccupied))
        {
            Debug.Log($"{name}: Target node is blocked, finding nearest walkable node");
            targetNode = gridManager.GetNearestWalkableNode(targetNode.WorldPosition);

            if (targetNode == null)
            {
                Debug.LogWarning($"{name}: Could not find any walkable node near target");
                state = UnitState.Idle;
                return;
            }
        }
        try
        {
            List<GridNode> searched;
            currentPath = pathfinder.FindPath(gridManager, startNode, targetNode, out searched);
            Debug.Log($"{name} path found with {currentPath?.Count ?? 0} nodes.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"{name} pathfinding failed: {ex.Message}");
            currentPath = null;
        }

        //start movement path traversal if there is a path
        if (currentPath != null && currentPath.Count > 0)
        {
            StartPathMovement(currentPath);
            state = UnitState.Moving;
        }
        else
        {
            Debug.LogWarning($"{name} could not find path to target.");
            state = UnitState.Idle;
        }
    }

    //reset the path and movement counters to start walking
    public void StartPathMovement(List<GridNode> path)
    {
        currentPath = path;
        pathIndex = 0;
        isMoving = true;
        state = UnitState.Moving;

        Debug.Log($"{name} is beginning movement along path with {path?.Count ?? 0} nodes.");
    }

    //for each frame while moving to progress along the path
    public override void DoMove()
    {
        if (!isMoving || currentPath == null || pathIndex >= currentPath.Count)
        {
            if (isMoving)
            {
                isMoving = false;
                state = UnitState.Idle;
                Debug.Log($"{name} stopped moving - reached destination or invalid path.");
            }
            return;
        }
        //setting the target movement node to the indexed node and setting the units speed based of its type
        Vector3 target = currentPath[pathIndex].WorldPosition + Vector3.up * 0.5f;
        float moveSpeed = unitType.MoveSpeed;

        if (moveSpeed <= 0)
        {
            Debug.LogError($"{name} has invalid move speed: {moveSpeed}");
            isMoving = false;
            state = UnitState.Idle;
            return;
        }
        //moving towards the target and increasing the index as it gets close to the square
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            pathIndex++;
            //Debug.Log($"{name} reached waypoint {pathIndex}/{currentPath.Count}");
        }
        //once the unit reaches the end of the list stop moving
        if (pathIndex >= currentPath.Count)
        {
            isMoving = false;
            state = UnitState.Idle;
            Debug.Log($"{name} reached destination.");
        }
    }

    //selecting the unit
    public void Select()
    {
        if (unitSkin != null)
        {
            unitSkin.material.color = Color.cyan;
        }
        Debug.Log($"{name} selected.");
    }

    //deselecting the unit
    public void Deselect()
    {
        if (unitSkin != null)
        {
            unitSkin.material.color = Color.white;
        }
        Debug.Log($"{name} deselected.");
    }

    // Enhanced attack target management
    public void SetAttackTarget(IDamageable target)
    {
        AttackTarget = target;

        // Stop movement when we set an attack target
        if (target != null && state == UnitState.Moving)
        {
            StopMovement();
        }
    }

    public void ClearAttackTarget()
    {
        AttackTarget = null;
    }
    private Transform GetTargetTransform(IDamageable target)
    {
        if (target is MonoBehaviour monoBehaviour)
        {
            return monoBehaviour.transform;
        }
        else if (target is Component component)
        {
            return component.transform;
        }
        return null;
    }

    private GridNode FindBestAttackPositionForBuilding(Vector3 buildingCenter)
    {
        Vector2Int centerGrid = gridManager.GetGridPositionFromWorld(buildingCenter);

        // Try to get actual building size
        BuildingHealth building = null;
        Collider buildingCollider = Physics.OverlapSphere(buildingCenter, 0.1f)
            ?.FirstOrDefault()?.GetComponent<Collider>();
        if (buildingCollider != null)
        {
            building = buildingCollider.GetComponent<BuildingHealth>();
        }

        int buildingRadius = building != null ? building.GetBuildingRadius() : 2; // Default to 4x4 if unknown
        int attackRange = AttackRange;

        List<GridNode> attackPositions = new List<GridNode>();

        // Find all positions within attack range of the building
        for (int x = -buildingRadius - attackRange; x <= buildingRadius + attackRange; x++)
        {
            for (int y = -buildingRadius - attackRange; y <= buildingRadius + attackRange; y++)
            {
                Vector2Int checkPos = new Vector2Int(centerGrid.x + x, centerGrid.y + y);
                GridNode node = gridManager.GetNode(checkPos.x, checkPos.y);

                if (node != null && node.walkable && !node.IsOccupied)
                {
                    // Check if this position can attack the building
                    int deltaX = Mathf.Max(0, Mathf.Abs(x) - buildingRadius);
                    int deltaY = Mathf.Max(0, Mathf.Abs(y) - buildingRadius);
                    int distanceToBuilding = deltaX + deltaY;

                    if (distanceToBuilding <= attackRange)
                    {
                        attackPositions.Add(node);
                    }
                }
            }
        }

        if (attackPositions.Count == 0) return null;

        // Sort by distance to our current position
        Vector3 ourPosition = transform.position;
        attackPositions.Sort((a, b) =>
        {
            float distA = Vector3.Distance(ourPosition, a.WorldPosition);
            float distB = Vector3.Distance(ourPosition, b.WorldPosition);
            return distA.CompareTo(distB);
        });

        return attackPositions[0];
    }

    public void Attackmode()
    {
        // Check if we have a specific attack target
        if (AttackTarget != null)
        {
            // Check if target is still valid (not destroyed)
            Transform targetTransform = GetTargetTransform(AttackTarget);
            if (targetTransform == null || !AttackTarget.IsAlive)
            {
                ClearAttackTarget();
                return;
            }

            // Use building-aware attack range check
            bool inAttackRange = IsInAttackRangeOfBuilding(AttackTarget);

            if (inAttackRange)
            {
                // Target is in range - we should be in attacking state
                state = UnitState.Attacking;

                // STOP MOVEMENT - Very important for buildings!
                if (isMoving)
                {
                    StopMovement();
                    Debug.Log($"{name}: Stopped movement to attack building");
                }

                // Check if enough time has passed since last attack
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformAttack(AttackTarget);
                    lastAttackTime = Time.time;
                }
            }
            else
            {
                // Target is out of range, move closer only if not already moving
                if (state != UnitState.Moving)
                {
                    // For buildings, try to find the best attack position
                    BuildingHealth building = targetTransform.GetComponent<BuildingHealth>();
                    if (building != null)
                    {
                        GridNode targetNode = FindBestAttackPositionForBuilding(targetTransform.position);
                        if (targetNode != null)
                        {
                            MoveTo(targetNode);
                            Debug.Log($"{name}: Moving to attack position for building");
                        }
                        else
                        {
                            Debug.LogWarning($"{name}: Cannot find attack position for building");
                            ClearAttackTarget();
                        }
                    }
                    else
                    {
                        // For units, use nearest walkable node
                        GridNode targetNode = gridManager.GetNearestWalkableNode(targetTransform.position);
                        if (targetNode != null)
                        {
                            MoveTo(targetNode);
                        }
                        else
                        {
                            Debug.LogWarning($"{name}: Cannot find walkable path to target during attack mode");
                            ClearAttackTarget();
                        }
                    }
                }
            }
        }
        else
        {
            // NO TARGET - Stop attacking!
            if (state == UnitState.Attacking)
            {
                state = UnitState.Idle;
            }
        }
    }

    private void PerformAttack(IDamageable target)
    {
        if (target == null) return;

        string targetName = GetTargetName(target);
        Debug.Log($"{name} attacks {targetName} for {AttackDamage} damage!");

        // Deal damage
        target.TakeDamage(AttackDamage, gameObject);

        // If target died, clear it
        if (!target.IsAlive)
        {
            if (AttackTarget == target)
            {
                ClearAttackTarget();
            }
            // Clear legacy target if it's the same
            if (target is UnitBase unitTarget && CurrentTarget == unitTarget)
            {
                CurrentTarget = null;
            }
            state = UnitState.Idle;
        }
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

    public void TakeDamage(int damage, GameObject attacker = null)
    {
        if (!IsAlive) return;


        // Calculate actual damage after defense
        int actualDamage = Mathf.Max(1, damage - Defense);
        int oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - actualDamage);

        UpdateHealthBar();

        Debug.Log($"{name} took {actualDamage} damage from {(attacker != null ? attacker.name : "unknown")}. Health: {currentHealth}/{MaxHealth}");

        // Check if unit died
        if (currentHealth <= 0 && oldHealth > 0)
        {
            Die();
        }
    }
    public void Die()
    {
        // Clear any attack targets pointing to this unit
        UnitInstance[] allUnits = FindObjectsOfType<UnitInstance>();
        foreach (var unit in allUnits)
        {
            if (unit.AttackTarget == this)
            {
                unit.ClearAttackTarget();
            }
            if (unit.CurrentTarget == this)
            {
                unit.CurrentTarget = null;
            }
        }
        // Remove from selection if selected
        if (UnitSelectionManager.Instance.selectedUnits.Contains(this))
        {
            UnitSelectionManager.Instance.selectedUnits.Remove(this);
        }
        // Remove from army manager
        if (armyManager != null)
        {
            armyManager.currentlyActiveUnits.Remove(this);
        }

        // Remove from all units list
        if (UnitSelectionManager.Instance.allUnitsList.Contains(this))
        {
            UnitSelectionManager.Instance.allUnitsList.Remove(this);
        }

        Destroy(gameObject, 0.5f); // Small delay for effects
    }
    private void UpdateAnimator()
    {
        if (animator == null) return;

        // Reset movement and idle states
        animator.SetBool("IsMoving", false);
        animator.SetBool("IsAttacking", false);
        animator.SetBool("IsIdle", false);

        // Set the appropriate state
        switch (_state)
        {
            case UnitState.Moving:
                animator.SetBool("IsMoving", true);
                break;
            case UnitState.Attacking:
                animator.SetBool("IsAttacking", true);
                break;
            case UnitState.Idle:
                animator.SetBool("IsIdle", true);
                break;
        }
    }
    private void InitializeHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = MaxHealth;
            healthSlider.value = currentHealth;
            healthSlider.interactable = false; // Players can't drag it
        }
    }
    private void UpdateHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }
    public void StopMovement()
    {
        // Clear the current path
        if (CurrentPath != null)
        {
            CurrentPath.Clear();
        }

        // Set state to idle if currently moving
        if (state == UnitState.Moving)
        {
            state = UnitState.Idle;
        }

        // Stop any movement coroutines if they exist
        // Note: Adjust this based on your actual movement implementation
        StopAllCoroutines();

        Debug.Log($"{name}: Movement stopped");
    }


    // Method to check if unit should stop moving when attacking
    public bool ShouldStopMovementForAttack()
    {
        return AttackTarget != null && state == UnitState.Moving;
    }

    // Get current target position for AI calculations
    public Vector3? GetAttackTargetPosition()
    {
        if (AttackTarget != null)
        {
            return AttackTarget.GetPosition();
        }
        return null;
    }
    private bool IsInAttackRangeOfBuilding(IDamageable target)
    {
        Transform targetTransform = GetTargetTransform(target);
        if (targetTransform == null) return false;

        // Check if target is a building
        BuildingHealth building = targetTransform.GetComponent<BuildingHealth>();
        if (building != null)
        {
            Vector2Int ourGrid = gridManager.GetGridPositionFromWorld(transform.position);
            Vector2Int buildingGrid = gridManager.GetGridPositionFromWorld(targetTransform.position);

            // Use actual building size
            int buildingRadius = building.GetBuildingRadius();

            // Calculate distance to building edge
            int deltaX = Mathf.Max(0, Mathf.Abs(ourGrid.x - buildingGrid.x) - buildingRadius);
            int deltaY = Mathf.Max(0, Mathf.Abs(ourGrid.y - buildingGrid.y) - buildingRadius);
            int distanceToEdge = deltaX + deltaY;

            bool inRange = distanceToEdge <= AttackRange;

            //Debug.Log($"{name}: Building attack check - Distance to edge: {distanceToEdge}, Attack range: {AttackRange}, In range: {inRange}, Building size: {building.BuildingSize}");

            return inRange;
        }
        else
        {
            // For regular units, use simple distance
            float distance = Vector3.Distance(transform.position, targetTransform.position);
            return distance <= AttackRange;
        }
    }

    private int GetBuildingRadius(IDamageable target)
    {
        Transform targetTransform = GetTargetTransform(target);
        if (targetTransform == null) return 1;

        BuildingHealth building = targetTransform.GetComponent<BuildingHealth>();
        if (building != null)
        {
            // Use actual building size if available
            return building.GetBuildingRadius();
        }

        // Fallback for units or buildings without size info
        return 0;
    }
}