using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class BuildingHealth : MonoBehaviour, IDamageable
{
    [Header("Building Settings")]
    [SerializeField] private BuildingData buildingData;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private int defense = 0;

    [Header("Army Settings")]
    [SerializeField] private int armyID = 0;
    private CurrentTeamArmyManager armyManager;

    [Header("Events")]
    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnBuildingDestroyed;

    [Header("UI References")]
    [SerializeField] private HealthBar healthBar;
    [SerializeField] private DamageFlash damageFlash;

    [Header("Destruction Settings")]
    [SerializeField] private bool destroyOnZeroHealth = true;
    [SerializeField] private float destroyDelay = 0.5f;

    [Header("Building Dimensions")]
    [SerializeField] private Vector2Int buildingSize = new Vector2Int(4, 4); // Width x Height in grid units
    [SerializeField] private Vector2Int buildingOffset = Vector2Int.zero; // Offset from center if needed

    // Grid tracking variables - these need to be set when the building is placed
    private Vector2Int buildingOrigin;
    private Vector2Int occupiedGridNode;
    private GridManager gridManager;
    private bool isDead = false;

    // IDamageable interface properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public int Defense => defense;
    public int ArmyID => armyID;
    public CurrentTeamArmyManager ArmyManager => armyManager;

    // Additional properties
    public bool IsDestroyed => currentHealth <= 0 || isDead;
    public string BuildingName => buildingData?.BuildingName ?? "Unknown Building";

    // Public getter for other scripts
    public Vector2Int BuildingSize => buildingSize;
    public Vector2Int BuildingOffset => buildingOffset;

    private void Start()
    {
        currentHealth = MaxHealth;
        InitializeBuilding();
    }

    private void InitializeBuilding()
    {
        currentHealth = maxHealth;
        FindArmyManager();
        FindGridManager();
        InitializeHealthBar();

        Debug.Log($"Building {BuildingName} initialized - Health: {currentHealth}/{maxHealth}, Defense: {defense}, ArmyID: {armyID}");
    }

    private void FindArmyManager()
    {
        CurrentTeamArmyManager[] managers = FindObjectsOfType<CurrentTeamArmyManager>();
        foreach (var manager in managers)
        {
            if (manager.armyID == armyID)
            {
                armyManager = manager;
                break;
            }
        }

        if (armyManager == null)
        {
            Debug.LogWarning($"{BuildingName}: Could not find army manager for Army ID {armyID}");
        }
    }

    private void FindGridManager()
    {
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogWarning($"{BuildingName}: Could not find GridManager in scene");
        }
    }

    // Method to set grid occupation data when building is placed
    public void SetGridOccupationData(Vector2Int origin, Vector2Int dimensions, GridManager manager)
    {
        buildingOrigin = origin;
        occupiedGridNode = dimensions;
        gridManager = manager;

        Debug.Log($"{BuildingName} grid data set - Origin: {buildingOrigin}, Dimensions: {occupiedGridNode}");
    }

    // IDamageable interface methods
    public void TakeDamage(int damage, GameObject attacker = null)
    {
        if (IsDestroyed) return;

        // Calculate actual damage after defense
        int actualDamage = Mathf.Max(1, damage - defense);
        int oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - actualDamage);

        damageFlash.Flash();
        UpdateHealthBar();
        OnHealthChanged?.Invoke(currentHealth);

        string attackerName = attacker != null ? attacker.name : "unknown";
        //Debug.Log($"{BuildingName} took {actualDamage} damage from {attackerName}. Health: {currentHealth}/{maxHealth}");

        // Check if building was destroyed
        if (currentHealth <= 0 && oldHealth > 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isDead) return;

        isDead = true;

        // Remove from army manager if assigned
        if (armyManager != null)
        {
            // Remove from any building lists the army manager might have
            Debug.Log($"{BuildingName} removed from Army {armyID}");
        }

        // Clear grid occupation 
        ClearGridOccupation();

        OnBuildingDestroyed?.Invoke();

        Debug.Log($"{BuildingName} has been destroyed!");

        // Destroy the building GameObject if enabled
        if (destroyOnZeroHealth)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    private void ClearGridOccupation()
    {
        if (gridManager == null)
        {
            Debug.LogWarning($"{BuildingName}: Cannot clear grid occupation - GridManager is null");
            return;
        }

        // If we don't have the grid data set, try to calculate it from current position
        if (occupiedGridNode == Vector2Int.zero)
        {
            // Fall back to using buildingSize and calculating origin from current position
            Vector2Int currentGridPos = gridManager.GetGridPositionFromWorld(transform.position);
            buildingOrigin = new Vector2Int(
                currentGridPos.x - buildingSize.x / 2,
                currentGridPos.y - buildingSize.y / 2
            );
            occupiedGridNode = buildingSize;

            Debug.LogWarning($"{BuildingName}: Grid data not set, using fallback calculation. Origin: {buildingOrigin}, Size: {occupiedGridNode}");
        }

        // Update grid occupancy using the stored dimensions
        for (int x = 0; x < occupiedGridNode.x; x++)
        {
            for (int y = 0; y < occupiedGridNode.y; y++)
            {
                GridNode node = gridManager.GetNode(buildingOrigin.x + x, buildingOrigin.y + y);
                if (node != null)
                {
                    node.walkable = true;
                    node.IsOccupied = false;
                    Debug.Log($"Freed grid square: ({buildingOrigin.x + x}, {buildingOrigin.y + y})");
                }
                else
                {
                    Debug.LogWarning($"Could not find grid node at ({buildingOrigin.x + x}, {buildingOrigin.y + y})");
                }
            }
        }

        Debug.Log($"{BuildingName}: Successfully cleared {occupiedGridNode.x}x{occupiedGridNode.y} grid area starting at {buildingOrigin}");
    }

    public Vector3 GetPosition()
    {
        return transform.position;
    }

    private void InitializeHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
        }
    }

    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
    }

    // Method to get the radius (distance from center to edge)
    public int GetBuildingRadius()
    {
        return Mathf.Max(
            Mathf.FloorToInt(buildingSize.x / 2f),
            Mathf.FloorToInt(buildingSize.y / 2f)
        );
    }

    // Method to check if a grid position is occupied by this building
    public bool OccupiesGridPosition(Vector2Int gridPos, Vector2Int buildingCenter)
    {
        Vector2Int halfSize = new Vector2Int(
            Mathf.FloorToInt(buildingSize.x / 2f),
            Mathf.FloorToInt(buildingSize.y / 2f)
        );

        return gridPos.x >= buildingCenter.x - halfSize.x &&
               gridPos.x <= buildingCenter.x + halfSize.x &&
               gridPos.y >= buildingCenter.y - halfSize.y &&
               gridPos.y <= buildingCenter.y + halfSize.y;
    }
}