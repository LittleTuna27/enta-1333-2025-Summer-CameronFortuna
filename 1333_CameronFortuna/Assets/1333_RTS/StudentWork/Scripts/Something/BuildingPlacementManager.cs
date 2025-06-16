using UnityEngine;

public class BuildingPlacementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;

    [Header("Ghost Preview Materials")]
    [SerializeField] private Material ghostValidMaterial;
    [SerializeField] private Material ghostInvalidMaterial;

    public static BuildingPlacementManager Instance;
    private BuildingData currentSelectedBuilding;

    private Camera mainCamera;
    private GameObject ghostInstance;
    private bool canPlaceHere = false;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!BuildModeController.Instance.IsInBuildMode || currentSelectedBuilding == null)
        {
            DestroyGhost();
            return;
        }

        UpdateGhostPreview();

        if (Input.GetMouseButtonDown(0) && canPlaceHere)
        {
            PlaceBuilding();
        }
    }

    public void SetActiveBuilding(BuildingData buildingData)
    {
        currentSelectedBuilding = buildingData;
        CreateGhostInstance();
    }

    private void CreateGhostInstance()
    {
        if (ghostInstance != null) Destroy(ghostInstance);

        ghostInstance = Instantiate(currentSelectedBuilding.BuildingPrefab);
        ghostInstance.transform.localScale = currentSelectedBuilding.Scale;

        ApplyGhostMaterial(ghostValidMaterial);
    }

    private void DestroyGhost()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
        }
    }

    private void ApplyGhostMaterial(Material mat)
    {
        if (ghostInstance == null) return;

        foreach (var renderer in ghostInstance.GetComponentsInChildren<Renderer>())
        {
            renderer.material = mat;
        }
    }

    private void UpdateGhostPreview()
    {
        if (ghostInstance == null || currentSelectedBuilding == null)
            return;
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector2Int gridCoords = gridManager.GetGridPositionFromWorld(hit.point);
            Vector3 placePos = CalculateWorldPosition(gridCoords, currentSelectedBuilding.BuildingWidth, currentSelectedBuilding.BuildingDepth, gridManager.nodeSize);

            ghostInstance.transform.position = placePos;

            canPlaceHere = CanPlaceBuildingAt(gridCoords.x, gridCoords.y, currentSelectedBuilding.BuildingWidth, currentSelectedBuilding.BuildingDepth);
            ApplyGhostMaterial(canPlaceHere ? ghostValidMaterial : ghostInvalidMaterial);
        }
    }

    private void PlaceBuilding()
    {
        Vector2Int gridCoords = gridManager.GetGridPositionFromWorld(ghostInstance.transform.position);

        GameObject newBuilding = Instantiate(currentSelectedBuilding.BuildingPrefab, ghostInstance.transform.position, Quaternion.identity);
        newBuilding.transform.localScale = currentSelectedBuilding.Scale;

        for (int x = 0; x < currentSelectedBuilding.BuildingWidth; x++)
        {
            for (int y = 0; y < currentSelectedBuilding.BuildingDepth; y++)
            {
                GridNode node = gridManager.GetNode(gridCoords.x + x, gridCoords.y + y);
                if (node != null)
                {
                    node.walkable = false;
                    node.IsOccupied = true;
                }
            }
        }
    }

    private bool CanPlaceBuildingAt(int startX, int startY, int width, int height)
    {
        for (int xOffset = 0; xOffset < width; xOffset++)
        {
            for (int yOffset = 0; yOffset < height; yOffset++)
            {
                int checkX = startX + xOffset;
                int checkY = startY + yOffset;

                GridNode node = gridManager.GetNode(checkX, checkY);
                if (node == null || !node.walkable || node.IsOccupied)
                    return false;
            }
        }
        return true;
    }

    private Vector3 CalculateWorldPosition(Vector2Int origin, int width, int height, float nodeSize)
    {
        float halfWidthOffset = (width - 1) * 0.5f * nodeSize;
        float halfHeightOffset = (height - 1) * 0.5f * nodeSize;

        Vector3 basePosition = gridManager.GridSettings.UseXZPlane
            ? new Vector3(origin.x, 0f, origin.y) * nodeSize
            : new Vector3(origin.x, origin.y, 0f) * nodeSize;

        return gridManager.GridSettings.UseXZPlane
            ? basePosition + new Vector3(halfWidthOffset, 0f, halfHeightOffset)
            : basePosition + new Vector3(halfWidthOffset, halfHeightOffset, 0f);
    }
}