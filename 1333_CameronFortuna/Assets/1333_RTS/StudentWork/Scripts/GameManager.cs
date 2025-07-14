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
        if (SoundPracticePlayer.Instance != null)
        {
            SoundPracticePlayer.Instance.PlayLoopingSound(0, AudioSourceType.Music);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            gridManager.InitializeGrid();
        }
    }
}
