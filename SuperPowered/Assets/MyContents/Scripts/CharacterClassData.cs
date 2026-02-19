using UnityEngine;

public enum HeroClassType
{
    Titan,   // Strength
    Stalker, // Agility
    Oracle   // Intelligence
}

[System.Serializable]
public struct CoreStats
{
    public int Strength;
    public int Agility;
    public int Intelligence;
}

[CreateAssetMenu(menuName = "Superpowered/Character Class Data", fileName = "NewCharacterClassData")]
public class CharacterClassData : ScriptableObject
{
    [Header("Identity")]
    public HeroClassType classType;

    [Header("Base Stats")]
    public CoreStats baseStats;

    [Header("Per Level Growth")]
    public CoreStats perLevelStats;

    [Header("Base Resources")]
    public float baseHealth = 100f;
    public float baseMana = 50f;

    [Header("Base Movement")]
    public float baseMoveSpeed = 6f;

    [Header("Scaling Multipliers (tweak to taste)")]
    public float healthPerStrength = 10f;
    public float manaPerIntelligence = 8f;
    public float moveSpeedPerAgility = 0.03f; // additive multiplier, e.g. 0.03 = +3% per Agi

    // Optional placeholders for combat scaling later:
    public float attackDamagePerStrength = 1.0f;
    public float attackSpeedPerAgility = 0.01f;
    public float abilityPowerPerIntelligence = 1.2f;
}
