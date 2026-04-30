using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BossEnemy : MonoBehaviour, IDamageable, ISlowable
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string playerTag = "Player";

    [Header("Stats")]
    [SerializeField] private float maxHealth = 1000f;
    [SerializeField] private float currentHealth;

    [Header("Movement")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float stoppingDistance = 3f;
    [SerializeField] private float faceTargetSpeed = 8f;

    [Header("State Timing")]
    [SerializeField] private float idleBeforeRunDuration = 1.2f;
    [SerializeField] private float runDuration = 2f;
    [SerializeField] private float summonCooldown = 12f;
    [SerializeField] private float slamCooldown = 8f;
    [SerializeField] private float runCooldown = 10f;

    [Header("Summon")]
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private int summonCount = 3;
    [SerializeField] private Transform summonCenter;
    [SerializeField] private float summonRadius = 6f;
    [SerializeField] private float summonCheckRadius = 0.8f;
    [SerializeField] private LayerMask summonBlockLayers;
    [SerializeField] private int maxSummonAttemptsPerEnemy = 20;

    [Header("Bulldoze")]
    [SerializeField] private float bulldozeTriggerRange = 12f;
    [SerializeField] private float bulldozeDamage = 40f;
    [SerializeField] private float bulldozeHitRadius = 1.5f;
    [SerializeField] private float bulldozeHitCooldown = 0.5f;
    private float nextBulldozeHitTime;

    [Header("Ground Slam")]
    [SerializeField] private Transform slamOrigin;
    [SerializeField] private GameObject waveProjectilePrefab;
    [SerializeField] private int waveCount = 15;
    [SerializeField] private float waveSpeed = 18f;
    [SerializeField] private float waveDamage = 20f;
    [SerializeField] private float waveLifetime = 5f;
    [SerializeField] private Vector3 waveRotationOffset = Vector3.zero;

    [Header("Phase 2")]
    [SerializeField] private bool useHalfHpPhase = true;
    [SerializeField, Range(0f, 1f)] private float halfHpThreshold = 0.5f;
    [SerializeField] private float phaseTwoSlamCooldown = 5f;
    [SerializeField] private float phaseTwoRunCooldown = 6f;
    [SerializeField] private float phaseTwoRunSpeed = 12f;
    [SerializeField] private float phaseTwoAgentAcceleration = 18f;
    [SerializeField] private int phaseTwoWaveCount = 24;
    [SerializeField] private float phaseTwoWaveSpeed = 26f;
    private bool halfHpPhaseActive;

    [Header("Slow")]
    [SerializeField] private bool canBeSlowed = true;
    [SerializeField, Range(0f, 1f)] private float slowResistance = 0.5f;
    [SerializeField] private Color slowTint = Color.cyan;
    private Coroutine slowRoutine;
    private float originalWalkSpeed;
    private float originalRunSpeed;
    private Renderer[] slowRenderers;
    private List<Color> originalSlowColors = new List<Color>();

    [Header("Hit Feedback")]
    [SerializeField] private Renderer[] blinkRenderers;
    [SerializeField] private int blinkCount = 3;
    [SerializeField] private float blinkInterval = 0.06f;

    [Header("Death Sequence")]
    [SerializeField] private float deathAnimationDelay = 2.5f;
    [SerializeField] private float bossDestroyDelay = 5f;

    [Header("Recovery")]
    [SerializeField] private float postActionIdleDuration = 1f;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string idleTrigger = "Idle";
    [SerializeField] private string summonTrigger = "Summon";
    [SerializeField] private string slamTrigger = "Slam";
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string deathTrigger = "Death";
    [SerializeField] private string bulldozeStartTrigger = "BulldozeStart";
    [SerializeField] private string bulldozeRunTrigger = "BulldozeRun";
    [SerializeField] private string runBool = "IsRunning";
    [SerializeField] private float bulldozeStartDuration = 1.2f;

    [Header("Action Damage Reduction")]
    [SerializeField, Range(0f, 1f)] private float actionDamageReduction = 0.2f;

    [Header("Player Warning FX")]
    [SerializeField] private CinemachineShake cameraShake;
    [SerializeField] private BossWarningVignette warningVignette;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip walkToRunCue;
    [SerializeField] private AudioClip summonCue;
    [SerializeField] private AudioClip slamCue;
    [SerializeField] private AudioClip deathCue;
    [SerializeField] private AudioClip[] hitGruntClips;
    [SerializeField, Range(0f, 1f)] private float hitGruntChance = 0.6f;
    [SerializeField, Range(0f, 1f)] private float hitGruntVolume = 1f;

    [Header("Footsteps Audio")]
    [SerializeField] private AudioSource footstepAudioSource;
    [SerializeField] private AudioClip[] leftFootstepClips;
    [SerializeField] private AudioClip[] rightFootstepClips;
    [SerializeField] private AudioClip runFootstepClip;
    [SerializeField, Range(0f, 1f)] private float footstepVolume = 1f;
    [SerializeField] private float footstepPitchMin = 0.9f;
    [SerializeField] private float footstepPitchMax = 1.1f;


    public BossState CurrentState { get; private set; }

    public event Action<float, float> OnBossHealthChanged;
    public event Action OnBossFightStarted;
    public event Action OnBossDied;

    private bool isDead;
    private bool actionRunning;

    private float nextSummonTime;
    private float nextSlamTime;
    private float nextRunTime;

    private Coroutine blinkRoutine;

    private bool slamInProgress;
    private bool bulldozeInProgress;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();

        if (!cameraShake)
            cameraShake = FindFirstObjectByType<CinemachineShake>();

        if (!warningVignette)
            warningVignette = FindFirstObjectByType<BossWarningVignette>();

        if (blinkRenderers == null || blinkRenderers.Length == 0)
            blinkRenderers = GetComponentsInChildren<Renderer>(true);

        originalWalkSpeed = walkSpeed;
        originalRunSpeed = runSpeed;

        slowRenderers = GetComponentsInChildren<Renderer>(true);

        currentHealth = maxHealth;

        if (agent)
        {
            agent.speed = walkSpeed;
            agent.stoppingDistance = stoppingDistance;
        }
    }

    private void Start()
    {
        if (!target)
        {
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player) target = player.transform;
        }

        nextSummonTime = Time.time + summonCooldown;
        nextSlamTime = Time.time + slamCooldown;
        nextRunTime = Time.time + runCooldown;

        StartBossFight();
    }

    private void Update()
    {
        if (isDead) return;
        if (!target || !agent) return;
        if (actionRunning) return;

        DecideNextAction();
        UpdateAnimation();
    }

    private void StartBossFight()
    {
        OnBossFightStarted?.Invoke();
        OnBossHealthChanged?.Invoke(currentHealth, maxHealth);
        SetState(BossState.Walk);
    }

    private void DecideNextAction()
    {
        float dist = Vector3.Distance(transform.position, target.position);

        if (Time.time >= nextSummonTime)
        {
            StartCoroutine(SummonRoutine());
            return;
        }

        if (Time.time >= nextSlamTime && dist <= 8f)
        {
            StartCoroutine(SlamRoutine());
            return;
        }

        if (Time.time >= nextRunTime && dist > bulldozeTriggerRange)
        {
            StartCoroutine(RunBulldozeRoutine());
            return;
        }

        WalkTowardsPlayer();
    }

    private void WalkTowardsPlayer()
    {
        SetState(BossState.Walk);

        agent.isStopped = false;
        agent.speed = walkSpeed;
        agent.SetDestination(target.position);

        FaceTarget();
    }

    private IEnumerator RunBulldozeRoutine()
    {
        actionRunning = true;
        bulldozeInProgress = true;

        if (animator)
            animator.SetBool(runBool, false);

        SetState(BossState.Idle);

        agent.ResetPath();
        agent.isStopped = true;
        agent.speed = walkSpeed;

        FaceTarget();

        if (audioSource && walkToRunCue)
            audioSource.PlayOneShot(walkToRunCue);

        if (animator && !string.IsNullOrEmpty(bulldozeStartTrigger))
            animator.SetTrigger(bulldozeStartTrigger);

        if (warningVignette)
            warningVignette.PlayWarning();

        yield return new WaitForSeconds(bulldozeStartDuration);

        SetState(BossState.Run);

        if (animator && !string.IsNullOrEmpty(bulldozeRunTrigger))
            animator.SetTrigger(bulldozeRunTrigger);

        if (animator)
            animator.SetBool(runBool, true);

        agent.isStopped = false;
        agent.speed = runSpeed;

        float endTime = Time.time + runDuration;

        while (Time.time < endTime && target != null)
        {
            agent.SetDestination(target.position);
            TryBulldozeDamage();
            yield return null;
        }

        if (animator)
            animator.SetBool(runBool, false);

        bulldozeInProgress = false;

        agent.ResetPath();
        agent.speed = walkSpeed;
        nextRunTime = Time.time + runCooldown;

        yield return StartCoroutine(PostActionIdleRoutine());

        actionRunning = false;
    }

    private void TryBulldozeDamage()
    {
        if (Time.time < nextBulldozeHitTime) return;
        if (!target) return;

        float sqrDist = (target.position - transform.position).sqrMagnitude;
        if (sqrDist > bulldozeHitRadius * bulldozeHitRadius) return;

        IDamageable dmg = target.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.TakeDamage(bulldozeDamage);
            nextBulldozeHitTime = Time.time + bulldozeHitCooldown;
        }
    }

    private IEnumerator SummonRoutine()
    {
        actionRunning = true;
        SetState(BossState.Summon);

        agent.ResetPath();
        agent.isStopped = true;
        FaceTarget();

        if (audioSource && summonCue)
            audioSource.PlayOneShot(summonCue);

        if (animator && !string.IsNullOrEmpty(summonTrigger))
            animator.SetTrigger(summonTrigger);

        yield return new WaitForSeconds(1.2f);

        SpawnSummons();

        nextSummonTime = Time.time + summonCooldown;

        yield return StartCoroutine(PostActionIdleRoutine());
        actionRunning = false;
    }

    private IEnumerator SlamRoutine()
    {
        actionRunning = true;
        slamInProgress = true;

        SetState(BossState.Slam);

        agent.ResetPath();
        agent.isStopped = true;
        FaceTarget();

        if (audioSource && slamCue)
            audioSource.PlayOneShot(slamCue);

        if (animator && !string.IsNullOrEmpty(slamTrigger))
            animator.SetTrigger(slamTrigger);

        if (cameraShake)
            cameraShake.Shake(3f, 2f, 0.3f);

        yield return new WaitForSeconds(1f);

        slamInProgress = false;
        nextSlamTime = Time.time + slamCooldown;

        yield return StartCoroutine(PostActionIdleRoutine());

        actionRunning = false;

        //actionRunning = true;
        //SetState(BossState.Slam);

        //agent.ResetPath();
        //agent.isStopped = true;
        //FaceTarget();

        //if (audioSource && slamCue)
        //    audioSource.PlayOneShot(slamCue);

        //if (animator && !string.IsNullOrEmpty(slamTrigger))
        //    animator.SetTrigger(slamTrigger);

        // If you use Animation Event, remove this wait+call and call FireSlamWaves() from animation.
        //yield return new WaitForSeconds(0.6f);

        //FireSlamWaves();

        //yield return new WaitForSeconds(1f);

        //nextSlamTime = Time.time + slamCooldown;

        //yield return StartCoroutine(PostActionIdleRoutine());
        //actionRunning = false;
    }

    private void SpawnSummons()
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) return;

        Vector3 center = summonCenter ? summonCenter.position : transform.position;

        for (int i = 0; i < summonCount; i++)
        {
            if (TryGetSummonPosition(center, out Vector3 spawnPos))
            {
                GameObject prefab = enemyPrefabs[UnityEngine.Random.Range(0, enemyPrefabs.Length)];
                Instantiate(prefab, spawnPos, Quaternion.identity);
            }
        }
    }

    private void CheckHalfHpPhase()
    {
        if (!useHalfHpPhase) return;
        if (halfHpPhaseActive) return;

        float hpPercent = currentHealth / maxHealth;

        if (hpPercent > halfHpThreshold) return;

        halfHpPhaseActive = true;

        slamCooldown = phaseTwoSlamCooldown;
        runCooldown = phaseTwoRunCooldown;
        runSpeed = phaseTwoRunSpeed;

        waveCount = phaseTwoWaveCount;
        waveSpeed = phaseTwoWaveSpeed;

        if (agent)
            agent.acceleration = phaseTwoAgentAcceleration;

        Debug.Log("Boss entered Phase 2!");
    }

    //Footsteps and Sounds

    public void PlayAudioCue(AudioClip clip, float volume = 1f, bool randomizePitch = false, float pitchMin = 0.9f, float pitchMax = 1.1f)
    {
        if (!audioSource || clip == null) return;

        if (randomizePitch)
            audioSource.pitch = UnityEngine.Random.Range(pitchMin, pitchMax);
        else
            audioSource.pitch = 1f;

        audioSource.PlayOneShot(clip, volume);
    }

    public void PlaySlam()
    {
        if (slamCue == null) return;
        PlayAudioCue(slamCue);
    }

    public void PlayLeftFootstep()
    {
        PlayFootstepFromArray(leftFootstepClips);
    }

    public void PlayRightFootstep()
    {
        PlayFootstepFromArray(rightFootstepClips);
    }

    public void PlayRunFootstep()
    {
        if (runFootstepClip == null) return;
        PlayFootstepClip(runFootstepClip);
    }

    private void PlayFootstepFromArray(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[UnityEngine.Random.Range(0, clips.Length)];
        PlayFootstepClip(clip);
    }

    private void PlayFootstepClip(AudioClip clip)
    {
        if (clip == null) return;

        if (!footstepAudioSource)
            footstepAudioSource = audioSource ? audioSource : GetComponent<AudioSource>();

        if (!footstepAudioSource) return;

        footstepAudioSource.pitch = UnityEngine.Random.Range(footstepPitchMin, footstepPitchMax);
        footstepAudioSource.PlayOneShot(clip, footstepVolume);
    }

    // Can be called by Animation Event on Ground Slam clip
    public void FireSlamWaves()
    {
        if (!waveProjectilePrefab) return;

        Vector3 origin = slamOrigin ? slamOrigin.position : transform.position;
        origin.y = transform.position.y + 0.1f;

        for (int i = 0; i < waveCount; i++)
        {
            float angle = 360f / waveCount * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            dir.y = 0f;
            dir.Normalize();

            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(waveRotationOffset);
            GameObject obj = Instantiate(waveProjectilePrefab, origin, rot);
            BossWaveProjectile wave = obj.GetComponent<BossWaveProjectile>();

            if (wave != null)
            {
                wave.Initialize(dir, waveSpeed, waveDamage, waveLifetime);
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (isDead) return;
        if (amount <= 0f) return;

        bool protectedAction = IsDoingProtectedAction();

        float finalDamage = protectedAction
            ? amount * (1f - actionDamageReduction)
            : amount;

        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);
        OnBossHealthChanged?.Invoke(currentHealth, maxHealth);

        CheckHalfHpPhase();

        TryPlayHitGrunt();
        StartBlink();

        if (currentHealth <= 0f)
        {
            Die();
            return;
        }

        // No interrupt Slam/Summon/Bulldoze with Hit animation
        if (protectedAction)
            return;

        if (animator && !string.IsNullOrEmpty(hitTrigger))
            animator.SetTrigger(hitTrigger);
    }

    private void Die()
    {
        if (isDead) return;

        ClearSlowVisual();

        isDead = true;
        CurrentState = BossState.Death;

        

        StopAllCoroutines();
        SetBlinkVisible(true);

        if (animator)
        {
            animator.SetBool(runBool, false);
            animator.SetFloat(speedParam, 0f);
        }

        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.enabled = false;
        }

        if (audioSource && deathCue)
            audioSource.PlayOneShot(deathCue);

        if (animator && !string.IsNullOrEmpty(deathTrigger))
            animator.SetTrigger(deathTrigger);

        OnBossHealthChanged?.Invoke(0f, maxHealth);

        // Tell GameManager immediately
        OnBossDied?.Invoke();

        // Destroy much later
        Destroy(gameObject, bossDestroyDelay);
    }

    //private IEnumerator DeathSequenceRoutine()
    //{
    //    yield return new WaitForSeconds(deathAnimationDelay);

    //    OnBossDied?.Invoke();

    //    Destroy(gameObject, bossDestroyDelay);
    //}

    private void SetState(BossState state)
    {
        CurrentState = state;
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

    private void UpdateAnimation()
    {
        if (!animator || !agent) return;

        if (CurrentState == BossState.Run || CurrentState == BossState.Slam || CurrentState == BossState.Summon || CurrentState == BossState.Idle)
            return;

        float normalized = agent.velocity.magnitude / Mathf.Max(walkSpeed, 0.001f);
        animator.SetFloat(speedParam, normalized, 0.1f, Time.deltaTime);
    }

    public void ApplySlow(float slowPercent, float duration)
    {
        if (!canBeSlowed) return;
        if (isDead) return;

        if (slowRoutine != null)
            StopCoroutine(slowRoutine);

        slowRoutine = StartCoroutine(SlowRoutine(slowPercent, duration));
    }

    private IEnumerator SlowRoutine(float slowPercent, float duration)
    {
        float finalSlow = Mathf.Clamp01(slowPercent * (1f - slowResistance));

        walkSpeed = originalWalkSpeed * (1f - finalSlow);
        runSpeed = originalRunSpeed * (1f - finalSlow);

        if (agent && CurrentState != BossState.Run)
            agent.speed = walkSpeed;

        ApplySlowVisual();

        yield return new WaitForSeconds(duration);

        walkSpeed = originalWalkSpeed;
        runSpeed = originalRunSpeed;

        if (agent && CurrentState != BossState.Run)
            agent.speed = walkSpeed;

        ClearSlowVisual();

        slowRoutine = null;
    }

    private void ApplySlowVisual()
    {
        originalSlowColors.Clear();

        for (int i = 0; i < slowRenderers.Length; i++)
        {
            Renderer r = slowRenderers[i];
            if (!r) continue;

            Material mat = r.material;

            if (mat.HasProperty("_BaseColor"))
            {
                originalSlowColors.Add(mat.GetColor("_BaseColor"));
                mat.SetColor("_BaseColor", slowTint);
            }
            else if (mat.HasProperty("_Color"))
            {
                originalSlowColors.Add(mat.color);
                mat.color = slowTint;
            }
            else
            {
                originalSlowColors.Add(Color.white);
            }
        }
    }

    private void ClearSlowVisual()
    {
        for (int i = 0; i < slowRenderers.Length; i++)
        {
            Renderer r = slowRenderers[i];
            if (!r) continue;
            if (i >= originalSlowColors.Count) continue;

            Material mat = r.material;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", originalSlowColors[i]);
            else if (mat.HasProperty("_Color"))
                mat.color = originalSlowColors[i];
        }

        originalSlowColors.Clear();
    }

    //Helper

    public void SetWarningReferences(BossWarningVignette vignette)
    {
        warningVignette = vignette;
    }

    private bool IsDoingProtectedAction()
    {
        return CurrentState == BossState.Slam ||
               CurrentState == BossState.Summon ||
               CurrentState == BossState.Run ||
               slamInProgress ||
               bulldozeInProgress;
    }

    private IEnumerator PostActionIdleRoutine()
    {
        SetState(BossState.Idle);

        if (agent)
        {
            agent.ResetPath();
            agent.isStopped = true;
            agent.speed = walkSpeed;
        }

        if (animator && !string.IsNullOrEmpty(idleTrigger))
            animator.SetTrigger(idleTrigger);

        yield return new WaitForSeconds(postActionIdleDuration);

        if (!isDead && agent)
            agent.isStopped = false;
    }

    private bool TryGetSummonPosition(Vector3 center, out Vector3 result)
    {
        for (int i = 0; i < maxSummonAttemptsPerEnemy; i++)
        {
            Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * summonRadius;
            Vector3 randomPoint = center + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                bool blocked = Physics.CheckSphere(
                    hit.position,
                    summonCheckRadius,
                    summonBlockLayers
                );

                if (!blocked)
                {
                    result = hit.position;
                    return true;
                }
            }
        }

        result = center;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = summonCenter ? summonCenter.position : transform.position;

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(center, summonRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, bulldozeTriggerRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, bulldozeHitRadius);
    }

    //Helper hit feedback

    private void TryPlayHitGrunt()
    {
        if (hitGruntClips == null || hitGruntClips.Length == 0) return;
        if (UnityEngine.Random.value > hitGruntChance) return;

        AudioClip clip = hitGruntClips[UnityEngine.Random.Range(0, hitGruntClips.Length)];
        PlayAudioCue(clip, hitGruntVolume, true);
    }

    private void StartBlink()
    {
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        blinkRoutine = StartCoroutine(BlinkRoutine());
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
        if (blinkRenderers == null) return;

        for (int i = 0; i < blinkRenderers.Length; i++)
        {
            if (blinkRenderers[i])
                blinkRenderers[i].enabled = visible;
        }
    }
}
