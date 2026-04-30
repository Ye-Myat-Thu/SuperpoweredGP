using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UniversalSpellSet : MonoBehaviour
{
    public enum SpellType
    {
        BlizzardRain,
        DragonsBreath,
        Enlightenment,
        Cataclysm
    }

    [Header("Equip State")]
    [SerializeField] private bool blizzardEquipped;
    [SerializeField] private bool breathEquipped;
    [SerializeField] private bool enlightenmentEquipped;
    [SerializeField] private bool cataclysmEquipped;

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Input Keys")]
    [SerializeField] private KeyCode equippedSpellKey = KeyCode.R;

    [Header("Raycast")]
    [SerializeField] private float maxRayDistance = 500f;

    [Header("Preview Prefabs (optional)")]
    [SerializeField] private GameObject aoeCirclePreviewPrefab; // flat circle mesh
    [SerializeField] private GameObject conePreviewPrefab;      // flat cone mesh

    [Header("VFX Prefabs (optional)")]
    [SerializeField] private ParticleSystem blizzardVfxPrefab;
    [SerializeField] private ParticleSystem breathVfxPrefab;

    [Header("Blizzard VFX")]
    [SerializeField] private Vector3 blizzardVfxScale = Vector3.one;
    [SerializeField] private Vector3 blizzardVfxOffset = Vector3.zero;
    [SerializeField] private int blizzardVfxSpawnCount = 12;
    [SerializeField] private float blizzardVfxSpawnInterval = 0.25f;
    [SerializeField] private bool blizzardVfxRandomRotation = true;

    [Header("Cataclysm VFX")]
    [SerializeField] private ParticleSystem cataclysmImplosionVfxPrefab;
    [SerializeField] private ParticleSystem cataclysmLightningVfxPrefab1;
    [SerializeField] private ParticleSystem cataclysmLightningVfxPrefab2;

    [SerializeField] private float cataclysmImplosionDuration = 0.8f;
    [SerializeField] private float cataclysmLightning1Duration = 0.1f;
    [SerializeField] private float cataclysmLightning2Duration = 1f;

    [SerializeField] private Vector3 cataclysmLightningScale = new Vector3(50f, 50f, 50f);
    [SerializeField] private Vector3 cataclysmVfxOffset = Vector3.zero;
    [SerializeField] private bool useLocalOffset = true;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip blizzardClip;
    [SerializeField] private AudioClip breathClip;
    [SerializeField] private AudioClip enlightenmentClip;
    [SerializeField] private AudioClip cataclysmFirstClip;
    [SerializeField] private AudioClip cataclysmSecondClip;

    [Header("Dragon's Breath Attach Point (optional)")]
    [SerializeField] private Transform mouthPoint; // assign head/mouth transform

    [Header("Spell Levels (1-3)")]
    [Range(1, 3)][SerializeField] private int blizzardLevel = 1;
    [Range(1, 3)][SerializeField] private int breathLevel = 1;
    [Range(1, 3)][SerializeField] private int enlightenmentLevel = 1;
    [Range(1, 3)][SerializeField] private int cataclysmLevel = 1;

    // -----------------------------
    // 1) Blizzard Rain (AOE point)
    // -----------------------------
    [Header("Blizzard Rain Stats (per level)")]
    [SerializeField] private float[] blizzardCooldown = { 20f, 16f, 12f };
    [SerializeField] private float[] blizzardRadius = { 5f, 9f, 15f };
    [SerializeField] private float[] blizzardDps = { 40f, 60f, 100f };     // per second
    [SerializeField] private float[] blizzardDuration = { 3f, 4f, 5f };
    [SerializeField] private float blizzardTickInterval = 0.25f;

    [Header("Blizzard Slow")]
    [SerializeField, Range(0f, 1f)] private float blizzardSlowPercent = 0.4f;
    [SerializeField] private float blizzardSlowDuration = 2f;

    private float blizzardNextReady;
    private bool blizzardAiming;
    private Vector3 blizzardPoint;
    private GameObject blizzardPreview;

    // ---------------------------------
    // 2) Dragon's Breath (cone AOE)
    // ---------------------------------
    [Header("Dragon's Breath Stats (per level)")]
    [SerializeField] private float[] breathCooldown = { 30f, 25f, 16f };
    [SerializeField] private float[] breathDps = { 60f, 100f, 200f };      // per second
    [SerializeField] private float[] breathDuration = { 3f, 4f, 5f };

    [SerializeField] private Vector3 breathBaseDamageBoxSize = Vector3.one;
    [SerializeField] private Vector3 breathBaseVfxScale = Vector3.one;
    [SerializeField] private float breathScaleIncreasePerLevel = 3f;
    //[SerializeField] private float breathRange = 10f;
    //[SerializeField] private float breathAngle = 45f;                      // half-angle
    [SerializeField] private float breathTickInterval = 0.1f;

    private float breathNextReady;
    private bool breathAiming;
    private Vector3 breathAimPoint;
    private GameObject breathPreview;

    // ---------------------------------
    // 3) Enlightenment (unit target)
    // ---------------------------------
    [Header("Enlightenment Stats (per level)")]
    [SerializeField] private int[] enlightenmentMaxCharges = { 1, 2, 3 };
    [SerializeField] private float enlightenmentCooldownPerCharge = 40f;   // fixed per charge
    [SerializeField] private float enlightenmentCastRange = 20f;
    [SerializeField] private Color brainwashedTint = Color.green;

    private int enlightenmentCharges;
    private float enlightenmentNextChargeTime;

    // ---------------------------------
    // 4) Cataclysm (global instant)
    // ---------------------------------
    [Header("Cataclysm Stats (per level)")]
    [SerializeField] private float[] cataclysmCooldown = { 110f, 105f, 100f };
    [SerializeField] private float[] cataclysmDamage = { 200f, 250f, 300f };
    [SerializeField] private float cataclysmGlobalRadius = 9999f; // big overlap sphere

    private float cataclysmNextReady;

    // -----------------------------
    private void Start()
    {
        // Enlightenment charges init
        enlightenmentCharges = GetEnlightenmentMaxCharges();
        enlightenmentNextChargeTime = 0f;
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        // Recharge enlightenment charges over time
        UpdateEnlightenmentCharges();

        // Spell key handling
        //if (Input.GetKeyDown(blizzardKey)) ToggleAim(SpellType.BlizzardRain);
        //if (Input.GetKeyDown(breathKey)) ToggleAim(SpellType.DragonsBreath);

        //if (Input.GetKeyDown(enlightenmentKey)) TryCastEnlightenment();
        //if (Input.GetKeyDown(cataclysmKey)) TryCastCataclysm();

        if (Input.GetKeyDown(equippedSpellKey))
        {
            CastEquippedSpell();
        }

        // Aiming updates
        if (blizzardAiming) UpdateBlizzardAim();
        if (breathAiming) UpdateBreathAim();

        if (enlightenmentTargeting)
        {
            UpdateEnlightenmentTargeting();
        }
    }

    private void CastEquippedSpell()
    {
        if (blizzardEquipped)
        {
            ToggleAim(SpellType.BlizzardRain);
            return;
        }

        if (breathEquipped)
        {
            //ToggleAim(SpellType.DragonsBreath);
            TryCastBreath();
            return;
        }

        if (enlightenmentEquipped)
        {
            BeginEnlightenmentTargeting();
            return;
        }

        if (cataclysmEquipped)
        {
            TryCastCataclysm();
            return;
        }
    }

    // =========================
    // Aiming toggles
    // =========================
    private void ToggleAim(SpellType spell)
    {
        if (spell == SpellType.BlizzardRain)
        {
            if (!IsReady(blizzardNextReady)) return;

            blizzardAiming = !blizzardAiming;
            if (blizzardAiming) BeginBlizzardAim();
            else EndBlizzardAim();
        }
        else if (spell == SpellType.DragonsBreath)
        {
            if (!IsReady(breathNextReady)) return;

            breathAiming = !breathAiming;
            if (breathAiming) BeginBreathAim();
            else EndBreathAim();
        }
    }

    // =========================
    // 1) Blizzard Rain
    // =========================
    private void BeginBlizzardAim()
    {
        if (aoeCirclePreviewPrefab && blizzardPreview == null)
            blizzardPreview = Instantiate(aoeCirclePreviewPrefab);
    }

    private void EndBlizzardAim()
    {
        blizzardAiming = false;
        if (blizzardPreview) Destroy(blizzardPreview);
        blizzardPreview = null;
    }

    private void UpdateBlizzardAim()
    {
        if (TryGetGroundPoint(out Vector3 p))
        {
            blizzardPoint = p;

            if (blizzardPreview)
            {
                blizzardPreview.transform.position = p + Vector3.up * 0.05f;
                float r = GetBlizzardRadius();
                blizzardPreview.transform.localScale = new Vector3(r * 2f, 1f, r * 2f);
            }
        }

        // left click confirm, right click cancel
        if (Input.GetMouseButtonDown(0))
        {
            TryCastBlizzard();
            EndBlizzardAim();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            EndBlizzardAim();
        }
    }

    private void TryCastBlizzard()
    {
        if (!IsReady(blizzardNextReady)) return;

        blizzardNextReady = Time.time + GetBlizzardCooldown();

        StartCoroutine(PlayLoopedAudioForDuration(blizzardClip, GetBlizzardDuration()));

        // VFX
        if (blizzardVfxPrefab)
        {
            StartCoroutine(BlizzardVfxSpawnRoutine(blizzardPoint, GetBlizzardRadius(), GetBlizzardDuration()));
        }

        //if (blizzardVfxPrefab)
        //{
        //    Vector3 spawnPos = blizzardPoint + blizzardVfxOffset;

        //    ParticleSystem fx = Instantiate(blizzardVfxPrefab, spawnPos, Quaternion.identity);
        //    fx.transform.localScale = blizzardVfxScale;
        //}

        // Damage over time
        StartCoroutine(BlizzardDamageRoutine(blizzardPoint, GetBlizzardRadius(), GetBlizzardDps(), GetBlizzardDuration()));
    }

    private IEnumerator BlizzardVfxSpawnRoutine(Vector3 center, float radius, float duration)
    {
        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            for (int i = 0; i < blizzardVfxSpawnCount; i++)
            {
                SpawnSingleBlizzardVfx(center, radius);
            }

            yield return new WaitForSeconds(blizzardVfxSpawnInterval);
        }
    }

    private void SpawnSingleBlizzardVfx(Vector3 center, float radius)
    {
        if (!blizzardVfxPrefab) return;

        Vector2 randomCircle = Random.insideUnitCircle * radius;
        Vector3 spawnPos = center + new Vector3(randomCircle.x, 0f, randomCircle.y);
        spawnPos += blizzardVfxOffset;

        Quaternion rot = blizzardVfxRandomRotation
            ? Quaternion.Euler(0f, Random.Range(0f, 360f), 0f)
            : Quaternion.identity;

        ParticleSystem fx = Instantiate(blizzardVfxPrefab, spawnPos, rot);
        fx.transform.localScale = blizzardVfxScale;

        Destroy(fx.gameObject, GetParticleSystemTotalDuration(fx));
    }

    private float GetParticleSystemTotalDuration(ParticleSystem ps)
    {
        if (!ps) return 2f;

        ParticleSystem.MainModule main = ps.main;

        float maxLifetime = 0f;

        if (main.startLifetime.mode == ParticleSystemCurveMode.Constant)
        {
            maxLifetime = main.startLifetime.constant;
        }
        else if (main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants)
        {
            maxLifetime = main.startLifetime.constantMax;
        }
        else
        {
            maxLifetime = main.startLifetime.constantMax;
        }

        return main.duration + maxLifetime;
    }

    private IEnumerator BlizzardDamageRoutine(Vector3 center, float radius, float dps, float duration)
    {
        float end = Time.time + duration;

        while (Time.time < end)
        {
            float tick = Mathf.Min(blizzardTickInterval, end - Time.time);
            float damageThisTick = dps * tick;

            Collider[] hits = Physics.OverlapSphere(center, radius, enemyLayers);
            for (int i = 0; i < hits.Length; i++)
            {
                IDamageable dmg = hits[i].GetComponentInParent<IDamageable>();
                if (dmg != null) dmg.TakeDamage(damageThisTick);

                ISlowable slowable = hits[i].GetComponentInParent<ISlowable>();
                if (slowable != null)
                    slowable.ApplySlow(blizzardSlowPercent, blizzardSlowDuration);
            }

            yield return new WaitForSeconds(tick);
        }
    }

    // =========================
    // 2) Dragon's Breath
    // =========================
    private void BeginBreathAim()
    {
        if (conePreviewPrefab && breathPreview == null)
            breathPreview = Instantiate(conePreviewPrefab);
    }

    private void EndBreathAim()
    {
        breathAiming = false;
        if (breathPreview) Destroy(breathPreview);
        breathPreview = null;
    }

    private void UpdateBreathAim()
    {
        // We aim by mouse direction on ground: from player -> mouse ground point
        if (TryGetGroundPoint(out Vector3 p))
        {
            breathAimPoint = p;

            Vector3 dir = (p - transform.position);
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion rot = Quaternion.LookRotation(dir.normalized, Vector3.up);

                if (breathPreview)
                {
                    breathPreview.transform.position = transform.position + Vector3.up * 0.05f;
                    breathPreview.transform.rotation = rot;

                    // Scale preview: you can tune preview mesh size here
                    Vector3 previewScale = GetBreathDamageBoxSize();
                    breathPreview.transform.localScale = previewScale;
                }
            }
        }

        // left click confirm, right click cancel
        if (Input.GetMouseButtonDown(0))
        {
            TryCastBreath();
            EndBreathAim();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            EndBreathAim();
        }
    }

    private void TryCastBreath()
    {
        if (!IsReady(breathNextReady)) return;

        breathNextReady = Time.time + GetBreathCooldown();

        StartCoroutine(PlayLoopedAudioForDuration(breathClip, GetBreathDuration()));

        // Direction from player to aimed point
        Vector3 dir = (breathAimPoint - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        dir.Normalize();

        // VFX from mouth/head if provided, else from player
        Transform spawn = mouthPoint ? mouthPoint : transform;
        if (breathVfxPrefab)
        {
            ParticleSystem fx = Instantiate(breathVfxPrefab, spawn.position, spawn.rotation, spawn);

            fx.transform.localScale = GetBreathVfxScale();
            //fx.transform.localScale = new Vector3(width, width, length);

            Destroy(fx.gameObject, GetBreathDuration() + 0.25f);
        }

        // Damage over time in cone
        StartCoroutine(BreathDamageRoutine(GetBreathDps(), GetBreathDuration()));
    }

    private IEnumerator BreathDamageRoutine(float dps, float duration)
    {
        float end = Time.time + duration;

        while (Time.time < end)
        {
            float tick = Mathf.Min(breathTickInterval, end - Time.time);
            float damageThisTick = dps * tick;

            // ALWAYS get fresh direction from mouth
            Transform originT = mouthPoint ? mouthPoint : transform;

            Vector3 origin = originT.position;
            Vector3 forwardDir = originT.forward;

            Vector3 boxSize = GetBreathDamageBoxSize();

            float length = boxSize.z;
            Vector3 center = origin + forwardDir * (length * 0.5f);
            Quaternion rotation = Quaternion.LookRotation(forwardDir, Vector3.up);

            Vector3 halfExtents = boxSize * 0.5f;

            Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, enemyLayers);

            HashSet<Transform> hitRoots = new HashSet<Transform>();

            for (int i = 0; i < hits.Length; i++)
            {
                Transform root = hits[i].transform.root;

                if (hitRoots.Contains(root)) continue;
                hitRoots.Add(root);

                IDamageable dmg = root.GetComponentInChildren<IDamageable>();
                if (dmg != null)
                {
                    dmg.TakeDamage(damageThisTick);
                }
            }

            yield return new WaitForSeconds(tick);
        }
    }

    // =========================
    // 3) Enlightenment (unit target + charges)
    // =========================

    [SerializeField] private Texture2D enlightenmentCursor;
    [SerializeField] private Vector2 enlightenmentCursorHotspot = Vector2.zero;

    [Header("Enlightenment Targeting Visuals")]
    [SerializeField] private Color enlightenmentHoverColor = Color.cyan;
    [SerializeField] private ParticleSystem enlightenmentCastVfxPrefab;
    [SerializeField] private Vector3 enlightenmentVfxOffset = Vector3.zero;

    private BaseEnemy hoveredEnlightenmentEnemy;
    private Renderer[] hoveredRenderers;
    private readonly List<Color> hoveredOriginalColors = new List<Color>();

    private bool enlightenmentTargeting;

    private void UpdateEnlightenmentCharges()
    {
        int maxCharges = GetEnlightenmentMaxCharges();

        // If level increased, clamp charges to new max (don’t auto-fill)
        enlightenmentCharges = Mathf.Clamp(enlightenmentCharges, 0, maxCharges);

        if (enlightenmentCharges >= maxCharges) return;

        if (Time.time >= enlightenmentNextChargeTime)
        {
            enlightenmentCharges++;
            if (enlightenmentCharges < maxCharges)
                enlightenmentNextChargeTime = Time.time + enlightenmentCooldownPerCharge;
        }
    }

    private void TryCastEnlightenment()
    {
        if (enlightenmentCharges <= 0) return;

        // Unit target under mouse
        if (!TryGetEnlightenmentTarget(out BaseEnemy targetEnemy))
            return;

        // Check range
        float dist = Vector3.Distance(transform.position, targetEnemy.transform.position);
        if (dist > enlightenmentCastRange)
            return;

        // Apply "brainwash" mark (prototype)
        BrainwashedUnit bw = targetEnemy.GetComponent<BrainwashedUnit>();
        if (bw == null)
            bw = targetEnemy.gameObject.AddComponent<BrainwashedUnit>();

        ClearEnlightenmentHover();
        SpawnEnlightenmentVfx(targetEnemy.transform.position);

        PlayOneShot(enlightenmentClip);

        bw.ApplyBrainwash(brainwashedTint);

        // Spend a charge + start recharge timer if needed
        enlightenmentCharges--;

        if (enlightenmentCharges < GetEnlightenmentMaxCharges())
            enlightenmentNextChargeTime = Time.time + enlightenmentCooldownPerCharge;

        EndEnlightenmentTargeting();
    }

    // Simple marker component for now
    public class Brainwashed : MonoBehaviour
    {
        private Renderer[] rends;
        private Color original;

        public void Apply(Color tint)
        {
            if (rends == null || rends.Length == 0)
                rends = GetComponentsInChildren<Renderer>(true);

            // Tint all renderers (basic)
            for (int i = 0; i < rends.Length; i++)
            {
                if (!rends[i]) continue;
                if (rends[i].material.HasProperty("_Color"))
                    rends[i].material.color = tint;
            }

            // NOTE: enemy target-priority behavior comes next (you said later)
        }
    }

    private void BeginEnlightenmentTargeting()
    {
        if (enlightenmentCharges <= 0) return;

        enlightenmentTargeting = true;
        Cursor.SetCursor(enlightenmentCursor, enlightenmentCursorHotspot, CursorMode.Auto);
    }

    private void EndEnlightenmentTargeting()
    {
        ClearEnlightenmentHover();

        enlightenmentTargeting = false;
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    private void UpdateEnlightenmentTargeting()
    {
        UpdateEnlightenmentHover();

        if (Input.GetMouseButtonDown(1))
        {
            ClearEnlightenmentHover();
            EndEnlightenmentTargeting();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryCastEnlightenment();
        }
    }

    private void UpdateEnlightenmentHover()
    {
        if (TryGetEnlightenmentTarget(out BaseEnemy targetEnemy))
        {
            if (hoveredEnlightenmentEnemy != targetEnemy)
            {
                ClearEnlightenmentHover();
                SetEnlightenmentHover(targetEnemy);
            }
        }
        else
        {
            ClearEnlightenmentHover();
        }
    }

    private void SetEnlightenmentHover(BaseEnemy enemy)
    {
        if (enemy == null) return;

        hoveredEnlightenmentEnemy = enemy;
        hoveredRenderers = enemy.GetComponentsInChildren<Renderer>(true);
        hoveredOriginalColors.Clear();

        for (int i = 0; i < hoveredRenderers.Length; i++)
        {
            Renderer r = hoveredRenderers[i];
            if (!r) continue;

            Material mat = r.material;

            if (mat.HasProperty("_Color"))
            {
                hoveredOriginalColors.Add(mat.color);
                mat.color = enlightenmentHoverColor;
            }
            else
            {
                hoveredOriginalColors.Add(Color.white);
            }
        }
    }

    private void ClearEnlightenmentHover()
    {
        if (hoveredRenderers != null)
        {
            for (int i = 0; i < hoveredRenderers.Length; i++)
            {
                Renderer r = hoveredRenderers[i];
                if (!r) continue;
                if (i >= hoveredOriginalColors.Count) continue;

                Material mat = r.material;

                if (mat.HasProperty("_Color"))
                    mat.color = hoveredOriginalColors[i];
            }
        }

        hoveredEnlightenmentEnemy = null;
        hoveredRenderers = null;
        hoveredOriginalColors.Clear();
    }

    private void SpawnEnlightenmentVfx(Vector3 position)
    {
        if (!enlightenmentCastVfxPrefab) return;

        ParticleSystem fx = Instantiate(enlightenmentCastVfxPrefab, position + enlightenmentVfxOffset, Quaternion.identity);

        Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax);
    }

    // =========================
    // 4) Cataclysm (global instant)
    // =========================
    private void TryCastCataclysm()
    {
        if (!IsReady(cataclysmNextReady)) return;

        cataclysmNextReady = Time.time + GetCataclysmCooldown();
        StartCoroutine(CataclysmSequenceRoutine());
    }

    private IEnumerator CataclysmSequenceRoutine()
    {
        Vector3 castPosition = GetCataclysmVfxSpawnPosition();
        Quaternion castRotation = transform.rotation;
        
        //Impl seq
        if (cataclysmImplosionVfxPrefab != null)
        {
            ParticleSystem implosion = Instantiate(cataclysmImplosionVfxPrefab, castPosition, castRotation);
            Destroy(implosion.gameObject, cataclysmImplosionDuration);
        }

        PlayOneShot(cataclysmFirstClip);

        yield return new WaitForSeconds(cataclysmImplosionDuration);

        //light1 seq
        if (cataclysmLightningVfxPrefab1 != null)
        {
            ParticleSystem lightning1 = Instantiate(cataclysmLightningVfxPrefab1, castPosition, castRotation);
            lightning1.transform.localScale = cataclysmLightningScale;
            Destroy(lightning1.gameObject, cataclysmLightning1Duration);
        }

        if (cataclysmLightningVfxPrefab2 != null)
        {
            ParticleSystem lightning2 = Instantiate(cataclysmLightningVfxPrefab2, castPosition, castRotation);
            lightning2.transform.localScale = cataclysmLightningScale;
            Destroy(lightning2.gameObject, cataclysmLightning2Duration);
        }

        PlayOneShot(cataclysmSecondClip);

        //Damage
        ApplyCataclysmDamage();
    }

    private void ApplyCataclysmDamage()
    {
        float dmgAmount = GetCataclysmDamage();

        Collider[] hits = Physics.OverlapSphere(transform.position, cataclysmGlobalRadius, enemyLayers);

        HashSet<Transform> hitRoots = new HashSet<Transform>();

        for (int i = 0; i < hits.Length; i++)
        {
            Transform root = hits[i].transform.root;
            if (hitRoots.Contains(root)) continue;
            hitRoots.Add(root);

            IDamageable dmg = root.GetComponentInChildren<IDamageable>();
            if (dmg != null)
            {
                dmg.TakeDamage(dmgAmount);
            }
        }
    }

    private Vector3 GetCataclysmVfxSpawnPosition()
    {
        if (useLocalOffset)
            return transform.TransformPoint(cataclysmVfxOffset);

        return transform.position + cataclysmVfxOffset;
    }

    // =========================
    // Helpers
    // =========================
    private bool TryGetGroundPoint(out Vector3 point)
    {
        point = default;
        Camera cam = Camera.main;
        if (!cam) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, groundLayers)
            ? (point = hit.point) == hit.point
            : false;
    }

    private bool TryGetUnitUnderMouse(out Transform root)
    {
        root = null;
        Camera cam = Camera.main;
        if (!cam) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRayDistance))
        {
            // Only accept enemies (unit target)
            if (((1 << hit.collider.gameObject.layer) & enemyLayers.value) == 0)
                return false;

            root = hit.collider.transform.root;
            return true;
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // Dragon's Breath preview gizmo
        if (breathEquipped || breathAiming)
        {
            Vector3 dir = (breathAimPoint - transform.position);
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();

                Vector3 boxSize = Application.isPlaying ? GetBreathDamageBoxSize() : breathBaseDamageBoxSize;

                float length = boxSize.z;
                Vector3 origin = mouthPoint ? mouthPoint.position : transform.position;
                Vector3 center = origin + dir * (length * 0.5f);
                Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

                Gizmos.color = Color.red;
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = Matrix4x4.TRS(center, rot, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, boxSize);
                Gizmos.matrix = oldMatrix;
            }
        }
    }

    private bool TryGetEnlightenmentTarget(out BaseEnemy targetEnemy)
    {
        targetEnemy = null;

        Camera cam = Camera.main;
        if (!cam) return false;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, enemyLayers))
            return false;

        BaseEnemy enemy = hit.collider.GetComponentInParent<BaseEnemy>();
        if (enemy == null) return false;
        if (enemy.IsDead) return false;

        BrainwashedUnit existingBrainwash = enemy.GetComponent<BrainwashedUnit>();
        if (existingBrainwash != null && existingBrainwash.IsBrainwashed)
            return false;

        if (enemy.UnitType == EnemyUnitType.Boss)
            return false;

        if (enlightenmentLevel <= 1)
        {
            if (enemy.UnitType != EnemyUnitType.Minion)
                return false;
        }
        else
        {
            if (enemy.UnitType != EnemyUnitType.Minion &&
                enemy.UnitType != EnemyUnitType.Warrior)
                return false;
        }

        targetEnemy = enemy;
        return true;
    }

    private void PlayOneShot(AudioClip clip)
    {
        if (!audioSource || !clip) return;
        audioSource.PlayOneShot(clip);
    }

    private IEnumerator PlayLoopedAudioForDuration(AudioClip clip, float duration)
    {
        if (!audioSource || !clip) yield break;

        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();

        yield return new WaitForSeconds(duration);

        if (audioSource.clip == clip)
        {
            audioSource.Stop();
            audioSource.loop = false;
            audioSource.clip = null;
        }
    }

    //DUMP
    private void DrawConeLine(LineRenderer lr, Vector3 origin, Vector3 forward, float range, float halfAngleDeg, int segments = 24)
    {
        if (!lr) return;

        Vector3 f = forward.normalized;
        Vector3 leftDir = Quaternion.AngleAxis(-halfAngleDeg, Vector3.up) * f;
        Vector3 rightDir = Quaternion.AngleAxis(halfAngleDeg, Vector3.up) * f;

        int pointCount = segments + 3;
        lr.positionCount = pointCount;

        lr.SetPosition(0, origin + Vector3.up * 0.05f);
        lr.SetPosition(1, origin + leftDir * range + Vector3.up * 0.05f);

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float ang = Mathf.Lerp(-halfAngleDeg, halfAngleDeg, t);
            Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * f;
            lr.SetPosition(2 + i, origin + dir * range + Vector3.up * 0.05f);
        }

        lr.SetPosition(pointCount - 1, origin + Vector3.up * 0.05f);
    }
    //

    private bool IsReady(float nextReady) => Time.time >= nextReady;

    // ===== Getters by level (1-3) =====
    private int L(int level) => Mathf.Clamp(level - 1, 0, 2);

    private float GetBlizzardCooldown() => blizzardCooldown[L(blizzardLevel)];
    private float GetBlizzardRadius() => blizzardRadius[L(blizzardLevel)];
    private float GetBlizzardDps() => blizzardDps[L(blizzardLevel)];
    private float GetBlizzardDuration() => blizzardDuration[L(blizzardLevel)];

    private float GetBreathCooldown() => breathCooldown[L(breathLevel)];
    private float GetBreathDps() => breathDps[L(breathLevel)];
    private float GetBreathDuration() => breathDuration[L(breathLevel)];

    private int GetEnlightenmentMaxCharges() => enlightenmentMaxCharges[L(enlightenmentLevel)];

    private float GetCataclysmCooldown() => cataclysmCooldown[L(cataclysmLevel)];
    private float GetCataclysmDamage() => cataclysmDamage[L(cataclysmLevel)];

    private float GetBreathUniformScaleBonus()
    {
        return (breathLevel - 1) * breathScaleIncreasePerLevel;
    }

    private Vector3 GetBreathDamageBoxSize()
    {
        float bonus = GetBreathUniformScaleBonus();
        return new Vector3(breathBaseDamageBoxSize.x + bonus, breathBaseDamageBoxSize.y, breathBaseDamageBoxSize.z + bonus);
    }

    private Vector3 GetBreathVfxScale()
    {
        float bonus = GetBreathUniformScaleBonus();
        return new Vector3(breathBaseVfxScale.x + bonus, breathBaseVfxScale.y, breathBaseVfxScale.z + bonus);
    }

    // ===== Upgrades and Equipments ===== //
    public void EquipSpell(SpellType SpellType)
    {
        blizzardEquipped = false;
        breathEquipped = false;
        enlightenmentEquipped = false;
        cataclysmEquipped = false;

        switch (SpellType)
        {
            case SpellType.BlizzardRain:
                blizzardEquipped = true;
                break;

            case SpellType.DragonsBreath:
                breathEquipped = true;
                break;

            case SpellType.Enlightenment:
                enlightenmentEquipped = true;
                break;

            case SpellType.Cataclysm:
                cataclysmEquipped = true;
                break;
        }

        Debug.Log($"Equipped universal spell: {SpellType}");
    }

    public void UpgradeSpell(SpellType SpellType)
    {
        switch (SpellType)
        {
            case SpellType.BlizzardRain:
                if (blizzardLevel < 3)
                    blizzardLevel++;
                break;

            case SpellType.DragonsBreath:
                if (breathLevel < 3)
                    breathLevel++;
                break;

            case SpellType.Enlightenment:
                if (enlightenmentLevel < 3)
                {
                    enlightenmentLevel++;
                    enlightenmentCharges = GetEnlightenmentMaxCharges();
                }
                break;

            case SpellType.Cataclysm:
                if (cataclysmLevel < 3)
                    cataclysmLevel++;
                break;
        }

        Debug.Log($"Upgraded universal spell: {SpellType} | New Level: {GetSpellLevel(SpellType)}");
    }

    public bool IsSpellEquipped(SpellType SpellType)
    {
        switch (SpellType)
        {
            case SpellType.BlizzardRain: return blizzardEquipped;
            case SpellType.DragonsBreath: return breathEquipped;
            case SpellType.Enlightenment: return enlightenmentEquipped;
            case SpellType.Cataclysm: return cataclysmEquipped;
            default: return false;
        }
    }

    public int GetSpellLevel(SpellType SpellType)
    {
        switch (SpellType)
        {
            case SpellType.BlizzardRain: return blizzardLevel;
            case SpellType.DragonsBreath: return breathLevel;
            case SpellType.Enlightenment: return enlightenmentLevel;
            case SpellType.Cataclysm: return cataclysmLevel;
            default: return 1;
        }
    }

    public bool CanUpgradeSpell(SpellType SpellType)
    {
        return GetSpellLevel(SpellType) < 3;
    }

    // ===== Cooldown UI ===== //

    public bool HasEquippedSpell()
    {
        return blizzardEquipped || breathEquipped || enlightenmentEquipped || cataclysmEquipped;
    }

    public SpellType? GetEquippedSpell()
    {
        if (blizzardEquipped) return SpellType.BlizzardRain;
        if (breathEquipped) return SpellType.DragonsBreath;
        if (enlightenmentEquipped) return SpellType.Enlightenment;
        if (cataclysmEquipped) return SpellType.Cataclysm;

        return null;
    }

    public float GetEquippedSpellCooldownRemaining()
    {
        if (blizzardEquipped)
            return Mathf.Max(0f, blizzardNextReady - Time.time);

        if (breathEquipped)
            return Mathf.Max(0f, breathNextReady - Time.time);

        if (enlightenmentEquipped)
        {
            if (enlightenmentCharges > 0)
                return 0f;

            return Mathf.Max(0f, enlightenmentNextChargeTime - Time.time);
        }

        if (cataclysmEquipped)
            return Mathf.Max(0f, cataclysmNextReady - Time.time);

        return 0f;
    }

    public float GetEquippedSpellCooldownDuration()
    {
        if (blizzardEquipped)
            return GetBlizzardCooldown();

        if (breathEquipped)
            return GetBreathCooldown();

        if (enlightenmentEquipped)
            return enlightenmentCooldownPerCharge;

        if (cataclysmEquipped)
            return GetCataclysmCooldown();

        return 0f;
    }

    public int GetEnlightenmentCharges()
    {
        return enlightenmentCharges;
    }

    public int GetEnlightenmentMaxChargesPublic()
    {
        return GetEnlightenmentMaxCharges();
    }

    public bool IsEnlightenmentEquipped()
    {
        return enlightenmentEquipped;
    }
}