using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Managers")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private UnitManager unitManager;
    [SerializeField] private PathFinderVisualization pathFinder;
    [SerializeField] private AStartPathfinding pathfindingLogic;
    [SerializeField] private CurrentTeamArmyManager currentTeamManager;
    [SerializeField] private CurrentTeamArmyManager enemyArmyManager;

    [Header("Castle Setup")]
    [SerializeField] private GameObject castlePrefab;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Wave Settings")]
    [SerializeField] private float waveStartDelay = 30f; // Time before enemies start spawning
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private int totalEnemiesInWave = 50;
    [SerializeField] private float enemySpawnInterval = 2f; // Time between enemy spawns

    // Game state
    private enum GameState { CastlePlacement, WaitingForWave, WaveActive, GameOver }
    private GameState currentState = GameState.CastlePlacement;

    // Castle placement
    private GameObject castleGhost;
    private GameObject placedCastle;
    private Camera mainCamera;
    private bool canPlaceCastle = false;

    // Wave spawning
    private float waveTimer;
    private int enemiesSpawned = 0;
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
                break;
        }
    }

    private void SetupSpawnCorners()
    {
        int gridSizeX = gridManager.GridSettings.GridSizeX;
        int gridSizeY = gridManager.GridSettings.GridSizeY;
        float nodeSize = gridManager.nodeSize;

        // Move 2 squares inward from edges
        int offset = 2;

        spawnCorners = new Vector3[4]
        {
        new Vector3(offset * nodeSize, 0, offset * nodeSize), // Bottom-left corner
        new Vector3((gridSizeX - offset) * nodeSize, 0, offset * nodeSize), // Bottom-right corner  
        new Vector3(offset * nodeSize, 0, (gridSizeY - offset) * nodeSize), // Top-left corner
        new Vector3((gridSizeX - offset) * nodeSize, 0, (gridSizeY - offset) * nodeSize) // Top-right corner
        };

        Debug.Log($"Spawn corners set up for grid {gridSizeX}x{gridSizeY} with node size {nodeSize}");
    }

    private void StartCastlePlacement()
    {
        currentState = GameState.CastlePlacement;
        UpdatePromptText("Place your Castle to begin!");
        CreateCastleGhost();
    }

    private void UpdatePromptText(string message)
    {
        if (promptText != null)
        {
            promptText.text = message;
            promptText.gameObject.SetActive(true);
        }
    }

    private void HidePromptText()
    {
        if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }

    private void CreateCastleGhost()
    {
        if (castleGhost == null && castlePrefab != null)
        {
            castleGhost = Instantiate(castlePrefab);

            // Preserve the prefab's original scale
            castleGhost.transform.localScale = castlePrefab.transform.localScale;

            // Disable colliders on ghost (following your BuildingPlacementManager pattern)
            Collider[] colliders = castleGhost.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            // Disable scripts on ghost (following your pattern)
            MonoBehaviour[] scripts = castleGhost.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null)
                {
                    script.enabled = false;
                }
            }

            // Apply transparent material (simplified version)
            MakeGhostTransparent();
        }
    }

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
                    // Make it semi-transparent
                    Color color = materials[i].color;
                    color.a = 0.5f;
                    materials[i].color = color;
                }
                renderer.materials = materials;
            }
        }
    }

    private void HandleCastlePlacement()
    {
        if (castleGhost == null) return;

        // Use your existing raycast approach (excluding DefensiveBuildings layer)
        int layerMask = ~LayerMask.GetMask("DefensiveBuildings");
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            // Get grid position using your GridManager method
            Vector2Int mouseGridCoords = gridManager.GetGridPositionFromWorld(hit.point);

            // Castle is 4x4, so calculate origin (following your building placement logic)
            Vector2Int buildingOrigin = CalculateBuildingOrigin(mouseGridCoords, 4, 4);

            // Calculate world position using your approach
            Vector3 worldPos = CalculateWorldPosition(buildingOrigin, 4, 4, gridManager.nodeSize);
            castleGhost.transform.position = worldPos;

            // Check if we can place the castle here (4x4 building)
            canPlaceCastle = CanPlaceBuildingAt(buildingOrigin.x, buildingOrigin.y, 4, 4);

            // Change ghost color based on validity
            UpdateGhostColor(canPlaceCastle);

            // Place castle on click
            if (Input.GetMouseButtonDown(0) && canPlaceCastle)
            {
                PlaceCastle(buildingOrigin, worldPos);
            }
        }
        else
        {
            canPlaceCastle = false;
            UpdateGhostColor(false);
        }
    }

    private Vector2Int CalculateBuildingOrigin(Vector2Int mouseGridPos, int buildingWidth, int buildingDepth)
    {
        // Using your exact logic from BuildingPlacementManager
        int halfWidth = buildingWidth / 2;
        int halfDepth = buildingDepth / 2;

        Vector2Int origin = new Vector2Int(
            mouseGridPos.x - halfWidth,
            mouseGridPos.y - halfDepth
        );

        return origin;
    }

    private Vector3 CalculateWorldPosition(Vector2Int origin, int width, int height, float nodeSize)
    {
        // Using your exact logic from BuildingPlacementManager
        float centerGridX = origin.x + (width - 1) * 0.5f;
        float centerGridY = origin.y + (height - 1) * 0.5f;

        Vector3 worldPosition = gridManager.GridSettings.UseXZPlane
            ? new Vector3(centerGridX * nodeSize, 0f, centerGridY * nodeSize)
            : new Vector3(centerGridX * nodeSize, centerGridY * nodeSize, 0f);

        return worldPosition;
    }

    private bool CanPlaceBuildingAt(int startX, int startY, int width, int height)
    {
        // Using your exact logic from BuildingPlacementManager
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

    private void PlaceCastle(Vector2Int buildingOrigin, Vector3 worldPos)
    {
        // Instantiate the actual castle
        placedCastle = Instantiate(castlePrefab, worldPos, Quaternion.identity);

        // Preserve the prefab's original scale
        placedCastle.transform.localScale = castlePrefab.transform.localScale;

        // Play placement sound (following your pattern)
        if (SoundPracticePlayer.Instance != null)
        {
            SoundPracticePlayer.Instance.PlaySound(2, AudioSourceType.UI);
        }

        // Update grid occupancy (following your exact pattern)
        for (int x = 0; x < 4; x++)
        {
            for (int y = 0; y < 4; y++)
            {
                GridNode node = gridManager.GetNode(buildingOrigin.x + x, buildingOrigin.y + y);
                if (node != null)
                {
                    node.walkable = false;
                    node.IsOccupied = true;
                    Debug.Log($"Occupied grid square: ({buildingOrigin.x + x}, {buildingOrigin.y + y})");
                }
            }
        }

        // Clean up ghost
        if (castleGhost != null)
        {
            Destroy(castleGhost);
            castleGhost = null;
        }

        // Transition to wave countdown
        currentState = GameState.WaitingForWave;
        waveTimer = waveStartDelay;

        Debug.Log($"Castle placed successfully at origin {buildingOrigin}");
    }

    private void HandleWaveCountdown()
    {
        waveTimer -= Time.deltaTime;

        int secondsLeft = Mathf.CeilToInt(waveTimer);
        UpdatePromptText($"Wave begins in: {secondsLeft} seconds");

        if (waveTimer <= 0)
        {
            StartWave();
        }
    }

    private void StartWave()
    {
        currentState = GameState.WaveActive;
        enemiesSpawned = 0;
        lastEnemySpawnTime = Time.time;

        // Hide the prompt text once the wave starts - gameplay begins!
        HidePromptText();
    }

    private void HandleWaveSpawning()
    {
        if (enemiesSpawned < totalEnemiesInWave && Time.time - lastEnemySpawnTime >= enemySpawnInterval)
        {
            SpawnEnemy();
            lastEnemySpawnTime = Time.time;
            enemiesSpawned++;

            if (enemiesSpawned >= totalEnemiesInWave)
            {
                CompleteWave();
            }
        }
    }

    private void SpawnEnemy()
    {
        if (enemyPrefabs.Length == 0) return;

        // Pick random enemy and spawn corner
        GameObject enemyPrefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];
        Vector3 spawnPosition = spawnCorners[Random.Range(0, spawnCorners.Length)];

        enemyArmyManager.SpawnUnit(spawnPosition);

        // Set enemy to target the castle if they have the right components
        // You might want to add specific logic here based on your enemy AI setup
        Debug.Log($"Spawned enemy at {spawnPosition}");
    }

    private void CompleteWave()
    {
        UpdatePromptText("Wave Complete! Defend your castle!");
        // You could transition to a victory state or start building phase here
    }

    public void OnCastleDestroyed()
    {
        currentState = GameState.GameOver;
        UpdatePromptText("Castle Destroyed! Game Over!");
    }
}