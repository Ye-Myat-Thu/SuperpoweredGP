using UnityEngine;
using UnityEngine.AI;

public class CharacterCombat : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private CombatProfile profile;

    [Header("Aiming")]
    [SerializeField] private LayerMask aimLayers;      // usually ground layer
    [SerializeField] private Transform firePoint;      // where projectiles spawn (weapon tip / hand)
    [SerializeField] private float aimTurnSpeed = 18f;

    [Header("Optional")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTrigger = "Attack";

    private float nextAttackTime;
    private Camera cam;
    private NavMeshAgent agent;

    void Awake()
    {
        cam = Camera.main;
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!profile) return;

        // Input Attack: Left mouse to attack (change to whatever you want)
        if (profile.attackType == AttackType.Melee)
        {
            if (Input.GetMouseButton(0))
            {
                TryAttack();
            }
        }
        else
        {
            TryAttack();
        }

            
    }

    private void TryAttack()
    {
        float cooldown = 1f / Mathf.Max(profile.attacksPerSecond, 0.01f);
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + cooldown;

        // Aim toward mouse (Dota-like)
        Vector3 aimPoint;
        if (TryGetAimPoint(out aimPoint))
        {
            FacePoint(aimPoint);
        }

        // Optional: stop moving briefly while attacking
        // if (agent) agent.ResetPath();

        if (animator && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);

        // If you want hits to sync with the animation, call AttackNow() from an Animation Event.
        // Otherwise, just do it instantly:
        AttackNow();
    }

    // Call this from an animation event for nicer timing (Titan melee especially)
    public void AttackNow()
    {
        switch (profile.attackType)
        {
            case AttackType.Melee:
                DoMelee();
                break;

            case AttackType.Projectile:
                DoProjectile();
                break;

            case AttackType.Hitscan:
                DoHitscan();
                break;
        }
    }

    private void DoMelee()
    {
        // hit center in front of character
        Vector3 center = transform.position + transform.forward * profile.meleeRange;

        Collider[] hits = Physics.OverlapSphere(center, profile.meleeRadius, profile.enemyLayers);
        for (int i = 0; i < hits.Length; i++)
        {
            var dmg = hits[i].GetComponentInParent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(profile.damage);
        }
    }

    private void DoProjectile()
    {
        if (!profile.projectilePrefab)
        {
            Debug.LogWarning("Projectile attack type but no projectilePrefab assigned.");
            return;
        }

        Transform spawn = firePoint ? firePoint : transform;

        Vector3 spawnPos = spawn.position + spawn.forward * profile.projectileSpawnOffset;
        Projectile p = Instantiate(profile.projectilePrefab, spawnPos, spawn.rotation);

        p.Init(profile.damage, profile.projectileSpeed, profile.projectileLifetime, profile.enemyLayers);
    }

    private void DoHitscan()
    {
        Transform origin = firePoint ? firePoint : transform;

        Ray ray = new Ray(origin.position, origin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, profile.hitscanRange, profile.enemyLayers))
        {
            var dmg = hit.collider.GetComponentInParent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(profile.damage);
        }
    }

    private bool TryGetAimPoint(out Vector3 point)
    {
        point = transform.position + transform.forward;

        if (!cam) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 500f, aimLayers))
        {
            point = hit.point;
            return true;
        }

        return false;
    }

    private void FacePoint(Vector3 worldPoint)
    {
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * aimTurnSpeed);
    }

    // Debug helper: shows melee hit area in Scene view
    private void OnDrawGizmosSelected()
    {
        if (!profile || profile.attackType != AttackType.Melee) return;
        Gizmos.color = Color.red;
        Vector3 center = transform.position + transform.forward * profile.meleeRange;
        Gizmos.DrawWireSphere(center, profile.meleeRadius);
    }
}
