using System;
using UnityEngine;
using UnityEngine.AI;

public interface IDamageable
{
    void TakeDamage(float amount);
}

public class BaseCharacter : MonoBehaviour, IDamageable
{
    [Header("Class Data")]
    [SerializeField] private CharacterClassData classData;

    [Header("Runtime Core Stats")]
    [SerializeField] private CoreStats coreStats;       // base + level growth
    [SerializeField] private CoreStats bonusStats;      // from items/upgrades later

    [Header("Level / XP")]
    [SerializeField] private int level = 1;
    [SerializeField] private float xp = 0f;
    [SerializeField] private float xpToNext = 100f;

    [Header("Runtime Resources")]
    [SerializeField] private float currentHealth;
    [SerializeField] private float currentMana;

    [Header("Components")]
    [SerializeField] private NavMeshAgent agent; // optional if you use click/hold-to-move with NavMesh
    [SerializeField] private Animator animator;  // optional

    // Derived stats (computed)
    public float MaxHealth { get; private set; }
    public float MaxMana { get; private set; }
    public float MoveSpeed { get; private set; }

    // Events for UI
    public event Action<float, float> OnHealthChanged; // current, max
    public event Action<float, float> OnManaChanged;   // current, max
    public event Action<int> OnLevelUp;

    public CharacterClassData ClassData => classData;
    public int Level => level;

    private void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!animator) animator = GetComponentInChildren<Animator>();

        InitializeFromClassData();
    }

    private void InitializeFromClassData()
    {
        if (!classData)
        {
            Debug.LogWarning($"{name}: No CharacterClassData assigned.");
            return;
        }

        // Build core stats from base + (level-1)*growth
        coreStats = classData.baseStats;
        ApplyLevelGrowth(level);

        RecalculateDerivedStats();

        currentHealth = MaxHealth;
        currentMana = MaxMana;

        PushUI();
        ApplyMoveSpeedToAgent();
    }

    private void ApplyLevelGrowth(int currentLevel)
    {
        // If level = 1 => add 0 growth. If level = 2 => add 1 growth, etc.
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

        // Move speed scaling (simple, tweak later)
        float agiMultiplier = 1f + totalAgi * classData.moveSpeedPerAgility;
        MoveSpeed = classData.baseMoveSpeed * agiMultiplier;

        // Clamp current resources to new max values
        currentHealth = Mathf.Clamp(currentHealth, 0f, MaxHealth);
        currentMana = Mathf.Clamp(currentMana, 0f, MaxMana);

        ApplyMoveSpeedToAgent();
        PushUI();
    }

    private void ApplyMoveSpeedToAgent()
    {
        if (agent)
        {
            agent.speed = MoveSpeed;
        }
    }

    // --- Health / Mana ---
    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);

        if (currentHealth <= 0f)
            Die();
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

    // --- XP / Leveling ---
    public void GainXP(float amount)
    {
        if (amount <= 0f) return;

        xp += amount;

        while (xp >= xpToNext)
        {
            xp -= xpToNext;
            LevelUpInternal();
        }
    }

    private void LevelUpInternal()
    {
        level++;

        // Apply one step of growth
        coreStats.Strength += classData.perLevelStats.Strength;
        coreStats.Agility += classData.perLevelStats.Agility;
        coreStats.Intelligence += classData.perLevelStats.Intelligence;

        // Basic XP curve (replace later with your tuned curve)
        xpToNext = Mathf.Ceil(xpToNext * 1.15f);

        RecalculateDerivedStats();

        // Optional: refill some resources on level up (tweak as desired)
        currentHealth = MaxHealth;
        currentMana = MaxMana;
        PushUI();

        OnLevelUp?.Invoke(level);
    }

    private void PushUI()
    {
        OnHealthChanged?.Invoke(currentHealth, MaxHealth);
        OnManaChanged?.Invoke(currentMana, MaxMana);
    }

    protected virtual void Die()
    {
        // Hook your death flow (UI, restart, etc.)
        // animator?.SetTrigger("Die");
        Debug.Log($"{name} died.");
    }

    // --- For items/upgrades later ---
    public void AddBonusStats(CoreStats add)
    {
        bonusStats.Strength += add.Strength;
        bonusStats.Agility += add.Agility;
        bonusStats.Intelligence += add.Intelligence;
        RecalculateDerivedStats();
    }
}
