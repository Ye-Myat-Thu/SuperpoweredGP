using System.Collections;
using UnityEngine;

public class KnifeFan : MonoBehaviour, IUpgradeableAbility, IAbilityCooldown
{
    [Header("Knife Fan Settings")]
    [SerializeField] private KeyCode castKey = KeyCode.E;
    [SerializeField] private GameObject daggerPrefab;
    [SerializeField] private Transform orbitCenter;

    [Header("Orbit")]
    [SerializeField] private int daggersPerSet = 5;
    [SerializeField] private float orbitRadius = 2f;
    [SerializeField] private float orbitDuration = 4f;
    [SerializeField] private float rotationSpeed = 180f;
    [SerializeField] private float setRadiusSpacing = 1.2f;

    [Header("Damage")]
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float hitCooldownPerEnemy = 0.25f;

    [Header("Level Up")]
    [SerializeField] private int abilityLevel = 0;
    [SerializeField] private int maxAbilityLevel = 5;

    public int AbilityLevel => abilityLevel;
    public int MaxAbilityLevel => maxAbilityLevel;
    public event System.Action<int> OnAbilityLevelChanged;

    [Header("Stats Per Level")]
    [SerializeField] private float[] cooldownByLevel = { 12f, 11.5f, 10.5f, 10f, 12f };
    [SerializeField] private float[] damageByLevel = { 15f, 20f, 25f, 30f, 70f };

    [Header("UI")]
    public float CooldownDuration => GetCooldown();
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip castClip;

    private float nextReadyTime;
    private bool isActive;

    private void Awake()
    {
        if (!orbitCenter)
            orbitCenter = transform;

        if (!audioSource)
            audioSource = GetComponentInParent<AudioSource>();
    }

    private void Update()
    {
        if (abilityLevel <= 0) return;

        if (Input.GetKeyDown(castKey))
        {
            CastKnifeFan();
        }
    }

    public bool CanCast => abilityLevel > 0 && Time.time >= nextReadyTime && !isActive;

    public void LevelUpAbility()
    {
        if (abilityLevel >= maxAbilityLevel) return;

        abilityLevel++;
        OnAbilityLevelChanged?.Invoke(abilityLevel);
    }

    public void CastKnifeFan()
    {
        if (!CanCast) return;
        if (!daggerPrefab) return;

        nextReadyTime = Time.time + GetCooldown();

        if (audioSource && castClip)
            audioSource.PlayOneShot(castClip);

        StartCoroutine(KnifeFanRoutine());
    }

    private IEnumerator KnifeFanRoutine()
    {
        isActive = true;

        int setCount = abilityLevel >= maxAbilityLevel ? 3 : 1;

        for (int setIndex = 0; setIndex < setCount; setIndex++)
        {
            float radius = orbitRadius + (setIndex * setRadiusSpacing);
            SpawnDaggerSet(radius, setIndex);
        }

        yield return new WaitForSeconds(orbitDuration);

        isActive = false;
    }

    private void SpawnDaggerSet(float radius, int setIndex)
    {
        float angleStep = 360f / daggersPerSet;

        GameObject setRoot = new GameObject($"Knife Fan Set {setIndex + 1}");
        setRoot.transform.position = orbitCenter.position;

        KnifeFanOrbitSet orbitSet = setRoot.AddComponent<KnifeFanOrbitSet>();
        orbitSet.Init(
            orbitCenter,
            rotationSpeed,
            orbitDuration
        );

        for (int i = 0; i < daggersPerSet; i++)
        {
            float angle = angleStep * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 spawnPos = orbitCenter.position + dir * radius;

            GameObject dagger = Instantiate(daggerPrefab, spawnPos, Quaternion.LookRotation(dir), setRoot.transform);

            KnifeFanDaggerHitbox hitbox = dagger.GetComponent<KnifeFanDaggerHitbox>();

            if (!hitbox)
                hitbox = dagger.AddComponent<KnifeFanDaggerHitbox>();

            hitbox.Init(GetDamage(), enemyLayers, hitCooldownPerEnemy);
        }

        Destroy(setRoot, orbitDuration + 0.1f);
    }

    private int L()
    {
        return Mathf.Clamp(abilityLevel - 1, 0, maxAbilityLevel - 1);
    }

    private float GetCooldown() => cooldownByLevel[L()];
    private float GetDamage() => damageByLevel[L()];
}