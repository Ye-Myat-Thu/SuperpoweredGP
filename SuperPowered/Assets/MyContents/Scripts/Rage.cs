using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Rage : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseCharacter owner;
    [SerializeField] private CombatProfile profile;
    [SerializeField] private NavMeshAgent agent;

    [Header("Cast")]
    [SerializeField] private KeyCode castKey = KeyCode.Q;
    [SerializeField] private float cooldown = 10f;
    [SerializeField] private float duration = 1.5f;

    [Header("Movement Boost")]
    [SerializeField] private float boostedMoveSpeed = 10f;

    [Header("Effect")]
    [SerializeField] private float damage = 25f;
    [SerializeField] private float collisionRadius = 1.5f;
    [SerializeField] private Vector3 collisionOffset = new Vector3(0f, 0f, 1f);
    [SerializeField] private float hitCheckInterval = 0.05f;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("VFX")]
    [SerializeField] private ParticleSystem speedEffectPrefab;
    [SerializeField] private Vector3 vfxOffset = new Vector3(0f, 0.5f, 1f);
    [SerializeField] private Vector3 vfxRotationOffset = new Vector3(0f, 180f, 0f);

    public bool IsActive { get; private set; }

    private float nextCastTime;
    private float endTime;
    private float nextHitCheckTime;
    private ParticleSystem activeVFX;
    private float originalMoveSpeed;

    private readonly HashSet<Transform> hitThisCast = new HashSet<Transform>();

    private void Awake()
    {
        if (!owner) owner = GetComponent<BaseCharacter>();
        if (!agent) agent = GetComponent<NavMeshAgent>();

        if (agent != null)
            originalMoveSpeed = agent.speed;

    }

    private void Update()
    {
        if (!IsActive)
        {
            if (Input.GetKeyDown(castKey) && Time.time >= nextCastTime)
            {
                StartAbility();
            }
            return;
        }

        if (Time.time >= nextHitCheckTime)
        {
            nextHitCheckTime = Time.time + hitCheckInterval;
            CheckCollisions();
        }

        if (Time.time >= endTime)
        {
            EndAbility();
        }
        UpdateVFXTransform();
    }

    private void StartAbility()
    {
        IsActive = true;
        endTime = Time.time + duration;
        nextCastTime = Time.time + cooldown;
        nextHitCheckTime = Time.time;

        hitThisCast.Clear();

        if (owner != null)
            owner.SetInvulnerable(true);

        if (agent != null)
        {
            originalMoveSpeed = agent.speed;
            agent.speed = boostedMoveSpeed;
        }

        SpawnVFX();
    }

    private void EndAbility()
    {
        IsActive = false;

        if (owner != null)
            owner.SetInvulnerable(false);

        if (agent != null)
            agent.speed = originalMoveSpeed;

        if (activeVFX != null)
        {
            Destroy(activeVFX.gameObject);
            activeVFX = null;
        }

        hitThisCast.Clear();
    }

    private void CheckCollisions()
    {
        LayerMask hitMask = profile ? profile.enemyLayers : ~0;
        Vector3 center = transform.TransformPoint(collisionOffset);

        Collider[] hits = Physics.OverlapSphere(center, collisionRadius, hitMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Transform root = hits[i].transform.root;

            if (hitThisCast.Contains(root))
                continue;

            hitThisCast.Add(root);

            IDamageable damageable = hits[i].GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(damage);
            }

            IKnockbackable knock = hits[i].GetComponentInParent<IKnockbackable>();
            if (knock != null)
            {
                Vector3 dir = root.position - transform.position;
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.001f)
                {
                    knock.ApplyKnockback(dir.normalized, knockbackForce, knockbackDuration);
                }
            }
        }
    }

    private void SpawnVFX()
    {
        if (!speedEffectPrefab || activeVFX != null) return;

        Vector3 spawnPos = transform.TransformPoint(vfxOffset);
        Quaternion spawnRot = transform.rotation * Quaternion.Euler(vfxRotationOffset);

        activeVFX = Instantiate(speedEffectPrefab, spawnPos, spawnRot, transform);
    }

    private void UpdateVFXTransform()
    {
        if (activeVFX == null) return;

        activeVFX.transform.position = transform.TransformPoint(vfxOffset);
        activeVFX.transform.rotation = transform.rotation * Quaternion.Euler(vfxRotationOffset);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 center = transform.TransformPoint(collisionOffset);
        Gizmos.DrawWireSphere(center, collisionRadius);
    }
}

