using UnityEngine;
using System.Collections.Generic;

public class UnitInstance : UnitBase
{
    [Header("Component References")]
    [SerializeField] private Animator animator; // for future animation use
    [SerializeField] private SkinnedMeshRenderer unitSkin; // for visual selection
    [SerializeField] private ParticleSystem hurtParticles; // hit feedback
    [SerializeField] private PathFinderVisulization pathFinderVisulization; // draws the path visually

    private GridManager gridManager;
    protected AStartPathfinding pathfinder;

    protected List<GridNode> currentPath = new(); // stores the active path
    protected int pathIndex = 0; // current node index in the path

    private bool isMoving = false; // flag to track if unit is currently moving
    private UnitState _state = UnitState.Idle;

    public bool IsMoving => isMoving;
    public List<GridNode> CurrentPath => currentPath;

    // Property to expose and log state changes
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
        // Register this unit to the selection manager if present
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

    // Sets up pathfinding, visuals, and team context for the unit
    public void Initialize(AStartPathfinding pathfinder, UnitType unitType, GridManager grid, PathFinderVisulization pathFinderVis)
    {
        this.pathfinder = pathfinder;
        this.unitType = unitType ?? ScriptableObject.CreateInstance<UnitType>();
        gridManager = grid;
        this.pathFinderVisulization = pathFinderVis;

        Debug.Log($"Unit {name} initialized - UnitType: {this.unitType?.name}, MoveSpeed: {this.unitType?.MoveSpeed}");

        // Validate references
        if (this.pathfinder == null) Debug.LogError($"{name}: pathfinder is null!");
        if (this.unitType == null) Debug.LogError($"{name}: unitType is null!");
        if (gridManager == null) Debug.LogError($"{name}: gridManager is null!");
    }

    // Public method to command the unit to move to a target node
    public override void MoveTo(GridNode targetNode)
    {
        GridNode startNode = gridManager.GetNodeFromWorldPosition(transform.position);

        List<GridNode> searched;
        currentPath = pathfinder.FindPath(gridManager, startNode, targetNode, out searched);
        Debug.Log($"{name} path found with {currentPath?.Count ?? 0} nodes.");

        // Visualize the chosen path
        if (pathFinderVisulization != null)
        {
            pathFinderVisulization.DrawPath(currentPath);
        }
        else
        {
            Debug.LogWarning($"{name} has no pathFinderVisulization assigned!");
        }

        // Begin path traversal if valid
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

    // Resets path and movement counters to start walking
    public void StartPathMovement(List<GridNode> path)
    {
        currentPath = path;
        pathIndex = 0;
        isMoving = true;
        state = UnitState.Moving;

        Debug.Log($"{name} is beginning movement along path with {path?.Count ?? 0} nodes.");
    }

    // Called each frame while moving to progress along the path
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
            Debug.Log($"{name} reached waypoint {pathIndex}/{currentPath.Count}");
        }

        if (pathIndex >= currentPath.Count)
        {
            isMoving = false;
            state = UnitState.Idle;
            Debug.Log($"{name} reached destination.");
        }
    }

    // moveing the uit every tick
    public override void PerTick()
    {
        if (state == UnitState.Moving)
            DoMove();
    }

    // Called when unit is selected
    public void Select()
    {
        if (unitSkin != null)
        {
            unitSkin.material.color = Color.cyan;
        }
        Debug.Log($"{name} selected.");
    }

    // Called when unit is deselected
    public void Deselect()
    {
        if (unitSkin != null)
        {
            unitSkin.material.color = Color.white;
        }
        Debug.Log($"{name} deselected.");
    }
}