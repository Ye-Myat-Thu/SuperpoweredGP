using UnityEngine;
using UnityEngine.UI;

public class AbilityCooldownFillUI : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private MonoBehaviour ability; 

    [Header("UI")]
    [SerializeField] private Image iconImage;

    public enum FillMode
    {
        FullWhenReady_CountdownToEmpty, 
        EmptyWhenReady_FillUpToFull     
    }

    [SerializeField] private FillMode fillMode = FillMode.FullWhenReady_CountdownToEmpty;

    private void Awake()
    {
        if (!iconImage) iconImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (!iconImage || ability == null) return;

        //read via known properties
        float cd = 0f;
        float remaining = 0f;

        // Whirl
        if (ability is TitanWhirlAbility whirl)
        {
            cd = whirl.CooldownDuration;
            remaining = whirl.CooldownRemaining;
        }
        // Rage
        else if (ability is Rage rage)
        {
            cd = rage.CooldownDuration;
            remaining = rage.CooldownRemaining;
        }
        // Iron Skin
        else if (ability is IronSkin iron)
        {
            cd = iron.CooldownDuration;
            remaining = iron.CooldownRemaining;
        }
        else
        {
            return;
        }

        float t = (cd <= 0.0001f) ? 0f : Mathf.Clamp01(remaining / cd);

        //fill behavior depending on how your Filled image is supposed to work
        iconImage.fillAmount = (fillMode == FillMode.FullWhenReady_CountdownToEmpty)
            ? (1f - t)  // cooldown: t=1 -> 0, ready: t=0 -> 1
            : t;        // cooldown: t=1 -> 1, ready: t=0 -> 0
    }
}
