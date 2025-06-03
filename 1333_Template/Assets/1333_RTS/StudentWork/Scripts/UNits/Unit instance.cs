using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class UnitInstance : UnitBase
{
    [SerializeField] private Animator Animator;
    [SerializeField] private SkinnedMeshRenderer _unitSkin;
    [SerializeField] private ParticleSystem _hurtParicles;

    private GridManager gridManager;
    protected AStartPathfinding _pathfinder;
    protected List<GridNode> _currentPath = new();
    protected int _pathIndex = 0;
    private bool _isMoving = false;

    public bool IsMoving => _isMoving;
    public List<GridNode> CurrentPath => _currentPath;
    public UnitState state;

    private Coroutine moveRoutine;

    void Start()
    {
        UnitSelectionManager.Instance.allUnitsList.Add(this);
        Debug.Log($"{name} registered to UnitSelectionManager.");
    }

    public void Initialize(AStartPathfinding pathfinder, UnitType unitType, GridManager grid)
    {
        _pathfinder = pathfinder;
        _unitType = unitType;
        gridManager = grid;

        Debug.Log($"{name} initialized with pathfinder and grid.");
    }

    public override void MoveTo(GridNode targetNode)
    {
        if (_pathfinder == null || targetNode == null)
        {
            Debug.LogWarning($"{name} can't move: missing pathfinder or target node.");
            return;
        }

        Debug.Log($"{name} starting pathfinding to node: {targetNode.Name}");

        List<GridNode> searched;
        _currentPath = _pathfinder.FindPath(gridManager,
            gridManager.GetNodeFromWorldPosition(transform.position),
            targetNode,
            out searched);

        Debug.Log($"{name} path found with {_currentPath.Count} nodes.");

        if (_currentPath.Count > 0)
        {
            StartPathMovement(_currentPath);
            state = UnitState.Moving;
        }
        else
        {
            Debug.LogWarning($"{name} could not find path to target.");
        }
    }

    public override void DoMove()
    {
        if (!_isMoving || _currentPath == null || _pathIndex >= _currentPath.Count)
        {
            _isMoving = false;
            return;
        }

        Vector3 target = _currentPath[_pathIndex].WorldPosition + Vector3.up * 0.5f;
        transform.position = Vector3.MoveTowards(transform.position, target, _unitType.MoveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.1f)
        {
            _pathIndex++;
            Debug.Log($"{name} reached waypoint {_pathIndex}/{_currentPath.Count}");
        }
    }

    public virtual void PerTick()
    {
        if (state == UnitState.Moving)
            DoMove();
    }

    public void Select()
    {
        _unitSkin.material.color = Color.cyan;
        Debug.Log($"{name} selected.");
    }

    public void Deselect()
    {
        _unitSkin.material.color = Color.white;
        Debug.Log($"{name} deselected.");
    }

    void OnDestroy()
    {
        if (UnitSelectionManager.Instance != null)
            UnitSelectionManager.Instance.allUnitsList.Remove(this);
    }

    public void StartPathMovement(List<GridNode> path)
    {
        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        Debug.Log($"{name} is beginning movement along path.");
        moveRoutine = StartCoroutine(MoveAlongPath(path));
    }

    private IEnumerator MoveAlongPath(List<GridNode> path)
    {
        _isMoving = true;

        for (int i = 0; i < path.Count; i++)
        {
            GridNode node = path[i];
            Vector3 target = node.WorldPosition + Vector3.up * 0.5f;

            Debug.Log($"{name} moving to node: {node.Name} at {target}");

            while (Vector3.Distance(transform.position, target) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, _unitType.MoveSpeed * Time.deltaTime);
                yield return null;
            }
        }

        Debug.Log($"{name} reached destination.");
        _isMoving = false;
        state = UnitState.Idle;
    }
}
