using System.Collections;
using System.Collections.Generic;
using System.Resources;
using UnityEngine;

public class CastleIncomeGenerator : MonoBehaviour
{
    [Header("Income Settings")]
    [SerializeField] private int coinsPerInterval = 10;
    [SerializeField] private float incomeInterval = 5f; // seconds

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private float timeSinceLastIncome = 0f;
    private BuildingHealth buildingHealth;

    private void Start()
    {
        buildingHealth = GetComponent<BuildingHealth>();

        if (ResourceManager.Instance == null)
        {
            Debug.LogError("ResourceManager not found! Castle income will not work.");
        }

        if (enableDebugLogs)
        {
            Debug.Log($"Castle income generator started: {coinsPerInterval} coins every {incomeInterval} seconds");
        }
    }

    private void Update()
    {
        // Only generate income if castle is alive and belongs to player
        if (buildingHealth != null && buildingHealth.ArmyID == 0 && buildingHealth.CurrentHealth > 0)
        {
            timeSinceLastIncome += Time.deltaTime;

            if (timeSinceLastIncome >= incomeInterval)
            {
                GenerateIncome();
                timeSinceLastIncome = 0f;
            }
        }
    }

    private void GenerateIncome()
    {
        if (ResourceManager.Instance != null)
        {
            ResourceManager.Instance.AddPeriodicIncome(coinsPerInterval);

            if (enableDebugLogs)
            {
                Debug.Log($"Castle generated {coinsPerInterval} coins!");
            }
        }
    }

    // Public method to get next income time (for UI display if needed)
    public float GetTimeUntilNextIncome()
    {
        return incomeInterval - timeSinceLastIncome;
    }

    // Public method to get income rate info
    public string GetIncomeInfo()
    {
        return $"{coinsPerInterval} coins every {incomeInterval} seconds";
    }
}