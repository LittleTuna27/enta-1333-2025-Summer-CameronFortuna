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

    public static AIManager Instance { get; private set; }

    private List<EnemyAI> activeEnemyAIs = new();

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
            playerCastle = GameObject.FindWithTag("Castle")?.transform;

        StartCoroutine(AIManagerUpdate());
    }

    private IEnumerator AIManagerUpdate()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            activeEnemyAIs.RemoveAll(ai => ai == null);
        }
    }

    public void RegisterEnemyAI(EnemyAI enemyAI)
    {
        if (!activeEnemyAIs.Contains(enemyAI))
        {
            activeEnemyAIs.Add(enemyAI);
            if (showDebugLogs)
                Debug.Log($"AIManager: Registered {enemyAI.name}");
        }
    }

    public void UnregisterEnemyAI(EnemyAI enemyAI)
    {
        if (activeEnemyAIs.Remove(enemyAI) && showDebugLogs)
            Debug.Log($"AIManager: Unregistered {enemyAI.name}");
    }

    public void SetPlayerCastle(Transform castle)
    {
        playerCastle = castle;
        Debug.Log($"Player castle set to: {(castle ? castle.name : "null")}");
    }
}
