using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Projectile Settings")]
    [SerializeField] private float speed = 10f;
    [SerializeField] private float maxLifetime = 5f;
    [SerializeField] private bool trackTarget = true;
    [SerializeField] private float explosionRadius = 0f; // 0 = no splash damage

    [Header("Visual Effects")]
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private TrailRenderer trail;

    private Transform target;
    private int damage;
    private GameObject shooter;
    private Vector3 targetPosition;
    private float lifetime = 0f;
    private bool hasHit = false;

    public void Initialize(Transform targetTransform, int damageAmount, GameObject shooterObject)
    {
        target = targetTransform;
        damage = damageAmount;
        shooter = shooterObject;

        if (target != null)
        {
            targetPosition = target.position;

            // Aim at target center
            Vector3 direction = (targetPosition - transform.position).normalized;
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    private void Update()
    {
        if (hasHit) return;

        lifetime += Time.deltaTime;

        // Destroy if lifetime exceeded
        if (lifetime >= maxLifetime)
        {
            DestroyProjectile();
            return;
        }

        MoveProjectile();
    }

    private void MoveProjectile()
    {
        Vector3 moveDirection;

        if (trackTarget && target != null)
        {
            // Update target position if tracking
            targetPosition = target.position + Vector3.up * 0.5f; // Aim for center of unit
            moveDirection = (targetPosition - transform.position).normalized;

            // Rotate to face target
            if (moveDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }
        }
        else
        {
            // Move in straight line to last known position
            moveDirection = (targetPosition - transform.position).normalized;
        }

        // Move the projectile
        float moveDistance = speed * Time.deltaTime;
        transform.position += moveDirection * moveDistance;

        // Check if we've reached the target
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
        if (distanceToTarget < 0.5f)
        {
            HitTarget();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Don't hit the shooter
        if (other.gameObject == shooter) return;

        // Check if this is a valid target
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            HitTarget(other.gameObject);
        }
    }

    private void HitTarget(GameObject hitObject = null)
    {
        if (hasHit) return;
        hasHit = true;

        // Deal damage
        if (explosionRadius > 0)
        {
            // Splash damage
            DealSplashDamage();
        }
        else
        {
            // Single target damage
            if (hitObject != null)
            {
                IDamageable damageable = hitObject.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage, shooter);
                }
            }
            else if (target != null)
            {
                IDamageable damageable = target.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(damage, shooter);
                }
            }
        }

        // Visual effects
        SpawnHitEffect();

        // Destroy projectile
        DestroyProjectile();
    }

    private void DealSplashDamage()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider hitCollider in hitColliders)
        {
            IDamageable damageable = hitCollider.GetComponent<IDamageable>();
            if (damageable != null && hitCollider.gameObject != shooter)
            {
                // Calculate damage based on distance (optional)
                float distance = Vector3.Distance(transform.position, hitCollider.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                int actualDamage = Mathf.RoundToInt(damage * damageMultiplier);

                damageable.TakeDamage(actualDamage, shooter);
            }
        }

        // Spawn explosion effect
        if (explosionEffect != null)
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
        }
    }

    private void SpawnHitEffect()
    {
        if (hitEffect != null)
        {
            GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
            Destroy(effect, 2f); // Clean up effect after 2 seconds
        }
    }

    private void DestroyProjectile()
    {
        // Disable trail if it exists
        if (trail != null)
        {
            trail.enabled = false;
        }

        Destroy(gameObject, 0.1f); // Small delay to let effects play
    }

    private void OnDrawGizmosSelected()
    {
        // Show explosion radius if applicable
        if (explosionRadius > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        // Show target line
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
        }
    }
}