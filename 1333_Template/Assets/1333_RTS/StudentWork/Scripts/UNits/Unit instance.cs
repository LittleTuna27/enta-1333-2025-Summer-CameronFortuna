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
    }

    public void Initialize(AStartPathfinding pathfinder, UnitType unitType, GridManager grid)
    {
        _pathfinder = pathfinder;
        _unitType = unitType;
        gridManager = grid;
    }
    public override void MoveTo(GridNode targetNode)
    {
        if (_pathfinder == null || targetNode == null) return;

        List<GridNode> searched;
        _currentPath = _pathfinder.FindPath(gridManager,
            gridManager.GetNodeFromWorldPosition(transform.position),
            targetNode,
            out searched);

        if (_currentPath.Count > 0)
        {
            StartPathMovement(_currentPath);
            state = UnitState.Moving;
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
    }

    public void Deselect()
    {
        _unitSkin.material.color = Color.white;
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

        moveRoutine = StartCoroutine(MoveAlongPath(path));
    }

    private IEnumerator MoveAlongPath(List<GridNode> path)
    {
        _isMoving = true;

        foreach (GridNode node in path)
        {
            Vector3 target = node.WorldPosition + Vector3.up * 0.5f;
            while (Vector3.Distance(transform.position, target) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, target, _unitType.MoveSpeed * Time.deltaTime);
                yield return null;
            }
        }

        _isMoving = false;
        state = UnitState.Idle;
    }
}