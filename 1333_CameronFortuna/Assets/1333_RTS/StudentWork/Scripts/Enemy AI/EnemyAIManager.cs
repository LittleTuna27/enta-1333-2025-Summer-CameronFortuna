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

    //singleton setup
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

    //initializes manager and finds player castle
    private void Start()
    {
        if (autoFindCastle && playerCastle == null)
        {
            FindPlayerCastle();
        }

        // Find any existing enemy AI units
        RegisterExistingEnemies();
    }

    //updates manager logic and triggers retargeting
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

    //sets player castle reference
    public void SetPlayerCastle(GameObject castle)
    {
        playerCastle = castle;
        Debug.Log($"EnemyAIManager: Player castle set to {castle?.name}");

        // Update all managed enemies with the new castle target
        UpdateAllEnemiesCastleTarget();
    }

    //registers a new enemy with the manager
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

    //unregisters an enemy from the manager
    public void UnregisterEnemy(EnemyAI enemy)
    {
        if (managedEnemies.Contains(enemy))
        {
            managedEnemies.Remove(enemy);
            Debug.Log($"EnemyAIManager: Unregistered enemy {enemy?.name}. Total managed: {managedEnemies.Count}");
        }
    }

    //registers all existing enemies at start
    private void RegisterExistingEnemies()
    {
        EnemyAI[] existingEnemies = FindObjectsOfType<EnemyAI>();
        foreach (var enemy in existingEnemies)
        {
            RegisterEnemy(enemy);
        }
    }

    //finds the player's castle automatically
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

    //updates all enemies with the current castle target
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

    //forces all enemies to update their targeting
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

    //removes destroyed enemies from the list
    private void CleanupDestroyedEnemies()
    {
        managedEnemies.RemoveAll(enemy => enemy == null);
    }

    //returns number of active enemies
    public int GetManagedEnemyCount()
    {
        CleanupDestroyedEnemies();
        return managedEnemies.Count;
    }

    //returns list of active enemies
    public List<EnemyAI> GetActiveEnemies()
    {
        CleanupDestroyedEnemies();
        return new List<EnemyAI>(managedEnemies);
    }

    //returns number of enemies attacking castle
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

    //returns number of enemies moving to castle
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

    //draws debug gizmos in scene view
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
