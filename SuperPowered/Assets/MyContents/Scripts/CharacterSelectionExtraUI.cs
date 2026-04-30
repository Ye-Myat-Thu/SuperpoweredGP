using UnityEngine;

public class CharacterSelectionExtraUI : MonoBehaviour
{
    [SerializeField] private GameObject spellOverviewUI;

    public void ShowUI()
    {
        if (spellOverviewUI)
            spellOverviewUI.SetActive(true);
    }

    public void HideUI()
    {
        if (spellOverviewUI)
            spellOverviewUI.SetActive(false);
    }
}