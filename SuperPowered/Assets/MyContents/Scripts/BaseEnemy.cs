using System;
using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class BaseEnemy : MonoBehaviour
{
    public enum State { Chasing, Stunned, Dead }

    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("NavMesh")]
    [SerializeField] private float repathInterval = 0.1f;
    [SerializeField] private float chaseStopDistance = 1.5f;
    [SerializeField] private bool rotateWithAgent = true;

    [Header("Combat")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.0f;
    [SerializeField] private int damage = 10;

    [Header("Stats")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string attackTrigger = "Attack";
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string dieTrigger = "Die";

    public State currentState { get; private set; } = State.Chasing;

    private NavMeshAgent agent;
    private float nextRepathTime;
    private float nextAttackTime;

    public event Action<BaseEnemy> OnDied;

    protected virtual void Awake()
    {
        agent = GetComponent < NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();

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

    protected virtual void TryAttack()
    {
        float sqrDist = (target.position - transform.position).sqrMagnitude;
        if (sqrDist > attackRange * attackRange) return;
        if (Time.time < nextAttackTime) return;

        nextAttackTime = Time.time + attackCooldown;

        // Prevent sliding while attacking
        agent.ResetPath();

        if (animator && !string.IsNullOrEmpty(attackTrigger))
        {
            animator.SetTrigger(attackTrigger);
        }

        //damage system to be implemented later
    }

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

        if (animator && !string.IsNullOrEmpty(hitTrigger))
            animator.SetTrigger(hitTrigger);

        if (currentHealth <= 0f)
            Die();
    }

    public virtual void Stun(float duration)
    {
        if (currentState == State.Dead) return;
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

        if (agent)
            agent.isStopped = false;

        currentState = State.Chasing;
    }

    protected virtual void Die()
    {
        currentState = State.Dead;

        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
        }

        if (animator && !string.IsNullOrEmpty(dieTrigger))
        {
            animator.SetTrigger(dieTrigger);
        }

        OnDied?.Invoke(this);

        Destroy(gameObject, 3f);
    }
}
