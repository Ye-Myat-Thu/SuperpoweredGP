using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UniversalSpellCooldownUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private UniversalSpellSet universalSpellSet;
    [SerializeField] private Image fillImage;

    [Header("Enlightenment Charges UI")]
    [SerializeField] private GameObject chargesRoot;
    [SerializeField] private TMP_Text chargesText;

    [Header("Behaviour")]
    [SerializeField] private bool fullWhenReady = true;
    [SerializeField] private bool hideWhenNoSpellEquipped = false;

    private void Awake()
    {
        if (!fillImage)
            fillImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (!fillImage || !universalSpellSet)
            return;

        bool hasSpell = universalSpellSet.HasEquippedSpell();

        if (!hasSpell)
        {
            if (hideWhenNoSpellEquipped)
                fillImage.enabled = false;
            else
                fillImage.fillAmount = 0f;

            SetChargesVisible(false);
            return;
        }

        fillImage.enabled = true;

        float remaining = universalSpellSet.GetEquippedSpellCooldownRemaining();
        float duration = universalSpellSet.GetEquippedSpellCooldownDuration();

        float t = (duration <= 0.0001f) ? 0f : Mathf.Clamp01(remaining / duration);

        fillImage.fillAmount = fullWhenReady ? (1f - t) : t;

        UpdateChargesUI();
    }

    private void UpdateChargesUI()
    {
        bool showCharges = universalSpellSet.IsEnlightenmentEquipped();

        SetChargesVisible(showCharges);

        if (!showCharges || !chargesText)
            return;

        int current = universalSpellSet.GetEnlightenmentCharges();
        int max = universalSpellSet.GetEnlightenmentMaxChargesPublic();

        chargesText.text = $"{current}/{max}";
    }

    private void SetChargesVisible(bool visible)
    {
        if (chargesRoot)
            chargesRoot.SetActive(visible);
    }
}
