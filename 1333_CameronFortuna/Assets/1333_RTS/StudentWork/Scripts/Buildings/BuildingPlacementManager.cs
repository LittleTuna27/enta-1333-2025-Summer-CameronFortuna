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
    private bool wasInBuildModeLastFrame = false;

    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
    }

    private void Update()
    {
        // Check if we just exited build mode
        if (wasInBuildModeLastFrame && !BuildModeController.Instance.IsInBuildMode)
        {
            OnExitBuildMode();
        }

        // Check if we just entered build mode
        if (!wasInBuildModeLastFrame && BuildModeController.Instance.IsInBuildMode)
        {
            OnEnterBuildMode();
        }

        wasInBuildModeLastFrame = BuildModeController.Instance.IsInBuildMode;

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
    private void OnEnterBuildMode()
    {
        Debug.Log("Entered build mode");
        // Clean up any existing state when entering build mode
        DestroyGhost();
        canPlaceHere = false;
    }
    private void OnExitBuildMode()
    {
        Debug.Log("Exited build mode");
        // Clean up everything when exiting build mode
        currentSelectedBuilding = null;
        DestroyGhost();
        canPlaceHere = false;
    }
    public void SetActiveBuilding(BuildingData buildingData)
    {
        Debug.Log($"SetActiveBuilding called with: {(buildingData != null ? buildingData.BuildingName : "null")}");

        // Always destroy existing ghost first
        DestroyGhost();

        currentSelectedBuilding = buildingData;

        if (buildingData != null && BuildModeController.Instance != null && BuildModeController.Instance.IsInBuildMode)
        {
            CreateGhostInstance();
        }
    }
    private void CreateGhostInstance()
    {
        if (currentSelectedBuilding == null)
        {
            Debug.LogError("Attempted to create ghost instance with null building data");
            return;
        }

        if (currentSelectedBuilding.BuildingPrefab == null)
        {
            Debug.LogError($"Building data {currentSelectedBuilding.BuildingName} has null BuildingPrefab");
            return;
        }

        // Ensure we destroy any existing ghost first
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
        }

        try
        {
            ghostInstance = Instantiate(currentSelectedBuilding.BuildingPrefab);
            ghostInstance.transform.localScale = currentSelectedBuilding.Scale;

            // Disable any colliders on the ghost to prevent interference
            Collider[] colliders = ghostInstance.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            // Disable any scripts that shouldn't run on the ghost
            MonoBehaviour[] scripts = ghostInstance.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts)
            {
                if (script != null)
                {
                    script.enabled = false;
                }
            }

            ApplyGhostMaterial(ghostValidMaterial);
            Debug.Log($"Created ghost instance for {currentSelectedBuilding.BuildingName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create ghost instance: {e.Message}");
            ghostInstance = null;
        }
    }
    private void DestroyGhost()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
            canPlaceHere = false;
        }
    }
    private void ApplyGhostMaterial(Material mat)
    {
        if (ghostInstance == null || mat == null) return;

        try
        {
            Renderer[] renderers = ghostInstance.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (renderer != null)
                {
                    renderer.material = mat;
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to apply ghost material: {e.Message}");
        }
    }
    private void UpdateGhostPreview()
    {
        if (ghostInstance == null || currentSelectedBuilding == null)
            return;

        try
        {
            // Exclude DefensiveBuildings layer from placement raycasts
            int layerMask = ~LayerMask.GetMask("DefensiveBuildings");

            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                // Get the grid position where the mouse is pointing
                Vector2Int mouseGridCoords = gridManager.GetGridPositionFromWorld(hit.point);

                // For multi-tile buildings, we need to adjust the origin to ensure proper alignment
                // This ensures the building placement aligns with the grid visualization
                Vector2Int buildingOrigin = CalculateBuildingOrigin(mouseGridCoords, currentSelectedBuilding.BuildingWidth, currentSelectedBuilding.BuildingDepth);

                Vector3 placePos = CalculateWorldPosition(buildingOrigin, currentSelectedBuilding.BuildingWidth, currentSelectedBuilding.BuildingDepth, gridManager.nodeSize);

                ghostInstance.transform.position = placePos;

                canPlaceHere = CanPlaceBuildingAt(buildingOrigin.x, buildingOrigin.y, currentSelectedBuilding.BuildingWidth, currentSelectedBuilding.BuildingDepth);
                ApplyGhostMaterial(canPlaceHere ? ghostValidMaterial : ghostInvalidMaterial);
            }
            else
            {
                canPlaceHere = false;
                ApplyGhostMaterial(ghostInvalidMaterial);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in UpdateGhostPreview: {e.Message}");
        }
    }
    private Vector2Int CalculateBuildingOrigin(Vector2Int mouseGridPos, int buildingWidth, int buildingDepth)
    {
        // For odd-sized buildings (like 3x3), we want the building centered on the mouse position
        // For even-sized buildings (like 2x2), we adjust to align properly

        int halfWidth = buildingWidth / 2;
        int halfDepth = buildingDepth / 2;

        // Calculate the origin (bottom-left corner) of the building
        Vector2Int origin = new Vector2Int(
            mouseGridPos.x - halfWidth,
            mouseGridPos.y - halfDepth
        );

        return origin;
    }
    private void PlaceBuilding()
    {
        if (currentSelectedBuilding == null || ghostInstance == null)
        {
            Debug.LogWarning("Cannot place building - missing data or ghost instance");
            return;
        }
        try
        {
            // Use the same logic as the ghost preview to determine grid coordinates
            int layerMask = ~LayerMask.GetMask("DefensiveBuildings");
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                Vector2Int mouseGridCoords = gridManager.GetGridPositionFromWorld(hit.point);
                Vector2Int buildingOrigin = CalculateBuildingOrigin(mouseGridCoords, currentSelectedBuilding.BuildingWidth, currentSelectedBuilding.BuildingDepth);

                GameObject newBuilding = Instantiate(currentSelectedBuilding.BuildingPrefab, ghostInstance.transform.position, Quaternion.identity);
                newBuilding.transform.localScale = currentSelectedBuilding.Scale;

                // Activate the building after placement
                DefensiveBuilding defensiveBuilding = newBuilding.GetComponent<DefensiveBuilding>();
                if (defensiveBuilding != null)
                {
                    defensiveBuilding.OnBuildingPlaced();
                    Debug.Log("Defensive building activated after placement");
                }

                // Check if this is a castle and notify enemy spawners
                BuildingHealth buildingHealth = newBuilding.GetComponent<BuildingHealth>();
                if (buildingHealth != null && buildingHealth.ArmyID == 0) // Player army
                {
                    if (IsPlayerCastle(currentSelectedBuilding.BuildingName, newBuilding.name))
                    {
                        NotifyEnemySpawnersOfCastle(newBuilding.transform);
                        Debug.Log("Castle placed! Enemy spawners have been notified.");
                    }
                }

                // Update grid occupancy using the calculated origin
                for (int x = 0; x < currentSelectedBuilding.BuildingWidth; x++)
                {
                    for (int y = 0; y < currentSelectedBuilding.BuildingDepth; y++)
                    {
                        GridNode node = gridManager.GetNode(buildingOrigin.x + x, buildingOrigin.y + y);
                        if (node != null)
                        {
                            node.walkable = false;
                            node.IsOccupied = true;
                            Debug.Log($"Occupied grid square: ({buildingOrigin.x + x}, {buildingOrigin.y + y})");
                        }
                    }
                }

                Debug.Log($"Successfully placed {currentSelectedBuilding.BuildingName} at origin {buildingOrigin}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to place building: {e.Message}");
        }
    }

   
    private bool CanPlaceBuildingAt(int startX, int startY, int width, int height)
    {
        if (gridManager == null) return false;

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

        float centerGridX = origin.x + (width - 1) * 0.5f;
        float centerGridY = origin.y + (height - 1) * 0.5f;

        Vector3 worldPosition = gridManager.GridSettings.UseXZPlane
            ? new Vector3(centerGridX * nodeSize, 0f, centerGridY * nodeSize)
            : new Vector3(centerGridX * nodeSize, centerGridY * nodeSize, 0f);

        return worldPosition;
    }
    private bool IsPlayerCastle(string buildingDataName, string gameObjectName)
    {
        // Check if this building is considered a castle
        string lowerBuildingName = buildingDataName.ToLower();
        string lowerGameObjectName = gameObjectName.ToLower();

        return lowerBuildingName.Contains("castle") ||
               lowerGameObjectName.Contains("castle");
    }

    private void NotifyEnemySpawnersOfCastle(Transform castle)
    {
        // Find all enemy spawners and tell them about the castle
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>();
        foreach (var spawner in spawners)
        {
            spawner.SetPlayerCastle(castle);
        }

        // Also notify AI Manager if it exists
        if (AIManager.Instance != null)
        {
            AIManager.Instance.SetPlayerCastle(castle);
        }

        Debug.Log($"Notified {spawners.Length} enemy spawners about castle placement");
    }
}