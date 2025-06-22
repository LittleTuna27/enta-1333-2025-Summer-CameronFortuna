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
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GridNode targetNode = gridManager.GetNodeFromWorldPosition(hit.point);

                foreach (var unit in UnitSelectionManager.Instance.selectedUnits)
                {
                    unit.MoveTo(targetNode); // This triggers A* and starts movement
                    unit.Attackmode();
                }
            }
        }
    }
}

