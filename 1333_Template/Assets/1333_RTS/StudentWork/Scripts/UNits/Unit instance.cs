using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class UnitInstance : UnitBase
{
    [SerializeField] private Animator animator; // Changed from Animator to follow camelCase convention
    [SerializeField] private SkinnedMeshRenderer unitSkin;
    [SerializeField] private ParticleSystem hurtParticles; // Fixed typo: hurtParicles -> hurtParticles
    [SerializeField] private PathFinderVisulization pathFinderVisulization;

    private GridManager gridManager;
    protected AStartPathfinding pathfinder;
    protected List<GridNode> currentPath = new();
    protected int pathIndex = 0;
    private bool isMoving = false;

    public bool IsMoving => isMoving;
    public List<GridNode> CurrentPath => currentPath;
    public UnitState state;

    private Coroutine moveRoutine;

    void Start()
    {
        // add the unit to the list so it can be managed by the selection system
        UnitSelectionManager.Instance.allUnitsList.Add(this);
        Debug.Log($"{name} registered to UnitSelectionManager.");
    }
    public void Initialize(AStartPathfinding pathfinder, UnitType unitType, GridManager grid, PathFinderVisulization pathFinderVis)
    {
        this.pathfinder = pathfinder;
        base.unitType = unitType;
        gridManager = grid;
        this.pathFinderVisulization = pathFinderVis; // <-- FIXED



    }

    

    public override void MoveTo(GridNode targetNode)
    {
        // check to make sure pathfinding is referenced
        if (pathfinder == null || targetNode == null)
        {
            Debug.LogWarning($"{name} can't move: missing pathfinder or target node.");
            return;
        }

        Debug.Log($"{name} starting pathfinding to node: {targetNode.Name}");
        // call a* pathfinding to find the path from the unit to the target node
        // target node being the one that is right clicked
        List<GridNode> searched;
        if (pathFinderVisulization == null)
        {
            Debug.LogError($"{name} has no pathFinderVisulization assigned!");
            return;
        }
       
        currentPath = pathfinder.FindPath(gridManager,
            gridManager.GetNodeFromWorldPosition(transform.position),
            targetNode,
            out searched);
        pathFinderVisulization.DrawPath(currentPath);
        Debug.Log($"{name} path found with {currentPath.Count} nodes.");
        // so long as there is a node in the path list call start path to start moving along the path
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

    public override void DoMove()
    {
        // check to see if they are moving and if there is a path to move along
        if (!isMoving || currentPath == null || pathIndex >= currentPath.Count)
        {
            isMoving = false;
            return;
        }
        // get the current node indexed in the list and move towards said node 
        Vector3 target = currentPath[pathIndex].WorldPosition + Vector3.up * 0.5f;
        transform.position = Vector3.MoveTowards(transform.position, target, unitType.MoveSpeed * Time.deltaTime);
        // once you get close enough increase index to move to the next node
        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            pathIndex++;
            Debug.Log($"{name} reached waypoint {pathIndex}/{currentPath.Count}");
        }
    }

    public virtual void PerTick()
    {
        // update movement if the unit is currently moving
        if (state == UnitState.Moving)
            DoMove();
    }

    public void Select()
    {
        // visual feedback for unit selection (commented out for now)
        // unitSkin.material.color = Color.cyan;
        Debug.Log($"{name} selected.");
    }

    public void Deselect()
    {
        // reset visual feedback when unit is deselected
        // unitSkin.material.color = Color.white;
        Debug.Log($"{name} deselected.");
    }

    public void StartPathMovement(List<GridNode> path)
    {
        // stop any existing movement routine to prevent conflicts
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        Debug.Log($"{name} is beginning movement along path.");
        // start the coroutine to handle smooth movement along the path
        moveRoutine = StartCoroutine(MoveAlongPath(path));
    }

    private IEnumerator MoveAlongPath(List<GridNode> path)
    {
        isMoving = true;

        // iterate through each node in the path
        for (int i = 0; i < path.Count; i++)
        {
            GridNode node = path[i];
            // offset the target position slightly above the ground
            Vector3 target = node.WorldPosition + Vector3.up * 0.5f;

            Debug.Log($"{name} moving to node: {node.Name} at {target}");

            // move towards the target position until close enough
            while (Vector3.Distance(transform.position, target) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, unitType.MoveSpeed * Time.deltaTime);
                yield return null; // wait for next frame
            }
        }

        Debug.Log($"{name} reached destination.");
        // movement complete, reset state
        isMoving = false;
        state = UnitState.Idle;
    }
}