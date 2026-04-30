using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class BossWarningVignette : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject warningRoot;
    [SerializeField] private Image vignetteImage;
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private CameraShake cameraShake;

    [Header("Settings")]
    [SerializeField] private string warningMessage = "DANGER";
    [SerializeField] private float maxAlpha = 0.45f;
    [SerializeField] private float fadeInTime = 0.15f;
    [SerializeField] private float holdTime = 0.4f;
    [SerializeField] private float fadeOutTime = 0.35f;

    private Coroutine routine;

    private void Awake()
    {
        if (!warningRoot)
            warningRoot = gameObject;

        if (!vignetteImage)
            vignetteImage = GetComponentInChildren<Image>(true);

        if (!warningText)
            warningText = GetComponentInChildren<TMP_Text>(true);

        if (!cameraShake)
            cameraShake = FindFirstObjectByType<CameraShake>();

        if (warningText)
            warningText.text = warningMessage;

        SetAlpha(0f);
        SetTextAlpha(0f);

        warningRoot.SetActive(false);
    }

    private void OnEnable()
    {
        EnemySpawnDirector.OnBossSpawned += RegisterBoss;
    }

    private void OnDisable()
    {
        EnemySpawnDirector.OnBossSpawned -= RegisterBoss;
    }

    private void RegisterBoss(BossEnemy boss)
    {
        if (!boss) return;

        boss.SetWarningReferences(this);

        Debug.Log("BossWarningVignette linked to spawned boss.");
    }

    public void PlayWarning()
    {
        if (routine != null)
            StopCoroutine(routine);

        warningRoot.SetActive(true);
        routine = StartCoroutine(WarningRoutine());
    }

    private IEnumerator WarningRoutine()
    {
        yield return FadeTo(maxAlpha, fadeInTime);
        yield return FadeTextTo(1f, fadeInTime);

        yield return new WaitForSeconds(holdTime);

        yield return FadeTo(0f, fadeOutTime);
        yield return FadeTextTo(0f, fadeOutTime);

        warningRoot.SetActive(false);
        routine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float time)
    {
        float startAlpha = vignetteImage.color.a;
        float timer = 0f;

        while (timer < time)
        {
            timer += Time.deltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, timer / time));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private IEnumerator FadeTextTo(float targetAlpha, float time)
    {
        if (!warningText) yield break;

        float startAlpha = warningText.color.a;
        float timer = 0f;

        while (timer < time)
        {
            timer += Time.deltaTime;

            Color c = warningText.color;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, timer / time);
            warningText.color = c;

            yield return null;
        }

        SetTextAlpha(targetAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (!vignetteImage) return;

        Color c = vignetteImage.color;
        c.a = alpha;
        vignetteImage.color = c;
    }

    private void SetTextAlpha(float alpha)
    {
        if (!warningText) return;

        Color c = warningText.color;
        c.a = alpha;
        warningText.color = c;
    }
}