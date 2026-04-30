using UnityEngine;
using UnityEngine.AI;

public class BaseAlly : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] private BaseEnemy sourceEnemy;
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;

    [Header("Targeting")]
    [SerializeField] private float repathInterval = 0.15f;
    [SerializeField] private float allySearchRadius = 50f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Animation")]
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string attackTrigger = "Attack";

    private Transform target;
    private float nextRepathTime;
    private float nextAttackTime;

    private void Awake()
    {
        if (!sourceEnemy) sourceEnemy = GetComponent<BaseEnemy>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        target = null;

        if (agent)
            agent.isStopped = false;
    }

    private void Update()
    {
        if (!enabled) return;
        if (!sourceEnemy || sourceEnemy.IsDead) return;
        if (!agent) return;

        RefreshTarget();

        if (!target)
        {
            ChaseTarget();
            TryAttack();
        }

        UpdateAnim();
    }

    private void RefreshTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, allySearchRadius, enemyLayers);

        Transform bestTarget = null;
        float bestDist = Mathf.Infinity;

        for (int i = 0; i < hits.Length; i++)
        {
            BaseEnemy enemy = hits[i].GetComponentInParent<BaseEnemy>();
            if (enemy == null) continue;
            if (enemy == sourceEnemy) continue;
            if (enemy.IsDead) continue;

            BrainwashedUnit bw = enemy.GetComponent<BrainwashedUnit>();
            if (bw != null) continue; // do not attack other brainwashed allies

            float sqrDist = (enemy.transform.position - transform.position).sqrMagnitude;
            if (sqrDist < bestDist)
            {
                bestDist = sqrDist;
                bestTarget = enemy.transform;
            }
        }

        target = bestTarget;
    }

    private void ChaseTarget()
    {
        if (Time.time < nextRepathTime) return;
        nextRepathTime = Time.time + repathInterval;

        if (target != null)
            agent.SetDestination(target.position);
    }

    private void TryAttack()
    {
        if (target == null) return;
        if (Time.time < nextAttackTime) return;

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        if (sqrDist > sourceEnemy.AttackRange * sourceEnemy.AttackRange) return;

        nextAttackTime = Time.time + sourceEnemy.AttackCooldown;

        if (agent)
            agent.ResetPath();

        if (animator && !string.IsNullOrEmpty(attackTrigger))
            animator.SetTrigger(attackTrigger);
           
        DealDamageNow();
    }

    public void DealDamageNow()
    {
        if (target == null) return;

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        if (sqrDist > sourceEnemy.AttackRange * sourceEnemy.AttackRange) return;

        IDamageable dmg = target.GetComponentInParent<IDamageable>();
        if (dmg != null)
            dmg.TakeDamage(sourceEnemy.AttackDamage);
    }

    private void UpdateAnim()
    {
        if (!animator || string.IsNullOrEmpty(speedParam) || !agent) return;

        float normalized = agent.velocity.magnitude / Mathf.Max(agent.speed, 0.001f);
        animator.SetFloat(speedParam, normalized, 0.1f, Time.deltaTime);
    }
}
