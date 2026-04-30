using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneFade : MonoBehaviour
{
    [SerializeField] private Image sceneFadeImage;
    [SerializeField] private TMP_Text gameoverText;
    [SerializeField] private CanvasGroup buttonsGroup;

    private void Awake()
    {
        if (!sceneFadeImage) sceneFadeImage = GetComponent<Image>();
        if (!gameoverText) gameoverText = GetComponentInChildren<TMP_Text>();

        SetImageAlpha(0f);
        SetTextAlpha(0f);

        if (buttonsGroup)
            buttonsGroup.alpha = 0f;

        gameObject.SetActive(false);
    }

    public IEnumerator FadeInCoroutine(float duration)
    {
        gameObject.SetActive(true);

        Color startColor = new Color(sceneFadeImage.color.r, sceneFadeImage.color.g, sceneFadeImage.color.b, 1f);
        Color targetColor = new Color(sceneFadeImage.color.r, sceneFadeImage.color.g, sceneFadeImage.color.b, 0f);

        yield return FadeImageCoroutine(startColor, targetColor, duration);

        gameObject.SetActive(false);
    }

    public IEnumerator GameOverFadeSequence(float imageFadeDuration, float textFadeDuration, float buttonsFadeDuration)
    {
        gameObject.SetActive(true);

        SetImageAlpha(0f);
        SetTextAlpha(0f);

        if (buttonsGroup)
        {
            buttonsGroup.alpha = 0f;
            buttonsGroup.interactable = false;
            buttonsGroup.blocksRaycasts = false;
        }

        Color imageStart = new Color(sceneFadeImage.color.r, sceneFadeImage.color.g, sceneFadeImage.color.b, 0f);
        Color imageTarget = new Color(sceneFadeImage.color.r, sceneFadeImage.color.g, sceneFadeImage.color.b, 1f);

        yield return FadeImageCoroutine(imageStart, imageTarget, imageFadeDuration);

        yield return FadeTextCoroutine(0f, 1f, textFadeDuration);

        if (buttonsGroup)
        {
            yield return FadeCanvasGroupCoroutine(buttonsGroup, 0f, 1f, buttonsFadeDuration);

            buttonsGroup.interactable = true;
            buttonsGroup.blocksRaycasts = true;
        }
    }

    public IEnumerator FadeOutCoroutine(float duration)
    {
        gameObject.SetActive(true);

        Color startColor = new Color(sceneFadeImage.color.r, sceneFadeImage.color.g, sceneFadeImage.color.b, 0f);
        Color targetColor = new Color(sceneFadeImage.color.r, sceneFadeImage.color.g, sceneFadeImage.color.b, 1f);

        
        yield return FadeImageCoroutine (startColor, targetColor, duration);
    }

    private IEnumerator FadeImageCoroutine(Color startColor, Color targetColor, float duration)
    {
        float elapsedTime = 0f;
        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            sceneFadeImage.color = Color.Lerp(startColor, targetColor, t);

            elapsedTime += Time.deltaTime;
            yield return null;
            
        }

        sceneFadeImage.color = targetColor;
    }

    private IEnumerator FadeTextCoroutine(float startAlpha, float targetAlpha, float duration)
    {
        float elapsedTime = 0f;

        Color c = gameoverText.color;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
            gameoverText.color = c;

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        c.a = targetAlpha;
        gameoverText.color = c;
    }

    private IEnumerator FadeCanvasGroupCoroutine(CanvasGroup group, float startAlpha, float targetAlpha, float duration)
    {
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            group.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        group.alpha = targetAlpha;
    }

    private void SetImageAlpha(float alpha)
    {
        if (!sceneFadeImage) return;

        Color c = sceneFadeImage.color;
        c.a = alpha;
        sceneFadeImage.color = c;
    }

    private void SetTextAlpha(float alpha)
    {
        if (!gameoverText) return;

        Color c = gameoverText.color;
        c.a = alpha;
        gameoverText.color = c;
    }
}
