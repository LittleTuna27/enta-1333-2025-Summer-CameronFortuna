using UnityEngine;
using System.Collections.Generic;

public class EnemyAIManager : MonoBehaviour
{
    [Header("AI Management")]
    [SerializeField] private GameObject playerCastle;
    [SerializeField] private bool autoFindCastle = true;
    [SerializeField] private float globalRetargetInterval = 5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = true;

    private List<EnemyAI> managedEnemies = new List<EnemyAI>();
    private float lastGlobalRetarget = 0f;

    public static EnemyAIManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        if (autoFindCastle && playerCastle == null)
        {
            FindPlayerCastle();
        }

        // Find any existing enemy AI units
        RegisterExistingEnemies();
    }

    private void Update()
    {
        // Periodically force all enemies to retarget
        if (Time.time - lastGlobalRetarget >= globalRetargetInterval)
        {
            ForceGlobalRetarget();
            lastGlobalRetarget = Time.time;
        }

        // Clean up destroyed enemies
        CleanupDestroyedEnemies();
    }

    public void SetPlayerCastle(GameObject castle)
    {
        playerCastle = castle;
        Debug.Log($"EnemyAIManager: Player castle set to {castle?.name}");

        // Update all managed enemies with the new castle target
        UpdateAllEnemiesCastleTarget();
    }

    public void RegisterEnemy(EnemyAI enemy)
    {
        if (enemy != null && !managedEnemies.Contains(enemy))
        {
            managedEnemies.Add(enemy);

            // Set the castle target if we have one
            if (playerCastle != null)
            {
                enemy.SetTargetCastle(playerCastle);
            }

            Debug.Log($"EnemyAIManager: Registered enemy {enemy.name}. Total managed: {managedEnemies.Count}");
        }
    }

    public void UnregisterEnemy(EnemyAI enemy)
    {
        if (managedEnemies.Contains(enemy))
        {
            managedEnemies.Remove(enemy);
            Debug.Log($"EnemyAIManager: Unregistered enemy {enemy?.name}. Total managed: {managedEnemies.Count}");
        }
    }

    private void RegisterExistingEnemies()
    {
        EnemyAI[] existingEnemies = FindObjectsOfType<EnemyAI>();
        foreach (var enemy in existingEnemies)
        {
            RegisterEnemy(enemy);
        }
    }

    private void FindPlayerCastle()
    {
        // Look for buildings with army ID 0 (typically player) that contain "castle" in the name
        BuildingHealth[] buildings = FindObjectsOfType<BuildingHealth>();

        foreach (var building in buildings)
        {
            if (building.ArmyID == 0) // Player army
            {
                string buildingName = building.BuildingName?.ToLower() ?? "";
                string gameObjectName = building.name.ToLower();

                if (buildingName.Contains("castle") || gameObjectName.Contains("castle"))
                {
                    SetPlayerCastle(building.gameObject);
                    return;
                }
            }
        }

        Debug.LogWarning("EnemyAIManager: Could not auto-find player castle");
    }

    private void UpdateAllEnemiesCastleTarget()
    {
        foreach (var enemy in managedEnemies)
        {
            if (enemy != null)
            {
                enemy.SetTargetCastle(playerCastle);
            }
        }
    }

    private void ForceGlobalRetarget()
    {
        int activeEnemies = 0;
        foreach (var enemy in managedEnemies)
        {
            if (enemy != null)
            {
                enemy.ForceRetarget();
                activeEnemies++;
            }
        }

        if (showDebugInfo && activeEnemies > 0)
        {
            Debug.Log($"EnemyAIManager: Forced retarget for {activeEnemies} enemies");
        }
    }

    private void CleanupDestroyedEnemies()
    {
        managedEnemies.RemoveAll(enemy => enemy == null);
    }

    // Public methods for external systems
    public int GetManagedEnemyCount()
    {
        CleanupDestroyedEnemies();
        return managedEnemies.Count;
    }

    public List<EnemyAI> GetActiveEnemies()
    {
        CleanupDestroyedEnemies();
        return new List<EnemyAI>(managedEnemies);
    }

    public int GetEnemiesAttackingCastle()
    {
        int count = 0;
        foreach (var enemy in managedEnemies)
        {
            if (enemy != null && enemy.CurrentAIState == EnemyAI.AIState.AttackingCastle)
            {
                count++;
            }
        }
        return count;
    }

    public int GetEnemiesMovingToCastle()
    {
        int count = 0;
        foreach (var enemy in managedEnemies)
        {
            if (enemy != null && enemy.CurrentAIState == EnemyAI.AIState.MovingToCastle)
            {
                count++;
            }
        }
        return count;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;

        // Draw connection lines from all enemies to the castle
        if (playerCastle != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(playerCastle.transform.position, Vector3.one * 2f);

            Gizmos.color = Color.yellow;
            foreach (var enemy in managedEnemies)
            {
                if (enemy != null)
                {
                    Gizmos.DrawLine(enemy.transform.position, playerCastle.transform.position);
                }
            }
        }
    }
}