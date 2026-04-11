using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

public interface IDamageable
{
    void TakeDamage(float amount);
}

public class BaseCharacter : MonoBehaviour, IDamageable, IRegenModifiable
{
    [Header("Class Data")]
    [SerializeField] private CharacterClassData classData;

    [Header("Runtime Core Stats")]
    [SerializeField] private CoreStats coreStats;       //base + level growth
    [SerializeField] private CoreStats bonusStats;      //from items/upgrades later

    [Header("Regen")]
    [SerializeField] private bool enableHealthRegen = true;
    private float regenBonusFlat;     // added by buffs like Iron Skin
    public float HealthRegenPerSecond { get; private set; }

    [Header("Level / XP")]
    [SerializeField] private int level = 1;
    [SerializeField] private float xp = 0f;
    [SerializeField] private float xpToNext = 100f;
    [SerializeField] private int levelCap = 30;
    [SerializeField] private float xpPerLevel = 100;

    [Header("Runtime Resources")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentMana;

    [Header("Components")]
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private Animator animator;
    [SerializeField] private List<MonoBehaviour> scriptsToBeDiabled = new List<MonoBehaviour>();
    private bool isDead;
    public bool IsDead => isDead;

    [Header("Animation")]
    [SerializeField] private string hitTrigger = "Hit";
    [SerializeField] private string dieTrigger = "Die";

    [Header("Blink on hit")]
    [SerializeField] private Renderer[] blinkRenderers;
    [SerializeField] private int blinkCount = 3;
    [SerializeField] private float blinkInterval = 0.06f;

    [Header("Rage Settings")]
    [SerializeField] private bool isInvulnerable;
    public bool IsInvulnerable => isInvulnerable;

    public void SetInvulnerable(bool value)
    {
        isInvulnerable = value;
    }

    private Coroutine blinkRoutine;

    //Derived stats (computed)
    public float MaxHealth { get; private set; }
    public float MaxMana { get; private set; }
    public float MoveSpeed { get; private set; }

    //Events for UI
    public event Action<float, float> OnHealthChanged; //current, max
    public event Action<float, float> OnManaChanged;   //current, max

    public event Action<int> OnLevelUp;
    public event Action<float, float> OnXPChanged;

    public float CurrentHealth => currentHealth;
    public float CurrentXP => xp;
    public float XPToNext => xpToNext;
    public int LevelCap => levelCap;

    public CoreStats CoreStats => coreStats;
    public CoreStats BonusStats => bonusStats;
    
    public int TotalStrength => coreStats.Strength + bonusStats.Strength;
    public int TotalAgility => coreStats.Agility + bonusStats.Agility;
    public int TotalIntelligence => coreStats.Intelligence + bonusStats.Intelligence;

    public event Action OnStatsChanged;

    public CharacterClassData ClassData => classData;
    public int Level => level;
    //==============

    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        if (blinkRenderers == null || blinkRenderers.Length == 0)
        {
            blinkRenderers = GetComponentsInChildren<Renderer>(true);
        }

        InitializeFromClassData();
    }

    private void Update()
    {
        if (!enableHealthRegen) return;
        if (currentHealth <= 0f) return;
        if (currentHealth >= MaxHealth) return;

        float amount = HealthRegenPerSecond * Time.deltaTime;
        if (amount > 0f)
            Heal(amount);
    }

    public void AddRegenBonus(float amount)
    {
        regenBonusFlat += amount;
        RecalculateDerivedStats();
    }

    public void RemoveRegenBonus(float amount)
    {
        regenBonusFlat -= amount;
        RecalculateDerivedStats();
    }

    private void InitializeFromClassData()
    {
        if (!classData)
        {
            Debug.LogWarning($"{name}: No CharacterClassData assigned.");
            return;
        }

        //Build core stats from base + (level-1)*growth
        coreStats = classData.baseStats;
        ApplyLevelGrowth(level);

        RecalculateDerivedStats();

        currentHealth = MaxHealth;
        currentMana = MaxMana;

        xpToNext = xpPerLevel;
        xp = Mathf.Max(0f, xp);
        PushUI();
        OnXPChanged?.Invoke(xp, xpToNext);
        ApplyMoveSpeedToAgent();
    }

    private void ApplyLevelGrowth(int currentLevel)
    {
        //If level = 1 => add 0 growth. If level = 2 => add 1 growth, etc.
        int levelsToApply = Mathf.Max(currentLevel - 1, 0);

        coreStats.Strength += classData.perLevelStats.Strength * levelsToApply;
        coreStats.Agility += classData.perLevelStats.Agility * levelsToApply;
        coreStats.Intelligence += classData.perLevelStats.Intelligence * levelsToApply;
    }

