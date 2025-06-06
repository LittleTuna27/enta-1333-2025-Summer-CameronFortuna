using System.Collections.Generic;
using UnityEngine;

public class UnitSelectionManager : MonoBehaviour
{
    public static UnitSelectionManager Instance;

    public List<UnitInstance> allUnitsList = new();
    public List<UnitInstance> selectedUnits = new();

    void Awake()
    {
        Instance = this;
    }

    public void DeselectAll()
    {
        foreach (var unit in selectedUnits)
            unit.Deselect();
        selectedUnits.Clear();
    }

    public void DragSelect(UnitInstance unit)
    {
        if (!selectedUnits.Contains(unit))
        {
            selectedUnits.Add(unit);
            unit.Select();
        }
    }
}