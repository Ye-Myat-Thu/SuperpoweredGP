using System.Collections;
using TMPro;
using UnityEngine;

public class AttributePanelUI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private BaseCharacter targetCharacter;

    [Header("Strength")]
    [SerializeField] private TMP_Text baseStrengthText;
    [SerializeField] private TMP_Text bonusStrengthText;

    [Header("Agility")]
    [SerializeField] private TMP_Text baseAgilityText;
    [SerializeField] private TMP_Text bonusAgilityText;

    [Header("Intelligence")]
    [SerializeField] private TMP_Text baseIntelligenceText;
    [SerializeField] private TMP_Text bonusIntelligenceText;

    private void Awake()
    {
        if (!targetCharacter)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                targetCharacter = player.GetComponent<BaseCharacter>();
        }
    }

    private void OnEnable()
    {
        if (targetCharacter != null)
        {
            targetCharacter.OnStatsChanged += Refresh;
        }
        Refresh();
        StartCoroutine(RefreshNextFrame());
    }

    private void OnDisable()
    {
        if (targetCharacter != null)
        {
            targetCharacter.OnStatsChanged -= Refresh;
        }
    }

    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        Refresh();
    }

    public void Refresh()
    {
        if (targetCharacter == null) return;

        CoreStats core = targetCharacter.CoreStats;
        CoreStats bonus = targetCharacter.BonusStats;

        if (baseStrengthText) baseStrengthText.text = core.Strength.ToString();
        if (bonusStrengthText) bonusStrengthText.text = FormatBonus(bonus.Strength);

        if (baseAgilityText) baseAgilityText.text = core.Agility.ToString();
        if (bonusAgilityText) bonusAgilityText.text = FormatBonus(bonus.Agility);

        if (baseIntelligenceText) baseIntelligenceText.text = core.Intelligence.ToString();
        if (bonusIntelligenceText) bonusIntelligenceText.text = FormatBonus(bonus.Intelligence);
    }

    private string FormatBonus(int value)
    {
        if (value > 0) return $"+{value}";
        if (value < 0) return value.ToString();
        return "+0";
    }
}
