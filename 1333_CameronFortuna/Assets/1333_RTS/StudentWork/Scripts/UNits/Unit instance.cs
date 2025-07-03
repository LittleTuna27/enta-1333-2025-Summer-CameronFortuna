using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;

public class UnitInstance : UnitBase
{
    [Header("Component References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SkinnedMeshRenderer unitSkin;
    [SerializeField] private ParticleSystem hurtParticles;
    [SerializeField] private PathFinderVisualization pathFinderVisulization;

    [Header("Army Settings")]
    [SerializeField] private int armyID = 0; // The army this unit belongs to
    private CurrentTeamArmyManager armyManager;


    private GridManager gridManager;
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

    public UnitBase CurrentTarget;
    public int currentHealth;

    //showing the state the unit is in

    public UnitState state
    {
        get => _state;
        set
        {
            if (_state != value)
            {
                Debug.Log($"{name} state changed from {_state} to {value}");
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
    }

    private void Update()
    {
        if (state == UnitState.Moving)
        { DoMove(); }

        Attackmode();

        if (Input.GetKeyDown(KeyCode.O))
        {
            IsThereEnemy();
        }
        //SetNodeOccipied();
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
    private void FindEnemyArmyManager()
    {
        // Find army managers that are NOT this unit's army
        CurrentTeamArmyManager[] managers = FindObjectsOfType<CurrentTeamArmyManager>();
        foreach (var manager in managers)
        {
            if (manager.armyID != armyID) // Different army = enemy
            {
                enemyArmyManager = manager;
                break;
            }
        }

        if (enemyArmyManager == null)
        {
            Debug.LogWarning($"{name}: Could not find enemy army manager");
        }
    }

    public void SetArmyID(int newArmyID)
    {
        if (armyID != newArmyID)
        {
            int oldArmyID = armyID;
            armyID = newArmyID;
            FindArmyManager(); // Find new army manager

            Debug.Log($"{name} switched from Army {oldArmyID} to Army {armyID}");
        }
    }

    //telling the unit to move to a certain node
    //handling getting the nodes and call pathfinding and visualizer to get the math and draw the path
    public override void MoveTo(GridNode targetNode)
    {
        GridNode startNode = gridManager.GetNodeFromWorldPosition(transform.position);

        List<GridNode> searched;
        currentPath = pathfinder.FindPath(gridManager, startNode, targetNode, out searched);
        Debug.Log($"{name} path found with {currentPath?.Count ?? 0} nodes.");

        // visualize the chosen path
        if (pathFinderVisulization != null)
        {
            pathFinderVisulization.DrawPath(currentPath);
        }
        else
        {
            Debug.LogWarning($"{name} has no pathFinderVisulization assigned!");
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
    public void IsThereEnemy()
    {
        // Add this null check at the beginning
        if (enemyArmyManager == null)
        {
            FindEnemyArmyManager();
            if (enemyArmyManager == null)
            {
                Debug.LogWarning($"{name}: No enemy army manager found!");
                return;
            }
        }

        CurrentTarget = null;
        float closestDistance = Mathf.Infinity;

        Vector3 myPosition = transform.position;

        for (int i = 0; i < enemyArmyManager.currentlyActiveUnits.Count; i++)
        {
            UnitBase enemy = enemyArmyManager.currentlyActiveUnits[i];
            float distance = Vector3.Distance(myPosition, enemy.transform.position);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                CurrentTarget = enemy;
            }
        }

        if (CurrentTarget != null)
        {
            GridNode targetNode = gridManager.GetNodeFromWorldPosition(CurrentTarget.transform.position);
            if (targetNode != null)
            {
                MoveTo(targetNode);
            }
        }
    }
    public UnitBase AttackTarget { get; private set; }

    public void SetAttackTarget(UnitBase target)
    {
        AttackTarget = target;
        CurrentTarget = target; // Also set as current target for existing attack logic

        // Move towards the target
        if (target != null)
        {
            GridNode targetNode = gridManager.GetNodeFromWorldPosition(target.transform.position);
            if (targetNode != null)
            {
                MoveTo(targetNode);
                state = UnitState.Moving;
            }
        }
    }

    public void ClearAttackTarget()
    {
        AttackTarget = null;
        CurrentTarget = null;
        // Optionally change state back to idle if not doing anything else
        if (state == UnitState.Attacking)
        {
            state = UnitState.Idle;
        }
    }
    public void Attackmode()
    {
        // Check if we have a specific attack target
        if (AttackTarget != null)
        {
            // Check if target is still valid (not destroyed)
            if (AttackTarget == null || AttackTarget.gameObject == null)
            {
                ClearAttackTarget();
                return;
            }

            float distanceToTarget = Vector3.Distance(transform.position, AttackTarget.transform.position);

            if (distanceToTarget <= AttackRange)
            {
                // Target is in range - we should be in attacking state
                state = UnitState.Attacking;

                // Check if enough time has passed since last attack
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformAttack(AttackTarget);
                    lastAttackTime = Time.time;
                }
            }
            else
            {
                // Target is out of range, keep moving towards it
                if (state != UnitState.Moving)
                {
                    GridNode targetNode = gridManager.GetNodeFromWorldPosition(AttackTarget.transform.position);
                    if (targetNode != null)
                    {
                        MoveTo(targetNode);
                    }
                }
            }
        }
        // Keep your existing logic for auto-found enemies as fallback
        else if (CurrentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, CurrentTarget.transform.position);
            if (distanceToTarget <= AttackRange)
            {
                // Target is in range - we should be in attacking state
                state = UnitState.Attacking;

                // Check cooldown for auto-found targets too
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformAttack(CurrentTarget);
                    lastAttackTime = Time.time;
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
    private void PerformAttack(UnitBase target)
    {

        if (target == null) return;

        Debug.Log($"{name} attacks {target.name} for {AttackDamage} damage!");

        // Deal damage
        UnitInstance targetUnit = target as UnitInstance;
        if (targetUnit != null)
        {
            targetUnit.TakeDamage(AttackDamage);
        }

        if (target == null)
        {
            state = UnitState.Idle;
        }
    }
    public void TakeDamage(int damage)
    {
        // Calculate actual damage after defense
        int actualDamage = Mathf.Max(1, damage - Defense); // Minimum 1 damage
        currentHealth -= actualDamage;

        Debug.Log($"{name} took {actualDamage} damage ({damage} - {Defense} defense). Health: {currentHealth}/{MaxHealth}");

        // Check if unit is dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        Debug.Log($"{name} has died!");

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

        // Optional: Play death animation/effects before destroying
        Destroy(gameObject);
    }
    void OnDrawGizmosSelected()
    {
        if (AttackTarget != null)
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, AttackRange);
            Gizmos.DrawLine(transform.position, AttackTarget.transform.position);

            // Show cooldown status
            float cooldownProgress = (Time.time - lastAttackTime) / attackCooldown;
            if (cooldownProgress < 1f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireCube(transform.position + Vector3.up * 2f, Vector3.one * 0.5f);
            }
        }
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
 
}