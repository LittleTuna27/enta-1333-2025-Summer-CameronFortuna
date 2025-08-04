using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpawner : MonoBehaviour
{
    [Header("Spawner Setup")]
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private Transform rallyPoint;
    [SerializeField] private int unitCost = 10;

    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private CurrentTeamArmyManager armyManager;

    [Header("Building Info")]
    private BuildingHealth buildingHealth;

    private void Start()
    {
        // Get building health to determine which army this belongs to
        buildingHealth = GetComponent<BuildingHealth>();

        if (gridManager == null)
            gridManager = FindAnyObjectByType<GridManager>();

        // If no army manager assigned, try to find the right one based on army ID
        if (armyManager == null && buildingHealth != null)
        {
            CurrentTeamArmyManager[] allManagers = FindObjectsOfType<CurrentTeamArmyManager>();
            foreach (var manager in allManagers)
            {
                if (manager.armyID == buildingHealth.ArmyID)
                {
                    armyManager = manager;
                    break;
                }
            }
        }
    }

    private void Update()
    {
        // Keep the debug key for testing
        if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            if (buildingHealth != null && buildingHealth.ArmyID == 0)
                SpawnUnitPlayer();
        }

    }

    public bool SpawnUnitPlayer()
    {
        // Check if this is a player building
        if (buildingHealth == null || buildingHealth.ArmyID != 0)
        {
            Debug.LogWarning("Trying to spawn player unit from non-player building!");
            return false;
        }

        // Check if player has enough coins
        if (ResourceManager.Instance == null || !ResourceManager.Instance.CanAfford(unitCost))
        {
            Debug.Log($"Cannot afford unit! Need {unitCost} coins, have {(ResourceManager.Instance != null ? ResourceManager.Instance.GetCoins() : 0)}");
            return false;
        }

        // Spend the coins
        if (!ResourceManager.Instance.SpendCoins(unitCost))
        {
            Debug.LogError("Failed to spend coins for unit!");
            return false;
        }

        // Spawn the unit
        return SpawnUnit();
    }

    public bool SpawnUnitEnemy()
    {
        // Enemies don't need to pay coins, just spawn
        return SpawnUnit();
    }

    private bool SpawnUnit()
    {
        if (armyManager == null)
        {
            Debug.LogError("No army manager assigned to spawner!");
            return false;
        }

        if (spawnPoint == null)
        {
            Debug.LogError("No spawn point assigned!");
            return false;
        }

        // Use the army manager to spawn the unit
        UnitInstance newUnit = armyManager.SpawnUnit(spawnPoint.position);

        if (newUnit == null)
        {
            Debug.LogError("Failed to spawn unit!");
            return false;
        }

        // Send unit to rally point if it exists
        if (rallyPoint != null && gridManager != null)
        {
            GridNode targetNode = gridManager.GetNodeFromWorldPosition(rallyPoint.position);
            if (targetNode != null)
            {
                newUnit.MoveTo(targetNode);
                Debug.Log($"Unit spawned and ordered to rally point");
            }
            else
            {
                Debug.LogWarning("Could not find rally node - unit will stay at spawn point");
            }
        }

        return true;
    }

    // Public method for UI to check if unit can be spawned
    public bool CanSpawnUnit()
    {
        if (buildingHealth == null) return false;

        // For player units, check coins
        if (buildingHealth.ArmyID == 0)
        {
            return ResourceManager.Instance != null && ResourceManager.Instance.CanAfford(unitCost);
        }

        // Enemies can always spawn (no resource restriction)
        return true;
    }

    // Public method to get unit cost (for UI display)
    public int GetUnitCost()
    {
        return unitCost;
    }

    // Public method to get army ID (for UI validation)
    public int GetArmyID()
    {
        return buildingHealth != null ? buildingHealth.ArmyID : -1;
    }
}