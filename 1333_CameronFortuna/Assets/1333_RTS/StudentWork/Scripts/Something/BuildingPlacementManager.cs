using System.Collections.Generic;
using UnityEngine;

public class BuildingPlacementManager : MonoBehaviour
{
    public static BuildingPlacementManager Instance;

    [SerializeField] private GridManager gridManager;
    [SerializeField] private Material ghostMaterial;

    private BuildingData currentSelectedBuilding;
    private GameObject ghostBuildingPreview;

    private Quaternion currentBuildingRotation = Quaternion.identity;
    private int totalRotationSteps = 0;

    private void Awake()
    {
        Instance = this;
    }

    public void SetActiveBuilding(BuildingData buildingData)
    {
        currentSelectedBuilding = buildingData;
        Debug.Log("Selected Building: " + buildingData.BuildingName);

        if (ghostBuildingPreview != null)
            Destroy(ghostBuildingPreview);

        if (buildingData.BuildingPrefab != null)
        {
            ghostBuildingPreview = Instantiate(buildingData.BuildingPrefab);

            // Apply ghost material to make building semi-transparent during placement
            Renderer[] allRenderers = ghostBuildingPreview.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in allRenderers)
            {
                renderer.material = ghostMaterial;
            }

            // Set ghost scale to match building data
            ghostBuildingPreview.transform.localScale = buildingData.Scale;
        }
    }

    private void Update()
    {
        HandleGhostPreview();
        HandlePlacementInput();

        // Handle building rotation with R key
        if (Input.GetKeyDown(KeyCode.R))
        {
            totalRotationSteps = (totalRotationSteps + 1) % 4;
            currentBuildingRotation = Quaternion.Euler(0, totalRotationSteps * 90f, 0);

            if (ghostBuildingPreview != null)
                ghostBuildingPreview.transform.rotation = currentBuildingRotation;
        }
    }

    private void HandleGhostPreview()
    {
        if (ghostBuildingPreview != null && currentSelectedBuilding != null)
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit raycastHit))
            {
                GridNode targetNode = gridManager.GetNodeFromWorldPosition(raycastHit.point);
                if (targetNode != null)
                {
                    ghostBuildingPreview.transform.position = targetNode.WorldPosition;
                }
            }
        }
    }

    private void HandlePlacementInput()
    {
        // Left click to place building
        if (Input.GetMouseButtonDown(0) && currentSelectedBuilding != null)
        {
            Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(mouseRay, out RaycastHit raycastHit))
            {
                GridNode buildingOriginNode = gridManager.GetNodeFromWorldPosition(raycastHit.point);
                if (buildingOriginNode == null) return;

                var buildingFootprintNodes = GetBuildingFootprint(buildingOriginNode, currentSelectedBuilding.BuildingWidth, currentSelectedBuilding.BuildingDepth);

                if (CanPlaceBuildingAt(buildingFootprintNodes))
                {
                    PlaceBuilding(buildingOriginNode, buildingFootprintNodes);
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
            ClearPlacementState();
        }
    }

    // Calculates all grid nodes that a building will occupy based on its width, height, and rotation
    private List<GridNode> GetBuildingFootprint(GridNode buildingOrigin, int buildingWidth, int buildingHeight)
    {
        List<GridNode> buildingFootprintNodes = new();
        Vector3 originWorldPosition = buildingOrigin.WorldPosition;

        // Loop through each position in the building's footprint
        for (int xOffsetFromOrigin = 0; xOffsetFromOrigin < buildingWidth; xOffsetFromOrigin++)
        {
            for (int zOffsetFromOrigin = 0; zOffsetFromOrigin < buildingHeight; zOffsetFromOrigin++)
            {
                // Calculate the local offset from the origin in grid coordinates
                Vector3 localGridOffset = new Vector3(
                    xOffsetFromOrigin * gridManager.GridSettings.NodeSize,
                    0,
                    zOffsetFromOrigin * gridManager.GridSettings.NodeSize
                );

                // Apply rotation to the offset to handle rotated buildings
                Vector3 rotatedGridOffset = currentBuildingRotation * localGridOffset;

                // Calculate the final world position for this grid node
                Vector3 finalNodeWorldPosition = originWorldPosition + rotatedGridOffset;

                // Get the grid node at this world position
                GridNode nodeAtPosition = gridManager.GetNodeFromWorldPosition(finalNodeWorldPosition);
                buildingFootprintNodes.Add(nodeAtPosition);
            }
        }

        return buildingFootprintNodes;
    }

    // Checks if a building can be placed at the given nodes
    // Simply checks if all nodes exist and are walkable (not occupied)
    private bool CanPlaceBuildingAt(List<GridNode> nodesToCheck)
    {
        foreach (var nodeToValidate in nodesToCheck)
        {
            // If walkable is false, it means the node is either:
            // - Naturally unwalkable (walls, obstacles, etc.)
            // - Already occupied by a building
            if (nodeToValidate == null || !nodeToValidate.walkable)
                return false;
        }
        return true;
    }
    // Places a building at the specified location and updates grid state

    private void PlaceBuilding(GridNode buildingOrigin, List<GridNode> occupiedNodes)
    {
        // Instantiate the actual building at the origin position
        GameObject actualBuilding = Instantiate(
            currentSelectedBuilding.BuildingPrefab,
            buildingOrigin.WorldPosition,
            currentBuildingRotation
        );
        actualBuilding.transform.localScale = currentSelectedBuilding.Scale;

        // Mark all occupied nodes as unwalkable (occupied by building)
        foreach (var occupiedNode in occupiedNodes)
        {
            occupiedNode.walkable = false;
        }

        Debug.Log($"Placed {currentSelectedBuilding.BuildingName} at {buildingOrigin.WorldPosition}");
        ClearPlacementState();
    }
    private void ClearPlacementState()
    {
        if (ghostBuildingPreview != null)
            Destroy(ghostBuildingPreview);

        ghostBuildingPreview = null;
        currentSelectedBuilding = null;
    }
}