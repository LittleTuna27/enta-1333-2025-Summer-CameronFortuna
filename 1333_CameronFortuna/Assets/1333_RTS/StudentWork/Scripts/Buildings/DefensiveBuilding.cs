using System.Collections;
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
    [SerializeField] private int ArmyID = 0; // Make sure this is set correctly in inspector

    private float lastAttackTime = 0.0f;
    private List<UnitInstance> unitsInRange = new List<UnitInstance>();
    private SphereCollider rangeCollider;
    private bool isActive = false;

    void Start()
    {
        // Set this building to the DefensiveBuildings layer
        gameObject.layer = LayerMask.NameToLayer("DefensiveBuildings");
        Debug.Log($"Tower {gameObject.name} set to layer: {gameObject.layer}");
    }

    void Update()
    {
        // Only operate if the building is active
        if (!isActive) return;

        // Clean up null references in unitsInRange
        unitsInRange.RemoveAll(unit => unit == null);

        // Find a target if we don't have one
        if (CurrentTarget == null)
        {
            FindNearestTarget();
        }

        // Attack current target if we have one
        if (CurrentTarget != null)
        {
            AttackCurrentTarget();
        }
    }

    public void OnBuildingPlaced()
    {
        isActive = true;
        SetupRangeCollider();
        Debug.Log($"Defensive building {gameObject.name} has been placed and activated");
        Debug.Log($"Tower ArmyID: {ArmyID}, Attack Range: {attackRange}, Damage: {towerDamage}");
    }

    private void SetupRangeCollider()
    {
        // Create or get the range collider
        rangeCollider = GetComponent<SphereCollider>();
        if (rangeCollider == null)
        {
            rangeCollider = gameObject.AddComponent<SphereCollider>();
        }

        // Configure as trigger with specified range
        rangeCollider.isTrigger = true;
        rangeCollider.radius = attackRange;

        Debug.Log($"Tower range collider set up with radius: {attackRange}");
    }

    private void OnTriggerEnter(Collider other)
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

                // If we don't have a target, set this as our target
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

    private void OnTriggerExit(Collider other)
    {
        UnitInstance unit = other.GetComponent<UnitInstance>();
        if (unit != null && unitsInRange.Contains(unit))
        {
            unitsInRange.Remove(unit);
            Debug.Log($"Unit {unit.name} left tower range");

            // If this was our current target, clear it
            if (CurrentTarget == unit)
            {
                ClearTarget();
            }
        }
    }

    private void SetTarget(UnitInstance unit)
    {
        CurrentTarget = unit;
        Debug.Log($"Tower targeting: {unit.name}");
    }

    private void ClearTarget()
    {
        if (CurrentTarget != null)
        {
            Debug.Log($"Tower lost target: {CurrentTarget.name}");
        }
        CurrentTarget = null;
    }

    private void FindNearestTarget()
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

    private void AttackCurrentTarget()
    {
        // Check if target still exists
        if (CurrentTarget == null)
        {
            Debug.Log("No current target");
            return;
        }

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

    // Debug method to manually test attacking
    [ContextMenu("Force Attack Nearest")]
    public void ForceAttackNearest()
    {
        if (unitsInRange.Count > 0)
        {
            SetTarget(unitsInRange[0]);
            lastAttackTime = 0; // Reset cooldown
            AttackCurrentTarget();
        }
        else
        {
            Debug.Log("No units in range to attack");
        }
    }

    // Debug visualization
    private void OnDrawGizmosSelected()
    {
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw line to current target
        if (CurrentTarget != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, CurrentTarget.transform.position);
        }
    }
}