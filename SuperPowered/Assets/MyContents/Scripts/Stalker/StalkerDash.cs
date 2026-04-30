using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class StalkerDash : MonoBehaviour, IUpgradeableAbility, IAbilityCooldown
{
    [Header("Dash Settings")]
    [SerializeField] private float dashDuration = 0.08f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Input")]
    [SerializeField] private KeyCode castKey = KeyCode.E;

    [Header("Level Up")]
    [SerializeField] private int abilityLevel = 0;
    [SerializeField] private int maxAbilityLevel = 5;

    public int AbilityLevel => abilityLevel;
    public int MaxAbilityLevel => maxAbilityLevel;
    public event System.Action<int> OnAbilityLevelChanged;

    [Header("Stats Per Level")]
    [SerializeField] private float[] cooldownByLevel = { 10f, 9f, 8f, 7f, 6f };
    [SerializeField] private float[] distanceByLevel = { 8f, 10f, 12f, 14f, 16f };

    [Header("UI")]
    public float CooldownDuration => GetCooldown();
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    [Header("VFX")]
    [SerializeField] private ParticleSystem dashStartVfx;
    [SerializeField] private ParticleSystem dashEndVfx;
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dashClip;

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;

    private float nextReadyTime;
    private bool isDashing;

    private void Awake()
    {
        if (!agent)
            agent = GetComponent<NavMeshAgent>();

        if (!audioSource)
            audioSource = GetComponentInParent<AudioSource>();
    }

    private void Update()
    {
        if (abilityLevel <= 0) return;

        if (Input.GetKeyDown(castKey))
        {
            CastDash();
        }
    }

    public bool CanCast => abilityLevel > 0 && Time.time >= nextReadyTime && !isDashing;

    public void LevelUpAbility()
    {
        if (abilityLevel >= maxAbilityLevel) return;

        abilityLevel++;
        OnAbilityLevelChanged?.Invoke(abilityLevel);
    }

    public void CastDash()
    {
        if (!CanCast) return;

        Vector3 targetPosition = GetDashTargetPosition();

        nextReadyTime = Time.time + GetCooldown();

        if (audioSource && dashClip)
            audioSource.PlayOneShot(dashClip);

        StartCoroutine(DashRoutine(targetPosition));
    }

    private IEnumerator DashRoutine(Vector3 targetPosition)
    {
        isDashing = true;

        SpawnVfx(dashStartVfx, transform.position);

        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.Warp(targetPosition);
            agent.isStopped = false;
        }
        else
        {
            transform.position = targetPosition;
        }

        SpawnVfx(dashEndVfx, targetPosition);

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
    }

    private Vector3 GetDashTargetPosition()
    {
        Vector3 direction = transform.forward;
        float distance = GetDashDistance();

        Vector3 desiredPosition = transform.position + direction * distance;

        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, out RaycastHit wallHit, distance, obstacleLayers))
        {
            desiredPosition = wallHit.point - direction * 0.75f;
        }

        if (NavMesh.SamplePosition(desiredPosition, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
        {
            return navHit.position;
        }

        return transform.position;
    }

    private void SpawnVfx(ParticleSystem prefab, Vector3 position)
    {
        if (!prefab) return;

        ParticleSystem fx = Instantiate(prefab, position + vfxOffset, Quaternion.identity);
        Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
    }

    private int L()
    {
        return Mathf.Clamp(abilityLevel - 1, 0, maxAbilityLevel - 1);
    }

    private float GetCooldown() => cooldownByLevel[L()];
    private float GetDashDistance() => distanceByLevel[L()];
}