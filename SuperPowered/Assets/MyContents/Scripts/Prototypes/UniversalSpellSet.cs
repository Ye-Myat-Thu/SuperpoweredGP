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

    [Header("Layers")]
    [SerializeField] private LayerMask groundLayers;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Input Keys")]
    [SerializeField] private KeyCode blizzardKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode breathKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode enlightenmentKey = KeyCode.Alpha3;
    [SerializeField] private KeyCode cataclysmKey = KeyCode.Alpha4;

    [Header("Raycast")]
    [SerializeField] private float maxRayDistance = 500f;

    [Header("Preview Prefabs (optional)")]
    [SerializeField] private GameObject aoeCirclePreviewPrefab; // flat circle mesh
    [SerializeField] private GameObject conePreviewPrefab;      // flat cone mesh

    [Header("VFX Prefabs (optional)")]
    [SerializeField] private ParticleSystem blizzardVfxPrefab;
    [SerializeField] private ParticleSystem breathVfxPrefab;
    [SerializeField] private ParticleSystem cataclysmVfxPrefab;

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
    [SerializeField] private float breathRange = 10f;
    [SerializeField] private float breathAngle = 45f;                      // half-angle
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

    private void Update()
    {
        // Recharge enlightenment charges over time
        UpdateEnlightenmentCharges();

        // Spell key handling
        if (Input.GetKeyDown(blizzardKey)) ToggleAim(SpellType.BlizzardRain);
        if (Input.GetKeyDown(breathKey)) ToggleAim(SpellType.DragonsBreath);

        if (Input.GetKeyDown(enlightenmentKey)) TryCastEnlightenment();
        if (Input.GetKeyDown(cataclysmKey)) TryCastCataclysm();

        // Aiming updates
        if (blizzardAiming) UpdateBlizzardAim();
        if (breathAiming) UpdateBreathAim();
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

        // VFX
        if (blizzardVfxPrefab)
            Instantiate(blizzardVfxPrefab, blizzardPoint, Quaternion.identity);

        // Damage over time
        StartCoroutine(BlizzardDamageRoutine(blizzardPoint, GetBlizzardRadius(), GetBlizzardDps(), GetBlizzardDuration()));
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
                    breathPreview.transform.localScale = new Vector3(breathRange, 1f, breathRange);
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
            Destroy(fx.gameObject, GetBreathDuration() + 0.25f);
        }

        // Damage over time in cone
        StartCoroutine(BreathDamageRoutine(dir, GetBreathDps(), GetBreathDuration()));
    }

    private IEnumerator BreathDamageRoutine(Vector3 forwardDir, float dps, float duration)
    {
        float end = Time.time + duration;

        while (Time.time < end)
        {
            float tick = Mathf.Min(breathTickInterval, end - Time.time);
            float damageThisTick = dps * tick;

            // Broadphase: overlap sphere in range
            Collider[] hits = Physics.OverlapSphere(transform.position, breathRange, enemyLayers);

            for (int i = 0; i < hits.Length; i++)
            {
                Transform root = hits[i].transform.root;
                Vector3 to = root.position - transform.position;
                to.y = 0f;

                if (to.sqrMagnitude < 0.0001f) continue;

                float dist = to.magnitude;
                if (dist > breathRange) continue;

                float angle = Vector3.Angle(forwardDir, to.normalized);
                if (angle > breathAngle) continue;

                IDamageable dmg = hits[i].GetComponentInParent<IDamageable>();
                if (dmg != null) dmg.TakeDamage(damageThisTick);
            }

            yield return new WaitForSeconds(tick);
        }
    }

    // =========================
    // 3) Enlightenment (unit target + charges)
    // =========================
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
        if (!TryGetUnitUnderMouse(out Transform unitRoot)) return;

        // Check range
        float dist = Vector3.Distance(transform.position, unitRoot.position);
        if (dist > enlightenmentCastRange) return;

        // Apply "brainwash" mark (prototype)
        Brainwashed bw = unitRoot.GetComponent<Brainwashed>();
        if (bw == null) bw = unitRoot.gameObject.AddComponent<Brainwashed>();
        bw.Apply(brainwashedTint);

        // Spend a charge + start recharge timer if needed
        enlightenmentCharges--;
        if (enlightenmentCharges < GetEnlightenmentMaxCharges())
            enlightenmentNextChargeTime = Time.time + enlightenmentCooldownPerCharge;
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

    // =========================
    // 4) Cataclysm (global instant)
    // =========================
    private void TryCastCataclysm()
    {
        if (!IsReady(cataclysmNextReady)) return;

        cataclysmNextReady = Time.time + GetCataclysmCooldown();

        // VFX on caster
        if (cataclysmVfxPrefab)
        {
            ParticleSystem fx = Instantiate(cataclysmVfxPrefab, transform.position, transform.rotation);
            Destroy(fx.gameObject, 4f);
        }

        float dmgAmount = GetCataclysmDamage();

        // Global damage: overlap huge radius using enemyLayers
        Collider[] hits = Physics.OverlapSphere(transform.position, cataclysmGlobalRadius, enemyLayers);

        // Avoid multi-hit same enemy due to multiple colliders
        HashSet<Transform> hitRoots = new HashSet<Transform>();

        for (int i = 0; i < hits.Length; i++)
        {
            Transform root = hits[i].transform.root;
            if (hitRoots.Contains(root)) continue;
            hitRoots.Add(root);

            IDamageable dmg = root.GetComponentInChildren<IDamageable>();
            if (dmg != null) dmg.TakeDamage(dmgAmount);
        }
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

    private void DrawConeLine(LineRenderer lr, Vector3 origin, Vector3 forward, float range, float halfAngleDeg, int segments = 24)
    {
        if (!lr) return;

        Vector3 f = forward.normalized;
        Vector3 leftDir = Quaternion.AngleAxis(-halfAngleDeg, Vector3.up) * f;
        Vector3 rightDir = Quaternion.AngleAxis(halfAngleDeg, Vector3.up) * f;

        // We'll draw: origin -> leftEdge -> arc points -> rightEdge -> origin
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
}