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
    public GridManager gridManager; // assign in Inspector


    public void SpawnUnit(Vector3 position)
    {
        GameObject baseUNit = Instantiate(unitPrefab, position, Quaternion.identity);
        UnitInstance unit = baseUNit.GetComponent<UnitInstance>();
        unit.Initialize(pathfinder, defaultUnitType, gridManager);
        //unit.SetMaterial(teamMaterials[armyID]);
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
