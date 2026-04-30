using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterInfoUI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private BaseCharacter targetCharacter;

    [Header("UI")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text classText;

    [Header("Class Name Override")]
    [SerializeField] private bool useDisplayNameOverride = true;
    [SerializeField] private string titanDisplayName = "Titan";
    [SerializeField] private string stalkerDisplayName = "Stalker";
    [SerializeField] private string oracleDisplayName = "Oracle";

    private void Start()
    {
        if (targetCharacter == null)
        {
            Debug.LogWarning("CharacterInfoUI: targetCharacter is empty");
            return;
        }

        targetCharacter.OnLevelUp += HandleLevelUp;

        RefreshAll();
    }

    private void OnDisable()
    {
        if (targetCharacter != null)
            targetCharacter.OnLevelUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        RefreshLevel();
    }

    public void RefreshAll()
    {
        RefreshPortrait();
        RefreshLevel();
        RefreshClassText();
    }

    public void RefreshPortrait()
    {
        if (portraitImage == null || targetCharacter == null || targetCharacter.ClassData == null)
            return;

        portraitImage.sprite = targetCharacter.ClassData.portrait;
        portraitImage.enabled = portraitImage.sprite != null;
    }

    public void RefreshLevel()
    {
        if (levelText == null || targetCharacter == null)
            return;

        levelText.text = "Lvl " + targetCharacter.Level.ToString();
    }

    public void RefreshClassText()
    {
        if (classText == null || targetCharacter == null || targetCharacter.ClassData == null)
            return;

        HeroClassType classType = targetCharacter.ClassData.classType;

        if (!useDisplayNameOverride)
        {
            classText.text = classType.ToString();
            return;
        }

        switch (classType)
        {
            case HeroClassType.Titan:
                classText.text = titanDisplayName;
                break;

            case HeroClassType.Stalker:
                classText.text = stalkerDisplayName;
                break;

            case HeroClassType.Oracle:
                classText.text = oracleDisplayName;
                break;

            default:
                classText.text = classType.ToString();
                break;
        }
    }
}
