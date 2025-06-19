using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private UnitManager unitManager;
    [SerializeField] private PathFinderVisualization pathFinder;
    [SerializeField] private AStartPathfinding pathfindingLogic;
    [SerializeField] private CurrentTeamArmyManager currentTeamManager;
    [SerializeField] private CurrentTeamArmyManager EnemyTeamManager;

    private void Awake()
    {
       
    }

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.L))
        {
           
            pathfindingLogic.ClearVisualization();

            StopAllCoroutines();
            gridManager.InitializeGrid();
            pathFinder.ResetFeild(); // Recalculates new path after grid regen
        }
    }
}
