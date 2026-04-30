using UnityEngine;
using UnityEngine.ProBuilder;

public enum AttackType
{
    Melee,
    MagicOverheatProjectile,
    FastProjectile
}

[CreateAssetMenu(menuName = "Superpowered/Combat Profile", fileName = "NewCombatProfile")]
public class CombatProfile : ScriptableObject
{
    [Header("Attack Type")]
    public AttackType attackType;

    [Header("Shared")]
    public float damage = 20f;
    public float attacksPerSecond = 1f;
    public LayerMask enemyLayers;

    [Header("Melee")]
    public float meleeRadius = 1.2f;

    [Header("Fast Projectile")]
    public FastProjectile fastProjectilePrefab;
    public float fastProjectileSpeed = 30f;
    public float fastProjectileLifetime = 3f;
    public float fastProjectileSpawnOffset = 1f;

    [Header("Magic Overheat Projectile")]
    public MagicOverheatProjectile magicProjectilePrefab;
    public float magicStartSpeed = 5f;
    public float magicMaxSpeed = 40f;
    public float magicAcceleration = 10f;
    public float magicProjectileLifetime = 4f;
    public float magicSpawnOffset = 1f;

    [Header("Overheat")]
    public float overheatMax = 100f;
    public float overheatPerShot = 8f;
    public float overheatCooldownPerSecond = 25f;
    public float overheatLockoutTime = 1.5f;

    //[Header("Core")]
    //public AttackType attackType = AttackType.Melee;
    //public float damage = 10f;
    //public float attacksPerSecond = 1.0f;

    //[Header("Melee")]
    //public float meleeRange = 2.0f;
    //public float meleeRadius = 1.0f;
    //public LayerMask enemyLayers;

    //[Header("Projectile")]
    //public Projectile projectilePrefab;
    //public float projectileSpeed = 18f;
    //public float projectileLifetime = 3f;
    //public float projectileSpawnOffset = 0.1f; //small forward offset to avoid self-collisions

    //[Header("Hitscan")]
    //public float hitscanRange = 30f;
}
