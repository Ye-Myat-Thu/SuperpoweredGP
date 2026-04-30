using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class CharacterCombat : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private CombatProfile profile;

    [Header("Melee Hit Position")]
    [SerializeField] private Vector3 meleeOffset = new Vector3(0f, 0f, 1.5f);
    [SerializeField] private float damage = 20f;
    [SerializeField] private float meleeRadius = 1.2f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Aiming")]
    [SerializeField] private LayerMask aimLayers;      // usually ground layer
    [SerializeField] private Transform firePoint;      // where projectiles spawn (weapon tip / hand)
    [SerializeField] private float aimTurnSpeed = 18f;

    [Header("Optional")]
    [SerializeField] private Animator animator;
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pistolClip;

    private float nextAttackTime;
    private Camera cam;
    private NavMeshAgent agent;

    private float currentOverheat;
    private bool overheated;
    private float overheatReadyTime;

    public float CurrentOverheat => currentOverheat;
    public float MaxOverheat => profile ? profile.overheatMax : 100f;
    public float OverheatNormalized => profile ? currentOverheat / profile.overheatMax : 0f;
    public bool IsOverheated => overheated;

    private float bonusDamage;
    private bool isLockedInAnimation;

    public void AddBonusDamage(float amount)
    {
        bonusDamage += amount;
    }

    public void RemoveBonusDamage(float amount)
    {
        bonusDamage -= amount;
    }

    public void SetAnimationLock(bool value)
    {
        isLockedInAnimation = value;
    }

    void Awake()
    {
        cam = Camera.main;
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (!audioSource)
            audioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        if (!profile) return;

        UpdateOverheat();

        if (IsPointerOverUI())
            return;

        if (profile.attackType == AttackType.Melee)
        {
            if (Input.GetMouseButton(0))
            {
                TryAttack();
            }
        }
        else if (profile.attackType == AttackType.MagicOverheatProjectile)
        {
            // Hold left mouse
            if (Input.GetMouseButton(0))
            {
                TryAttack();
            }
        }
        else if (profile.attackType == AttackType.FastProjectile)
        {
            // Press left mouse once
            if (Input.GetMouseButtonDown(0))
            {
                TryAttack();
            }
        }

        if (isLockedInAnimation)
        {
            return;
        }
    }

    private bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
    //old TryAttack
    //private void TryAttack()
    //{
    //    float cooldown = 1f / Mathf.Max(profile.attacksPerSecond, 0.01f);
    //    if (Time.time < nextAttackTime) return;
    //    nextAttackTime = Time.time + cooldown;

    //    // Aim toward mouse (Dota-like)
    //    Vector3 aimPoint;
    //    if (TryGetAimPoint(out aimPoint))
    //    {
    //        FacePoint(aimPoint);
    //    }

    //    // Optional: stop moving briefly while attacking
    //    // if (agent) agent.ResetPath();

    //    if (animator && !string.IsNullOrEmpty(attackTrigger))
    //        animator.SetTrigger(attackTrigger);

    //    // If you want hits to sync with the animation, call AttackNow() from an Animation Event.
    //    // Otherwise, just do it instantly:
    //    AttackNow();
    //}

    private void TryAttack()
    {
        float cooldown = 1f / Mathf.Max(profile.attacksPerSecond, 0.01f);
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + cooldown;

        Vector3 aimPoint;
        if (TryGetAimPoint(out aimPoint))
        {
            //FacePoint(aimPoint);
            if (profile.attackType == AttackType.Melee)
                FacePoint(aimPoint);
            else
                FacePointInstant(aimPoint);
        }

        if (animator && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);
        else
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

            case AttackType.FastProjectile:
                DoFastProjectile();
                break;

            case AttackType.MagicOverheatProjectile:
                DoMagicOverheatProjectile();
                break;
        }
    }


    //old melee
    //private void DoMelee()
    //{
    //    // hit center in front of character
    //    Vector3 center = transform.position + transform.forward * profile.meleeRange;

    //    Collider[] hits = Physics.OverlapSphere(center, profile.meleeRadius, profile.enemyLayers);
    //    for (int i = 0; i < hits.Length; i++)
    //    {
    //        var dmg = hits[i].GetComponentInParent<IDamageable>();
    //        if (dmg != null)
    //            dmg.TakeDamage(profile.damage);
    //    }
    //}

    public void DoMelee()
    {
        Vector3 hitCenter = transform.TransformPoint(meleeOffset);

        Collider[] hits = Physics.OverlapSphere(hitCenter, profile.meleeRadius, profile.enemyLayers);

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable dmg = hits[i].GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(profile.damage + bonusDamage);
            }
        }
    }

    private void UpdateOverheat()
    {
        if (!profile) return;
        if (profile.attackType != AttackType.MagicOverheatProjectile) return;

        if (overheated)
        {
            if (Time.time >= overheatReadyTime)
            {
                overheated = false;
            }

            return;
        }

        if (!Input.GetMouseButton(0))
        {
            currentOverheat -= profile.overheatCooldownPerSecond * Time.deltaTime;
            currentOverheat = Mathf.Max(0f, currentOverheat);
        }
    }

    private void DoFastProjectile()
    {
        if (!profile.fastProjectilePrefab)
        {
            Debug.LogWarning("Fast projectile prefab missing.");
            return;
        }

        Transform spawn = firePoint ? firePoint : transform;

        Vector3 spawnPos = spawn.position + spawn.forward * profile.fastProjectileSpawnOffset;
        FastProjectile p = Instantiate(profile.fastProjectilePrefab, spawnPos, spawn.rotation);

        p.Init(
            profile.damage,
            profile.fastProjectileSpeed,
            profile.fastProjectileLifetime,
            profile.enemyLayers
        );

        if (audioSource && pistolClip != null )
            audioSource.PlayOneShot(pistolClip);
    }

    private void DoMagicOverheatProjectile()
    {
        if (overheated) return;

        if (!profile.magicProjectilePrefab)
        {
            Debug.LogWarning("Magic projectile prefab missing.");
            return;
        }

        currentOverheat += profile.overheatPerShot;

        if (currentOverheat >= profile.overheatMax)
        {
            currentOverheat = profile.overheatMax;
            overheated = true;
            overheatReadyTime = Time.time + profile.overheatLockoutTime;
            return;
        }

        Transform spawn = firePoint ? firePoint : transform;

        Vector3 spawnPos = spawn.position + spawn.forward * profile.magicSpawnOffset;
        MagicOverheatProjectile p = Instantiate(profile.magicProjectilePrefab, spawnPos, spawn.rotation);

        p.Init(
            profile.damage,
            profile.magicStartSpeed,
            profile.magicMaxSpeed,
            profile.magicAcceleration,
            profile.magicProjectileLifetime,
            profile.enemyLayers
        );
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

    private void FacePointInstant(Vector3 worldPoint)
    {
        Vector3 dir = worldPoint - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    // Debug helper: shows melee hit area in Scene view

    //old Gizmo
    //private void OnDrawGizmosSelected()
    //{
    //    if (!profile || profile.attackType != AttackType.Melee) return;
    //    Gizmos.color = Color.red;
    //    Vector3 center = transform.position + transform.forward * profile.meleeRange;
    //    Gizmos.DrawWireSphere(center, profile.meleeRadius);
    //}

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;

        Vector3 hitCenter = transform.TransformPoint(meleeOffset);
        Gizmos.DrawWireSphere(hitCenter, meleeRadius);
    }
}
