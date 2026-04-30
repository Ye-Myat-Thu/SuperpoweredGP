using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public enum EnemyUnitType
{
    Minion,
    Warrior,
    Boss
}

public enum EnemyAttackType
{
    Melee,
    Ranged
}

[System.Serializable]
public class EnemyAudioSet
{
    public AudioClip[] gruntClips;
    public AudioClip[] deathClips;
}

public class BaseEnemy : MonoBehaviour, IDamageable, IKnockbackable, ISlowable
{
    public enum State { Chasing, GettingHit, Stunned, Dead }

    [Header("Enemy Type")]
    [SerializeField] private EnemyUnitType unitType = EnemyUnitType.Minion;
    public EnemyUnitType UnitType => unitType;

    [Header("Targeting")]
    [SerializeField] private float retargetInterval = 0.25f;

    private float nextRetargetTime;

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("NavMesh")]
    [SerializeField] private float repathInterval = 0.1f;
    [SerializeField] private float chaseStopDistance = 1.5f;
    [SerializeField] private bool rotateWithAgent = true;

    [Header("XP Drop")]
    [SerializeField] private GameObject xpDropPrefab;
    [SerializeField] private Vector3 xpDropOffset = Vector3.up * 0.2f;

    [Header("Combat")]
    [SerializeField] private EnemyAttackType attackType = EnemyAttackType.Melee;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private int damage = 10;

    [Header("Ranged")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 12f;

    [Header("Ranged Behaviour")]
    [SerializeField] private bool useAnimationEventForAttack = true;
    [SerializeField] private float faceTargetSpeed = 10f;

    [Header("Rouge Resurrection")]
    [SerializeField] private bool canResurrect;
    [SerializeField] private float resurrectHealthPercent = 0.5f;
    [SerializeField] private float fakeDeathDuration = 2f;
    [SerializeField] private float resurrectDuration = 2.7f;
    [SerializeField] private string fakeDeathTrigger = "FakeDeath";
    [SerializeField] private string resurrectTrigger = "Resurrect";

    [Header("Slow")]
    [SerializeField] private bool canBeSlowed = true;
    [SerializeField] private Color slowTint = Color.cyan;

    private Renderer[] slowRenderers;
    private List<Color> originalColors = new List<Color>();

    private Coroutine slowRoutine;
    private float originalAgentSpeed;

    private bool hasResurrected;
    private bool isResurrecting;

    [Header("Knockback")]
    [SerializeField] private float knockbackResistance = 0f;
    private Coroutine knockbackRoutine;

    [Header("Hit Reaction")]
    [SerializeField] private float hitRecoverTime = 0.35f; // how long enemy is interrupted after taking damage

    [Header("Blink on hit")]
    [SerializeField] private Renderer[] blinkRenderers;
    [SerializeField] private int blinkCount = 3;
    [SerializeField] private float blinkInterval = 0.06f;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string rangedAttackTrigger = "ThrowRanged";
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string dieTrigger = "Die";

    [Header("Capsule Collider")]
    [SerializeField] private CapsuleCollider capsuleCollider;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [SerializeField] private EnemyAudioSet minionAudio;
    [SerializeField] private EnemyAudioSet warriorAudio;
    [SerializeField] private EnemyAudioSet bossAudio;

    [SerializeField, Range(0f, 1f)] private float gruntChance = 0.5f;
    [SerializeField, Range(0f, 1f)] private float deathChance = 1f;

    [SerializeField] private Vector2 pitchRange = new Vector2(0.9f, 1.1f);

    [Header("Misc Components")]
    [SerializeField] private List<Component> componentsToDisable = new();
    private bool isDead;
    public bool IsDead => isDead;

    public State currentState { get; private set; } = State.Chasing;

    private NavMeshAgent agent;
    private float nextRepathTime;
    private float nextAttackTime;
    private Coroutine blinkRoutine;
    private Coroutine hitRoutine;

    public event Action<BaseEnemy> OnDied;

    //public EnemyUnitType UnitType => unitType;
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public int AttackDamage => damage;
    public float AttackRange => attackRange;
    public float AttackCooldown => attackCooldown;

    public event Action<float, float> OnHealthChanged;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (!capsuleCollider)
            capsuleCollider = GetComponent<CapsuleCollider>();

        if (blinkRenderers == null || blinkRenderers.Length == 0)
        {
            blinkRenderers = GetComponentsInChildren<Renderer>(true);
        }

        slowRenderers = GetComponentsInChildren<Renderer>(true);

        currentHealth = maxHealth;

        if (agent)
        {
            originalAgentSpeed = agent.speed;
            agent.stoppingDistance = chaseStopDistance;
            agent.updateRotation = rotateWithAgent;
        }

        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    protected virtual void Start()
    {
        if (!target)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj) target = playerObj.transform;
        }
    }

    protected virtual void Update()
    {
        // Old
        //if (currentState == State.Dead) return;
        //if (!target || !agent) return;

        //if (currentState == State.Chasing)
        //{
        //    ChaseTarget();
        //    TryAttack();
        //}

        //UpdateAnim();

        if (isResurrecting) return;

        if (currentState == State.Dead) return;
        if (!agent) return;

        if (Time.time >= nextRetargetTime)
        {
            nextRetargetTime = Time.time + retargetInterval;
            RefreshTarget();
        }

        if (!target) return;

        if (currentState == State.Chasing)
        {
            ChaseTarget();
            TryAttack();
        }

        UpdateAnim();
    }

    //===== Audio Helper =====//
    private EnemyAudioSet GetAudioSet()
    {
        switch (unitType)
        {
            case EnemyUnitType.Minion: return minionAudio;
            case EnemyUnitType.Warrior: return warriorAudio;
            case EnemyUnitType.Boss: return bossAudio;
            default: return null;
        }
    }

    private void TryPlayGrunt()
    {
        if (UnityEngine.Random.value > gruntChance) return;

        EnemyAudioSet set = GetAudioSet();
        if (set == null || set.gruntClips.Length == 0) return;

        AudioClip clip = set.gruntClips[UnityEngine.Random.Range(0, set.gruntClips.Length)];

        audioSource.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip);
    }

    private void TryPlayDeath()
    {
        if (UnityEngine.Random.value > deathChance) return;

        EnemyAudioSet set = GetAudioSet();
        if (set == null || set.deathClips.Length == 0) return;

        AudioClip clip = set.deathClips[UnityEngine.Random.Range(0, set.deathClips.Length)];

        audioSource.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        audioSource.PlayOneShot(clip);
    }
    //===== Audio Helper =====//

    protected virtual void ChaseTarget()
    {
        if (!target || !agent) return;

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        float attackRangeSqr = attackRange * attackRange;

        if (attackType == EnemyAttackType.Ranged && sqrDist <= attackRangeSqr)
        {
            agent.isStopped = false;
            agent.ResetPath();
            FaceTarget();
            return;
        }

        if (Time.time < nextRepathTime) return;
        nextRepathTime = Time.time + repathInterval;

        agent.isStopped = false;
        agent.SetDestination(target.position);
    }

    private void FaceTarget()
    {
        if (!target) return;

        Vector3 dir = target.position - transform.position;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * faceTargetSpeed);
    }

    protected virtual void RefreshTarget()
    {
        BrainwashedUnit[] brainwashedUnits = FindObjectsByType<BrainwashedUnit>(FindObjectsSortMode.None);

        Transform bestTarget = null;
        float bestDist = Mathf.Infinity;

        for (int i = 0; i < brainwashedUnits.Length; i++)
        {
            BrainwashedUnit bw = brainwashedUnits[i];
            if (bw == null || bw.IsDead) continue;
            
            float sqrDist = (bw.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < bestDist)
            {
                bestDist = sqrDist;
                bestTarget = bw.transform;
            }
        }

        if (bestTarget != null)
        {
            target = bestTarget;
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            target = playerObj.transform;
    }


    //old TryAttack
    //protected virtual void TryAttack()
    //{
    //    if (currentState != State.Chasing) return;

    //    float sqrDist = (target.position - transform.position).sqrMagnitude;
    //    if (sqrDist > attackRange * attackRange) return;
    //    if (Time.time < nextAttackTime) return;

    //    nextAttackTime = Time.time + attackCooldown;

    //    // Prevent sliding while attacking
    //    agent.ResetPath();

    //    if (animator && !string.IsNullOrEmpty(attackTrigger))
    //    {
    //        animator.SetTrigger(attackTrigger);
    //    }

    //    IDamageable dmg = target.GetComponentInParent<IDamageable>();
    //    if (dmg != null)
    //    {
    //        dmg.TakeDamage(damage);
    //    }
    //}

    protected virtual void TryAttack()
    {
        if (currentState != State.Chasing) return;
        if (!target) return;

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        if (sqrDist > attackRange * attackRange) return;
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;

        //if (agent)
        //{
        //    agent.isStopped = false;
        //    agent.ResetPath();
        //}

        if (CanUseAgent())
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        FaceTarget();

        if (animator)
        {
            string triggerToUse = attackType == EnemyAttackType.Ranged
                ? rangedAttackTrigger
                : attackTrigger;

            if (!string.IsNullOrEmpty(triggerToUse))
            {
                animator.ResetTrigger(triggerToUse);
                animator.SetTrigger(triggerToUse);

                if (!useAnimationEventForAttack)
                {
                    DealDamageNow();
                }

                return;
            }
        }

        DealDamageNow();
    }

    public void DealDamageNow()
    {
        if (currentState != State.Chasing) return;
        if (!target) return;

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        if (sqrDist > attackRange * attackRange) return;

        if (attackType == EnemyAttackType.Melee)
        {
            IDamageable dmg = target.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(damage);
            }
        }
        else if (attackType == EnemyAttackType.Ranged)
        {
            FireProjectile();
        }
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        if (!canBeSlowed) return;
        if (!agent) return;
        if (currentState == State.Dead) return;

        if (slowRoutine != null)
            StopCoroutine(slowRoutine);

        slowRoutine = StartCoroutine(SlowRoutine(slowPercent, duration));
    }

    private IEnumerator SlowRoutine(float slowPercent, float duration)
    {
        float clampedSlow = Mathf.Clamp01(slowPercent);
        agent.speed = originalAgentSpeed * (1f - clampedSlow);

        ApplySlowVisual();

        yield return new WaitForSeconds(duration);

        if (agent && currentState != State.Dead)
            agent.speed = originalAgentSpeed;

        ClearSlowVisual();

        slowRoutine = null;
    }

    private void ApplySlowVisual()
    {
        originalColors.Clear();

        for (int i = 0; i < slowRenderers.Length; i++)
        {
            Renderer r = slowRenderers[i];
            if (!r) continue;

            Material mat = r.material;

            if (mat.HasProperty("_BaseColor"))
            {
                originalColors.Add(mat.GetColor("_BaseColor"));
                mat.SetColor("_BaseColor", slowTint);
            }
            else if (mat.HasProperty("_Color"))
            {
                originalColors.Add(mat.color);
                mat.color = slowTint;
            }
            else
            {
                originalColors.Add(Color.white);
            }
        }
    }

    private void ClearSlowVisual()
    {
        for (int i = 0; i < slowRenderers.Length; i++)
        {
            Renderer r = slowRenderers[i];
            if (!r) continue;
            if (i >= originalColors.Count) continue;

            Material mat = r.material;

            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", originalColors[i]);
            }
            else if (mat.HasProperty("_Color"))
            {
                mat.color = originalColors[i];
            }
        }

        originalColors.Clear();
    }

    private void FireProjectile()
    {
        if (projectilePrefab == null || target == null) return;

        Transform spawn = projectileSpawnPoint ? projectileSpawnPoint : transform;

        GameObject projectileObj = Instantiate(projectilePrefab, spawn.position, Quaternion.identity);

        EnemyProjectile projectile = projectileObj.GetComponent<EnemyProjectile>();
        if (projectile != null)
        {
            projectile.Initialize(target, damage, projectileSpeed);
        }
    }


    // -- Knockback -- //
    public void ApplyKnockback(Vector3 direction, float force, float duration)
    {
        if (currentState == State.Dead) return;

        float finalForce = force * (1f - Mathf.Clamp01(knockbackResistance));
        if (finalForce <= 0f) return;

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(KnockbackRoutine(direction.normalized, finalForce, duration));
    }

    private IEnumerator KnockbackRoutine(Vector3 direction, float force, float duration)
    {
        //if (agent)
        //{
        //    agent.ResetPath();
        //    agent.isStopped = true;
        //}

        if (CanUseAgent())
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            float t = 1f - (elapsed / duration); // slows down over time
            Vector3 move = direction * force * t * Time.deltaTime;

            transform.position += move;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentState != State.Dead && currentState != State.Stunned)
        {
            if (agent)
                agent.isStopped = false;

            if (currentState != State.GettingHit)
                currentState = State.Chasing;
        }

        knockbackRoutine = null;
    }

    // -- Knockback -- //

    protected virtual void UpdateAnim()
    {
        if (!animator || string.IsNullOrEmpty(speedParam) || !agent) return;

        float normalized = agent.velocity.magnitude / Mathf.Max(agent.speed, 0.001f);
        animator.SetFloat(speedParam, normalized, 0.1f, Time.deltaTime);
    }

    // -- Damage / States --

    public virtual void TakeDamage(float amount)
    {
        if (currentState == State.Dead) return;

        currentHealth -= amount;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        TryPlayGrunt();

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        if (animator && !string.IsNullOrEmpty(hitTrigger))
            animator.SetTrigger(hitTrigger);

        //Blink routine reacion
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        blinkRoutine = StartCoroutine(BlinkRoutine());

        // Restart hit reaction each time damage is taken
        if (hitRoutine != null)
            StopCoroutine(hitRoutine);

        hitRoutine = StartCoroutine(HitReactionRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        for (int i = 0; i < blinkCount; i++)
        {
            SetBlinkVisible(false);
            yield return new WaitForSeconds(blinkInterval);

            SetBlinkVisible(true);
            yield return new WaitForSeconds(blinkInterval);
        }

        SetBlinkVisible(true);
        blinkRoutine = null;
    }

    private void SetBlinkVisible(bool visible)
    {
        for (int i = 0; i < blinkRenderers.Length; i++)
        {
            if (blinkRenderers[i] != null)
                blinkRenderers[i].enabled = visible;
        }
    }

    private IEnumerator HitReactionRoutine()
    {
        currentState = State.GettingHit;

        if (CanUseAgent())
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        //if (agent)
        //{
        //    agent.ResetPath();
        //    agent.isStopped = true;
        //}

        yield return new WaitForSeconds(hitRecoverTime);

        if (currentState != State.Dead)
        {
            if (agent)
                agent.isStopped = false;

            currentState = State.Chasing;
        }

        hitRoutine = null;
    }

    public virtual void Stun(float duration)
    {
        if (currentState == State.Dead) return;

        if (hitRoutine != null)
        {
            StopCoroutine(hitRoutine);
            hitRoutine = null;
        }

        StopAllCoroutines();
        StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        currentState = State.Stunned;

        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        yield return new WaitForSeconds(duration);

        if (currentState != State.Dead)
        {
            if (agent)
                agent.isStopped = false;

            currentState = State.Chasing;
        }
    }

    private void DisableScriptsOnDeath()
    {
        for (int i = 0; i < componentsToDisable.Count; i++)
        {
            var c = componentsToDisable[i];
            if (!c) continue;

            if (c is Behaviour b) b.enabled = false;     // MonoBehaviour, Animator, NavMeshAgent
            //else if (c is Collider col) col.enabled = false; // CapsuleCollider, etc.
            else if (c is Collider col)
            {
                if (col == capsuleCollider)
                    col.isTrigger = true; // keep enabled, just trigger
                else
                    col.enabled = false;
            }

            else if (c is Renderer r) r.enabled = false; // optional
        }
    }

    protected virtual void Die()
    {
        if (isDead) return;

        ClearSlowVisual();

        if (canResurrect && !hasResurrected)
        {
            StartCoroutine(ResurrectionRoutine());
            return;
        }

        currentHealth = 0f;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        TryPlayDeath();

        currentState = State.Dead;

        DisableScriptsOnDeath();

        if (CanUseAgent())
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        if (agent)
        {
            agent.enabled = false;
        }

        if (capsuleCollider)
        {
            capsuleCollider.isTrigger = false;
        }

        if (animator && !string.IsNullOrEmpty(dieTrigger))
        {
            animator.SetTrigger(dieTrigger);
        }

        SetBlinkVisible(true);
        OnDied?.Invoke(this);

        if (IsDead) return;
        isDead = true;

        if (xpDropPrefab != null)
        {
            Instantiate(xpDropPrefab, transform.position + xpDropOffset, Quaternion.identity);
        }

        

        Destroy(gameObject, 3f);
    }

    private bool CanUseAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

    private IEnumerator ResurrectionRoutine()
    {
        hasResurrected = true;
        isResurrecting = true;
        currentState = State.Dead;

        currentHealth = 0f;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        if (animator && !string.IsNullOrEmpty(fakeDeathTrigger))
            animator.SetTrigger(fakeDeathTrigger);

        SetBlinkVisible(true);

        yield return new WaitForSeconds(fakeDeathDuration);

        if (animator && !string.IsNullOrEmpty(resurrectTrigger))
            animator.SetTrigger(resurrectTrigger);

        yield return new WaitForSeconds(resurrectDuration);

        currentHealth = maxHealth * resurrectHealthPercent;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        currentState = State.Chasing;

        if (agent)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }

        isResurrecting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        if (attackType == EnemyAttackType.Melee)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin, attackRange);
        }
        else if (attackType == EnemyAttackType.Ranged)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, attackRange);

            if (projectileSpawnPoint != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(projectileSpawnPoint.position, projectileSpawnPoint.position + projectileSpawnPoint.forward * 1.5f);
                Gizmos.DrawWireSphere(projectileSpawnPoint.position, 0.1f);
            }
        }
    }

    //old
    //public enum State { Chasing, Stunned, Dead }

    //[Header("Target")]
    //[SerializeField] private Transform target;

    //[Header("NavMesh")]
    //[SerializeField] private float repathInterval = 0.1f;
    //[SerializeField] private float chaseStopDistance = 1.5f;
    //[SerializeField] private bool rotateWithAgent = true;

    //[Header("Combat")]
    //[SerializeField] private float attackRange = 1.8f;
    //[SerializeField] private float attackCooldown = 1.0f;
    //[SerializeField] private int damage = 10;

    //[Header("Stats")]
    //[SerializeField] private float maxHealth = 100f;
    //[SerializeField] private float currentHealth;

    //[Header("Animation")]
    //[SerializeField] private Animator animator;
    //[SerializeField] private string speedParam = "Speed";
    //[SerializeField] private string attackTrigger = "Attack";
    //[SerializeField] private string hitTrigger = "Hit";
    //[SerializeField] private string dieTrigger = "Die";

    //public State currentState { get; private set; } = State.Chasing;

    //private NavMeshAgent agent;
    //private float nextRepathTime;
    //private float nextAttackTime;

    //public event Action<BaseEnemy> OnDied;

    //protected virtual void Awake()
    //{
    //    agent = GetComponent < NavMeshAgent>();
    //    if (!animator) animator = GetComponentInChildren<Animator>();

    //    currentHealth = maxHealth;

    //    if (agent)
    //    {
    //        agent.stoppingDistance = chaseStopDistance;
    //        agent.updateRotation = rotateWithAgent;
    //    }
    //}

    //protected virtual void Start()
    //{
    //    if (!target)
    //    {
    //        var playerObj = GameObject.FindGameObjectWithTag("Player");
    //        if (playerObj) target = playerObj.transform;
    //    }
    //}

    //protected virtual void Update()
    //{
    //    if (currentState == State.Dead) return;
    //    if (!target || !agent) return;

    //    if (currentState == State.Chasing)
    //    {
    //        ChaseTarget();
    //        TryAttack();
    //    }

    //    UpdateAnim();
    //}

    //protected virtual void ChaseTarget()
    //{
    //    if (Time.time < nextRepathTime) return;
    //    nextRepathTime = Time.time + repathInterval;

    //    agent.SetDestination(target.position);
    //}

    //protected virtual void TryAttack()
    //{
    //    float sqrDist = (target.position - transform.position).sqrMagnitude;
    //    if (sqrDist > attackRange * attackRange) return;
    //    if (Time.time < nextAttackTime) return;

    //    nextAttackTime = Time.time + attackCooldown;

    //    // Prevent sliding while attacking
    //    agent.ResetPath();

    //    if (animator && !string.IsNullOrEmpty(attackTrigger))
    //    {
    //        animator.SetTrigger(attackTrigger);
    //    }

    //    //full damage system to be implemented later
    //    IDamageable dmg = target.GetComponent<IDamageable>();
    //    if (dmg != null)
    //    {
    //        dmg.TakeDamage(damage);
    //    }
    //}

    //protected virtual void UpdateAnim()
    //{
    //    if (!animator || string.IsNullOrEmpty(speedParam) || !agent) return;

    //    float normalized = agent.velocity.magnitude / Mathf.Max(agent.speed, 0.001f);
    //    animator.SetFloat(speedParam, normalized, 0.1f, Time.deltaTime);
    //}

    //// -- Damage / States --

    //public virtual void TakeDamage(float amount)
    //{
    //    if (currentState == State.Dead) return;

    //    currentHealth -= amount;

    //    if (animator && !string.IsNullOrEmpty(hitTrigger))
    //        animator.SetTrigger(hitTrigger);

    //    if (currentHealth <= 0f)
    //        Die();
    //}

    //public virtual void Stun(float duration)
    //{
    //    if (currentState == State.Dead) return;
    //    StopAllCoroutines();
    //    StartCoroutine(StunRoutine(duration));
    //}

    //private IEnumerator StunRoutine(float duration)
    //{
    //    currentState = State.Stunned;

    //    if (agent)
    //    {
    //        agent.ResetPath();
    //        agent.isStopped = true;
    //    }

    //    yield return new WaitForSeconds(duration);

    //    if (agent)
    //        agent.isStopped = false;

    //    currentState = State.Chasing;
    //}

    //protected virtual void Die()
    //{
    //    currentState = State.Dead;

    //    if (agent)
    //    {
    //        agent.ResetPath();
    //        agent.isStopped = true;
    //    }

    //    if (animator && !string.IsNullOrEmpty(dieTrigger))
    //    {
    //        animator.SetTrigger(dieTrigger);
    //    }

    //    OnDied?.Invoke(this);

    //    Destroy(gameObject, 3f);
    //}
}
