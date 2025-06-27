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

    private float lastAttackTime = 0.0f;
    private List<UnitInstance> unitsInRange = new List<UnitInstance>();
    private SphereCollider rangeCollider;

    [SerializeField] private int ArmyID;

    void Start()
    {
        // Don't setup range collider in Start - wait for placement
    }

    void Update()
    {
        // Only operate if the building has been placed and activated
        if (rangeCollider == null || !rangeCollider.enabled)
            return;

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

    // Call this method when the building is successfully placed
    public void OnBuildingPlaced()
    {
        SetupRangeCollider();
        Debug.Log("Defensive building activated and ready for combat");
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
        rangeCollider.enabled = true;

        Debug.Log($"Tower range collider activated with radius: {attackRange}");
    }

    private void OnTriggerEnter(Collider other)
    {
        UnitInstance unit = other.GetComponent<UnitInstance>();
        Debug.Log($"Collider detected: {other.name}");

        if (unit != null && !unitsInRange.Contains(unit) && unit.ArmyID != ArmyID)
        {
            unitsInRange.Add(unit);
            Debug.Log($"Unit {unit.name} entered tower range");

            // If we don't have a target, set this as our target
            if (CurrentTarget == null)
            {
                SetTarget(unit);
            }
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
            return;
        }

        // Check if enough time has passed since last attack
        if (Time.time < lastAttackTime + attackCooldown)
        {
            return;
        }

        // Verify target is still in range (in case they moved very fast)
        float distanceToTarget = Vector3.Distance(transform.position, CurrentTarget.transform.position);
        if (distanceToTarget > attackRange)
        {
            Debug.Log("Target moved out of range");
            ClearTarget();
            return;
        }

        // Attack the target
        Debug.Log($"Tower attacking {CurrentTarget.name} for {towerDamage} damage");
        CurrentTarget.TakeDamage(towerDamage);
        lastAttackTime = Time.time;

        // Check if target was destroyed by our attack
        if (CurrentTarget == null || CurrentTarget.gameObject == null)
        {
            ClearTarget();
        }
    }
}