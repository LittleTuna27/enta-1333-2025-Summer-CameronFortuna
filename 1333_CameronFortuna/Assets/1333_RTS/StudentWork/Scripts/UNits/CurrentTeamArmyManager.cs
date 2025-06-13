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
        // Method 1: Use the simple teamMaterials array
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

    // Method to change a unit's army allegiance
    public void ChangeUnitArmy(UnitInstance unit, int newArmyID)
    {
        if (unit != null && currentlyActiveUnits.Contains(unit))
        {
            unit.SetArmyID(newArmyID);

            // Remove from current army if switching to different army
            if (newArmyID != armyID)
            {
                currentlyActiveUnits.Remove(unit);
                Debug.Log($"Unit {unit.name} transferred from Army {armyID} to Army {newArmyID}");
            }
        }
    }

    // Method to recruit a unit from another army
    public void RecruitUnit(UnitInstance unit)
    {
        if (unit != null && !currentlyActiveUnits.Contains(unit))
        {
            unit.SetArmyID(armyID);
            currentlyActiveUnits.Add(unit);
            ApplyTeamVisuals(unit);
            Debug.Log($"Recruited unit {unit.name} to Army {armyID}");
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

    // Get unit count for this army
    public int GetUnitCount()
    {
        return currentlyActiveUnits.Count;
    }
}

// Optional: Advanced Army Visual Settings ScriptableObject
[System.Serializable]
public class ArmyVisualSettings : ScriptableObject
{
    [System.Serializable]
    public class ArmySettings
    {
        public int armyID;
        public Material unitSkin;
        public Color primaryColor = Color.white;
        public Color secondaryColor = Color.gray;
        public Material weaponMaterial;
        public Material armorMaterial;
    }

    public ArmySettings[] armySettingsLookup;

    public bool TryGetArmySettings(int armyID, out ArmySettings settings)
    {
        settings = null;
        foreach (var armySetting in armySettingsLookup)
        {
            if (armySetting.armyID == armyID)
            {
                settings = armySetting;
                return true;
            }
        }
        return false;
    }

    public void ApplyArmyVisuals(UnitInstance unit, int armyID)
    {
        if (TryGetArmySettings(armyID, out var settings))
        {
            // Apply main unit skin
            if (settings.unitSkin != null)
            {
                SkinnedMeshRenderer[] renderers = unit.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var renderer in renderers)
                {
                    renderer.material = settings.unitSkin;
                }
            }

            // Apply colors if needed
            if (settings.primaryColor != Color.white)
            {
                // You can implement color tinting here
                // For example, modify material properties
            }

            Debug.Log($"Applied advanced army visuals for Army {armyID}");
        }
    }
}