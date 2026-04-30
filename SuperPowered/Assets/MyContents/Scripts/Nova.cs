using System.Collections;
using UnityEngine;

public class Nova : MonoBehaviour, IUpgradeableAbility, IAbilityCooldown, IIntelligenceScalable
{
    [Header("Nova Settings")]
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private float slowPercent = 0.4f;
    [SerializeField] private float slowDuration = 2f;

    [Header("Intelligence Scaling Debug")]
    [SerializeField] private float intelligenceBonusDamage;
    [SerializeField] private float debugFinalDamage;

    [Header("Input")]
    [SerializeField] private KeyCode castKey = KeyCode.Q;

    [Header("Level Up")]
    [SerializeField] private int abilityLevel = 0;
    [SerializeField] private int maxAbilityLevel = 5;

    public int AbilityLevel => abilityLevel;
    public int MaxAbilityLevel => maxAbilityLevel;
    public event System.Action<int> OnAbilityLevelChanged;

    [Header("Stats Per Level")]
    [SerializeField] private float[] cooldownByLevel = { 15f, 14f, 13f, 12f, 11f };
    [SerializeField] private float[] radiusByLevel = { 4f, 5f, 6f, 7f, 8f };
    [SerializeField] private float[] damageByLevel = { 15f, 17f, 19f, 22f, 25f };

    [Header("UI")]
    public float CooldownDuration => GetCooldown();
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    [Header("VFX")]
    [SerializeField] private ParticleSystem novaVfxPrefab;
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip novaClip;

    private float nextReadyTime;

    private void Awake()
    {
        if (!audioSource)
            audioSource = GetComponentInParent<AudioSource>();
    }

    private void Update()
    {
        if (abilityLevel <= 0) return;

        if (Input.GetKeyDown(castKey))
        {
            CastNova();
        }
    }

    public void SetIntelligenceBonusDamage(float bonusDamage)
    {
        intelligenceBonusDamage = bonusDamage;
        debugFinalDamage = GetDamage();
    }

    public bool CanCast => abilityLevel > 0 && Time.time >= nextReadyTime;

    public void LevelUpAbility()
    {
        if (abilityLevel >= maxAbilityLevel) return;

        abilityLevel++;
        debugFinalDamage = GetDamage();
        OnAbilityLevelChanged?.Invoke(abilityLevel);
    }

    public void CastNova()
    {
        if (!CanCast) return;

        nextReadyTime = Time.time + GetCooldown();

        if (audioSource && novaClip)
            audioSource.PlayOneShot(novaClip);

        SpawnNovaVfx();
        ApplyNovaDamageAndSlow();
    }

    private void ApplyNovaDamageAndSlow()
    {
        float radius = GetRadius();
        float damage = GetDamage();

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, enemyLayers);

        for (int i = 0; i < hits.Length; i++)
        {
            IDamageable dmg = hits[i].GetComponentInParent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(damage);

            ISlowable slowable = hits[i].GetComponentInParent<ISlowable>();
            if (slowable != null)
                slowable.ApplySlow(slowPercent, slowDuration);
        }
    }

    private void SpawnNovaVfx()
    {
        if (!novaVfxPrefab) return;

        ParticleSystem fx = Instantiate(
            novaVfxPrefab,
            transform.position + vfxOffset,
            Quaternion.identity
        );

        fx.transform.localScale = Vector3.one * GetRadius();

        Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
    }

    private int L()
    {
        return Mathf.Clamp(abilityLevel - 1, 0, maxAbilityLevel - 1);
    }

    private float GetCooldown() => cooldownByLevel[L()];
    private float GetRadius() => radiusByLevel[L()];

    private float GetDamage()
    {
        float final = damageByLevel[L()] + intelligenceBonusDamage;
        debugFinalDamage = final;
        return final;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        float radius = abilityLevel > 0
            ? radiusByLevel[Mathf.Clamp(abilityLevel - 1, 0, radiusByLevel.Length - 1)]
            : radiusByLevel[0];

        Gizmos.DrawWireSphere(transform.position, radius);
    }
}