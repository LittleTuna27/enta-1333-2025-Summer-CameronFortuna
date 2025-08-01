using UnityEngine;

/// <summary>
/// Add this component to your enemy unit prefabs or modify your spawner to add EnemyAI automatically
/// </summary>
public class EnemyAISpawnerIntegration : MonoBehaviour
{
    [Header("Auto-Setup")]
    [SerializeField] private bool autoAddEnemyAI = true;
    [SerializeField] private bool autoRegisterWithManager = true;

    private void Start()
    {
        SetupEnemyAI();
    }

    private void SetupEnemyAI()
    {
        UnitInstance unitInstance = GetComponent<UnitInstance>();
        if (unitInstance == null)
        {
            Debug.LogWarning($"{name}: No UnitInstance found, cannot setup EnemyAI");
            return;
        }

        // Check if this is an enemy unit (not player army)
        if (unitInstance.ArmyID == 0)
        {
            Debug.Log($"{name}: This is a player unit (ArmyID: 0), not setting up EnemyAI");
            return;
        }

        if (autoAddEnemyAI)
        {
            EnemyAI existingAI = GetComponent<EnemyAI>();
            if (existingAI == null)
            {
                // Add EnemyAI component
                EnemyAI enemyAI = gameObject.AddComponent<EnemyAI>();

                
                Debug.Log($"{name}: EnemyAI component added and configured");
            }

            if (autoRegisterWithManager && EnemyAIManager.Instance != null)
            {
                EnemyAI ai = GetComponent<EnemyAI>();
                if (ai != null)
                {
                    EnemyAIManager.Instance.RegisterEnemy(ai);
                }
            }
        }
    }

    // Call this method from your spawner after instantiating enemy units
    public static void SetupEnemyUnit(GameObject enemyUnit, GameObject targetCastle = null)
    {
        if (enemyUnit == null) return;

        UnitInstance unitInstance = enemyUnit.GetComponent<UnitInstance>();
        if (unitInstance == null || unitInstance.ArmyID == 0) return; // Skip player units

        // Add EnemyAI if not present
        EnemyAI enemyAI = enemyUnit.GetComponent<EnemyAI>();
        if (enemyAI == null)
        {
            enemyAI = enemyUnit.AddComponent<EnemyAI>();
        }

        // Set target castle if provided
        if (targetCastle != null)
        {
            enemyAI.SetTargetCastle(targetCastle);
        }

        // Register with manager
        if (EnemyAIManager.Instance != null)
        {
            EnemyAIManager.Instance.RegisterEnemy(enemyAI);
        }

        Debug.Log($"Enemy unit {enemyUnit.name} setup complete with EnemyAI");
    }
}