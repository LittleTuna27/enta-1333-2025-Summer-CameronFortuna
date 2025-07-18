using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject enemyUnitPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 10f;
    [SerializeField] private int maxEnemies = 20;
    [SerializeField] private bool autoSpawn = true;

    [Header("Enemy Settings")]
    [SerializeField] private UnitType enemyUnitType;
    [SerializeField] private Transform playerCastle;

    [Header("System References")]
    [SerializeField] private CurrentTeamArmyManager enemyArmyManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private AStartPathfinding pathfinder;
    [SerializeField] private PathFinderVisualization visualizer;

    [Header("AI Settings")]
    [SerializeField] private bool useManualSpawning = false;
    [SerializeField] private bool enableDebugLogs = true;

    private Coroutine spawnCoroutine;
    private int currentEnemyCount = 0;

    void Start()
    {
        // Auto-find references if not assigned
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (pathfinder == null) pathfinder = FindObjectOfType<AStartPathfinding>();
        if (visualizer == null) visualizer = FindObjectOfType<PathFinderVisualization>();

        // Find enemy army manager (army ID 1)
        if (enemyArmyManager == null && !useManualSpawning)
        {
            CurrentTeamArmyManager[] managers = FindObjectsOfType<CurrentTeamArmyManager>();
            foreach (var manager in managers)
            {
                if (manager.armyID == 1)
                {
                    enemyArmyManager = manager;
                    break;
                }
            }
        }

        // Auto-find player castle if not assigned
        if (playerCastle == null)
        {
            playerCastle = FindPlayerCastle();
        }

        if (autoSpawn && spawnPoints.Length > 0)
        {
            spawnCoroutine = StartCoroutine(AutoSpawnRoutine());
        }

        Debug.Log($"EnemySpawner initialized - Castle: {playerCastle?.name}");
    }

    private Transform FindPlayerCastle()
    {
        // Try tagged approach first
        GameObject taggedCastle = GameObject.FindWithTag("Castle");
        if (taggedCastle != null) return taggedCastle.transform;

        // Fallback: search for BuildingHealth components
        BuildingHealth[] buildings = FindObjectsOfType<BuildingHealth>();
        foreach (var building in buildings)
        {
            if (building.ArmyID == 0) // Player army
            {
                if (building.name.ToLower().Contains("castle"))
                {
                    return building.transform;
                }
            }
        }
        return null;
    }

    private IEnumerator AutoSpawnRoutine()
    {
        while (autoSpawn)
        {
            yield return new WaitForSeconds(spawnInterval);

            currentEnemyCount = CountLiveEnemies();

            if (currentEnemyCount < maxEnemies)
            {
                SpawnEnemy();
            }
        }
    }

    private int CountLiveEnemies()
    {
        int count = 0;
        UnitInstance[] allUnits = FindObjectsOfType<UnitInstance>();
        foreach (var unit in allUnits)
        {
            if (unit.ArmyID == 1 && unit.IsAlive)
            {
                count++;
            }
        }
        return count;
    }

    public void SpawnEnemy()
    {
        if (enemyUnitPrefab == null || spawnPoints.Length == 0)
        {
            Debug.LogError($"{name}: Cannot spawn enemy - missing prefab or spawn points!");
            return;
        }

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        if (enemyArmyManager != null && !useManualSpawning)
        {
            SpawnWithArmyManager(spawnPoint.position);
        }
        else
        {
            SpawnEnemyManual(spawnPoint.position);
        }

        if (enableDebugLogs)
            Debug.Log($"{name}: Spawned enemy at {spawnPoint.position}");
    }

    private void SpawnWithArmyManager(Vector3 position)
    {
        enemyArmyManager.SpawnUnit(position);
        StartCoroutine(SetupNewlySpawnedUnit());
    }

    private IEnumerator SetupNewlySpawnedUnit()
    {
        yield return null;

        UnitInstance[] units = FindObjectsOfType<UnitInstance>();
        UnitInstance newestUnit = null;

        foreach (var unit in units)
        {
            if (unit.ArmyID == 1 && unit.GetComponent<UnifiedEnemyAI>() == null)
            {
                newestUnit = unit;
                break;
            }
        }

        if (newestUnit != null)
        {
            SetupUnifiedAI(newestUnit);
        }
    }

    private void SpawnEnemyManual(Vector3 position)
    {
        GameObject newEnemy = Instantiate(enemyUnitPrefab, position, Quaternion.identity);
        UnitInstance unit = newEnemy.GetComponent<UnitInstance>();

        if (unit == null)
        {
            Debug.LogError($"{name}: Enemy prefab must have UnitInstance component!");
            Destroy(newEnemy);
            return;
        }

        unit.Initialize(pathfinder, enemyUnitType, gridManager, visualizer, 1);
        SetupUnifiedAI(unit);
    }

    private void SetupUnifiedAI(UnitInstance unit)
    {
        // Add the unified AI component
        UnifiedEnemyAI unifiedAI = unit.gameObject.AddComponent<UnifiedEnemyAI>();
        unifiedAI.SetDebugLogs(enableDebugLogs);

        // Set castle target
        if (playerCastle != null)
        {
            unifiedAI.SetCastleTarget(playerCastle);
        }

        if (enableDebugLogs)
            Debug.Log($"{name}: Added UnifiedEnemyAI to {unit.name}");
    }

    public void SetSpawnInterval(float interval)
    {
        spawnInterval = interval;
    }

    public void SetMaxEnemies(int max)
    {
        maxEnemies = max;
    }

    public void SetPlayerCastle(Transform castle)
    {
        playerCastle = castle;

        // Update all existing unified AIs
        UnifiedEnemyAI[] existingAIs = FindObjectsOfType<UnifiedEnemyAI>();
        foreach (var ai in existingAIs)
        {
            ai.SetCastleTarget(castle);
        }

        if (enableDebugLogs)
            Debug.Log($"Player castle set to: {(castle ? castle.name : "null")}");
    }

    void OnDestroy()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }
}