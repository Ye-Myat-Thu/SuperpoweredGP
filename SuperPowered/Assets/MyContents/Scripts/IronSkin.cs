using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IronSkin : MonoBehaviour, IUpgradeableAbility
{
    [Header("IronSkin Settings")]
    [SerializeField] private float duration = 5f;
    [SerializeField] private float cooldown = 15f;
    [SerializeField] private float bonusHealthRegen = 10f;

    [Header("Input")]
    [SerializeField] private KeyCode castKey = KeyCode.E;

    [Header("Level Up")]
    [SerializeField] private int abilityLevel = 1;
    [SerializeField] private int maxAbilityLevel = 5;
    public int AbilityLevel => abilityLevel;
    public int MaxAbilityLevel => maxAbilityLevel;
    public event System.Action<int> OnAbilityLevelChanged;

    [Header("UI")]
    public float CooldownDuration => cooldown;
    public float CooldownRemaining => Mathf.Max(0f, nextReadyTime - Time.time);

    [Header("Behaviour")]
    [SerializeField] private bool castAllAbilitiesOnStart = true;
    [SerializeField] private List<MonoBehaviour> abilitiesToFreeCast = new List<MonoBehaviour>();

    [Header("References")]
    [SerializeField] private BaseCharacter character;

    private bool isActive;
    private float nextReadyTime;

    private IRegenModifiable regenMod;
    private readonly List<ICooldownOverrideable> cachedAbilities = new();

    private void Awake()
    {
        //regen character
        //if (character == null)
        //{
        //    regenMod = character as IRegenModifiable;
        //}

        if (!character) character = GetComponent<BaseCharacter>();

        if (regenMod == null)
            regenMod = GetComponent<IRegenModifiable>();

        //Cache ability interfaces
        cachedAbilities.Clear();
        for ( int i = 0; i < abilitiesToFreeCast.Count; i++)
        {
            if (abilitiesToFreeCast[i] is ICooldownOverrideable a)
                cachedAbilities.Add(a);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(castKey))
        {
            CastIronSkin();
            Debug.Log("Iron Skin is casted.");
        }
    }

    public void LevelUpAbility()
    {
        if (abilityLevel >= maxAbilityLevel) return;
        abilityLevel++;
        OnAbilityLevelChanged?.Invoke(abilityLevel);

        bonusHealthRegen += 1.5f;
        cooldown -= 1.5f;
    }

    public bool CanCast => Time.time >= nextReadyTime && !isActive;

    public void CastIronSkin()
    {
        if (!CanCast) return;

        nextReadyTime = Time.time + cooldown;
        StartCoroutine(IronSkinRoutine());
    }

    private IEnumerator IronSkinRoutine()
    {
        isActive = true;

        //regen applkcation
        //if (regenMod != null)
        //    regenMod.AddRegenBonus(bonusHealthRegen);
        character.AddRegenBonus(bonusHealthRegen);

        //remove cooldowns
        for (int i = 0; i < cachedAbilities.Count; i++)
            cachedAbilities[i].SetCooldownOverride(true, 0f);

        //castr everything simultaneously
        if (castAllAbilitiesOnStart)
        {
            for (int i = 0; i < cachedAbilities.Count; i++)
                cachedAbilities[i].ForceCast();
        }

        yield return new WaitForSeconds(duration);

        //restore cooldown beahvr
        for (int i = 0; i < cachedAbilities.Count; i++)
            cachedAbilities[i].SetCooldownOverride(false, 0f);

        //if (regenMod != null)
        //    regenMod.RemoveRegenBonus(bonusHealthRegen);
        character.RemoveRegenBonus(bonusHealthRegen);

        isActive = false;
    }
}
