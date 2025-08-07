using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private AStartPathfinding pathfindingLogic;
    [SerializeField] private CurrentTeamArmyManager currentTeamManager;
    [SerializeField] private CurrentTeamArmyManager enemyArmyManager;

    [Header("Castle Setup")]
    [SerializeField] private GameObject castlePrefab;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Wave Settings")]
    [SerializeField] private float waveStartDelay = 30f;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private int baseEnemiesPerWave = 10;
    [SerializeField] private float enemySpawnInterval = 2f;
    [SerializeField] private int maxWaves = 5;
    [SerializeField] private float timeBetweenWaves = 10f;

    [SerializeField] private string YouWinScene = "GameWinScene";

    // Wave tracking
    private int currentWave = 0;
    private int enemiesSpawned = 0;
    private int enemiesAlive = 0;
    private List<UnitInstance> currentWaveEnemies = new List<UnitInstance>();

    // Game state
    private enum GameState { CastlePlacement, WaitingForWave, WaveActive, WaveComplete, Victory, GameOver }
    private GameState currentState = GameState.CastlePlacement;

    // Castle placement
    private GameObject castleGhost;
    private GameObject placedCastle;
    private Camera mainCamera;
    private bool canPlaceCastle = false;

    // Wave spawning
    private float waveTimer;
    private float lastEnemySpawnTime;
    private Vector3[] spawnCorners;

    private void Awake()
    {
        if (SoundPracticePlayer.Instance != null)
        {
            SoundPracticePlayer.Instance.PlayLoopingSound(0, AudioSourceType.Music);
        }

        mainCamera = Camera.main;
        SetupSpawnCorners();
        StartCastlePlacement();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            gridManager.InitializeGrid();
        }

        switch (currentState)
        {
            case GameState.CastlePlacement:
                HandleCastlePlacement();
                break;
            case GameState.WaitingForWave:
                HandleWaveCountdown();
                break;
            case GameState.WaveActive:
                HandleWaveSpawning();
                CheckEnemyStatus();
                break;
            case GameState.WaveComplete:
                HandleWaveComplete();
                break;
        }
    }
    //setting corners for enemy units to spawn
    private void SetupSpawnCorners()
    {
        int gridSizeX = gridManager.GridSettings.GridSizeX;
        int gridSizeY = gridManager.GridSettings.GridSizeY;
        float nodeSize = gridManager.nodeSize;

        int offset = 2;

        spawnCorners = new Vector3[4]
        {
        new Vector3(offset * nodeSize, 0, offset * nodeSize),
        new Vector3((gridSizeX - offset) * nodeSize, 0, offset * nodeSize),
        new Vector3(offset * nodeSize, 0, (gridSizeY - offset) * nodeSize),
        new Vector3((gridSizeX - offset) * nodeSize, 0, (gridSizeY - offset) * nodeSize)
        };
    }
    //put the game into castle placement mode
    private void StartCastlePlacement()
    {
        currentState = GameState.CastlePlacement;
        UpdatePromptText("Place your Castle to begin!");
        CreateCastleGhost();
    }

    //updates the text message shown to the player
    private void UpdatePromptText(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
            promptText.gameObject.SetActive(true);
        }
    }

    //hide the prompt text from the screen
    private void HidePromptText()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    //create a transparent "ghost" version of the castle for preview placement
    private void CreateCastleGhost()
    {
        if (castleGhost == null && castlePrefab != null)
        {
            castleGhost = Instantiate(castlePrefab);
            castleGhost.transform.localScale = castlePrefab.transform.localScale;

            // Disable colliders so ghost doesn’t block placement raycasts
            Collider[] colliders = castleGhost.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            // Disable any scripts on the ghost
            MonoBehaviour[] scripts = castleGhost.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null)
                {
                    script.enabled = false;
                }
            }

            MakeGhostTransparent(); // Change material so ghost looks semi-transparent
        }
    }

    //set the ghost castle’s materials to semi-transparent
    private void MakeGhostTransparent()
    {
        if (castleGhost == null) return;

        Renderer[] renderers = castleGhost.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                Material[] materials = new Material[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    materials[i] = new Material(renderer.materials[i]);
                    Color color = materials[i].color;
                    color.a = 0.5f; // Set half transparency
                    materials[i].color = color;
                }
                renderer.materials = materials;
            }
        }
    }

    //handle player input for placing the castle
    private void HandleCastlePlacement()
    {
        if (castleGhost == null) return;

        int layerMask = ~LayerMask.GetMask("DefensiveBuildings");
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            // Get grid coordinates under the mouse
            Vector2Int mouseGridCoords = gridManager.GetGridPositionFromWorld(hit.point);

            Vector2Int buildingOrigin = CalculateBuildingOrigin(mouseGridCoords, 4, 4);
            
            Vector3 worldPos = CalculateWorldPosition(buildingOrigin, 4, 4, gridManager.nodeSize);
            castleGhost.transform.position = worldPos;

            canPlaceCastle = CanPlaceBuildingAt(buildingOrigin.x, buildingOrigin.y, 4, 4);
            UpdateGhostColor(canPlaceCastle);

            if (Input.GetMouseButtonDown(0) && canPlaceCastle)
            {
                PlaceCastle(buildingOrigin, worldPos);
            }
        }
        else
        {
            // Not pointing at a valid spot
            canPlaceCastle = false;
            UpdateGhostColor(false);
        }
    }

    //calculate where the top-left corner of the building should go on the grid
    private Vector2Int CalculateBuildingOrigin(Vector2Int mouseGridPos, int buildingWidth, int buildingDepth)
    {
        int halfWidth = buildingWidth / 2;
        int halfDepth = buildingDepth / 2;

        Vector2Int origin = new Vector2Int(
            mouseGridPos.x - halfWidth,
            mouseGridPos.y - halfDepth
        );

        return origin;
    }

    //convert grid position into a world position
    private Vector3 CalculateWorldPosition(Vector2Int origin, int width, int height, float nodeSize)
    {
        float centerGridX = origin.x + (width - 1) * 0.5f;
        float centerGridY = origin.y + (height - 1) * 0.5f;

        Vector3 worldPosition = gridManager.GridSettings.UseXZPlane
            ? new Vector3(centerGridX * nodeSize, 0f, centerGridY * nodeSize)
            : new Vector3(centerGridX * nodeSize, centerGridY * nodeSize, 0f);

        return worldPosition;
    }

    //check if a building can be placed at a spot on the grid
    private bool CanPlaceBuildingAt(int startX, int startY, int width, int height)
    {
        if (gridManager == null) return false;

        for (int xOffset = 0; xOffset < width; xOffset++)
        {
            for (int yOffset = 0; yOffset < height; yOffset++)
            {
                int checkX = startX + xOffset;
                int checkY = startY + yOffset;

                GridNode node = gridManager.GetNode(checkX, checkY);
                if (node == null || !node.walkable || node.IsOccupied)
                    return false;
            }
        }
        return true;
    }

    //change the ghost color to green if valid, red if invalid
    private void UpdateGhostColor(bool isValid)
    {
        if (castleGhost == null) return;

        Color ghostColor = isValid ? Color.green : Color.red;
        ghostColor.a = 0.5f;

        Renderer[] renderers = castleGhost.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i].color = ghostColor;
                }
            }
        }
    }

    //finalize castle placement
    private void PlaceCastle(Vector2Int buildingOrigin, Vector3 worldPos)
    {
        placedCastle = Instantiate(castlePrefab, worldPos, Quaternion.identity);
        placedCastle.transform.localScale = castlePrefab.transform.localScale;

        // Play sound on placement
        if (SoundPracticePlayer.Instance != null)
        {
            SoundPracticePlayer.Instance.PlaySound(2, AudioSourceType.UI);
        }

        // Mark grid nodes as occupied
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                GridNode node = gridManager.GetNode(buildingOrigin.x + x, buildingOrigin.y + y);
                if (node != null)
                {
                    node.walkable = false;
                    node.IsOccupied = true;
                }
            }
        }

        // Remove ghost
        if (castleGhost != null)
        {
            Destroy(castleGhost);
            castleGhost = null;
        }

        // Start wave countdown
        currentState = GameState.WaitingForWave;
        waveTimer = waveStartDelay;
    }

    //handle countdown before wave starts
    private void HandleWaveCountdown()
    {
        waveTimer -= Time.deltaTime;

        int secondsLeft = Mathf.CeilToInt(waveTimer);
        UpdatePromptText($"Wave {currentWave + 1} begins in: {secondsLeft} seconds");

        if (waveTimer <= 0)
        {
            StartWave();
        }
    }

    //start a new wave of enemies
    private void StartWave()
    {
        currentState = GameState.WaveActive;
        enemiesSpawned = 0;
        enemiesAlive = 0;
        lastEnemySpawnTime = Time.time;
        currentWaveEnemies.Clear();

        HidePromptText();
    }

    //how many enemies spawn in this wave
    private int GetEnemiesForWave(int wave)
    {
        return baseEnemiesPerWave + (wave * 2);
    }
    // Handle spawning enemies during a wave
    private void HandleWaveSpawning()
    {
        // Spawn enemies at intervals
        if (enemiesSpawned < GetEnemiesForWave(currentWave) && Time.time - lastEnemySpawnTime >= enemySpawnInterval)
        {
            SpawnEnemy();
            lastEnemySpawnTime = Time.time;
            enemiesSpawned++;
        }

        // If all spawned and none alive, complete the wave
        if (enemiesSpawned >= GetEnemiesForWave(currentWave) && enemiesAlive <= 0)
        {
            CompleteWave();
        }
    }
    //remove dead enemies from the list
    private void CheckEnemyStatus()
    {
        for (int i = currentWaveEnemies.Count - 1; i >= 0; i--)
        {
            if (currentWaveEnemies[i] == null)
            {
                currentWaveEnemies.RemoveAt(i);
                enemiesAlive--;
            }
        }
    }
    //handle timer between waves after one is complete
    private void HandleWaveComplete()
    {
        waveTimer -= Time.deltaTime;

        int secondsLeft = Mathf.CeilToInt(waveTimer);
        UpdatePromptText($"Wave {currentWave + 1} begins in: {secondsLeft} seconds");

        if (waveTimer <= 0)
        {
            StartWave();
        }
    }
    //spawn a single enemy at a random corner
    private void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0) return;

        Vector3 spawnPosition = spawnCorners[Random.Range(0, spawnCorners.Length)];
        UnitInstance enemy = enemyArmyManager.SpawnUnit(spawnPosition);

        if (enemy != null)
        {
            currentWaveEnemies.Add(enemy);
            enemiesAlive++;

            // Give enemy a target (the placed castle)
            EnemyAI enemyAI = enemy.GetComponent<EnemyAI>();
            if (enemyAI != null && placedCastle != null)
            {
                enemyAI.SetTargetCastle(placedCastle);
            }
        }
    }
    //complete the wave and check for victory
    private void CompleteWave()
    {
        currentWave++;
        currentWaveEnemies.Clear();

        if (currentWave >= maxWaves)
        {
            // Player has won the game
            currentState = GameState.Victory;
            UpdatePromptText("All waves defeated! Victory!");
            StartCoroutine(LoadVictoryScene());
        }
        else
        {
            // More waves remaining
            currentState = GameState.WaveComplete;
            waveTimer = timeBetweenWaves;
            UpdatePromptText($"Wave {currentWave} Complete! Next wave in {timeBetweenWaves} seconds");
        }
    }

    //load the win screen after a short delay
    private IEnumerator LoadVictoryScene()
    {
        yield return new WaitForSeconds(3f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(YouWinScene);
    }
}