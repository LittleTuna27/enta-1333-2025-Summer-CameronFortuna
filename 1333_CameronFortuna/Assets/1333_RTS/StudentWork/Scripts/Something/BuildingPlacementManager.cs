using System.Collections.Generic;
using UnityEngine;
using System;

public class BuildingPlacementManager : MonoBehaviour
{
    public static BuildingPlacementManager Instance;

    [Header("Dependencies")]
    [SerializeField] private GridManager gridManager;

    [Header("Materials")]
    [SerializeField] private Material ghostMaterial;
    [SerializeField] private Material validPlacementMaterial;
    [SerializeField] private Material invalidPlacementMaterial;

    [Header("Settings")]
    [SerializeField] private LayerMask groundLayerMask = 1;
    [SerializeField] private bool showPlacementPreview = true;

    // Events
    public static event Action<BuildingData, Vector3> OnBuildingPlaced;
    public static event Action<BuildingData, Vector3> OnBuildingRemoved;
    public static event Action<BuildingData> OnBuildingSelected;

    private BuildingData currentSelectedBuilding;
    private GameObject ghostInstance;
    private Dictionary<GridNode, BuildingInstance> placedBuildings = new();

    private Quaternion currentRotation = Quaternion.identity;
    private int currentRotationSteps = 0;

    // Cache for performance
    private Camera mainCamera;
    private List<GridNode> cachedFootprint = new();

    [System.Serializable]
    public class BuildingInstance
    {
        public GameObject gameObject;
        public BuildingData buildingData;
        public List<GridNode> occupiedNodes;
        public Quaternion rotation;

        public BuildingInstance(GameObject go, BuildingData data, List<GridNode> nodes, Quaternion rot)
        {
            gameObject = go;
            buildingData = data;
            occupiedNodes = new List<GridNode>(nodes);
            rotation = rot;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            mainCamera = Camera.main;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void SetActiveBuilding(BuildingData buildingData)
    {
        currentSelectedBuilding = buildingData;
        OnBuildingSelected?.Invoke(buildingData);

        Debug.Log($"Selected Building: {buildingData.BuildingName}");

        CreateGhostInstance();
        ResetRotation();
    }

    public void ClearSelection()
    {
        ClearPlacementState();
    }

    private void CreateGhostInstance()
    {
        DestroyGhostInstance();

        if (currentSelectedBuilding?.BuildingPrefab != null)
        {
            ghostInstance = Instantiate(currentSelectedBuilding.BuildingPrefab);
            ghostInstance.name = $"Ghost_{currentSelectedBuilding.BuildingName}";

            // Disable colliders on ghost
            Collider[] colliders = ghostInstance.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }

            // Apply ghost material
            ApplyGhostMaterial();

            // Set ghost scale
            ghostInstance.transform.localScale = currentSelectedBuilding.Scale;
        }
    }

