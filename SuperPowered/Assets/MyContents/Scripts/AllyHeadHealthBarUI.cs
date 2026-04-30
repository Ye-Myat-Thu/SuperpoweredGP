using UnityEngine;
using UnityEngine.UI;

public class AllyHeadHealthBarUI : MonoBehaviour
{
    [Header("Ref")]
    [SerializeField] private BaseEnemy targetEnemy;
    [SerializeField] private Image fillImage;
    [SerializeField] private bool billboardToCamera = true;

    private void Awake()
    {
        if (!targetEnemy)
            targetEnemy = GetComponentInParent<BaseEnemy>();
    }

    private void OnEnable()
    {
        if (targetEnemy != null)
            targetEnemy.OnHealthChanged += Refresh;

        if (targetEnemy != null)
            Refresh(targetEnemy.CurrentHealth, targetEnemy.MaxHealth);
    }

    private void OnDisable()
    {
        if (targetEnemy != null)
            targetEnemy.OnHealthChanged -= Refresh;
    }

    private void LateUpdate()
    {
        if (!billboardToCamera) return;
        if (!Camera.main) return;

        transform.forward = Camera.main.transform.forward;
    }

    private void Refresh(float current, float max)
    {
        if (!fillImage) return;
        fillImage.fillAmount = max > 0f ? current / max : 0f;
    }
}
