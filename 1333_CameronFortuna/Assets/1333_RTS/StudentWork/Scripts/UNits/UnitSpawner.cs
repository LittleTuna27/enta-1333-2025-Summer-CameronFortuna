using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [Header("Spawner Setup")]
    [SerializeField] private GameObject unitPrefab;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform rallyPoint;

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private AStartPathfinding pathfindingLogic;
    [SerializeField] private UnitType unitType; // Optional: you can define unit stats via ScriptableObject
    [SerializeField] private PathFinderVisualization pathVis; // Optional: for showing path on spawn
    
    private void Start()
    {
        if (gridManager == null)
            gridManager = FindAnyObjectByType<GridManager>();

        if (pathfindingLogic == null)
            pathfindingLogic = FindAnyObjectByType<AStartPathfinding>();

        if (pathVis == null)
            pathVis = FindAnyObjectByType<PathFinderVisualization>();
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha9)) // Press 1 to spawn a unit
        {
            SpawnUnitPlayer();
        }
    }
    public void SpawnUnitPlayer()
    {
        if (unitPrefab == null || spawnPoint == null || rallyPoint == null || gridManager == null || pathfindingLogic == null)
        {
            Debug.LogError("Missing references for spawning!");
            return;
        }

        GameObject newUnit = Instantiate(unitPrefab, spawnPoint.position, Quaternion.identity);
        UnitInstance unit = newUnit.GetComponent<UnitInstance>();

        unit.Initialize(pathfindingLogic, unitType, gridManager, pathVis, 0);

        // Send unit to rally point
        GridNode targetNode = gridManager.GetNodeFromWorldPosition(rallyPoint.position);
        if (targetNode != null)
        {
            unit.MoveTo(targetNode);
        }
        else
        {
            Debug.LogWarning("Could not find rally node.");
        }
    }
    public void SpawnUnitEnemy()
    {
        if (unitPrefab == null || spawnPoint == null || rallyPoint == null || gridManager == null || pathfindingLogic == null)
        {
            Debug.LogError("Missing references for spawning!");
            return;
        }

        GameObject newUnit = Instantiate(unitPrefab, spawnPoint.position, Quaternion.identity);
        UnitInstance unit = newUnit.GetComponent<UnitInstance>();

        unit.Initialize(pathfindingLogic, unitType, gridManager, pathVis, 1);

        // Send unit to rally point
        GridNode targetNode = gridManager.GetNodeFromWorldPosition(rallyPoint.position);
        if (targetNode != null)
        {
            unit.MoveTo(targetNode);
        }
        else
        {
            Debug.LogWarning("Could not find rally node.");
        }
    }
}
