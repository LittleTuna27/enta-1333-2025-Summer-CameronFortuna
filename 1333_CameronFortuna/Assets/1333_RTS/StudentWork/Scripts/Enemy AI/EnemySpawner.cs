using UnityEngine;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [SerializeField] private GameObject enemyUnitPrefab;
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float spawnInterval = 10f; // Spawn every 10 seconds
    [SerializeField] private int maxEnemies = 20; // Maximum enemies at once
    [SerializeField] private bool autoSpawn = true;

    [Header("Enemy Settings")]
    [SerializeField] private UnitType enemyUnitType;
    [SerializeField] private Transform playerCastle; // Reference to player castle

    [Header("System References")]
    [SerializeField] private CurrentTeamArmyManager enemyArmyManager;
    [SerializeField] private GridManager gridManager;
    [SerializeField] private AStartPathfinding pathfinder;
    [SerializeField] private PathFinderVisualization visualizer;

    private Coroutine spawnCoroutine;
    private int currentEnemyCount = 0;

    void Start()
    {
        // Auto-find references if not assigned
        if (gridManager == null) gridManager = FindObjectOfType<GridManager>();
        if (pathfinder == null) pathfinder = FindObjectOfType<AStartPathfinding>();
        if (visualizer == null) visualizer = FindObjectOfType<PathFinderVisualization>();

        // Find enemy army manager (army ID 1)
        if (enemyArmyManager == null)
        {
            CurrentTeamArmyManager[] managers = FindObjectsOfType<CurrentTeamArmyManager>();
            foreach (var manager in managers)
            {
                if (manager.armyID == 1) // Enemy army
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
    }

    private Transform FindPlayerCastle()
    {
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

            // Count current live enemies
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
            if (unit.ArmyID == 1 && unit.IsAlive) // Enemy army
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

        // Choose random spawn point
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Use army manager to spawn if available, otherwise spawn manually
        if (enemyArmyManager != null)
        {
            enemyArmyManager.SpawnUnit(spawnPoint.position);

            // Get the newly spawned unit and add AI component
            UnitInstance[] units = FindObjectsOfType<UnitInstance>();
            UnitInstance newestUnit = null;
            foreach (var unit in units)
            {
                if (unit.ArmyID == 1 && unit.GetComponent<EnemyAI>() == null)
                {
                    newestUnit = unit;
                    break;
                }
            }

            if (newestUnit != null)
            {
                SetupEnemyAI(newestUnit);
            }
        }
        else
        {
            // Manual spawning if no army manager
            SpawnEnemyManual(spawnPoint.position);
        }

        Debug.Log($"{name}: Spawned enemy at {spawnPoint.position}");
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

        // Initialize the unit
        unit.Initialize(pathfinder, enemyUnitType, gridManager, visualizer, 1);

        // Setup AI
        SetupEnemyAI(unit);
    }

    private void SetupEnemyAI(UnitInstance unit)
    {
        // Add AI component if it doesn't exist
        EnemyAI ai = unit.GetComponent<EnemyAI>();
        if (ai == null)
        {
            ai = unit.gameObject.AddComponent<EnemyAI>();
        }

        // Add SimpleAutoAttack for intelligent combat
        SimpleAutoAttack autoAttack = unit.GetComponent<SimpleAutoAttack>();
        if (autoAttack == null)
        {
            autoAttack = unit.gameObject.AddComponent<SimpleAutoAttack>();
        }

        // Set castle target and immediately start moving
        if (playerCastle != null)
        {
            ai.SetCastleTarget(playerCastle);

            // Force immediate pathfinding to castle
            StartCoroutine(StartMovementTowardsCastle(unit));
        }

        Debug.Log($"{name}: Added AI and SimpleAutoAttack to {unit.name}");
    }

    private IEnumerator StartMovementTowardsCastle(UnitInstance unit)
    {
        // Wait a frame for the unit to fully initialize
        yield return null;

        if (unit != null && playerCastle != null)
        {
            // Get grid manager and find path to castle
            if (gridManager != null)
            {
                GridNode castleNode = gridManager.GetNearestWalkableNode(playerCastle.position);
                if (castleNode != null)
                {
                    unit.MoveTo(castleNode);
                    Debug.Log($"{unit.name}: Started moving toward castle at {playerCastle.position}");
                }
                else
                {
                    Debug.LogWarning($"{unit.name}: Could not find walkable path to castle!");
                }
            }
        }
    }

    // Manual spawn for testing
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) // Press E to spawn enemy
        {
            SpawnEnemy();
        }

        if (Input.GetKeyDown(KeyCode.T)) // Press T to toggle auto-spawn
        {
            ToggleAutoSpawn();
        }
    }

    public void ToggleAutoSpawn()
    {
        autoSpawn = !autoSpawn;

        if (autoSpawn && spawnCoroutine == null)
        {
            spawnCoroutine = StartCoroutine(AutoSpawnRoutine());
        }
        else if (!autoSpawn && spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        Debug.Log($"{name}: Auto-spawn {(autoSpawn ? "enabled" : "disabled")}");
    }

    public void SetSpawnInterval(float interval)
    {
        spawnInterval = interval;
    }

    public void SetMaxEnemies(int max)
    {
        maxEnemies = max;
    }

    void OnDestroy()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }
    }
    public void SetPlayerCastle(Transform castle)
    {
        playerCastle = castle;
        Debug.Log($"Player castle set to: {(castle ? castle.name : "null")}");
    }
}