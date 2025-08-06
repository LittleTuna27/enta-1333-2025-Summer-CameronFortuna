using UnityEngine;
using System.Collections.Generic;

public class CurrentTeamArmyManager : MonoBehaviour
{
    [Header("Army Settings")]
    public int armyID;
    public bool isPlayer => armyID == 0;

    [Header("Unit Management")]
    public List<UnitBase> currentlyActiveUnits = new List<UnitBase>();
    public GameObject unitPrefab;
    public UnitType defaultUnitType;

    [Header("Systems")]
    public AStartPathfinding pathfinder;
    public GridManager gridManager;

    [Header("Visual Settings")]
    public Material[] teamMaterials; // Array of materials indexed by army ID

    // Just change this method in CurrentTeamArmyManager.cs
    public UnitInstance SpawnUnit(Vector3 position)
    {
        // Spawn the base unit
        GameObject baseUnit = Instantiate(unitPrefab, position, Quaternion.identity);
        UnitInstance unit = baseUnit.GetComponent<UnitInstance>();

        if (unit == null)
        {
            Debug.LogError($"{name}: Spawned unit doesn't have UnitInstance component!");
            Destroy(baseUnit);
            return null;
        }

        // Initialize the unit with army ID
        unit.Initialize(pathfinder, defaultUnitType, gridManager, armyID);
        currentlyActiveUnits.Add(unit);

        // Apply team material using the improved system
        ApplyTeamVisuals(unit);

        Debug.Log($"{name}: Spawned unit {unit.name} for Army {armyID} at {position}");

        return unit; // Return the spawned unit
    }

    private void ApplyTeamVisuals(UnitInstance unit)
    {
       
        if (teamMaterials != null && armyID < teamMaterials.Length && teamMaterials[armyID] != null)
        {
            Material teamMaterial = teamMaterials[armyID];

            // Apply to all SkinnedMeshRenderers in the unit
            SkinnedMeshRenderer[] renderers = unit.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (var renderer in renderers)
            {
                renderer.material = teamMaterial;
            }

            Debug.Log($"Applied team material for Army {armyID} to unit {unit.name}");
        }

     
    }
    // Get all units of this army
    public List<UnitInstance> GetArmyUnits()
    {
        List<UnitInstance> armyUnits = new List<UnitInstance>();
        foreach (var unit in currentlyActiveUnits)
        {
            if (unit is UnitInstance unitInstance)
            {
                armyUnits.Add(unitInstance);
            }
        }
        return armyUnits;
    }
}