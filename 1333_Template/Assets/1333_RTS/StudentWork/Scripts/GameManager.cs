using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private UnitManager unitManager;
    [SerializeField] private PathFinderVisulization pathFinder;
    [SerializeField] private AStartPathfinding pathfindingLogic;

    private void Awake()
    {
        gridManager.InitializeGrid();
        pathFinder.ResetFeild(); // Triggers A* pathfinding through PathFinder
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.R))
        {
           
            pathfindingLogic.ClearVisualization();

            StopAllCoroutines();
            gridManager.InitializeGrid();
            pathFinder.ResetFeild(); // Recalculates new path after grid regen
        }
    }
}
