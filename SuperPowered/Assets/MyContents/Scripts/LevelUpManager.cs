using System;
using System.Collections.Generic;
using UnityEngine;

public class LevelUpManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseCharacter character;

    [Header("Skill Points")]
    [SerializeField] private bool grantPointAtLevel1 = true;
    [SerializeField] private int skillPoints;

    public int SkillPoints => skillPoints;

    public event Action<int> OnSkillPointsChanged;

    private void Awake()
    {
        if (!character) character = GetComponent<BaseCharacter>();
    }

    private void OnEnable()
    {
        if (character != null)
            character.OnLevelUp += HandleLevelUp;
    }

    private void OnDisable()
    {
        if (character != null)
            character.OnLevelUp -= HandleLevelUp;
    }

    private void Start()
    {
        if (grantPointAtLevel1)
        {
            skillPoints = 1;
            OnSkillPointsChanged?.Invoke(skillPoints);
        }
    }

    private void HandleLevelUp(int newLevel)
    {
        // one point per level-up
        skillPoints++;
        OnSkillPointsChanged?.Invoke(skillPoints);
    }

    public bool CanSpendPointOn(IUpgradeableAbility ability)
    {
        if (ability == null) return false;
        if (skillPoints <= 0) return false;
        return ability.AbilityLevel < ability.MaxAbilityLevel;
    }

    public bool TryLevelUpAbility(IUpgradeableAbility ability)
    {
        if (!CanSpendPointOn(ability)) return false;

        skillPoints--;
        ability.LevelUpAbility();
        OnSkillPointsChanged?.Invoke(skillPoints);
        return true;
    }
}