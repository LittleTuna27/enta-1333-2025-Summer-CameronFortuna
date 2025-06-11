using UnityEngine;
using System.Collections.Generic;

public class BuildingPlacementManager : MonoBehaviour
{
    public static BuildingPlacementManager Instance;

    [SerializeField] private GridManager gridManager;
    [SerializeField] private Material ghostMaterial;

    private BuildingData currentSelectedBuilding;
    private GameObject ghostInstance;
    private Dictionary<GridNode, GameObject> placedBuildings = new();

    private void Awake()
    {
        Instance = this;
    }

    public void SetActiveBuilding(BuildingData buildingData)
    {
        currentSelectedBuilding = buildingData;
        Debug.Log("Selected Building: " + buildingData.BuildingName);

        if (ghostInstance != null)
            Destroy(ghostInstance);

        if (buildingData.BuildingPrefab != null)
        {
            ghostInstance = Instantiate(buildingData.BuildingPrefab);

            // Apply ghost material
            Renderer[] renderers = ghostInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.material = ghostMaterial;
            }
        }
    }

    private void Update()
    {
        HandleGhostPreview();
        HandlePlacementInput();
    }

    private void HandleGhostPreview()
    {
        if (ghostInstance != null && currentSelectedBuilding != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GridNode node = gridManager.GetNodeFromWorldPosition(hit.point);
                if (node != null)
                {
                    ghostInstance.transform.position = node.WorldPosition;
                }
            }
        }
    }

    private void HandlePlacementInput()
    {
        // Left click to place
        if (Input.GetMouseButtonDown(0) && currentSelectedBuilding != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                GridNode originNode = gridManager.GetNodeFromWorldPosition(hit.point);
                if (originNode == null)
                    return;

                List<GridNode> footprint = new();
                bool canPlace = true;

                int width = Mathf.Max(1, currentSelectedBuilding.Width);
                int height = Mathf.Max(1, currentSelectedBuilding.Height);

                Vector3 basePos = originNode.WorldPosition;

                for (int dx = 0; dx < width; dx++)
                {
                    for (int dy = 0; dy < height; dy++)
                    {
                        Vector3 offsetPos = basePos + new Vector3(dx * gridManager.GridSettings.NodeSize, 0, dy * gridManager.GridSettings.NodeSize);
                        GridNode checkNode = gridManager.GetNodeFromWorldPosition(offsetPos);
                        if (checkNode == null || !checkNode.walkable || placedBuildings.ContainsKey(checkNode))
                        {
                            canPlace = false;
                            break;
                        }
                        footprint.Add(checkNode);
                    }
                    if (!canPlace) break;
                }

                if (canPlace)
                {
                    GameObject building = Instantiate(currentSelectedBuilding.BuildingPrefab, originNode.WorldPosition, Quaternion.identity);
                    foreach (var node in footprint)
                    {
                        placedBuildings[node] = building;
                        node.walkable = false;
                    }

                    Debug.Log($"Placed {currentSelectedBuilding.BuildingName} at {originNode.WorldPosition}");

                    Destroy(ghostInstance);
                    ghostInstance = null;
                    currentSelectedBuilding = null;
                }
                else
                {
                    Debug.Log("Cannot place building here: Some tiles are not walkable or already occupied.");
                }
            }
        }

        // Right click to cancel placement
        if (Input.GetMouseButtonDown(1) && currentSelectedBuilding != null)
        {
            Debug.Log("Canceled building placement.");
            Destroy(ghostInstance);
            ghostInstance = null;
            currentSelectedBuilding = null;
        }
    }
}