    public void RecalculateDerivedStats()
    {
        if (!classData) return;

        int totalStr = coreStats.Strength + bonusStats.Strength;
        int totalAgi = coreStats.Agility + bonusStats.Agility;
        int totalInt = coreStats.Intelligence + bonusStats.Intelligence;

        MaxHealth = classData.baseHealth + totalStr * classData.healthPerStrength;
        MaxMana = classData.baseMana + totalInt * classData.manaPerIntelligence;

        //Move speed scaling (simple, tweak later)
        float agiMultiplier = 1f + totalAgi * classData.moveSpeedPerAgility;
        MoveSpeed = classData.baseMoveSpeed * agiMultiplier;

        // Health regen scaling
        float baseRegen = classData.baseHealthRegen;
        float strRegen = totalStr * classData.healthRegenPerStrength;
        HealthRegenPerSecond = baseRegen + strRegen + regenBonusFlat;

        //Clamp current resources to new max values
        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
        currentMana = Mathf.Clamp(currentMana, 0f, MaxMana);

        ApplyMoveSpeedToAgent();
        OnStatsChanged?.Invoke();
        PushUI();
    }

    private void ApplyMoveSpeedToAgent()
    {
        if (agent)
        {
            agent.speed = MoveSpeed;
        }
    }

    //-- Health / Mana --
    public void TakeDamage(float amount)
    {
        if (isInvulnerable) return;
        if (amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (animator != null)
        {
            animator.SetTrigger(hitTrigger);
        }

        //Blink routine reacion
        if (blinkRoutine != null)
            StopCoroutine(blinkRoutine);

        blinkRoutine = StartCoroutine(BlinkRoutine());

        if (currentHealth <= 0f)
            Die();

        Debug.Log("Taking Damage");
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
        for (int i = 0; i < blinkRenderers.Length; i++)
        {
            if (blinkRenderers[i] != null)
                blinkRenderers[i].enabled = visible;
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f) return;

        currentHealth = Mathf.Min(MaxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
    }

    public bool SpendMana(float amount)
    {
        if (amount <= 0f) return true;
        if (currentMana < amount) return false;

        currentMana -= amount;
        OnManaChanged?.Invoke(currentMana, MaxMana);
        return true;
    }

    public void RestoreMana(float amount)
    {
        if (amount <= 0f) return;

        currentMana = Mathf.Min(MaxMana, currentMana + amount);
        OnManaChanged?.Invoke(currentMana, MaxMana);
    }

    //-- XP / Leveling --
    public void GainXP(float amount)
    {
        if (amount <= 0f) return;
        if (level >= levelCap) return;

        xp += amount;

        OnXPChanged?.Invoke(xp, xpToNext);

        while (xp >= xpToNext)
        {
            xp -= xpToNext;
            LevelUpInternal();
            OnXPChanged?.Invoke(xp, xpToNext);
        }
    }

    private void LevelUpInternal()
    {
        if (level >= levelCap) return;
        
        level++;

        //Apply one step of growth
        coreStats.Strength += classData.perLevelStats.Strength;
        coreStats.Agility += classData.perLevelStats.Agility;
        coreStats.Intelligence += classData.perLevelStats.Intelligence;

        //Basic XP curve
        //xpToNext = Mathf.Ceil(xpToNext * 1.15f);
        xpToNext = xpPerLevel;
        OnXPChanged?. Invoke(xp, xpToNext);

        RecalculateDerivedStats();

        currentHealth = MaxHealth;
        currentMana = MaxMana;
        PushUI();

        OnLevelUp?.Invoke(level);
    }

    private void PushUI()
    {
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        OnManaChanged?.Invoke(currentMana, MaxMana);
        OnXPChanged?.Invoke(xp, xpToNext);
    }

    protected virtual void Die()
    {
        if (IsDead) return;
        isDead = true;

        DisableScriptsOnDeath();
        
        //Hook your death flow (UI, restart, etc.)
        if (animator != null)
        {
            animator.SetTrigger(dieTrigger);
        }
        DestroyObject(gameObject, 3f);
        Debug.Log($"{name} died.");
    }

    private void DisableScriptsOnDeath()
    {
        for (int i = 0; i < scriptsToBeDiabled.Count; i++)
        {
            var mb = scriptsToBeDiabled[i];
            if (mb != null)
            {
                mb.enabled = false;
            }
        }
    }

    //-- For items/upgrades later --
    public void AddBonusStats(CoreStats add)
    {
        bonusStats.Strength += add.Strength;
        bonusStats.Agility += add.Agility;
        bonusStats.Intelligence += add.Intelligence;
        RecalculateDerivedStats();
    }
}
