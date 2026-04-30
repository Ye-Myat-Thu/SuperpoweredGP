using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class OracleBlink : MonoBehaviour, IUpgradeableAbility, IAbilityCooldown
{
    [Header("Blink Settings")]
    [SerializeField] private float blinkDistance = 6f;
    [SerializeField] private float blinkDuration = 0.08f;
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Input")]
    [SerializeField] private KeyCode castKey = KeyCode.Q;

    [Header("Level Up")]
    [SerializeField] private int abilityLevel = 0;
    [SerializeField] private int maxAbilityLevel = 5;

    public int AbilityLevel => abilityLevel;
    public int MaxAbilityLevel => maxAbilityLevel;
    public event System.Action<int> OnAbilityLevelChanged;

    [Header("Cooldown Per Level")]
    [SerializeField] private float[] cooldownByLevel = { 12f, 11f, 10f, 9f, 8f };

    [Header("Distance Per Level")]
    [SerializeField] private float[] distanceByLevel = { 6f, 7.5f, 9f, 10.5f, 12f };

    [Header("Arrival Preview")]
    [SerializeField] private GameObject arrivalMarkerPrefab;
    [SerializeField] private float tapCastThreshold = 0.18f;

    private GameObject arrivalMarkerInstance;
    private bool isHoldingBlink;
    private float blinkKeyDownTime;

    [Header("UI")]
    public float CooldownDuration => GetCooldown();
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    [Header("VFX")]
    [SerializeField] private ParticleSystem blinkStartVfx;
    [SerializeField] private ParticleSystem blinkEndVfx;
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip blinkClip;

    [Header("References")]
    [SerializeField] private NavMeshAgent agent;

    private float nextReadyTime;
    private bool isBlinking;

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
            blinkKeyDownTime = Time.time;
            isHoldingBlink = true;
            ShowArrivalMarker();
        }

        if (isHoldingBlink && Input.GetKey(castKey))
        {
            UpdateArrivalMarker();
        }

        if (isHoldingBlink && Input.GetKeyUp(castKey))
        {
            float heldTime = Time.time - blinkKeyDownTime;

            HideArrivalMarker();

            // quick tap or hold release both cast
            CastBlink();

            isHoldingBlink = false;
        }
    }

    public bool CanCast => abilityLevel > 0 && Time.time >= nextReadyTime && !isBlinking;

    public void LevelUpAbility()
    {
        if (abilityLevel >= maxAbilityLevel) return;

        abilityLevel++;

        blinkDistance = GetBlinkDistance();

        OnAbilityLevelChanged?.Invoke(abilityLevel);
    }

    public void CastBlink()
    {
        HideArrivalMarker();

        if (!CanCast) return;

        Vector3 targetPosition = GetBlinkTargetPosition();

        nextReadyTime = Time.time + GetCooldown();

        if (audioSource && blinkClip)
            audioSource.PlayOneShot(blinkClip);

        StartCoroutine(BlinkRoutine(targetPosition));
    }

    private IEnumerator BlinkRoutine(Vector3 targetPosition)
    {
        isBlinking = true;

        SpawnVfx(blinkStartVfx, transform.position);

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

        SpawnVfx(blinkEndVfx, targetPosition);

        yield return new WaitForSeconds(blinkDuration);

        isBlinking = false;
    }

    private void ShowArrivalMarker()
    {
        if (!arrivalMarkerPrefab) return;

        if (!arrivalMarkerInstance)
            arrivalMarkerInstance = Instantiate(arrivalMarkerPrefab);

        arrivalMarkerInstance.SetActive(true);
        UpdateArrivalMarker();
    }

    private void UpdateArrivalMarker()
    {
        if (!arrivalMarkerInstance) return;

        Vector3 targetPosition = GetBlinkTargetPosition();
        arrivalMarkerInstance.transform.position = targetPosition + Vector3.up * 0.05f;
    }

    private void HideArrivalMarker()
    {
        if (arrivalMarkerInstance)
            arrivalMarkerInstance.SetActive(false);
    }

    private Vector3 GetBlinkTargetPosition()
    {
        Vector3 direction = transform.forward;
        Vector3 desiredPosition = transform.position + direction * GetBlinkDistance();

        // Stop before wall/obstacle
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, direction, out RaycastHit wallHit, GetBlinkDistance(), obstacleLayers))
        {
            desiredPosition = wallHit.point - direction * 0.75f;
        }

        // Snap to NavMesh
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

    private float GetCooldown()
    {
        if (abilityLevel <= 0) return cooldownByLevel[0];

        int index = Mathf.Clamp(abilityLevel - 1, 0, cooldownByLevel.Length - 1);
        return cooldownByLevel[index];
    }

    private float GetBlinkDistance()
    {
        if (abilityLevel <= 0) return distanceByLevel[0];

        int index = Mathf.Clamp(abilityLevel - 1, 0, distanceByLevel.Length - 1);
        return distanceByLevel[index];
    }
}