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

    private void Start()
    {
        currentHealth = MaxHealth;
        InitializeBuilding();
    }

    private void InitializeBuilding()
    {
        currentHealth = maxHealth;
        FindArmyManager();
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

        OnBuildingDestroyed?.Invoke();

        Debug.Log($"{BuildingName} has been destroyed!");

        // Destroy the building GameObject if enabled
        if (destroyOnZeroHealth)
        {
            Destroy(gameObject, destroyDelay);
        }
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

    [Header("Building Dimensions")]
    [SerializeField] private Vector2Int buildingSize = new Vector2Int(4, 4); // Width x Height in grid units
    [SerializeField] private Vector2Int buildingOffset = Vector2Int.zero; // Offset from center if needed

    // Public getter for other scripts
    public Vector2Int BuildingSize => buildingSize;
    public Vector2Int BuildingOffset => buildingOffset;

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