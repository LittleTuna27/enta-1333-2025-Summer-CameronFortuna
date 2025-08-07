using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class UnitInstance : UnitBase, IDamageable
{
    [Header("Component References")]
    [SerializeField] private Animator animator;
    [SerializeField] private SkinnedMeshRenderer unitSkin;
    [SerializeField] private ParticleSystem hurtParticles;
    [SerializeField] private DamageFlash damageFlash;

    [Header("Army Settings")]
    [SerializeField] private int armyID = 0; // the army this unit belongs to
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
    [SerializeField] private float attackCooldown = 2f; // attack every 2 seconds
    private float lastAttackTime = 0f;

    // properties to get values from UnitType
    public int AttackDamage => unitType?.Damage ?? 0;
    public int MaxHealth => unitType?.MaxHp ?? 100;
    public int Defense => unitType?.Defence ?? 0;
    public int AttackRange => unitType?.Range ?? 1;

    // attack target management
    public IDamageable AttackTarget { get; private set; }
    public UnitBase CurrentTarget; // legacy compatibility
    public int currentHealth;
    public int CurrentHealth => currentHealth;
    public bool IsAlive => currentHealth > 0;

    [Header("UI References")]
    [SerializeField] private Slider healthSlider;

    // return unit position in world space
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
                _state = value;
                UpdateAnimator();
            }
        }
    }

    // called on start to register unit and set health
    private void Start()
    {
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

    // initialize unit health and health bar
    private void InitializeHealth()
    {
        currentHealth = MaxHealth;
        InitializeHealthBar();
    }

    // update loop for movement and combat
    private void Update()
    {
        if (state == UnitState.Moving)
        {
            DoMove();
        }

        Attackmode();
    }

    // setup unit instance with pathfinder, type, grid, and army id
    public void Initialize(AStartPathfinding pathfinder, UnitType unitType, GridManager grid, int armyID)
    {
        this.pathfinder = pathfinder;
        this.unitType = unitType ?? ScriptableObject.CreateInstance<UnitType>();
        gridManager = grid;
        this.armyID = armyID;
        currentHealth = MaxHealth;
        FindArmyManager();

        Debug.Log($"Unit {name} initialized - UnitType: {this.unitType?.name}, " +
                  $"MoveSpeed: {this.unitType?.MoveSpeed}, Health: {currentHealth}/{MaxHealth}, " +
                  $"Damage: {AttackDamage}, Defense: {Defense}, ArmyID: {this.armyID}");

        if (this.pathfinder == null) Debug.LogError($"{name}: pathfinder is null!");
        if (this.unitType == null) Debug.LogError($"{name}: unitType is null!");
        if (gridManager == null) Debug.LogError($"{name}: gridManager is null!");
    }

    // find matching army manager for this unit
    private void FindArmyManager()
    {
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
            Debug.LogWarning($"{name}: could not find army manager for Army ID {armyID}");
        }
    }

    // move unit to target node using pathfinding
    public override void MoveTo(GridNode targetNode)
    {
        GridNode startNode = gridManager.GetNodeFromWorldPosition(transform.position);

        if (targetNode != null && (!targetNode.walkable || targetNode.IsOccupied))
        {
            Debug.Log($"{name}: target node is blocked, finding nearest walkable node");
            targetNode = gridManager.GetNearestWalkableNode(targetNode.WorldPosition);

            if (targetNode == null)
            {
                Debug.LogWarning($"{name}: could not find any walkable node near target");
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

    // start following a new path
    public void StartPathMovement(List<GridNode> path)
    {
        currentPath = path;
        pathIndex = 0;
        isMoving = true;
        state = UnitState.Moving;

        Debug.Log($"{name} is beginning movement along path with {path?.Count ?? 0} nodes.");
    }

    // handle frame-by-frame path movement
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

        Vector3 target = currentPath[pathIndex].WorldPosition + Vector3.up * 0.5f;
        float moveSpeed = unitType.MoveSpeed;

        if (moveSpeed <= 0)
        {
            Debug.LogError($"{name} has invalid move speed: {moveSpeed}");
            isMoving = false;
            state = UnitState.Idle;
            return;
        }

        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            pathIndex++;
        }

        if (pathIndex >= currentPath.Count)
        {
            isMoving = false;
            state = UnitState.Idle;
            Debug.Log($"{name} reached destination.");
        }
    }

    // visually mark unit as selected
    public void Select()
    {
        if (unitSkin != null)
        {
            unitSkin.material.color = Color.cyan;
        }
        Debug.Log($"{name} selected.");
    }

    // visually mark unit as deselected
    public void Deselect()
    {
        if (unitSkin != null)
        {
            unitSkin.material.color = Color.white;
        }
        Debug.Log($"{name} deselected.");
    }

    // assign a new attack target
    public void SetAttackTarget(IDamageable target)
    {
        AttackTarget = target;

        if (target != null && state == UnitState.Moving)
        {
            StopMovement();
        }
    }

    // clear the current attack target
    public void ClearAttackTarget()
    {
        AttackTarget = null;
    }

    // find transform of attack target
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

    // find best attack position for a building
    private GridNode FindBestAttackPositionForBuilding(Vector3 buildingCenter)
    {
        Vector2Int centerGrid = gridManager.GetGridPositionFromWorld(buildingCenter);

        BuildingHealth building = null;
        Collider buildingCollider = Physics.OverlapSphere(buildingCenter, 0.1f)
            ?.FirstOrDefault()?.GetComponent<Collider>();
        if (buildingCollider != null)
        {
            building = buildingCollider.GetComponent<BuildingHealth>();
        }

        int buildingRadius = building != null ? building.GetBuildingRadius() : 2;
        int attackRange = AttackRange;

        List<GridNode> attackPositions = new List<GridNode>();

        for (int x = -buildingRadius - attackRange; x <= buildingRadius + attackRange; x++)
        {
            for (int y = -buildingRadius - attackRange; y <= buildingRadius + attackRange; y++)
            {
                Vector2Int checkPos = new Vector2Int(centerGrid.x + x, centerGrid.y + y);
                GridNode node = gridManager.GetNode(checkPos.x, checkPos.y);

                if (node != null && node.walkable && !node.IsOccupied)
                {
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

        Vector3 ourPosition = transform.position;
        attackPositions.Sort((a, b) =>
        {
            float distA = Vector3.Distance(ourPosition, a.WorldPosition);
            float distB = Vector3.Distance(ourPosition, b.WorldPosition);
            return distA.CompareTo(distB);
        });

        return attackPositions[0];
    }

    // handle attacking logic each frame
    public void Attackmode()
    {
        if (AttackTarget != null)
        {
            Transform targetTransform = GetTargetTransform(AttackTarget);
            if (targetTransform == null || !AttackTarget.IsAlive)
            {
                ClearAttackTarget();
                return;
            }

            bool inAttackRange = IsInAttackRangeOfBuilding(AttackTarget);

            if (inAttackRange)
            {
                state = UnitState.Attacking;

                if (isMoving)
                {
                    StopMovement();
                }

                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    PerformAttack(AttackTarget);
                    lastAttackTime = Time.time;
                }
            }
            else
            {
                if (state != UnitState.Moving)
                {
                    BuildingHealth building = targetTransform.GetComponent<BuildingHealth>();
                    if (building != null)
                    {
                        GridNode targetNode = FindBestAttackPositionForBuilding(targetTransform.position);
                        if (targetNode != null)
                        {
                            MoveTo(targetNode);
                            Debug.Log($"{name}: moving to attack position for building");
                        }
                        else
                        {
                            Debug.LogWarning($"{name}: cannot find attack position for building");
                            ClearAttackTarget();
                        }
                    }
                    else
                    {
                        GridNode targetNode = gridManager.GetNearestWalkableNode(targetTransform.position);
                        if (targetNode != null)
                        {
                            MoveTo(targetNode);
                        }
                        else
                        {
                            Debug.LogWarning($"{name}: cannot find walkable path to target during attack mode");
                            ClearAttackTarget();
                        }
                    }
                }
            }
        }
        else
        {
            if (state == UnitState.Attacking)
            {
                state = UnitState.Idle;
            }
        }
    }

    // perform an attack on a target
    private void PerformAttack(IDamageable target)
    {
        if (target == null) return;

        string targetName = GetTargetName(target);
        target.TakeDamage(AttackDamage, gameObject);

        if (!target.IsAlive)
        {
            if (AttackTarget == target)
            {
                ClearAttackTarget();
            }
            if (target is UnitBase unitTarget && CurrentTarget == unitTarget)
            {
                CurrentTarget = null;
            }
            state = UnitState.Idle;
        }
    }

    // get target name for logs
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

    // apply damage and update health
    public void TakeDamage(int damage, GameObject attacker = null)
    {
        if (!IsAlive) return;

        int actualDamage = Mathf.Max(1, damage - Defense);
        int oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - actualDamage);

        damageFlash.Flash();
        UpdateHealthBar();

        Debug.Log($"{name} took {actualDamage} damage from {(attacker != null ? attacker.name : "unknown")}. Health: {currentHealth}/{MaxHealth}");

        if (currentHealth <= 0 && oldHealth > 0)
        {
            Die();
        }
    }

    // handle unit death cleanup
    public void Die()
    {
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

        if (UnitSelectionManager.Instance.selectedUnits.Contains(this))
        {
            UnitSelectionManager.Instance.selectedUnits.Remove(this);
        }

        if (armyManager != null)
        {
            armyManager.currentlyActiveUnits.Remove(this);
        }

        if (UnitSelectionManager.Instance.allUnitsList.Contains(this))
        {
            UnitSelectionManager.Instance.allUnitsList.Remove(this);
        }

        Destroy(gameObject, 0.5f);
    }

    // update animator with current state
    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetBool("IsMoving", false);
        animator.SetBool("IsAttacking", false);
        animator.SetBool("IsIdle", false);

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

    // setup health bar values
    private void InitializeHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue = MaxHealth;
            healthSlider.value = currentHealth;
            healthSlider.interactable = false;
        }
    }

    // update health bar ui
    private void UpdateHealthBar()
    {
        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }
    }

    // stop all unit movement
    public void StopMovement()
    {
        if (CurrentPath != null)
        {
            CurrentPath.Clear();
        }

        if (state == UnitState.Moving)
        {
            state = UnitState.Idle;
        }

        StopAllCoroutines();
    }

    // check if movement should stop for attack
    public bool ShouldStopMovementForAttack()
    {
        return AttackTarget != null && state == UnitState.Moving;
    }

    // get attack target position
    public Vector3? GetAttackTargetPosition()
    {
        if (AttackTarget != null)
        {
            return AttackTarget.GetPosition();
        }
        return null;
    }

    // check if target is in attack range
    private bool IsInAttackRangeOfBuilding(IDamageable target)
    {
        Transform targetTransform = GetTargetTransform(target);
        if (targetTransform == null) return false;

        BuildingHealth building = targetTransform.GetComponent<BuildingHealth>();
        if (building != null)
        {
            Vector2Int ourGrid = gridManager.GetGridPositionFromWorld(transform.position);
            Vector2Int buildingGrid = gridManager.GetGridPositionFromWorld(targetTransform.position);

            int buildingRadius = building.GetBuildingRadius();

            int deltaX = Mathf.Max(0, Mathf.Abs(ourGrid.x - buildingGrid.x) - buildingRadius);
            int deltaY = Mathf.Max(0, Mathf.Abs(ourGrid.y - buildingGrid.y) - buildingRadius);
            int distanceToEdge = deltaX + deltaY;

            return distanceToEdge <= AttackRange;
        }
        else
        {
            float distance = Vector3.Distance(transform.position, targetTransform.position);
            return distance <= AttackRange;
        }
    }
}