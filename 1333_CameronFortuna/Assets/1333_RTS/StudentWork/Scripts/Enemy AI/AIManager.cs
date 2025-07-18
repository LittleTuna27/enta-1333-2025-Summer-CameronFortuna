using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIManager : MonoBehaviour
{
    [Header("AI Configuration")]
    [SerializeField] private float globalPathfindingInterval = 2f;
    [SerializeField] private float globalDetectionRange = 5f;
    [SerializeField] private float globalCastleDetectionRange = 15f;
    [SerializeField] private bool enableBuildingDestruction = true;

    [Header("Castle References")]
    [SerializeField] private Transform playerCastle;
    [SerializeField] private Transform enemyCastle;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private bool enableAIDebugLogs = true;

    public static AIManager Instance { get; private set; }

    private List<UnifiedEnemyAI> activeEnemyAIs = new();

    // Public properties
    public Transform PlayerCastle => playerCastle;
    public float GlobalPathfindingInterval => globalPathfindingInterval;
    public float GlobalDetectionRange => globalDetectionRange;
    public float GlobalCastleDetectionRange => globalCastleDetectionRange;
    public bool EnableBuildingDestruction => enableBuildingDestruction;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        if (playerCastle == null)
        {
            playerCastle = FindPlayerCastle();
        }

        StartCoroutine(AIManagerUpdate());
    }

    private Transform FindPlayerCastle()
    {
        GameObject taggedCastle = GameObject.FindWithTag("Castle");
        if (taggedCastle != null) return taggedCastle.transform;

        BuildingHealth[] buildings = FindObjectsOfType<BuildingHealth>();
        foreach (var building in buildings)
        {
            if (building.ArmyID == 0 && building.name.ToLower().Contains("castle"))
            {
                return building.transform;
            }
        }
        return null;
    }

    private IEnumerator AIManagerUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);

            activeEnemyAIs.RemoveAll(ai => ai == null);

            if (showDebugLogs && Time.frameCount % 300 == 0)
            {
                Debug.Log($"AIManager: Managing {activeEnemyAIs.Count} active enemy AIs");
            }
        }
    }

    public void RegisterEnemyAI(UnifiedEnemyAI enemyAI)
    {
        if (!activeEnemyAIs.Contains(enemyAI))
        {
            activeEnemyAIs.Add(enemyAI);
            enemyAI.SetDebugLogs(enableAIDebugLogs);

            if (showDebugLogs)
                Debug.Log($"AIManager: Registered {enemyAI.name} (Total: {activeEnemyAIs.Count})");
        }
    }

    public void UnregisterEnemyAI(UnifiedEnemyAI enemyAI)
    {
        if (activeEnemyAIs.Remove(enemyAI) && showDebugLogs)
            Debug.Log($"AIManager: Unregistered {enemyAI.name} (Total: {activeEnemyAIs.Count})");
    }

    public void SetPlayerCastle(Transform castle)
    {
        playerCastle = castle;

        foreach (var ai in activeEnemyAIs)
        {
            if (ai != null)
            {
                ai.SetCastleTarget(castle);
            }
        }

        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();
        foreach (var spawner in spawners)
        {
            spawner.SetPlayerCastle(castle);
        }

        if (showDebugLogs)
            Debug.Log($"Player castle set to: {(castle ? castle.name : "null")}");
    }

    public void SetGlobalDetectionRange(float range)
    {
        globalDetectionRange = range;
        if (showDebugLogs)
            Debug.Log($"Global detection range set to {range}");
    }

    public void SetGlobalPathfindingInterval(float interval)
    {
        globalPathfindingInterval = interval;
        if (showDebugLogs)
            Debug.Log($"Global pathfinding interval set to {interval}");
    }

    public void SetAIDebugLogs(bool enabled)
    {
        enableAIDebugLogs = enabled;

        foreach (var ai in activeEnemyAIs)
        {
            if (ai != null)
            {
                ai.SetDebugLogs(enabled);
            }
        }

        if (showDebugLogs)
            Debug.Log($"AI debug logs {(enabled ? "enabled" : "disabled")}");
    }

    public int GetActiveAICount() => activeEnemyAIs.Count;

    public List<UnifiedEnemyAI> GetActiveAIs() => new List<UnifiedEnemyAI>(activeEnemyAIs);

    public int GetAIsInState(UnifiedEnemyAI.AIState state)
    {
        int count = 0;
        foreach (var ai in activeEnemyAIs)
        {
            if (ai != null && ai.GetCurrentState() == state)
                count++;
        }
        return count;
    }
}