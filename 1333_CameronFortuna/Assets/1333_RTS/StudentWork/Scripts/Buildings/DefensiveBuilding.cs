using System.Collections.Generic;
using UnityEngine;

public class DefensiveBuilding : MonoBehaviour
{
    [Header("Combat Settings")]
    public int towerDamage = 10;
    public float attackCooldown = 3.0f;
    public float attackRange = 10.0f;

    [Header("Debug Info")]
    public UnitInstance CurrentTarget;

    [Header("Army Settings")]
    [SerializeField] private int ArmyID = 0; // set in inspector to determine team

    private float lastAttackTime = 0.0f;
    private List<UnitInstance> unitsInRange = new List<UnitInstance>();
    private SphereCollider rangeCollider;
    private bool isActive = false;

    //called on start, sets tower layer
    void Start()
    {
        gameObject.layer = LayerMask.NameToLayer("DefensiveBuildings");
        Debug.Log($"Tower {gameObject.name} set to layer: {gameObject.layer}");
    }

    //main update loop for checking targets and attacking
    void Update()
    {
        if (!isActive) return;

        unitsInRange.RemoveAll(unit => unit == null);

        if (CurrentTarget == null)
        {
            FindNearestTarget();
        }

        if (CurrentTarget != null)
        {
            AttackCurrentTarget();
        }
    }

    //called when the building is placed in the world
    public void OnBuildingPlaced()
    {
        isActive = true;
        SetupRangeCollider();
        Debug.Log($"Defensive building {gameObject.name} has been placed and activated");
        Debug.Log($"Tower ArmyID: {ArmyID}, Attack Range: {attackRange}, Damage: {towerDamage}");
    }

    //sets up the trigger collider for detecting enemies
    public void SetupRangeCollider()
    {
        rangeCollider = GetComponent<SphereCollider>();
        if (rangeCollider == null)
        {
            rangeCollider = gameObject.AddComponent<SphereCollider>();
        }

        rangeCollider.isTrigger = true;
        rangeCollider.radius = attackRange;
        Debug.Log($"Tower range collider set up with radius: {attackRange}");
    }

    //called when a unit enters tower range
    public void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        Debug.Log($"Trigger Enter detected: {other.name} on layer {other.gameObject.layer}");

        UnitInstance unit = other.GetComponent<UnitInstance>();

        if (unit != null)
        {
            Debug.Log($"Unit found: {unit.name}, Unit ArmyID: {unit.ArmyID}, Tower ArmyID: {ArmyID}");

            if (!unitsInRange.Contains(unit) && unit.ArmyID != ArmyID)
            {
                unitsInRange.Add(unit);
                Debug.Log($"Unit {unit.name} entered tower range (Enemy unit detected!)");

                if (CurrentTarget == null)
                {
                    SetTarget(unit);
                }
            }
            else if (unit.ArmyID == ArmyID)
            {
                Debug.Log($"Unit {unit.name} is friendly (same ArmyID), ignoring");
            }
        }
        else
        {
            Debug.Log($"No UnitInstance component found on {other.name}");
        }
    }

    //called when a unit leaves tower range
    public void OnTriggerExit(Collider other)
    {
        UnitInstance unit = other.GetComponent<UnitInstance>();
        if (unit != null && unitsInRange.Contains(unit))
        {
            unitsInRange.Remove(unit);
            Debug.Log($"Unit {unit.name} left tower range");

            if (CurrentTarget == unit)
            {
                ClearTarget();
            }
        }
    }

    //sets the current target for the tower
    public void SetTarget(UnitInstance unit)
    {
        CurrentTarget = unit;
        Debug.Log($"Tower targeting: {unit.name}");
    }

    //clears the current target
    public void ClearTarget()
    {
        if (CurrentTarget != null)
        {
            Debug.Log($"Tower lost target: {CurrentTarget.name}");
        }
        CurrentTarget = null;
    }

    //finds the nearest enemy unit to target
    public void FindNearestTarget()
    {
        if (unitsInRange.Count == 0) return;

        UnitInstance nearestUnit = null;
        float nearestDistance = float.MaxValue;

        foreach (UnitInstance unit in unitsInRange)
        {
            if (unit == null) continue;

            float distance = Vector3.Distance(transform.position, unit.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestUnit = unit;
            }
        }

        if (nearestUnit != null)
        {
            SetTarget(nearestUnit);
        }
    }

    //handles attacking the current target
    public void AttackCurrentTarget()
    {
        // Check if enough time has passed since last attack
        if (Time.time < lastAttackTime + attackCooldown)
        {
            return; // Still on cooldown
        }

        // Verify target is still in range
        float distanceToTarget = Vector3.Distance(transform.position, CurrentTarget.transform.position);
        if (distanceToTarget > attackRange)
        {
            Debug.Log($"Target {CurrentTarget.name} moved out of range (distance: {distanceToTarget})");
            ClearTarget();
            return;
        }

        // Attack the target
        Debug.Log($"Tower attacking {CurrentTarget.name} for {towerDamage} damage!");

        // Check if the target has a TakeDamage method
        if (CurrentTarget.GetComponent<UnitInstance>() != null)
        {
            CurrentTarget.TakeDamage(towerDamage);
            lastAttackTime = Time.time;
        }
        else
        {
            Debug.LogError($"Target {CurrentTarget.name} doesn't have a valid TakeDamage method!");
        }

        // Check if target was destroyed by our attack
        if (CurrentTarget == null || CurrentTarget.gameObject == null)
        {
            Debug.Log("Target destroyed by attack");
            ClearTarget();
        }
    }
}

