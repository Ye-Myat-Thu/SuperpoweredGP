using UnityEngine;

public class CharacterSelectionOverviewAutoUI : MonoBehaviour
{
    [SerializeField] private GameObject spellOverviewUI;

    private void OnEnable()
    {
        if (spellOverviewUI)
            spellOverviewUI.SetActive(true);
    }

    private void OnDisable()
    {
        if (spellOverviewUI)
            spellOverviewUI.SetActive(false);
    }
}