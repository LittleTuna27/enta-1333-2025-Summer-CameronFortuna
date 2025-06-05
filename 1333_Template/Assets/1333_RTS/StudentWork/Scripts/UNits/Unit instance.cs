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
    public UnitState state;

    public bool IsMoving => isMoving;
    public List<GridNode> CurrentPath => currentPath;

    private void Start()
    {
        // register with the global selection manager
        UnitSelectionManager.Instance.allUnitsList.Add(this);
        Debug.Log($"{name} registered to UnitSelectionManager.");
    }

    public void Initialize(AStartPathfinding pathfinder, UnitType unitType, GridManager grid, PathFinderVisulization pathFinderVis)
    {
        // assign dependencies at runtime from army manager
        this.pathfinder = pathfinder;
        base.unitType = unitType;
        gridManager = grid;
        this.pathFinderVisulization = pathFinderVis;
    }

    public override void MoveTo(GridNode targetNode)
    {
        // validate input
        if (pathfinder == null || targetNode == null)
        {
            Debug.LogWarning($"{name} can't move: missing pathfinder or target node.");
            return;
        }

        // find the start node from current world position
        GridNode startNode = gridManager.GetNodeFromWorldPosition(transform.position);
        List<GridNode> searched;

        // generate A* path for the uunit to follow
        currentPath = pathfinder.FindPath(gridManager, startNode, targetNode, out searched);
        Debug.Log($"{name} path found with {currentPath.Count} nodes.");

        // draws out the current path
        if (pathFinderVisulization != null)
        {
            pathFinderVisulization.DrawPath(currentPath);
        }
        else
        {
            Debug.LogWarning($"{name} has no pathFinderVisulization assigned!");
        }

        // if path is valid, start moving
        if (currentPath.Count > 0)
        {
            StartPathMovement(currentPath);
            state = UnitState.Moving;
        }
        else
        {
            Debug.LogWarning($"{name} could not find path to target.");
        }
    }

    public void StartPathMovement(List<GridNode> path)
    {
        // reset movement state
        currentPath = path;
        pathIndex = 0;
        isMoving = true;

        Debug.Log($"{name} is beginning movement along path.");
    }

    public override void DoMove()
    {
        // check if we're still allowed to move
        if (!isMoving || currentPath == null || pathIndex >= currentPath.Count)
        {
            isMoving = false;
            return;
        }

        // move toward the current target node
        Vector3 target = currentPath[pathIndex].WorldPosition + Vector3.up * 0.5f;
        transform.position = Vector3.MoveTowards(transform.position, target, unitType.MoveSpeed * Time.deltaTime);

        // advance to next node if close enough
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            pathIndex++;
            Debug.Log($"{name} reached waypoint {pathIndex}/{currentPath.Count}");
        }

        // stop when done
        if (pathIndex >= currentPath.Count)
        {
            isMoving = false;
            state = UnitState.Idle;
            Debug.Log($"{name} reached destination.");
        }
    }

    public override void PerTick()
    {
        // called by the RTS update manager
        if (state == UnitState.Moving)
            DoMove();
    }

    public void Select()
    {
        // highlight on selection (optional)
        // unitSkin.material.color = Color.cyan;
        Debug.Log($"{name} selected.");
    }

    public void Deselect()
    {
        // remove selection highlight (optional)
        // unitSkin.material.color = Color.white;
        Debug.Log($"{name} deselected.");
    }
}