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
        // Don't exclude any layers - we want to be able to target buildings now
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // First, check if we hit something with IDamageable (units or buildings)
            IDamageable hitTarget = hit.collider.GetComponent<IDamageable>();
            if (hitTarget == null)
            {
                hitTarget = hit.collider.GetComponentInParent<IDamageable>();
            }

            if (hitTarget != null)
            {
                HandleTargetClick(hitTarget, hit.collider.gameObject);
            }
            else
            {
                HandleTerrainClick(hit.point);
            }
        }
    }

    private void HandleTargetClick(IDamageable target, GameObject targetObject)
    {
        foreach (var selectedUnit in UnitSelectionManager.Instance.selectedUnits)
        {
            // Check if target is an enemy
            if (IsEnemyTarget(selectedUnit, target))
            {
                // It's an enemy - set as attack target (this will handle pathfinding internally)
                selectedUnit.SetAttackTarget(target);
                Debug.Log($"{selectedUnit.name} ordered to attack {GetTargetName(targetObject)}");
            }
            else
            {
                // It's a friendly target - move to nearest walkable position around it
                GridNode targetNode = gridManager.GetNearestWalkableNode(target.GetPosition());
                if (targetNode != null)
                {
                    selectedUnit.MoveTo(targetNode);
                    Debug.Log($"{selectedUnit.name} ordered to move to friendly {GetTargetName(targetObject)}");
                }
                else
                {
                    Debug.LogWarning($"Cannot find walkable path to friendly {GetTargetName(targetObject)}");
                }
            }
        }
    }

    private bool IsEnemyTarget(UnitInstance unit, IDamageable target)
    {
        // Check if target is a unit
        if (target is UnitInstance targetUnit)
        {
            return unit.ArmyID != targetUnit.ArmyID;
        }

        // Check if target is a building
        if (target is BuildingHealth targetBuilding)
        {
            return unit.ArmyID != targetBuilding.ArmyID;
        }

        // If we can't determine army, assume it's an enemy
        return true;
    }

    private string GetTargetName(GameObject targetObject)
    {
        // Try to get a more descriptive name
        if (targetObject.GetComponent<UnitInstance>() != null)
        {
            return $"unit {targetObject.name}";
        }
        else if (targetObject.GetComponent<BuildingHealth>() != null)
        {
            var building = targetObject.GetComponent<BuildingHealth>();
            return $"building {building.BuildingName}";
        }

        return targetObject.name;
    }
    private void HandleTerrainClick(Vector3 worldPosition)
    {
        // Use GetNearestWalkableNode to ensure we're targeting a walkable position
        GridNode targetNode = gridManager.GetNearestWalkableNode(worldPosition);

        if (targetNode != null)
        {
            foreach (var unit in UnitSelectionManager.Instance.selectedUnits)
            {
                unit.MoveTo(targetNode); // This triggers A* and starts movement
                unit.ClearAttackTarget(); // Clear any existing attack target
            }
            Debug.Log($"Units ordered to move to terrain position");
        }
        else
        {
            Debug.LogWarning("Cannot find walkable position to move to");
        }
    }
}