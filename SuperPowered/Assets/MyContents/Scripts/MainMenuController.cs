using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class MainMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private CanvasGroup buttonsGroup;
    [SerializeField] private CanvasGroup characterSelectionGroup;
    [SerializeField] private GameObject mainMenuRoot;

    [Header("Start Screen")]
    [SerializeField] private CanvasGroup startScreenGroup;
    [SerializeField] private float startFadeDuration = 0.5f;

    [Header("Cinemachine")]
    [SerializeField] private CinemachineCamera menuCamera;
    [SerializeField] private float mainMenuFollowOffsetY = 12f;
    [SerializeField] private float characterSelectFollowOffsetY = 5f;
    [SerializeField] private float cameraTransitionDuration = 1f;

    [Header("Fade Timings")]
    [SerializeField] private float titleFadeDuration = 1f;
    [SerializeField] private float buttonsFadeDuration = 0.6f;
    [SerializeField] private float characterFadeDuration = 0.5f;

    [Header("Character Selection Extras")]
    [SerializeField] private SelectCharacter selectCharacter;
    [SerializeField] private CanvasGroup[] spellOverviewGroups;

    private CinemachineFollow follow;
    private bool startPressed;
    private bool isTransitioning;

    private void Awake()
    {
        if (menuCamera)
            follow = menuCamera.GetComponent<CinemachineFollow>();

        if (mainMenuRoot)
            mainMenuRoot.SetActive(false);

        SetCanvasGroup(startScreenGroup, 1f, true);
        SetCanvasGroup(titleGroup, 0f, false);
        SetCanvasGroup(buttonsGroup, 0f, false);
        SetCanvasGroup(characterSelectionGroup, 0f, false);

        HideAllSpellOverviews();
    }

    private void Start()
    {
        StartCoroutine(StartScreenRoutine());
    }

    private void Update()
    {
        if (!startPressed)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
            {
                startPressed = true;
                StartCoroutine(EnterMainMenuRoutine());
            }
        }
    }

    private IEnumerator StartScreenRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(BlinkPressStart());
    }

    private IEnumerator BlinkPressStart()
    {
        while (!startPressed)
        {
            if (startScreenGroup)
                startScreenGroup.alpha = Mathf.PingPong(Time.time * 1.5f, 1f);

            yield return null;
        }
    }

    private IEnumerator EnterMainMenuRoutine()
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        yield return FadeCanvasGroup(startScreenGroup, 1f, 0f, startFadeDuration);

        if (mainMenuRoot)
            mainMenuRoot.SetActive(true);

        yield return FadeCanvasGroup(titleGroup, 0f, 1f, titleFadeDuration);
        yield return FadeCanvasGroup(buttonsGroup, 0f, 1f, buttonsFadeDuration);

        SetCanvasGroup(buttonsGroup, 1f, true);

        isTransitioning = false;
    }

    public void PressPlay()
    {
        if (isTransitioning) return;
        StartCoroutine(GoToCharacterSelectionRoutine());
    }

    private IEnumerator GoToCharacterSelectionRoutine()
    {
        isTransitioning = true;

        SetCanvasGroup(buttonsGroup, 1f, false);

        StartCoroutine(FadeCanvasGroup(titleGroup, 1f, 0f, 0.3f));
        yield return FadeCanvasGroup(buttonsGroup, 1f, 0f, 0.3f);

        if (mainMenuRoot)
            mainMenuRoot.SetActive(false);

        yield return FadeCameraFollowOffsetY(
            mainMenuFollowOffsetY,
            characterSelectFollowOffsetY,
            cameraTransitionDuration
        );

        yield return FadeCanvasGroup(characterSelectionGroup, 0f, 1f, characterFadeDuration);
        SetCanvasGroup(characterSelectionGroup, 1f, true);

        if (selectCharacter)
{
            selectCharacter.number = 0;

            for (int i = 0; i < selectCharacter.characters.Length; i++)
                selectCharacter.characters[i].SetActive(false);

            if (selectCharacter.characters.Length > 0)
                selectCharacter.characters[0].SetActive(true);
        }

        yield return ShowCurrentSpellOverview();

        isTransitioning = false;
    }

    public void PressBackFromCharacterSelection()
    {
        if (isTransitioning) return;
        StartCoroutine(BackToMainMenuRoutine());
    }

    private IEnumerator BackToMainMenuRoutine()
    {
        isTransitioning = true;

        HideAllSpellOverviews();

        SetCanvasGroup(characterSelectionGroup, characterSelectionGroup.alpha, false);

        yield return FadeCanvasGroup(characterSelectionGroup, 1f, 0f, 0.3f);

        yield return FadeCameraFollowOffsetY(
            characterSelectFollowOffsetY,
            mainMenuFollowOffsetY,
            cameraTransitionDuration
        );

        if (mainMenuRoot)
            mainMenuRoot.SetActive(true);

        yield return FadeCanvasGroup(titleGroup, 0f, 1f, 0.4f);
        yield return FadeCanvasGroup(buttonsGroup, 0f, 1f, 0.3f);

        SetCanvasGroup(buttonsGroup, 1f, true);

        isTransitioning = false;
    }

    public void ChangeCharacterAndOverview(int direction)
    {
        if (isTransitioning) return;
        if (!selectCharacter) return;

        StartCoroutine(ChangeCharacterAndOverviewRoutine(direction));
    }

    private IEnumerator ChangeCharacterAndOverviewRoutine(int direction)
    {
        isTransitioning = true;

        HideAllSpellOverviews();

        selectCharacter.ChangeCharacter(direction);

        yield return ShowCurrentSpellOverview();

        isTransitioning = false;
    }

    private void HideAllSpellOverviews()
    {
        if (spellOverviewGroups == null) return;

        for (int i = 0; i < spellOverviewGroups.Length; i++)
        {
            if (!spellOverviewGroups[i]) continue;

            spellOverviewGroups[i].alpha = 0f;
            spellOverviewGroups[i].interactable = false;
            spellOverviewGroups[i].blocksRaycasts = false;
            spellOverviewGroups[i].gameObject.SetActive(false);
        }
    }

    private IEnumerator ShowCurrentSpellOverview()
    {
        if (!selectCharacter) yield break;
        if (spellOverviewGroups == null) yield break;

        int index = selectCharacter.number;

        if (index < 0 || index >= spellOverviewGroups.Length) yield break;

        CanvasGroup group = spellOverviewGroups[index];
        if (!group) yield break;

        yield return FadeCanvasGroup(group, 0f, 1f, characterFadeDuration);
        SetCanvasGroup(group, 1f, true);
    }

    private IEnumerator FadeCameraFollowOffsetY(float startY, float targetY, float duration)
    {
        if (!follow) yield break;

        float elapsed = 0f;
        Vector3 offset = follow.FollowOffset;
        offset.y = startY;
        follow.FollowOffset = offset;

        while (elapsed < duration)
        {
            float t = elapsed / duration;

            offset = follow.FollowOffset;
            offset.y = Mathf.Lerp(startY, targetY, t);
            follow.FollowOffset = offset;

            elapsed += Time.deltaTime;
            yield return null;
        }

        offset = follow.FollowOffset;
        offset.y = targetY;
        follow.FollowOffset = offset;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float start, float target, float duration)
    {
        if (!group) yield break;

        group.gameObject.SetActive(true);

        float elapsed = 0f;
        group.alpha = start;
        group.interactable = false;
        group.blocksRaycasts = false;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            group.alpha = Mathf.Lerp(start, target, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        group.alpha = target;

        bool visible = target > 0f;
        group.interactable = visible;
        group.blocksRaycasts = visible;

        if (!visible)
            group.gameObject.SetActive(false);
    }

    private void SetCanvasGroup(CanvasGroup group, float alpha, bool interactable)
    {
        if (!group) return;

        group.alpha = alpha;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
        group.gameObject.SetActive(alpha > 0f);
    }

    public void ToggleObjectWithFade(GameObject target)
    {
        if (!target) return;

        CanvasGroup group = target.GetComponent<CanvasGroup>();

        if (target.activeSelf)
        {
            if (group)
                StartCoroutine(FadeObjectRoutine(target, group, group.alpha, 0f, 0.25f));
            else
                target.SetActive(false);
        }
        else
        {
            target.SetActive(true);

            if (group)
                StartCoroutine(FadeObjectRoutine(target, group, 0f, 1f, 0.25f));
        }
    }

    private IEnumerator FadeObjectRoutine(GameObject target, CanvasGroup group, float start, float end, float duration)
    {
        float elapsed = 0f;

        group.alpha = start;
        group.interactable = false;
        group.blocksRaycasts = false;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            group.alpha = Mathf.Lerp(start, end, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        group.alpha = end;

        bool enabled = end > 0f;
        group.interactable = enabled;
        group.blocksRaycasts = enabled;

        if (!enabled)
            target.SetActive(false);
    }
}