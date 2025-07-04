using UnityEngine;
public interface IDamageable
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    bool IsAlive { get; }
    void TakeDamage(int damage, GameObject attacker = null);
    void Die();
    Vector3 GetPosition();
}