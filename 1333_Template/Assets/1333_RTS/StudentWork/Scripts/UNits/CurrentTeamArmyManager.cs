using UnityEngine;
using System.Collections.Generic;

public class CurrentTeamArmyManager : MonoBehaviour
{
    public int armyID;
    public bool isPlayer => armyID == 0;

    public List<UnitBase> currentlyActiveUnits = new List<UnitBase>();
    public GameObject unitPrefab;
    public AStartPathfinding pathfinder;
    public Material[] teamMaterials;
    public UnitType defaultUnitType;
    public PathFinderVisulization visualizer;
    public GridManager gridManager;



    public void SpawnUnit(Vector3 position)
    {
        GameObject baseUnit = Instantiate(unitPrefab, position, Quaternion.identity);
        UnitInstance unit = baseUnit.GetComponent<UnitInstance>();
        unit.Initialize(pathfinder, defaultUnitType, gridManager, visualizer); // <-- FIXED
        currentlyActiveUnits.Add(unit);
    }

    void Update()
    {
        foreach (UnitBase unit in currentlyActiveUnits)
        {
            //unit.PerTick();
        }
    }
}
