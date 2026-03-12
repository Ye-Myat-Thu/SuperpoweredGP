using UnityEngine;
using UnityEngine.UI;

public class HUDUniversalBars : MonoBehaviour
{
    [Header("Target (leave empty to auto-find Player by tag)")]
    [SerializeField] private BaseCharacter target;

    [Header("UI References")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Image xpFill;

    private void Awake()
    {
        if (!target)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player) target = player.GetComponent<BaseCharacter>();
        }
    }

    private void OnEnable()
    {
        Bind(target);
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Bind(BaseCharacter bc)
    {
        target = bc;
        if (!target) return;

        target.OnHealthChanged += HandleHealthChanged;
        target.OnXPChanged += HandleXPChanged;

        // Initial refresh WITHOUT invoking events
        HandleHealthChanged(target.CurrentHealth, target.MaxHealth);
        HandleXPChanged(target.CurrentXP, target.XPToNext);
    }

    private void Unbind()
    {
        if (!target) return;

        target.OnHealthChanged -= HandleHealthChanged;
        target.OnXPChanged -= HandleXPChanged;
    }

    private void HandleHealthChanged(float current, float max)
    {
        if (!healthFill) return;
        float t = (max <= 0.0001f) ? 0f : Mathf.Clamp01(current / max);
        healthFill.fillAmount = t;
    }

    private void HandleXPChanged(float current, float toNext)
    {
        if (!xpFill) return;
        float t = (toNext <= 0.0001f) ? 0f : Mathf.Clamp01(current / toNext);
        xpFill.fillAmount = t; // starts empty at 0 XP
    }

    // Optional: if you swap heroes at runtime
    public void SetTarget(BaseCharacter newTarget)
    {
        if (target == newTarget) return;

        Unbind();
        Bind(newTarget);
    }
}