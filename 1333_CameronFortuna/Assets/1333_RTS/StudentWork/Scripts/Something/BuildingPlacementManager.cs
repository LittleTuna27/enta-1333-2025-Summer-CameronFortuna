using UnityEngine;

public class BuildingPlacementManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridManager gridManager;
    [SerializeField] private GameObject buildingPrefab;

    public static BuildingPlacementManager Instance;
    private BuildingData currentSelectedBuilding;

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }
    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Place building when in build mode
        if (BuildModeController.Instance.IsInBuildMode)
        {
            if (Input.GetMouseButtonDown(0)) // Left click
            {
                TryPlaceBuildingAtMouseClick();
            }
            return; // Prevents spawning units while in build mode
        }
    }
    public void SetActiveBuilding(BuildingData buildingData)
    {
        currentSelectedBuilding = buildingData;
    }
    private void TryPlaceBuildingAtMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 hitPos = hit.point;
            Vector2Int gridCoords = gridManager.GetGridPositionFromWorld(hitPos);

            int width = 2;
            int height = 2;

            if (CanPlaceBuildingAt(gridCoords.x, gridCoords.y, width, height))
            {
                Vector3 placePos = CalculateWorldPosition(gridCoords, width, height, gridManager.nodeSize);
                Instantiate(buildingPrefab, placePos, Quaternion.identity);

                // Mark grid nodes as unwalkable
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        GridNode node = gridManager.GetNode(gridCoords.x + x, gridCoords.y + y);
                        if (node != null) node.walkable = false;
                    }
                }
            }
            else
            {
                Debug.Log("Can't place building here.");
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
                if (node == null || !node.walkable)
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
