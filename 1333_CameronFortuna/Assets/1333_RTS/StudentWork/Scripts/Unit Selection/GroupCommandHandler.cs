using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroupCommandHandler : MonoBehaviour
{
    [SerializeField] private GridManager gridManager;
    [SerializeField] private Camera cam;

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Right-click
        {
            HandleRightClick();
        }
    }
    private void HandleRightClick()
    {
        // Exclude DefensiveBuildings layer from raycasts
        int layerMask = ~LayerMask.GetMask("DefensiveBuildings");

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            // Check if we hit a unit
            UnitInstance hitUnit = hit.collider.GetComponent<UnitInstance>();
            if (hitUnit == null)
            {
                hitUnit = hit.collider.GetComponentInParent<UnitInstance>();
            }

            if (hitUnit != null)
            {
                HandleUnitClick(hitUnit);
            }
            else
            {
                HandleTerrainClick(hit.point);
            }
        }
    }

    private void HandleUnitClick(UnitInstance targetUnit)
    {
        foreach (var selectedUnit in UnitSelectionManager.Instance.selectedUnits)
        {
            // Check if target is an enemy (different army ID)
            if (selectedUnit.ArmyID != targetUnit.ArmyID)
            {
                // It's an enemy - set as attack target and move towards it
                selectedUnit.SetAttackTarget(targetUnit);
                Debug.Log($"{selectedUnit.name} ordered to attack {targetUnit.name}");
            }
            else
            {
                // It's a friendly unit - just move to its location
                GridNode targetNode = gridManager.GetNodeFromWorldPosition(targetUnit.transform.position);
                selectedUnit.MoveTo(targetNode);
                Debug.Log($"{selectedUnit.name} ordered to move to friendly unit {targetUnit.name}");
            }
        }
    }

    private void HandleTerrainClick(Vector3 worldPosition)
    {
        GridNode targetNode = gridManager.GetNodeFromWorldPosition(worldPosition);

        foreach (var unit in UnitSelectionManager.Instance.selectedUnits)
        {
            unit.MoveTo(targetNode); // This triggers A* and starts movement
            unit.ClearAttackTarget(); // Clear any existing attack target
        }

        Debug.Log($"Units ordered to move to terrain position");
    }
}