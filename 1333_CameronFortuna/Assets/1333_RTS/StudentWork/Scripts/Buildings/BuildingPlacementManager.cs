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

    //rotation system variables
    private int currentRotation = 0; // 0, 90, 180, 270 degrees
    private readonly int[] rotationAngles = { 0, 90, 180, 270 };

    //initialize instance and camera reference
    private void Awake()
    {
        Instance = this;
        mainCamera = Camera.main;
    }

    //handles build mode state, rotation, ghost preview, and placement
    private void Update()
    {
        if (wasInBuildModeLastFrame && !BuildModeController.Instance.IsInBuildMode)
        {
            OnExitBuildMode();
        }

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

        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateBuilding();
        }

        UpdateGhostPreview();

        if (Input.GetMouseButtonDown(0) && canPlaceHere)
        {
            PlaceBuilding();
        }
    }

    //rotates the selected building by 90 degrees
    private void RotateBuilding()
    {
        currentRotation = (currentRotation + 1) % rotationAngles.Length;

        if (ghostInstance != null)
        {
            ApplyRotationToGhost();
        }

        Debug.Log($"Building rotated to {rotationAngles[currentRotation]} degrees");
    }

    //applies current rotation to the ghost instance
    private void ApplyRotationToGhost()
    {
        if (ghostInstance != null)
        {
            ghostInstance.transform.rotation = Quaternion.Euler(0, rotationAngles[currentRotation], 0);
        }
    }

    //called when entering build mode
    private void OnEnterBuildMode()
    {
        Debug.Log("Entered build mode");
        DestroyGhost();
        canPlaceHere = false;
        currentRotation = 0;
    }

    //called when exiting build mode
    private void OnExitBuildMode()
    {
        Debug.Log("Exited build mode");
        currentSelectedBuilding = null;
        DestroyGhost();
        canPlaceHere = false;
        currentRotation = 0;
    }

    //sets the currently active building to place
    public void SetActiveBuilding(BuildingData buildingData)
    {
        Debug.Log($"SetActiveBuilding called with: {(buildingData != null ? buildingData.BuildingName : "null")}");
        DestroyGhost();

        currentSelectedBuilding = buildingData;
        currentRotation = 0;

        if (buildingData != null && BuildModeController.Instance != null && BuildModeController.Instance.IsInBuildMode)
        {
            CreateGhostInstance();
        }
    }

    //creates a ghost preview of the selected building
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

        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
        }

        try
        {
            ghostInstance = Instantiate(currentSelectedBuilding.BuildingPrefab);
            ghostInstance.transform.localScale = currentSelectedBuilding.Scale;
            ApplyRotationToGhost();

            Collider[] colliders = ghostInstance.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders) col.enabled = false;

            MonoBehaviour[] scripts = ghostInstance.GetComponentsInChildren<MonoBehaviour>();
            foreach (MonoBehaviour script in scripts) if (script != null) script.enabled = false;

            ApplyGhostMaterial(ghostValidMaterial);
            Debug.Log($"Created ghost instance for {currentSelectedBuilding.BuildingName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create ghost instance: {e.Message}");
            ghostInstance = null;
        }
    }

    //destroys the current ghost preview
    private void DestroyGhost()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
            canPlaceHere = false;
        }
    }

    //applies the given material to the ghost instance
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

    //updates the ghost preview position, rotation, and placement validity
    private void UpdateGhostPreview()
    {
        if (ghostInstance == null || currentSelectedBuilding == null) return;

        try
        {
            int layerMask = ~LayerMask.GetMask("DefensiveBuildings");
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                Vector2Int mouseGridCoords = gridManager.GetGridPositionFromWorld(hit.point);
                Vector2Int rotatedDimensions = GetRotatedBuildingDimensions(currentSelectedBuilding.BuildingWidth, currentSelectedBuilding.BuildingDepth);
                Vector2Int buildingOrigin = CalculateBuildingOrigin(mouseGridCoords, rotatedDimensions.x, rotatedDimensions.y);

                Vector3 placePos = CalculateWorldPosition(buildingOrigin, rotatedDimensions.x, rotatedDimensions.y, gridManager.nodeSize);
                ghostInstance.transform.position = placePos;
                ApplyRotationToGhost();

                canPlaceHere = CanPlaceBuildingAt(buildingOrigin.x, buildingOrigin.y, rotatedDimensions.x, rotatedDimensions.y);
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

    //returns the correct dimensions after rotation
    private Vector2Int GetRotatedBuildingDimensions(int width, int depth)
    {
        if (currentRotation == 1 || currentRotation == 3)
        {
            return new Vector2Int(depth, width);
        }
        else
        {
            return new Vector2Int(width, depth);
        }
    }

    //calculates the building's origin grid cell based on mouse position
    private Vector2Int CalculateBuildingOrigin(Vector2Int mouseGridPos, int buildingWidth, int buildingDepth)
    {
        int halfWidth = buildingWidth / 2;
        int halfDepth = buildingDepth / 2;

        return new Vector2Int(mouseGridPos.x - halfWidth, mouseGridPos.y - halfDepth);
    }

    //places the selected building on the grid
    public void PlaceBuilding()
    {
        if (currentSelectedBuilding == null || ghostInstance == null)
        {
            Debug.LogWarning("Cannot place building - missing data or ghost instance");
            return;
        }

        try
        {
            int layerMask = ~LayerMask.GetMask("DefensiveBuildings");
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
            {
                Vector2Int mouseGridCoords = gridManager.GetGridPositionFromWorld(hit.point);
                Vector2Int rotatedDimensions = GetRotatedBuildingDimensions(currentSelectedBuilding.BuildingWidth, currentSelectedBuilding.BuildingDepth);
                Vector2Int buildingOrigin = CalculateBuildingOrigin(mouseGridCoords, rotatedDimensions.x, rotatedDimensions.y);

                Quaternion buildingRotation = Quaternion.Euler(0, rotationAngles[currentRotation], 0);
                GameObject newBuilding = Instantiate(currentSelectedBuilding.BuildingPrefab, ghostInstance.transform.position, buildingRotation);
                newBuilding.transform.localScale = currentSelectedBuilding.Scale;

                if (SoundPracticePlayer.Instance != null)
                {
                    SoundPracticePlayer.Instance.PlaySound(2, AudioSourceType.UI);
                }

                DefensiveBuilding defensiveBuilding = newBuilding.GetComponent<DefensiveBuilding>();
                if (defensiveBuilding != null)
                {
                    defensiveBuilding.OnBuildingPlaced();
                    Debug.Log("Defensive building activated after placement");
                }

                BuildingHealth buildingHealth = newBuilding.GetComponent<BuildingHealth>();
                if (buildingHealth != null && buildingHealth.ArmyID == 0)
                {
                    bool isCastle = IsPlayerCastle(currentSelectedBuilding.BuildingName, newBuilding.name);
                    if (isCastle)
                    {
                        Debug.Log("Castle placed! Enemy spawners have been notified.");
                    }
                }

                for (int x = 0; x < rotatedDimensions.x; x++)
                {
                    for (int y = 0; y < rotatedDimensions.y; y++)
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

                Debug.Log($"Successfully placed {currentSelectedBuilding.BuildingName} at origin {buildingOrigin} with {rotationAngles[currentRotation]}° rotation");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to place building: {e.Message}");
        }
    }

    //checks if the building can be placed at the given grid area
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

    //converts grid origin and size into a world position
    private Vector3 CalculateWorldPosition(Vector2Int origin, int width, int height, float nodeSize)
    {
        float centerGridX = origin.x + (width - 1) * 0.5f;
        float centerGridY = origin.y + (height - 1) * 0.5f;

        return gridManager.GridSettings.UseXZPlane
            ? new Vector3(centerGridX * nodeSize, 0f, centerGridY * nodeSize)
            : new Vector3(centerGridX * nodeSize, centerGridY * nodeSize, 0f);
    }

    //checks if the selected building is a player castle
    private bool IsPlayerCastle(string buildingDataName, string gameObjectName)
    {
        string lowerBuildingName = buildingDataName.ToLower();
        string lowerGameObjectName = gameObjectName.ToLower();

        return lowerBuildingName.Contains("castle") || lowerGameObjectName.Contains("castle");
    }

    //gets the current rotation angle
    public int GetCurrentRotation()
    {
        return rotationAngles[currentRotation];
    }
}
