using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class SimpleAutoAttack : MonoBehaviour
{
    [Header("Auto Attack Settings")]
    [SerializeField] private float checkInterval = 0.5f; // How often to check for enemies

    private UnitInstance unitInstance;
    private Coroutine autoAttackCoroutine;

    void Start()
    {
        unitInstance = GetComponent<UnitInstance>();
        if (unitInstance == null)
        {
            Debug.LogError($"{name}: SimpleAutoAttack requires UnitInstance component!");
            enabled = false;
            return;
        }

        // Start checking for enemies
        autoAttackCoroutine = StartCoroutine(AutoAttackLoop());
    }

    private IEnumerator AutoAttackLoop()
    {
        while (unitInstance != null && unitInstance.IsAlive)
        {
            // Only auto-attack if we don't already have a target
            if (unitInstance.AttackTarget == null)
            {
                CheckForEnemiesInRange();
            }

            yield return new WaitForSeconds(checkInterval);
        }
    }

    private void CheckForEnemiesInRange()
    {
        if (!unitInstance.IsAlive) return;

        // Get all colliders within attack range
        Collider[] nearbyColliders = Physics.OverlapSphere(transform.position, unitInstance.AttackRange);

        IDamageable nearestEnemy = null;
        float nearestDistance = float.MaxValue;

        foreach (var collider in nearbyColliders)
        {
            // Try to get an IDamageable component
            IDamageable target = collider.GetComponent<IDamageable>();
            if (target == null)
            {
                target = collider.GetComponentInParent<IDamageable>();
            }

            if (target == null || !target.IsAlive) continue;

            // Check if it's an enemy
            if (IsEnemy(target))
            {
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestEnemy = target;
                }
            }
        }

        // If we found an enemy, attack it
        if (nearestEnemy != null)
        {
            unitInstance.SetAttackTarget(nearestEnemy);
            Debug.Log($"{name}: Auto-attacking {GetTargetName(nearestEnemy)} in range");
        }
    }

    private bool IsEnemy(IDamageable target)
    {
        // Check if target is an enemy unit
        if (target is UnitInstance enemyUnit)
        {
            return enemyUnit.ArmyID != unitInstance.ArmyID;
        }

        // Check if target is an enemy building
        if (target is BuildingHealth enemyBuilding)
        {
            return enemyBuilding.ArmyID != unitInstance.ArmyID;
        }

        // If we can't determine army, assume it's an enemy
        return true;
    }

    private string GetTargetName(IDamageable target)
    {
        if (target is MonoBehaviour monoBehaviour)
        {
            return monoBehaviour.name;
        }
        else if (target is Component component)
        {
            return component.name;
        }
        return "Unknown Target";
    }

    void OnDestroy()
    {
        if (autoAttackCoroutine != null)
        {
            StopCoroutine(autoAttackCoroutine);
        }
    }

    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (unitInstance != null)
        {
            // Draw attack range
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, unitInstance.AttackRange);
        }
    }
}