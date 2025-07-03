using UnityEngine;
using UnityEngine.Pool;

public class ShootOverTime : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField] private ArcingProjectile ProjectilePrefab;
    [SerializeField] private Transform ProjectileStartPoint;

    // Replace this with some kind of targeted enemy position
    [SerializeField] private Transform DebugEndPoint;

    [Header("Flight Settings")]
    [SerializeField][Min(0.1f)] private float ProjectileSpeed = 5f; // units / second
    [SerializeField] private float ArcHeight = 2f; // world units

    public int maxPoolSize = 1000; // world units
    public bool collectionChecks = true;

    [SerializeField] private ObjectPool<ArcingProjectile> _projectilePool;

    /// <summary>
    ///     Fires the projectile
    /// </summary>
    public void Shoot()
    {
        // Get projectile from pool instead of instantiating
        var proj = _projectilePool.Get();

        // Set position
        proj.transform.position = ProjectileStartPoint != null ? ProjectileStartPoint.position : transform.position;
        proj.transform.rotation = Quaternion.identity;

        proj.Launch(transform.position, DebugEndPoint.position, ProjectileSpeed, ArcHeight);
    }
    private void Start()
    {
        // Create the object pool with proper delegates
        _projectilePool = new ObjectPool<ArcingProjectile>(
            createFunc: CreateProjectile,
            actionOnGet: OnGetFromPool,
            actionOnRelease: OnReturnToPool,
            actionOnDestroy: OnDestroyProjectile,
            collectionCheck: collectionChecks,
            defaultCapacity: 10,
            maxSize: maxPoolSize
        );
    }

    // Pool delegate methods
    private ArcingProjectile CreateProjectile()
    {
        var projectile = Instantiate(ProjectilePrefab);
        return projectile;
    }

    private void OnGetFromPool(ArcingProjectile projectile)
    {
        projectile.gameObject.SetActive(true);
    }

    private void OnReturnToPool(ArcingProjectile projectile)
    {
        projectile.gameObject.SetActive(false);
    }

    private void OnDestroyProjectile(ArcingProjectile projectile)
    {
        Destroy(projectile.gameObject);
    }

    private void ReturnProjectileToPool(ArcingProjectile projectile)
    {
        _projectilePool.Release(projectile);
    }
}