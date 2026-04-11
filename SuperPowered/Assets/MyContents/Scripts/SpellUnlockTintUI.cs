using UnityEngine;
using UnityEngine.UI;

public class SpellUnlockTintUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonoBehaviour abilityScript;
    [SerializeField] private Image spellIcon;

    [Header("Color Settings")]
    [SerializeField] private Color lockedColor = Color.black;
    [SerializeField] private Color unlockedColor = Color.white;

    private IUpgradeableAbility ability;

    private void Awake()
    {
        ability = abilityScript as IUpgradeableAbility;

        if (!spellIcon)
            spellIcon = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (ability != null)
        {
            ability.OnAbilityLevelChanged += HandleAbilityLevelChanged;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (ability != null)
        {
            ability.OnAbilityLevelChanged -= HandleAbilityLevelChanged;
        }
    }

    private void HandleAbilityLevelChanged(int newLevel)
    {
        Refresh();
    }

    public void Refresh()
    {
        if (spellIcon == null || ability == null)
            return;

        spellIcon.color = ability.AbilityLevel <= 0 ? lockedColor : unlockedColor;
    }
}
