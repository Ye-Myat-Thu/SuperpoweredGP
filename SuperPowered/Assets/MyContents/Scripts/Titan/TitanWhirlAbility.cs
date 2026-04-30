using UnityEngine;
using UnityEngine.AI;

public interface IKnockbackable
{
    void ApplyKnockback(UnityEngine.Vector3 direction, float force, float duration);
}

public class TitanWhirlAbility : MonoBehaviour, ICooldownOverrideable, IUpgradeableAbility, IAbilityCooldown
{

    
    [Header("References")]
    [SerializeField] private CombatProfile profile;

    [Header("Input")]
    [SerializeField] private KeyCode castKey = KeyCode.Q;

    [Header("UI")]
    public float CooldownDuration => baseCooldown;
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    [Header("Level Up")]
    [SerializeField] private int abilityLevel = 0;
    [SerializeField] private int maxAbilityLevel = 5;
    public int AbilityLevel => abilityLevel;
    public int MaxAbilityLevel => maxAbilityLevel;
    public event System.Action<int> OnAbilityLevelChanged;

    [Header("Whirl Settings")]
    [SerializeField] private float baseCooldown = 10f;
    [SerializeField] private float activeDuration = 2f;
    [SerializeField] private float damageTickInterval = 0.4f;
    [SerializeField] private float whirlRadius = 2.5f;
    [SerializeField] private float whirlDamage = 20f;
    [SerializeField] private Vector3 whirlOffset = Vector3.zero;

    [Header("Knockback")]
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private float knockbackDuration = 0.2f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip whirlClip;

    [Header("VFX")]
    [SerializeField] private ParticleSystem swirlPrefab;
    [SerializeField] private Transform vfxSpawnPoint;
    [SerializeField] private bool spawnVfxOnDamageTick = true;
    [SerializeField] private float vfxLifetime = 2f;
    [SerializeField] private bool scaleVfxWithRadius = true;
    [SerializeField] private float vfxRadiusScaleMultiplier = 1f;
    [SerializeField] private Vector3 baseVfxScale = Vector3.one;

    public bool IsWhirling { get; private set; }

    private float endTime;
    private float nextDamageTick;
    private ParticleSystem activeVFX;

    //cooldown state
    private float nextReadyTime;

    //Override
    private bool overrideCooldownEnabled;
    private float overrideCooldownSeconds;

    public void SetCooldownOverride(bool enabled, float overrideCooldownSeconds)
    {
        overrideCooldownEnabled = enabled;
        this.overrideCooldownSeconds = overrideCooldownSeconds;

        if (enabled)
            nextReadyTime = Time.time;
    }

    public void ForceCast() => Cast();

    public void Cast()
    {
        //if (IsWhirling) return;

        //float cd = overrideCooldownEnabled ? overrideCooldownSeconds : baseCooldown;
        //if (Time.time < nextReadyTime) return;

        //StartWhirl();
        //nextReadyTime = Time.time + cd;

        if (abilityLevel <= 0) return;

        if (IsWhirling) return;

        if (!overrideCooldownEnabled && Time.time < nextReadyTime) return;

        StartWhirl();

        float cd = overrideCooldownEnabled ? overrideCooldownSeconds : baseCooldown;
        nextReadyTime = Time.time + cd;
    }

    private void Awake()
    {
        if (!audioSource)
            audioSource = GetComponentInParent<AudioSource>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(castKey))
        {
            Cast();
        }

        if (!IsWhirling) return;

        if (Time.time >= nextDamageTick)
        {
            nextDamageTick = Time.time + damageTickInterval;
            DealWhirlDamage();

            if (spawnVfxOnDamageTick)
                SpawnWhirlVFXPulse();
        }

        if (Time.time >= endTime)
        {
            EndWhirl();
        }
    }

    public void LevelUpAbility()
    {
        if (abilityLevel >= MaxAbilityLevel) return;

        abilityLevel++;

        if (abilityLevel > 1)
        {
            whirlDamage += 5f;
            baseCooldown = Mathf.Max(0.5f, baseCooldown - 1.5f);
            whirlRadius += 2.5f;
            activeDuration += 0.45f;
            damageTickInterval = Mathf.Max(0.05f, damageTickInterval - 0.05f);
        }

        OnAbilityLevelChanged?.Invoke(abilityLevel);

        //===== old =====//
        //if (abilityLevel >= MaxAbilityLevel) return;
        //abilityLevel++;
        //OnAbilityLevelChanged?.Invoke(abilityLevel);

        //whirlDamage += 5f;
        //baseCooldown -= 1.5f;
        //whirlRadius += 2.5f;
        //activeDuration += 0.45f;
        //damageTickInterval -= 0.25f;
    }

    private void StartWhirl()
    {
        IsWhirling = true;
        endTime = Time.time + activeDuration;
        
        

        nextDamageTick = Time.time;
    }

    private void EndWhirl()
    {
        IsWhirling = false;
    }

    private void SpawnWhirlVFXPulse()
    {
        if (!swirlPrefab) return;

        Transform spawnRef = vfxSpawnPoint ? vfxSpawnPoint : transform;

        ParticleSystem vfx = Instantiate(swirlPrefab, spawnRef.position, spawnRef.rotation, spawnRef);

        if (scaleVfxWithRadius)
        {
            float scale = whirlRadius * vfxRadiusScaleMultiplier;
            vfx.transform.localScale = Vector3.one * scale;
        }
        else
        {
            vfx.transform.localScale = baseVfxScale;
        }

        if (audioSource && whirlClip != null)
        {
            audioSource.PlayOneShot(whirlClip);
        }

        Destroy(vfx.gameObject, vfxLifetime);
    }

    private void DealWhirlDamage()
    {
        Vector3 center = transform.TransformPoint(whirlOffset);

        LayerMask hitMask = profile ? profile.enemyLayers : ~0;
        Collider[] hits = Physics.OverlapSphere(center, whirlRadius, hitMask);

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;

            IDamageable dmg = hitTransform.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(whirlDamage);
            }

            IKnockbackable knock = hitTransform.GetComponentInParent<IKnockbackable>();
            if (knock != null)
            {
                Vector3 dir = (hitTransform.position - transform.position);
                dir.y = 0f;

                if (dir.sqrMagnitude > 0.001f)
                {
                    knock.ApplyKnockback(dir.normalized, knockbackForce, knockbackDuration);
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = transform.TransformPoint(whirlOffset);
        Gizmos.DrawWireSphere(center, whirlRadius);
    }
}

