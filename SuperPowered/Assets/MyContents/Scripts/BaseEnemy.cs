using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public class BaseEnemy : MonoBehaviour, IDamageable, IKnockbackable
{
    public enum State { Chasing, GettingHit, Stunned, Dead }

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
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private int damage = 10;

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
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string dieTrigger = "Die";

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

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (blinkRenderers == null || blinkRenderers.Length == 0)
        {
            blinkRenderers = GetComponentsInChildren<Renderer>(true);
        }

        currentHealth = maxHealth;

        if (agent)
        {
            agent.stoppingDistance = chaseStopDistance;
            agent.updateRotation = rotateWithAgent;
        }


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
        if (currentState == State.Dead) return;
        if (!target || !agent) return;

        if (currentState == State.Chasing)
        {
            ChaseTarget();
            TryAttack();
        }

        UpdateAnim();
    }

    protected virtual void ChaseTarget()
    {
        if (Time.time < nextRepathTime) return;
        nextRepathTime = Time.time + repathInterval;

        agent.SetDestination(target.position);
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

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        if (sqrDist > attackRange * attackRange) return;
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;

        agent.ResetPath();

        if (animator && !string.IsNullOrEmpty(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
        }
        else
        {
            DealDamageNow();
        }
    }

    public void DealDamageNow()
    {
        if (currentState != State.Chasing) return;
        if (!target) return;

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        if (sqrDist > attackRange * attackRange) return;

        IDamageable dmg = target.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(damage);
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
        if (agent)
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

        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

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
            else if (c is Collider col) col.enabled = false; // CapsuleCollider, etc.
            else if (c is Renderer r) r.enabled = false; // optional
        }
    }

    protected virtual void Die()
    {
        currentState = State.Dead;

        DisableScriptsOnDeath();

        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
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
