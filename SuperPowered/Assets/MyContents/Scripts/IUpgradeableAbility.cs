using UnityEngine;
using System;

public interface IUpgradeableAbility
{
    int AbilityLevel { get; }
    int MaxAbilityLevel { get; }
    void LevelUpAbility();

    event Action<int> OnAbilityLevelChanged;
}
