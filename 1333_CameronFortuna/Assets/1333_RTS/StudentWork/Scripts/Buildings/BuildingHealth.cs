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
    [SerializeField] private Vector2Int buildingSize = new Vector2Int(4, 4); // width x height in grid units
    [SerializeField] private Vector2Int buildingOffset = Vector2Int.zero; // offset from center if needed

    //grid tracking variables - these need to be set when the building is placed
    private Vector2Int buildingOrigin;
    private Vector2Int occupiedGridNode;
    private GridManager gridManager;
    private bool isDead = false;

    //idamageable interface properties
    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;
    public bool IsAlive => currentHealth > 0;
    public int Defense => defense;
    public int ArmyID => armyID;
    public CurrentTeamArmyManager ArmyManager => armyManager;

    //additional properties
    public bool IsDestroyed => currentHealth <= 0 || isDead;
    public string BuildingName => buildingData?.BuildingName ?? "Unknown Building";

    //public getter for other scripts
    public Vector2Int BuildingSize => buildingSize;
    public Vector2Int BuildingOffset => buildingOffset;

    //called when the building is first created
    private void Start()
    {
        currentHealth = MaxHealth;
        InitializeBuilding();
    }

    //sets up the building's health, army manager, grid manager, and health bar
    private void InitializeBuilding()
    {
        currentHealth = maxHealth;
        FindArmyManager();
        FindGridManager();
        InitializeHealthBar();

        Debug.Log($"Building {BuildingName} initialized - Health: {currentHealth}/{maxHealth}, Defense: {defense}, ArmyID: {armyID}");
    }

    //finds the army manager for this building's army id
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

    //finds the grid manager in the scene
    private void FindGridManager()
    {
        gridManager = FindObjectOfType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogWarning($"{BuildingName}: Could not find GridManager in scene");
        }
    }

    //sets grid occupation data when building is placed
    public void SetGridOccupationData(Vector2Int origin, Vector2Int dimensions, GridManager manager)
    {
        buildingOrigin = origin;
        occupiedGridNode = dimensions;
        gridManager = manager;

        Debug.Log($"{BuildingName} grid data set - Origin: {buildingOrigin}, Dimensions: {occupiedGridNode}");
    }

    //applies damage to the building and checks if it is destroyed
    public void TakeDamage(int damage, GameObject attacker = null)
    {
        if (IsDestroyed) return;

        int actualDamage = Mathf.Max(1, damage - defense);
        int oldHealth = currentHealth;
        currentHealth = Mathf.Max(0, currentHealth - actualDamage);

        damageFlash.Flash();
        UpdateHealthBar();
        OnHealthChanged?.Invoke(currentHealth);

        string attackerName = attacker != null ? attacker.name : "unknown";

        if (currentHealth <= 0 && oldHealth > 0)
        {
            Die();
        }
    }

    //handles the building's destruction logic
    public void Die()
    {
        if (isDead) return;

        isDead = true;

        if (armyManager != null)
        {
            Debug.Log($"{BuildingName} removed from Army {armyID}");
        }

        ClearGridOccupation();
        OnBuildingDestroyed?.Invoke();

        Debug.Log($"{BuildingName} has been destroyed!");

        if (destroyOnZeroHealth)
        {
            Destroy(gameObject, destroyDelay);
        }
    }

    //clears the grid nodes occupied by this building
    private void ClearGridOccupation()
    {
        if (gridManager == null)
        {
            Debug.LogWarning($"{BuildingName}: Cannot clear grid occupation - GridManager is null");
            return;
        }

        if (occupiedGridNode == Vector2Int.zero)
        {
            Vector2Int currentGridPos = gridManager.GetGridPositionFromWorld(transform.position);
            buildingOrigin = new Vector2Int(
                currentGridPos.x - buildingSize.x / 2,
                currentGridPos.y - buildingSize.y / 2
            );
            occupiedGridNode = buildingSize;

            Debug.LogWarning($"{BuildingName}: Grid data not set, using fallback calculation. Origin: {buildingOrigin}, Size: {occupiedGridNode}");
        }

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

    //returns the world position of this building
    public Vector3 GetPosition()
    {
        return transform.position;
    }

    //sets up the building's health bar
    private void InitializeHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.SetMaxHealth(maxHealth);
        }
    }

    //updates the building's health bar with the current health
    private void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            healthBar.SetHealth(currentHealth);
        }
    }

    //returns the radius of the building in grid units
    public int GetBuildingRadius()
    {
        return Mathf.Max(
            Mathf.FloorToInt(buildingSize.x / 2f),
            Mathf.FloorToInt(buildingSize.y / 2f)
        );
    }

    //checks if the building occupies a specific grid position
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