    private void ApplyGhostMaterial()
    {
        if (ghostInstance == null) return;

        Renderer[] renderers = ghostInstance.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = ghostMaterial;
            }
            renderer.materials = materials;
        }
    }

    private void Update()
    {
        if (currentSelectedBuilding == null) return;

        HandleRotationInput();
        HandleGhostPreview();
        HandlePlacementInput();
    }

    private void HandleRotationInput()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RotateBuilding();
        }
    }

    private void RotateBuilding()
    {
        currentRotationSteps = (currentRotationSteps + 1) % 4;
        currentRotation = Quaternion.Euler(0, currentRotationSteps * 90f, 0);

        if (ghostInstance != null)
        {
            ghostInstance.transform.rotation = currentRotation;
        }
    }

    private void ResetRotation()
    {
        currentRotationSteps = 0;
        currentRotation = Quaternion.identity;

        if (ghostInstance != null)
        {
            ghostInstance.transform.rotation = currentRotation;
        }
    }

    private void HandleGhostPreview()
    {
        if (ghostInstance == null || currentSelectedBuilding == null) return;

        if (TryGetMouseWorldPosition(out Vector3 worldPos))
        {
            GridNode node = gridManager.GetNodeFromWorldPosition(worldPos);
            if (node != null)
            {
                ghostInstance.transform.position = node.WorldPosition;

                if (showPlacementPreview)
                {
                    UpdateGhostMaterial(node);
                }
            }
        }
    }

    private bool TryGetMouseWorldPosition(out Vector3 worldPosition)
    {
        worldPosition = Vector3.zero;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayerMask) &&
               (worldPosition = hit.point) != Vector3.zero;
    }

    private void UpdateGhostMaterial(GridNode originNode)
    {
        if (!showPlacementPreview) return;

        var footprint = GetFootprint(originNode, currentSelectedBuilding.Width, currentSelectedBuilding.Height);
        bool canPlace = CanPlaceAt(footprint);

        Material materialToUse = canPlace ? validPlacementMaterial ?? ghostMaterial : invalidPlacementMaterial ?? ghostMaterial;

        Renderer[] renderers = ghostInstance.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material[] materials = new Material[renderer.materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i] = materialToUse;
            }
            renderer.materials = materials;
        }
    }

    private void HandlePlacementInput()
    {
        // Place building
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceBuilding();
        }

        // Remove building
        if (Input.GetMouseButtonDown(1))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                TryRemoveBuilding();
            }
            else
            {
                ClearPlacementState();
            }
        }

        // Cancel placement
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ClearPlacementState();
        }
    }

    private void TryPlaceBuilding()
    {
        if (currentSelectedBuilding == null) return;

        if (TryGetMouseWorldPosition(out Vector3 worldPos))
        {
            GridNode originNode = gridManager.GetNodeFromWorldPosition(worldPos);
            if (originNode == null)
            {
                Debug.LogWarning("Cannot place building: Invalid grid position");
                return;
            }

            var footprint = GetFootprint(originNode, currentSelectedBuilding.Width, currentSelectedBuilding.Height);
            string errorMessage = GetPlacementError(footprint);

            if (string.IsNullOrEmpty(errorMessage))
            {
                PlaceBuilding(originNode, footprint);
            }
            else
            {
                Debug.LogWarning($"Cannot place building: {errorMessage}");
            }
        }
    }

    private void TryRemoveBuilding()
    {
        if (TryGetMouseWorldPosition(out Vector3 worldPos))
        {
            GridNode node = gridManager.GetNodeFromWorldPosition(worldPos);
            if (node != null && placedBuildings.TryGetValue(node, out BuildingInstance buildingInstance))
            {
                RemoveBuilding(buildingInstance);
            }
        }
    }

    private List<GridNode> GetFootprint(GridNode originNode, int width, int height)
    {
        cachedFootprint.Clear();

        Vector3 centerOffset = new Vector3(
            (width - 1) * gridManager.GridSettings.NodeSize * 0.5f,
            0,
            (height - 1) * gridManager.GridSettings.NodeSize * 0.5f
        );

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                // Calculate local position relative to center
                Vector3 localPos = new Vector3(
                    (x - (width - 1) * 0.5f) * gridManager.GridSettings.NodeSize,
                    0,
                    (z - (height - 1) * 0.5f) * gridManager.GridSettings.NodeSize
                );

                // Apply rotation
                Vector3 rotatedPos = currentRotation * localPos;

                // Get world position
                Vector3 worldPos = originNode.WorldPosition + rotatedPos;
                GridNode node = gridManager.GetNodeFromWorldPosition(worldPos);

                cachedFootprint.Add(node);
            }
        }

        return cachedFootprint;
    }

    private string GetPlacementError(List<GridNode> nodes)
    {
        if (nodes == null || nodes.Count == 0)
            return "Invalid footprint";

        foreach (var node in nodes)
        {
            if (node == null)
                return "Building extends outside grid bounds";

            if (!node.walkable)
                return "Some tiles are not walkable";

            if (placedBuildings.ContainsKey(node))
                return "Space is already occupied";
        }

        return null; // No error
    }

    private bool CanPlaceAt(List<GridNode> nodes)
    {
        return string.IsNullOrEmpty(GetPlacementError(nodes));
    }

    private void PlaceBuilding(GridNode origin, List<GridNode> footprint)
    {
        GameObject building = Instantiate(currentSelectedBuilding.BuildingPrefab, origin.WorldPosition, currentRotation);
        building.transform.localScale = currentSelectedBuilding.Scale;
        building.name = $"{currentSelectedBuilding.BuildingName}_{Time.time}";

        BuildingInstance buildingInstance = new BuildingInstance(building, currentSelectedBuilding, footprint, currentRotation);

        // Mark nodes as occupied
        foreach (var node in footprint)
        {
            if (node != null)
            {
                placedBuildings[node] = buildingInstance;
                node.walkable = false;
            }
        }

        OnBuildingPlaced?.Invoke(currentSelectedBuilding, origin.WorldPosition);
        Debug.Log($"Placed {currentSelectedBuilding.BuildingName} at {origin.WorldPosition}");
    }

    private void RemoveBuilding(BuildingInstance buildingInstance)
    {
        if (buildingInstance == null) return;

        // Free up the nodes
        foreach (var node in buildingInstance.occupiedNodes)
        {
            if (node != null)
            {
                placedBuildings.Remove(node);
                node.walkable = true;
            }
        }

        Vector3 position = buildingInstance.gameObject.transform.position;

        // Destroy the building
        if (buildingInstance.gameObject != null)
        {
            Destroy(buildingInstance.gameObject);
        }

        OnBuildingRemoved?.Invoke(buildingInstance.buildingData, position);
        Debug.Log($"Removed {buildingInstance.buildingData.BuildingName} from {position}");
    }

    private void DestroyGhostInstance()
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
            ghostInstance = null;
        }
    }

    private void ClearPlacementState()
    {
        DestroyGhostInstance();
        currentSelectedBuilding = null;
        ResetRotation();
    }

    // Public utility methods
    public bool IsBuildingAt(Vector3 worldPosition)
    {
        GridNode node = gridManager.GetNodeFromWorldPosition(worldPosition);
        return node != null && placedBuildings.ContainsKey(node);
    }

    public BuildingInstance GetBuildingAt(Vector3 worldPosition)
    {
        GridNode node = gridManager.GetNodeFromWorldPosition(worldPosition);
        return node != null && placedBuildings.TryGetValue(node, out BuildingInstance building) ? building : null;
    }

    public void RemoveBuildingAt(Vector3 worldPosition)
    {
        BuildingInstance building = GetBuildingAt(worldPosition);
        if (building != null)
        {
            RemoveBuilding(building);
        }
    }

    public List<BuildingInstance> GetAllBuildings()
    {
        var buildings = new List<BuildingInstance>();
        var addedBuildings = new HashSet<BuildingInstance>();

        foreach (var kvp in placedBuildings)
        {
            if (!addedBuildings.Contains(kvp.Value))
            {
                buildings.Add(kvp.Value);
                addedBuildings.Add(kvp.Value);
            }
        }

        return buildings;
    }
}