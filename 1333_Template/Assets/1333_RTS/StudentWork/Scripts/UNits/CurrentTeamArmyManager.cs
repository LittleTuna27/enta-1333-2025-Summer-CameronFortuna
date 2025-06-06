using UnityEngine;
using System.Collections.Generic;

public class CurrentTeamArmyManager : MonoBehaviour
{
    public int armyID;
    public bool isPlayer => armyID == 0;

    public List<UnitBase> currentlyActiveUnits = new List<UnitBase>();
    public GameObject unitPrefab;
    public UnitType defaultUnitType;

    public AStartPathfinding pathfinder;
    public GridManager gridManager;
    public PathFinderVisulization visualizer;

    public Material[] teamMaterials;

    public void SpawnUnit(Vector3 position)
    {
        if (unitPrefab == null)
        {
            Debug.LogError($"{name}: Cannot spawn unit - unitPrefab is null!");
            return;
        }

        GameObject baseUnit = Instantiate(unitPrefab, position, Quaternion.identity);
        UnitInstance unit = baseUnit.GetComponent<UnitInstance>();

        if (unit == null)
        {
            Debug.LogError($"{name}: Spawned unit doesn't have UnitInstance component!");
            Destroy(baseUnit);
            return;
        }

        unit.Initialize(pathfinder, defaultUnitType, gridManager, visualizer);
        currentlyActiveUnits.Add(unit);

        // Apply team material if available
        if (teamMaterials != null && armyID < teamMaterials.Length && teamMaterials[armyID] != null)
        {
            SkinnedMeshRenderer renderer = unit.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                renderer.material = teamMaterials[armyID];
            }
        }

        Debug.Log($"{name}: Spawned unit {unit.name} at {position}");
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
}
