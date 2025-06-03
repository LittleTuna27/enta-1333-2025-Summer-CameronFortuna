using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private UnitManager unitManager;
    [SerializeField] private PathFinderVisulization pathFinder;
    [SerializeField] private AStartPathfinding pathfindingLogic;
    [SerializeField] private CurrentTeamArmyManager currentTeamManager;

    private void Awake()
    {
        gridManager.InitializeGrid();
        pathFinder.ResetFeild();

        // ? Spawn a unit at (0, 0, 0)
        currentTeamManager.SpawnUnit(new Vector3(0, 0, 0));
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