//Auto cast
//[Header("References")]
//[SerializeField] private CombatProfile profile;

//[Header("Whirl Settings")]
//[SerializeField] private float baseCooldown = 10f;
//[SerializeField] private float activeDuration = 2f;
//[SerializeField] private float damageTickInterval = 0.4f;
//[SerializeField] private float whirlRadius = 2.5f;
//[SerializeField] private float whirlDamage = 20f;
//[SerializeField] private Vector3 whirlOffset = Vector3.zero;

//[Header("Knockback")]
//[SerializeField] private float knockbackForce = 4f;
//[SerializeField] private float knockbackDuration = 0.2f;

//[Header("VFX")]
//[SerializeField] private ParticleSystem swirlPrefab;
//[SerializeField] private Transform vfxSpawnPoint;

//public bool IsWhirling { get; private set; }

//private float nextCastTime;
//private float endTime;
//private float nextDamageTick;
//private ParticleSystem activeVFX;

////---------------------------------
//[Header("Cooldown override interface")]
//[SerializeField] private float cooldown = 10f;
//private float nextReadyTime;
//private bool overrideCooldownEnabled;
//private float overrideCooldownSeconds;

//public void SetCooldownOverride(bool enabled, float overrideCooldownSeconds)
//{
//    overrideCooldownEnabled = enabled;
//    this.overrideCooldownSeconds = overrideCooldownSeconds;
//}

//public void ForceCast()
//{
//    TryCast();
//}

//public void TryCast()
//{
//    float cd = overrideCooldownEnabled ? overrideCooldownSeconds : cooldown;
//    if (Time.time < nextReadyTime) return;

//    if (!IsWhirling)
//    {
//        if (Time.time >= nextCastTime)
//        {
//            StartWhirl();
//        }
//        return;
//    }

//    // While active, keep dealing damage in ticks
//    if (Time.time >= nextDamageTick)
//    {
//        nextDamageTick = Time.time + damageTickInterval;
//        DealWhirlDamage();
//    }

//    // End after duration
//    if (Time.time >= endTime)
//    {
//        EndWhirl();
//    }

//    nextReadyTime = Time.time + cd;
//}
////---------------------------------

//private void Start()
//{
//    nextCastTime = Time.time + baseCooldown;
//}

//private void Update()
//{
//    if (!IsWhirling)
//    {
//        if (Time.time >= nextCastTime)
//        {
//            StartWhirl();
//        }
//        return;
//    }

//    // While active, keep dealing damage in ticks
//    if (Time.time >= nextDamageTick)
//    {
//        nextDamageTick = Time.time + damageTickInterval;
//        DealWhirlDamage();
//    }

//    // End after duration
//    if (Time.time >= endTime)
//    {
//        EndWhirl();
//    }
//}

//private void StartWhirl()
//{
//    IsWhirling = true;
//    endTime = Time.time + activeDuration;
//    nextDamageTick = Time.time; // damage immediately on cast
//    nextCastTime = Time.time + baseCooldown;

//    SpawnWhirlVFX();
//}

//private void EndWhirl()
//{
//    IsWhirling = false;

//    if (activeVFX != null)
//    {
//        Destroy(activeVFX.gameObject);
//        activeVFX = null;
//    }
//}

//private void SpawnWhirlVFX()
//{
//    if (!swirlPrefab) return;
//    if (activeVFX != null) return;

//    Transform spawnRef = vfxSpawnPoint ? vfxSpawnPoint : transform;
//    activeVFX = Instantiate(swirlPrefab, spawnRef.position, spawnRef.rotation, spawnRef);
//}

//private void DealWhirlDamage()
//{
//    Vector3 center = transform.TransformPoint(whirlOffset);

//    LayerMask hitMask = profile ? profile.enemyLayers : ~0;
//    Collider[] hits = Physics.OverlapSphere(center, whirlRadius, hitMask);

//    for (int i = 0; i < hits.Length; i++)
//    {
//        Transform hitTransform = hits[i].transform;

//        IDamageable dmg = hitTransform.GetComponentInParent<IDamageable>();
//        if (dmg != null)
//        {
//            dmg.TakeDamage(whirlDamage);
//        }

//        IKnockbackable knock = hitTransform.GetComponentInParent<IKnockbackable>();
//        if (knock != null)
//        {
//            Vector3 dir = (hitTransform.position - transform.position);
//            dir.y = 0f;

//            if (dir.sqrMagnitude > 0.001f)
//            {
//                knock.ApplyKnockback(dir.normalized, knockbackForce, knockbackDuration);
//            }
//        }
//    }
//}

//private void OnDrawGizmosSelected()
//{
//    Gizmos.color = Color.cyan;
//    Vector3 center = transform.TransformPoint(whirlOffset);
//    Gizmos.DrawWireSphere(center, whirlRadius);
//}
