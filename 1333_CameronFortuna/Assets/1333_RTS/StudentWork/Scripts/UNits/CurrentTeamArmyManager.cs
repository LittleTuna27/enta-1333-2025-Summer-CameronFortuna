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
    public PathFinderVisulization visualizer;

    [Header("Visual Settings")]
    public Material[] teamMaterials; // Array of materials indexed by army ID

    public void SpawnUnit(Vector3 position)
    {
        if (unitPrefab == null)
        {
            Debug.LogError($"{name}: Cannot spawn unit - unitPrefab is null!");
            return;
        }

        // Spawn the base unit
        GameObject baseUnit = Instantiate(unitPrefab, position, Quaternion.identity);
        UnitInstance unit = baseUnit.GetComponent<UnitInstance>();

        if (unit == null)
        {
            Debug.LogError($"{name}: Spawned unit doesn't have UnitInstance component!");
            Destroy(baseUnit);
            return;
        }

        // Initialize the unit with army ID
        unit.Initialize(pathfinder, defaultUnitType, gridManager, visualizer, armyID);
        currentlyActiveUnits.Add(unit);

        // Apply team material using the improved system
        ApplyTeamVisuals(unit);

        Debug.Log($"{name}: Spawned unit {unit.name} for Army {armyID} at {position}");
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
    void Update()
    {
        // Update all units
        foreach (UnitBase unit in currentlyActiveUnits)
        {
            if (unit != null)
            {
                unit.PerTick();
            }
        }

        // Clean up destroyed units
        currentlyActiveUnits.RemoveAll(unit => unit == null);
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