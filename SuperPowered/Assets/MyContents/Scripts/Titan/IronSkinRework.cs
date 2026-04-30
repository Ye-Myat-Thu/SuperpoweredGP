using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IronSkinRework : MonoBehaviour, ICooldownOverrideable, IUpgradeableAbility, IAbilityCooldown
{
    [Header("References")]
    [SerializeField] private CombatProfile profile;

    [Header("Input")]
    [SerializeField] private KeyCode castKey = KeyCode.E;

    [Header("Level")]
    [SerializeField] private int abilityLevel = 0;
    [SerializeField] private int maxAbilityLevel = 5;
    public int AbilityLevel => abilityLevel;
    public int MaxAbilityLevel => maxAbilityLevel;
    public event System.Action<int> OnAbilityLevelChanged;

    [Header("Cooldown")]
    [SerializeField] private float baseCooldown = 10f;
    public float CooldownDuration => baseCooldown;
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip ironskinClip;

    [Header("Sword Spell")]
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private float activeDuration = 5f;
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float orbitSpeed = 180f;
    [SerializeField] private float swordSelfSpinSpeed = 360f;
    [SerializeField] private float swordDamage = 20f;
    [SerializeField] private Vector3 swordRotationOffset = Vector3.zero;
    [SerializeField] private Vector3 swordScale = Vector3.one;

    private float nextReadyTime;
    private bool isActive;
    private readonly List<OrbitingSword> spawnedSwords = new();

    private bool overrideCooldownEnabled;
    private float overrideCooldownSeconds;

    private void Awake()
    {
        if (!audioSource)
            audioSource = GetComponentInParent<AudioSource>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(castKey))
            Cast();
    }

    public void Cast()
    {
        if (abilityLevel <= 0) return;
        if (isActive) return;
        if (!overrideCooldownEnabled && Time.time < nextReadyTime) return;

        StartCoroutine(SwordRoutine());

        float cd = overrideCooldownEnabled ? overrideCooldownSeconds : baseCooldown;
        nextReadyTime = Time.time + cd;
    }

    private IEnumerator SwordRoutine()
    {
        isActive = true;

        if (audioSource && ironskinClip != null)
            audioSource.PlayOneShot(ironskinClip);

        SpawnSwords();

        yield return new WaitForSeconds(activeDuration);

        ClearSwords();

        isActive = false;
    }

    private void SpawnSwords()
    {
        ClearSwords();

        int swordCount = Mathf.Clamp(abilityLevel, 1, maxAbilityLevel);

        for (int i = 0; i < swordCount; i++)
        {
            float angle = (360f / swordCount) * i;

            GameObject obj = Instantiate(swordPrefab, transform.position, Quaternion.identity);

            obj.transform.localScale = swordScale;

            OrbitingSword sword = obj.GetComponent<OrbitingSword>();
            if (!sword)
                sword = obj.AddComponent<OrbitingSword>();

            LayerMask hitMask = profile ? profile.enemyLayers : ~0;

            sword.Initialize(
                transform,
                orbitRadius,
                orbitSpeed,
                swordSelfSpinSpeed,
                angle,
                swordDamage,
                hitMask,
                swordRotationOffset
            );

            spawnedSwords.Add(sword);
        }
    }

    private void ClearSwords()
    {
        for (int i = spawnedSwords.Count - 1; i >= 0; i--)
        {
            if (spawnedSwords[i])
                Destroy(spawnedSwords[i].gameObject);
        }

        spawnedSwords.Clear();
    }

    public void LevelUpAbility()
    {
        if (abilityLevel >= maxAbilityLevel) return;

        abilityLevel++;

        if (abilityLevel > 1)
        {
            swordDamage += 5f;
            baseCooldown = Mathf.Max(1f, baseCooldown - 0.75f);
            activeDuration += 0.5f;
            orbitSpeed += 25f;
        }

        OnAbilityLevelChanged?.Invoke(abilityLevel);
    }

    public void SetCooldownOverride(bool enabled, float overrideCooldownSeconds)
    {
        overrideCooldownEnabled = enabled;
        this.overrideCooldownSeconds = overrideCooldownSeconds;

        if (enabled)
            nextReadyTime = Time.time;
    }

    public void ForceCast()
    {
        Cast();
    }
}