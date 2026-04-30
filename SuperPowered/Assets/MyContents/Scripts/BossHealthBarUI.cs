using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private BossEnemy boss;
    [SerializeField] private GameObject root;
    [SerializeField] private Image healthFill;
    [SerializeField] private TMP_Text bossNameText;

    [Header("Text")]
    [SerializeField] private string bossName = "Boss";

    private void Awake()
    {
        if (root)
            root.SetActive(false);

        if (bossNameText)
            bossNameText.text = bossName;
    }

    private void OnEnable()
    {
        EnemySpawnDirector.OnBossSpawned += RegisterBoss;
    }

    private void OnDisable()
    {
        EnemySpawnDirector.OnBossSpawned -= RegisterBoss;
        UnsubscribeBoss();
    }

    private void RegisterBoss(BossEnemy newBoss)
    {
        if (!newBoss) return;

        UnsubscribeBoss();

        boss = newBoss;

        boss.OnBossHealthChanged += UpdateHealth;
        boss.OnBossDied += Hide;

        Show();
        UpdateHealth(boss.CurrentHealth, boss.MaxHealth);

        Debug.Log("Boss HP UI registered spawned boss.");
    }

    private void UnsubscribeBoss()
    {
        if (!boss) return;

        boss.OnBossHealthChanged -= UpdateHealth;
        boss.OnBossDied -= Hide;
    }

    private void Show()
    {
        if (root)
            root.SetActive(true);
    }

    private void Hide()
    {
        if (root)
            root.SetActive(false);
    }

    private void UpdateHealth(float current, float max)
    {
        if (!healthFill) return;

        float t = max <= 0f ? 0f : Mathf.Clamp01(current / max);
        healthFill.fillAmount = t;
    }
}