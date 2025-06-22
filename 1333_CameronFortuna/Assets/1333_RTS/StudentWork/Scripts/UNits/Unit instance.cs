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


    public int AttackRange => unitType?.Range ?? 1;

    public UnitBase CurrentTarget;

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

    }
    private void Update()
    {
        if (state == UnitState.Moving)
        { DoMove(); }

        Attackmode();

        if (Input.GetKeyDown(KeyCode.O))
        { IsThereEnemy();
        }
        //SetNodeOccipied();
    }
    //setting up the pathfinding, visuals, and team context for each unit
    public void Initialize(AStartPathfinding pathfinder, UnitType unitType, GridManager grid, PathFinderVisualization pathFinderVis, int armyID)
    {
        this.pathfinder = pathfinder;
        this.unitType = unitType ?? ScriptableObject.CreateInstance<UnitType>();
        gridManager = grid;
        this.pathFinderVisulization = pathFinderVis;
        this.armyID = armyID;

        // Find the army manager for this army ID
        FindArmyManager();

        Debug.Log($"Unit {name} initialized - UnitType: {this.unitType?.name}, MoveSpeed: {this.unitType?.MoveSpeed}, ArmyID: {this.armyID}");

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
            Debug.Log($"{name} reached waypoint {pathIndex}/{currentPath.Count}");
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
    public void Attackmode()
    {
        if (CurrentTarget != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, CurrentTarget.transform.position);
            if (distanceToTarget <= AttackRange)
            {
                state = UnitState.Attacking;
                Debug.Log("is attacking now");
            }
        }
    }
    public void SetNodeOccipied()
    {
         currentNodeUnitON = gridManager.GetNodeFromWorldPosition(CurrentTarget.transform.position);
            
        //currentNodeUnitON.IsOccupied = true;
    }

}