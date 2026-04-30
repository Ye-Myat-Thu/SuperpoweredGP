using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcaneBeam : MonoBehaviour, IUpgradeableAbility, IAbilityCooldown
{
    [Header("Arcane Beam Settings")]
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private Transform castPoint;
    [SerializeField] private float beamLifetime = 0.4f;

    [Header("Input")]
    [SerializeField] private KeyCode castKey = KeyCode.Q;

    [Header("Level Up")]
    [SerializeField] private int abilityLevel = 0;
    [SerializeField] private int maxAbilityLevel = 5;

    public int AbilityLevel => abilityLevel;
    public int MaxAbilityLevel => maxAbilityLevel;
    public event System.Action<int> OnAbilityLevelChanged;

    [Header("Stats Per Level")]
    [SerializeField] private float[] cooldownByLevel = { 10f, 9f, 8f, 7f, 6f };
    [SerializeField] private float[] damageByLevel = { 20f, 22f, 24f, 26f, 30f };

    [Header("UI")]
    public float CooldownDuration => GetCooldown();
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    [Header("VFX")]
    [SerializeField] private GameObject beamVfxPrefab;
    [SerializeField] private Vector3 vfxOffset = Vector3.zero;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip beamClip;

    private float nextReadyTime;
    private bool isCasting;

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
            CastArcaneBeam();
        }
    }

    public bool CanCast => abilityLevel > 0 && Time.time >= nextReadyTime && !isCasting;

    public void LevelUpAbility()
    {
        if (abilityLevel >= maxAbilityLevel) return;

        abilityLevel++;
        OnAbilityLevelChanged?.Invoke(abilityLevel);
    }

    public void CastArcaneBeam()
    {
        if (!CanCast) return;

        nextReadyTime = Time.time + GetCooldown();

        if (audioSource && beamClip)
            audioSource.PlayOneShot(beamClip);

        StartCoroutine(BeamRoutine());
    }

    private IEnumerator BeamRoutine()
    {
        isCasting = true;

        Transform spawn = castPoint ? castPoint : transform;

        GameObject beamObj = null;

        Quaternion rot = spawn.rotation * Quaternion.Euler(0f, -90f, 0f);

        if (beamVfxPrefab)
        {
            beamObj = Instantiate(beamVfxPrefab, spawn.position + spawn.TransformDirection(vfxOffset), rot);
        }

        if (beamObj)
        {
            ArcaneBeamHitbox[] hitboxes = beamObj.GetComponentsInChildren<ArcaneBeamHitbox>(true);

            for (int i = 0; i < hitboxes.Length; i++)
            {
                hitboxes[i].Init(GetDamage(), enemyLayers);
            }

            Destroy(beamObj, beamLifetime);
        }

        yield return new WaitForSeconds(beamLifetime);

        isCasting = false;
    }

    private void DamageEnemiesInsideBeam(GameObject beamObj)
    {
        Collider[] beamColliders = beamObj.GetComponentsInChildren<Collider>(true);
        HashSet<Transform> damagedRoots = new HashSet<Transform>();

        for (int i = 0; i < beamColliders.Length; i++)
        {
            Collider beamCol = beamColliders[i];
            if (!beamCol || !beamCol.isTrigger) continue;

            Collider[] hits = Physics.OverlapBox(
                beamCol.bounds.center,
                beamCol.bounds.extents,
                beamCol.transform.rotation,
                enemyLayers
            );

            for (int h = 0; h < hits.Length; h++)
            {
                Transform root = hits[h].transform.root;

                if (damagedRoots.Contains(root))
                    continue;

                damagedRoots.Add(root);

                IDamageable dmg = hits[h].GetComponentInParent<IDamageable>();
                if (dmg != null)
                    dmg.TakeDamage(GetDamage());
            }
        }
    }

    private int L()
    {
        return Mathf.Clamp(abilityLevel - 1, 0, maxAbilityLevel - 1);
    }

    private float GetCooldown() => cooldownByLevel[L()];
    private float GetDamage() => damageByLevel[L()];
}