using UnityEngine;
using UnityEngine.ProBuilder;

public enum AttackType
{
    Melee,
    Projectile,
    Hitscan //(instant ray shot)
}

[CreateAssetMenu(menuName = "Superpowered/Combat Profile", fileName = "NewCombatProfile")]
public class CombatProfile : ScriptableObject
{
    [Header("Core")]
    public AttackType attackType = AttackType.Melee;
    public float damage = 10f;
    public float attacksPerSecond = 1.0f;

    [Header("Melee")]
    public float meleeRange = 2.0f;
    public float meleeRadius = 1.0f;
    public LayerMask enemyLayers;

    [Header("Projectile")]
    public Projectile projectilePrefab;
    public float projectileSpeed = 18f;
    public float projectileLifetime = 3f;
    public float projectileSpawnOffset = 0.1f; //small forward offset to avoid self-collisions

    [Header("Hitscan")]
    public float hitscanRange = 30f;
}
