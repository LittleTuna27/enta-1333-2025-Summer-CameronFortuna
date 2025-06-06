using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitSelectionBox : MonoBehaviour
{
    Camera myCam;
    [SerializeField] RectTransform boxVisual;
    [SerializeField] private GridManager gridManager; // At the top of the script

    Rect selectionBox;
    Vector2 startPosition;
    Vector2 endPosition;

    private void Start()
    {
        myCam = Camera.main;
        startPosition = Vector2.zero;
        endPosition = Vector2.zero;
        DrawVisual();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (!Input.GetKey(KeyCode.LeftShift))
                UnitSelectionManager.Instance.DeselectAll();

            startPosition = Input.mousePosition;
            selectionBox = new Rect();
        }

        if (Input.GetMouseButton(0))
        {
            endPosition = Input.mousePosition;
            DrawVisual();
            DrawSelection();
        }

        if (Input.GetMouseButtonUp(0))
        {
            SelectUnits();
            startPosition = Vector2.zero;
            endPosition = Vector2.zero;
            DrawVisual();
        }
        if (Input.GetMouseButtonDown(2)) // Middle Click for debug
        {
            Ray ray = myCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GridNode node = gridManager.GetNodeFromWorldPosition(hit.point);
                Debug.Log($"Clicked Grid Cell: {node.Name} at {node.WorldPosition}");
            }
        }
    }

    void DrawVisual()
    {
        Vector2 boxStart = startPosition;
        Vector2 boxEnd = endPosition;
        Vector2 boxCenter = (boxStart + boxEnd) / 2;
        boxVisual.position = boxCenter;
        Vector2 boxSize = new Vector2(Mathf.Abs(boxStart.x - boxEnd.x), Mathf.Abs(boxStart.y - boxEnd.y));
        boxVisual.sizeDelta = boxSize;
    }

    void DrawSelection()
    {
        selectionBox.xMin = Mathf.Min(startPosition.x, Input.mousePosition.x);
        selectionBox.xMax = Mathf.Max(startPosition.x, Input.mousePosition.x);
        selectionBox.yMin = Mathf.Min(startPosition.y, Input.mousePosition.y);
        selectionBox.yMax = Mathf.Max(startPosition.y, Input.mousePosition.y);
    }

    void SelectUnits()
    {
        foreach (var unit in UnitSelectionManager.Instance.allUnitsList)
        {
            if (selectionBox.Contains(myCam.WorldToScreenPoint(unit.transform.position)))
            {
                UnitSelectionManager.Instance.DragSelect(unit);
            }
        }
    }
}