using System.Collections;
using UnityEngine;

public class StalkerFocus : MonoBehaviour, IUpgradeableAbility, IAbilityCooldown
{
    [Header("Settings")]
    [SerializeField] private float duration = 1.2f;
    [SerializeField] private float cooldown = 2f;

    [Header("Damage Per Level")]
    [SerializeField] private float[] damageBonus = { 20f, 25f, 30f, 35f, 40f };

    [Header("Input")]
    [SerializeField] private KeyCode castKey = KeyCode.E;

    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string focusTrigger = "Focus";

    [Header("References")]
    [SerializeField] private CharacterCombat combat;

    private int abilityLevel;
    private int maxLevel = 5;

    public int AbilityLevel => abilityLevel;
    public int MaxAbilityLevel => maxLevel;
    public event System.Action<int> OnAbilityLevelChanged;

    private float nextReadyTime;
    private bool isActive;

    // UI
    public float CooldownDuration => cooldown;
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    private void Awake()
    {
        if (!combat)
            combat = GetComponent<CharacterCombat>();

        if (!animator)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (abilityLevel <= 0) return;

        if (Input.GetKeyDown(castKey))
        {
            CastFocus();
        }
    }

    public bool CanCast => Time.time >= nextReadyTime && !isActive;

    public void CastFocus()
    {
        if (!CanCast) return;

        nextReadyTime = Time.time + cooldown;

        StartCoroutine(FocusRoutine());
    }

    private IEnumerator FocusRoutine()
    {
        isActive = true;

        float bonus = GetDamageBonus();

        // LOCK animation
        if (combat)
            combat.SetAnimationLock(true);

        // Apply damage buff
        if (combat)
            combat.AddBonusDamage(bonus);

        // Trigger animation
        if (animator && !string.IsNullOrEmpty(focusTrigger))
            animator.SetTrigger(focusTrigger);

        yield return new WaitForSeconds(duration);

        // Remove buff
        if (combat)
            combat.RemoveBonusDamage(bonus);

        // Unlock animation
        if (combat)
            combat.SetAnimationLock(false);

        isActive = false;
    }

    public void LevelUpAbility()
    {
        if (abilityLevel >= maxLevel) return;

        abilityLevel++;
        OnAbilityLevelChanged?.Invoke(abilityLevel);
    }

    private float GetDamageBonus()
    {
        int index = Mathf.Clamp(abilityLevel - 1, 0, damageBonus.Length - 1);
        return damageBonus[index];
    }
}