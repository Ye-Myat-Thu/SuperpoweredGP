using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class UniversalSpellDisplayData
{
    public UniversalSpellSet.SpellType spellType;
    public string displayName;
    [TextArea(2, 5)] public string equipDescription;
    [TextArea(2, 5)] public string upgradeDescription;
    public Sprite icon;
}

[System.Serializable]
public class AttributeDisplayData
{
    

    public AttributeType type;
    public Sprite icon;
}

public class UpgradeManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BaseCharacter playerCharacter;
    [SerializeField] private UniversalSpellSet universalSpellSet;

    //[Header("Attribute Icons")]
    //[SerializeField] private Sprite strengthIcon;
    //[SerializeField] private Sprite agilityIcon;
    //[SerializeField] private Sprite intelligenceIcon;

    [Header("Upgrade UI Roots")]
    [SerializeField] private GameObject upgradeHUDRoot;
    [SerializeField] private GameObject upgrade1Root;
    [SerializeField] private GameObject upgrade2Root;
    [SerializeField] private GameObject upgrade3Root;

    [Header("Upgrade Cards")]
    [SerializeField] private UpgradeCardUI[] upgrade1Cards;
    [SerializeField] private UpgradeCardUI[] upgrade2Cards;
    [SerializeField] private UpgradeCardUI[] upgrade3Cards;

    //[Header("Universal Spell Icons")]
    //[SerializeField] private Sprite blizzardIcon;
    //[SerializeField] private Sprite dragonsBreathIcon;
    //[SerializeField] private Sprite enlightenmentIcon;
    //[SerializeField] private Sprite cataclysmIcon;
    [Header("Universal Spell Dispaly Data")]
    [SerializeField] private List<UniversalSpellDisplayData> universalSpellDisplayData = new();

    [Header("Attribute Display Data")]
    [SerializeField] private List<AttributeDisplayData> attributeDisplayData = new();

    [Header("HUD Spell Slot")]
    [SerializeField] private UnityEngine.UI.Image universalSpellHudIcon;

    private bool shownLevel6;
    private bool shownLevel12;
    private bool shownLevel18;

    private UniversalSpellSet.SpellType? equippedUniversalSpell = null;
    private UniversalSpellSet.SpellType? randomSpellOfferedAt12 = null;

    private readonly List<UniversalSpellSet.SpellType> allUniversalSpells = new()
    {
        UniversalSpellSet.SpellType.BlizzardRain,
        UniversalSpellSet.SpellType.DragonsBreath,
        UniversalSpellSet.SpellType.Enlightenment,
        UniversalSpellSet.SpellType.Cataclysm
    };

    private void OnEnable()
    {
        if (playerCharacter != null)
        {
            playerCharacter.OnLevelUp += HandleLevelUp;
        }
    }

    private void OnDisable()
    {
        if (playerCharacter != null)
        {
            playerCharacter.OnLevelUp -= HandleLevelUp;
        }
    }

    private void Start()
    {
        if (!upgradeHUDRoot)
            upgradeHUDRoot = gameObject;

        if (upgradeHUDRoot) upgradeHUDRoot.SetActive(false);

        if (upgrade1Root) upgrade1Root.SetActive(false);
        if (upgrade2Root) upgrade2Root.SetActive(false);
        if (upgrade3Root) upgrade3Root.SetActive(false);
    }

    private void HandleLevelUp(int newLevel)
    {
        if (newLevel >= 6 && !shownLevel6)
        {
            shownLevel6 = true;
            OpenUpgrade1();
            return;
        }

        if (newLevel >= 12 && !shownLevel12)
        {
            shownLevel12 = true;
            OpenUpgrade2();
            return;
        }

        if (newLevel >= 18 && !shownLevel18)
        {
            shownLevel18 = true;
            OpenUpgrade3();
        }
    }

    private void OpenUpgrade1()
    {
        List<UpgradeOption> options = BuildUpgrade1Options();
        ShowUpgradeUI(upgrade1Root, upgrade1Cards, options);
    }

    private void OpenUpgrade2()
    {
        List<UpgradeOption> options = BuildUpgrade2Options();
        ShowUpgradeUI(upgrade2Root, upgrade2Cards, options);
    }

    private void OpenUpgrade3()
    {
        List<UpgradeOption> options = BuildUpgrade3Options();
        ShowUpgradeUI(upgrade3Root, upgrade3Cards, options);
    }

    private void ShowUpgradeUI(GameObject root, UpgradeCardUI[] cards, List<UpgradeOption> options)
    {
        Time.timeScale = 0f;

        if (upgradeHUDRoot) upgradeHUDRoot.SetActive(true);
        if (root) root.SetActive(true);

        for (int i = 0; i < cards.Length; i++)
        {
            if (i < options.Count)
            {
                cards[i].gameObject.SetActive(true);
                cards[i].Setup(options[i], OnUpgradeSelected);
            }
            else
            {
                cards[i].gameObject.SetActive(false);
            }
        }
    }

    private void CloseAllUpgradeUI()
    {
        if (upgrade1Root) upgrade1Root.SetActive(false);
        if (upgrade2Root) upgrade2Root.SetActive(false);
        if (upgrade3Root) upgrade3Root.SetActive(false);
        if (upgradeHUDRoot) upgradeHUDRoot.SetActive(false);

        Time.timeScale = 1f;
    }

    private void OnUpgradeSelected(UpgradeOption option)
    {
        ApplyUpgrade(option);
        CloseAllUpgradeUI();
    }

    private void ApplyUpgrade(UpgradeOption option)
    {
        switch (option.choiceType)
        {
            case UpgradeChoiceType.EquipUniversalSpell:
                EquipUniversalSpell(option.spellType);
                break;

            case UpgradeChoiceType.UpgradeUniversalSpell:
                UpgradeUniversalSpell(option.spellType);
                break;

            case UpgradeChoiceType.AttributeBonus:
                ApplyAttributeBonus(option.attributeType, option.amount);
                break;
        }
    }

    private void EquipUniversalSpell(UniversalSpellSet.SpellType spellType)
    {
        equippedUniversalSpell = spellType;

        if (universalSpellSet != null)
        {
            universalSpellSet.EquipSpell(spellType);
        }

        if (universalSpellHudIcon != null)
        {
            universalSpellHudIcon.sprite = GetSpellIcon(spellType);
            universalSpellHudIcon.enabled = true;
        }
    }

    private void UpgradeUniversalSpell(UniversalSpellSet.SpellType spellType)
    {
        if (universalSpellSet != null)
        {
            universalSpellSet.UpgradeSpell(spellType);
            universalSpellSet.EquipSpell(spellType);
        }

        if (universalSpellHudIcon != null)
        {
            universalSpellHudIcon.sprite = GetSpellIcon(spellType);
            universalSpellHudIcon.enabled = true;
        }

        equippedUniversalSpell = spellType;
    }

    private void ApplyAttributeBonus(AttributeType attributeType, int amount)
    {
        CoreStats bonus = new CoreStats();

        switch (attributeType)
        {
            case AttributeType.Strength:
                bonus.Strength = amount;
                break;
            case AttributeType.Agility:
                bonus.Agility = amount;
                break;
            case AttributeType.Intelligence:
                bonus.Intelligence = amount;
                break;
        }

        if (playerCharacter != null)
            playerCharacter.AddBonusStats(bonus);
    }

    private List<UpgradeOption> BuildUpgrade1Options()
    {
        List<UniversalSpellSet.SpellType> picks = allUniversalSpells
            .OrderBy(_ => Random.value)
            .Take(3)
            .ToList();

        List<UpgradeOption> result = new();

        foreach (var spell in picks)
        {
            result.Add(BuildEquipSpellOption(spell));
        }

        return result.OrderBy(_ => Random.value).ToList();
    }

    private List<UpgradeOption> BuildUpgrade2Options()
    {
        List<UpgradeOption> result = new();

        if (equippedUniversalSpell.HasValue)
            result.Add(BuildUpgradeSpellOption(equippedUniversalSpell.Value));

        List<UniversalSpellSet.SpellType> randomPool = new(allUniversalSpells);

        if (equippedUniversalSpell.HasValue)
            randomPool.Remove(equippedUniversalSpell.Value);

        UniversalSpellSet.SpellType randomSpell = randomPool[Random.Range(0, randomPool.Count)];
        randomSpellOfferedAt12 = randomSpell;
        result.Add(BuildUpgradeSpellOption(randomSpell));

        result.Add(BuildRandomAttributeOption());

        return result.OrderBy(_ => Random.value).ToList();
    }

    private List<UpgradeOption> BuildUpgrade3Options()
    {
        List<UpgradeOption> result = new();

        if (equippedUniversalSpell.HasValue)
            result.Add(BuildUpgradeSpellOption(equippedUniversalSpell.Value));

        List<UniversalSpellSet.SpellType> randomPool = new(allUniversalSpells);

        if (equippedUniversalSpell.HasValue)
            randomPool.Remove(equippedUniversalSpell.Value);

        if (randomSpellOfferedAt12.HasValue)
            randomPool.Remove(randomSpellOfferedAt12.Value);

        if (randomPool.Count > 0)
        {
            UniversalSpellSet.SpellType randomSpell = randomPool[Random.Range(0, randomPool.Count)];
            result.Add(BuildUpgradeSpellOption(randomSpell));
        }

        result.Add(BuildRandomAttributeOption());

        return result.OrderBy(_ => Random.value).ToList();
    }

    private UpgradeOption BuildEquipSpellOption(UniversalSpellSet.SpellType spellType)
    {
        UniversalSpellDisplayData data = GetSpellDisplayData(spellType);

        return new UpgradeOption
        {
            choiceType = UpgradeChoiceType.EquipUniversalSpell,
            spellType = spellType,
            title = data != null && !string.IsNullOrWhiteSpace(data.displayName) ? data.displayName : GetSpellName(spellType),
            description = data != null ? data.equipDescription : "Equip this universal spell.",
            icon = data != null ? data.icon : GetSpellIcon(spellType)
        };
    }

    private UpgradeOption BuildUpgradeSpellOption(UniversalSpellSet.SpellType spellType)
    {
        UniversalSpellDisplayData data = GetSpellDisplayData(spellType);
        string spellName = data != null && !string.IsNullOrWhiteSpace(data.displayName) ? data.displayName : GetSpellName(spellType);

        return new UpgradeOption
        {
            choiceType = UpgradeChoiceType.UpgradeUniversalSpell,
            spellType = spellType,
            title = $"{spellName} Upgrade",
            description = data != null ? data.upgradeDescription : $"Upgrade {spellName} to the next level.",
            icon = data != null ? data.icon : GetSpellIcon(spellType)
        };
    }

    private UpgradeOption BuildRandomAttributeOption()
    {
        AttributeType attr = (AttributeType)Random.Range(0, 3);

        return new UpgradeOption
        {
            choiceType = UpgradeChoiceType.AttributeBonus,
            attributeType = attr,
            amount = 2,
            title = $"{attr} Boost",
            description = $"+2 {attr}",
            icon = GetAttributeIcon(attr)
        };
    }

    private string GetSpellName(UniversalSpellSet.SpellType spellType)
    {
        return spellType switch
        {
            UniversalSpellSet.SpellType.BlizzardRain => "Blizzard Rain",
            UniversalSpellSet.SpellType.DragonsBreath => "Dragon's Breath",
            UniversalSpellSet.SpellType.Enlightenment => "Enlightenment",
            UniversalSpellSet.SpellType.Cataclysm => "Cataclysm",
            _ => "Spell"
        };
    }

    //private Sprite GetSpellIcon(UniversalSpellSet.SpellType spellType)
    //{
    //    return spellType switch
    //    {
    //        UniversalSpellSet.SpellType.BlizzardRain => blizzardIcon,
    //        UniversalSpellSet.SpellType.DragonsBreath => dragonsBreathIcon,
    //        UniversalSpellSet.SpellType.Enlightenment => enlightenmentIcon,
    //        UniversalSpellSet.SpellType.Cataclysm => cataclysmIcon,
    //        _ => null
    //    };
    //}
    private Sprite GetSpellIcon(UniversalSpellSet.SpellType spellType)
    {
        UniversalSpellDisplayData data = GetSpellDisplayData(spellType);
        return data != null ? data.icon : null;
    }

    private UniversalSpellDisplayData GetSpellDisplayData(UniversalSpellSet.SpellType spellType)
    {
        for (int i = 0; i < universalSpellDisplayData.Count; i++)
        {
            if (universalSpellDisplayData[i] != null && universalSpellDisplayData[i].spellType == spellType)
                return universalSpellDisplayData[i];
        }

        return null;
    }

    private Sprite GetAttributeIcon(AttributeType attributeType)
    {
        for (int i = 0; i < attributeDisplayData.Count; i++)
        {
            if (attributeDisplayData[i] != null && attributeDisplayData[i].type == attributeType)
                return attributeDisplayData[i].icon;
        }

        return null;
    }
}
