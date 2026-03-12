using UnityEngine;
using UnityEngine.UI;

public class SpellPlusUI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private LevelUpManager levelManager;
    [SerializeField] private MonoBehaviour abilityScript; // Whirl/Rage/IronSkin
    [SerializeField] private GameObject plusIcon;         // + visual
    [SerializeField] private Button plusButton;

    [Header("Frame")]
    [SerializeField] private Image frameImage;            // parent frame image to tint

    [Header("Frame Colors (by ability level)")]
    [SerializeField] private Color level1Color = Color.white;
    [SerializeField] private Color level2Color = Color.yellow;
    [SerializeField] private Color level3Color = Color.blue;
    [SerializeField] private Color level4Color = Color.green;
    [SerializeField] private Color level5Color = Color.red;

    private IUpgradeableAbility ability;

    private void Awake()
    {
        ability = abilityScript as IUpgradeableAbility;

        if (!frameImage)
            frameImage = GetComponent<Image>(); // if this script is on the frame object

        if (plusButton != null)
            plusButton.onClick.AddListener(OnPlusClicked);
    }

    private void OnEnable()
    {
        if (levelManager != null)
            levelManager.OnSkillPointsChanged += HandleSkillPointsChanged;

        if (ability != null)
            ability.OnAbilityLevelChanged += HandleAbilityLevelChanged;

        Refresh();
        RefreshFrameColor();
    }

    private void OnDisable()
    {
        if (levelManager != null)
            levelManager.OnSkillPointsChanged -= HandleSkillPointsChanged;

        if (ability != null)
            ability.OnAbilityLevelChanged -= HandleAbilityLevelChanged;
    }

    private void HandleSkillPointsChanged(int _)
    {
        Refresh();
    }

    private void HandleAbilityLevelChanged(int _)
    {
        Refresh();
        RefreshFrameColor();
    }

    private void Refresh()
    {
        if (!plusIcon || levelManager == null || ability == null) return;

        bool show = levelManager.SkillPoints > 0 && ability.AbilityLevel < ability.MaxAbilityLevel;
        plusIcon.SetActive(show);
    }

    private void RefreshFrameColor()
    {
        if (!frameImage || ability == null) return;

        switch (ability.AbilityLevel)
        {
            case 1: frameImage.color = level1Color; break;
            case 2: frameImage.color = level2Color; break;
            case 3: frameImage.color = level3Color; break;
            case 4: frameImage.color = level4Color; break;
            default: frameImage.color = level5Color; break; // 5+
        }
    }

    private void OnPlusClicked()
    {
        if (levelManager == null || ability == null) return;

        if (levelManager.TryLevelUpAbility(ability))
        {
            // ability event will also refresh color, but this keeps it instant even if you change later
            Refresh();
            RefreshFrameColor();
        }
    }
}