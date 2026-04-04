using UnityEngine;

[System.Serializable]
public class UpgradeOption
{
    public UpgradeChoiceType choiceType;

    public UniversalSpellSet.SpellType spellType;
    public AttributeType attributeType;
    public int amount;

    public string title;
    [TextArea] public string description;
    public Sprite icon;
}
