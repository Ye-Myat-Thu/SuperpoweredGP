using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class UpgradeCardUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Button button;

    private UpgradeOption currentOption;
    private Action<UpgradeOption> onSelected;

    public void Setup(UpgradeOption option, Action<UpgradeOption> onClick)
    {
        currentOption = option;
        onSelected = onClick;

        if (iconImage) iconImage.sprite = option.icon;
        if (titleText) titleText.text = option.title;
        if (descriptionText) descriptionText.text = option.description;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(HandleClick);
    }

    private void HandleClick()
    {
        onSelected?.Invoke(currentOption);
    }
}
