using UnityEngine;

public class BrainwashedUnit : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] private BaseEnemy baseEnemy;
    [SerializeField] private BaseAlly baseAlly;
    [SerializeField] private Renderer[] tintRenderers;
    [SerializeField] private GameObject allyHpBarRoot;

    private bool isBrainwashed;
    private Color[] originalColors;

    public bool IsDead => baseEnemy != null && baseEnemy.IsDead;
    public bool IsBrainwashed => isBrainwashed;

    private void Awake()
    {
        if (!baseEnemy) baseEnemy = GetComponent<BaseEnemy>();
        if (!baseAlly) baseAlly = GetComponent<BaseAlly>();

        if (tintRenderers == null || tintRenderers.Length == 0)
            tintRenderers = GetComponentsInChildren<Renderer>(true);
    }

    public void ApplyBrainwash(Color tint)
    {
        if (isBrainwashed) return;

        isBrainwashed = true;

        CacheOriginalColors();
        ApplyTint(tint);

        if (baseEnemy != null)
            baseEnemy.enabled = false;

        if (baseAlly != null)
            baseAlly.enabled = true;

        if (allyHpBarRoot != null)
            allyHpBarRoot.SetActive(true);
    }

    private void CacheOriginalColors()
    {
        originalColors = new Color[tintRenderers.Length];

        for (int i = 0; i < tintRenderers.Length; i++)
        {
            if (tintRenderers[i] != null && tintRenderers[i].material.HasProperty("_Color"))
                originalColors[i] = tintRenderers[i].material.color;
        }
    }

    private void ApplyTint(Color tint)
    {
        for (int i = 0; i < tintRenderers.Length; i++)
        {
            if (tintRenderers[i] == null) continue;
            if (tintRenderers[i].material.HasProperty("_Color"))
                tintRenderers[i].material.color = tint;
        }
    }
}
