using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Gameplay Scene Names")]
    [SerializeField] private string[] gameplayScenes =
    {
        "Titan_Scene",
        "Stalker_Scene",
        "Oracle_Scene"
    };

    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [SerializeField] private SceneFade gameOverFade;

    [Header("Music")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip[] menuMusic;
    [SerializeField] private AudioClip[] gameplayMusic;
    [SerializeField] private AudioClip bossFightMusic;

    [Header("Character Selection")]
    [SerializeField] private SelectCharacter characterSelector;

    [Header("Post Processing")]
    [SerializeField] private Volume globalVolume;

    [Header("UI")]
    [SerializeField] private GameObject victoryUI;
    [SerializeField] private GameObject gameOverUI;

    [Header("Timer")]
    [SerializeField] private TMP_Text victoryTimerText;
    [SerializeField] private TMP_Text gameOverTimerText;

    [Header("Victory Seqience")]
    [SerializeField] private float victorySlowMotionScale = 0.25f;
    //[SerializeField] private float victorySlowMotionDuration = 2f;
    [SerializeField] private float victoryUIDelay = 2f;
    [SerializeField] private float victoryUIFadeAlpha = 0.75f;
    [SerializeField] private CanvasGroup victoryCanvasGroup;

    [Header("Pause UI")]
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private CanvasGroup pauseCanvasGroup;
    [SerializeField] private float pauseFadeDuration = 0.15f;

    private bool isPaused;
    private Coroutine pauseFadeRoutine;

    private Vignette vignette;
    private ColorAdjustments colorAdjustments;

    private float runTimer;
    private bool timerRunning;

    private AudioClip[] currentPlaylist;
    private int currentTrackIndex;
    private bool isBossMusicPlaying;
    private bool gameEnded;

    private void Awake()
    {
        if (globalVolume && globalVolume.profile)
        {
            globalVolume.profile.TryGet(out vignette);
            globalVolume.profile.TryGet(out colorAdjustments);
        }

        Instance = this;

        //if (Instance != null && Instance != this)
        //{
        //    Destroy(gameObject);
        //    return;
        //}

        //Instance = this;
        //DontDestroyOnLoad(gameObject);

        if (!musicSource)
            musicSource = GetComponent<AudioSource>();

        SceneManager.sceneLoaded += OnSceneLoaded;
        EnemySpawnDirector.OnBossSpawned += OnBossSpawned;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EnemySpawnDirector.OnBossSpawned -= OnBossSpawned;
    }

    private void Start()
    {
        SetupSceneMusic(SceneManager.GetActiveScene().name);
    }

    private void Update()
    {
        if (timerRunning && !gameEnded)
        {
            runTimer += Time.deltaTime;
        }

        if (!musicSource || isBossMusicPlaying) return;

        if (!musicSource.isPlaying && currentPlaylist != null && currentPlaylist.Length > 0)
        {
            PlayNextTrack();
        }

    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        gameEnded = false;
        isBossMusicPlaying = false;

        SetupSceneUI();
        SetupSceneMusic(scene.name);
        StartCoroutine(RegisterPlayerDeathDelayed());

        if (IsGameplayScene(scene.name))
        {
            runTimer = 0f;
            timerRunning = true;
        }
        else
        {
            runTimer = 0f;
            timerRunning = false;
        }
    }

    private void SetupSceneMusic(string sceneName)
    {
        if (IsGameplayScene(sceneName))
        {
            SetPlaylist(gameplayMusic);
        }
        else
        {
            SetPlaylist(menuMusic);
        }
    }

    private bool IsGameplayScene(string sceneName)
    {
        for (int i = 0; i < gameplayScenes.Length; i++)
        {
            if (sceneName == gameplayScenes[i])
                return true;
        }

        return false;
    }

    private void SetPlaylist(AudioClip[] playlist)
    {
        currentPlaylist = playlist;
        currentTrackIndex = 0;

        if (currentPlaylist == null || currentPlaylist.Length == 0)
            return;

        ShufflePlaylist(currentPlaylist);
        PlayNextTrack();
    }

    private void PlayNextTrack()
    {
        if (currentPlaylist == null || currentPlaylist.Length == 0) return;

        if (currentTrackIndex >= currentPlaylist.Length)
        {
            currentTrackIndex = 0;
            ShufflePlaylist(currentPlaylist);
        }

        AudioClip clip = currentPlaylist[currentTrackIndex];
        currentTrackIndex++;

        if (!clip) return;

        musicSource.loop = false;
        musicSource.clip = clip;
        musicSource.Play();
    }

    private void ShufflePlaylist(AudioClip[] playlist)
    {
        for (int i = 0; i < playlist.Length; i++)
        {
            int randomIndex = Random.Range(i, playlist.Length);
            AudioClip temp = playlist[i];
            playlist[i] = playlist[randomIndex];
            playlist[randomIndex] = temp;
        }
    }

    private void OnBossSpawned(BossEnemy boss)
    {
        if (!boss) return;

        boss.OnBossDied += OnBossDied;

        PlayBossMusic();
    }

    private void PlayBossMusic()
    {
        if (!musicSource || !bossFightMusic) return;

        isBossMusicPlaying = true;

        musicSource.Stop();
        musicSource.clip = bossFightMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

    private void OnBossDied()
    {
        if (gameEnded) return;

        gameEnded = true;
        StartCoroutine(VictorySequenceRoutine());
        //if (musicSource)
        //    musicSource.Stop();

        //ShowVictoryUI();
    }

    //private void RegisterPlayerDeath()
    //{
    //    BaseCharacter player = FindFirstObjectByType<BaseCharacter>();

    //    if (player != null)
    //    {
    //        player.OnCharacterDied += OnPlayerDied;
    //    }
    //}

    private IEnumerator VictorySequenceRoutine()
    {
        timerRunning = false;

        Time.timeScale = victorySlowMotionScale;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;

        if (vignette != null)
        {
            StartCoroutine(LerpVignette(0.25f, 0.6f));
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = 0.3f;
        }

        yield return new WaitForSecondsRealtime(victoryUIDelay);

        if (musicSource)
            musicSource.Stop();

        if (victoryTimerText)
            victoryTimerText.text = "Completion Time:\n" + FormatTime(runTimer);

        if (victoryUI)
            victoryUI.SetActive(true);

        if (victoryCanvasGroup)
        {
            victoryCanvasGroup.alpha = victoryUIFadeAlpha;
            victoryCanvasGroup.interactable = true;
            victoryCanvasGroup.blocksRaycasts = true;
        }
    }

    private IEnumerator RegisterPlayerDeathDelayed()
    {
        yield return null;

        BaseCharacter player = FindFirstObjectByType<BaseCharacter>();

        if (player != null)
        {
            player.OnCharacterDied += OnPlayerDied;
            Debug.Log("Player death registered.");
        }
        else
        {
            Debug.LogWarning("No BaseCharacter found for death registration.");
        }
    }

    private void OnPlayerDied()
    {
        if (gameEnded) return;

        gameEnded = true;

        if (musicSource)
            musicSource.Stop();

        ShowGameOverUI();
    }

    private void SetupSceneUI()
    {
        if (!gameOverFade)
            gameOverFade = FindFirstObjectByType<SceneFade>(FindObjectsInactive.Include);

        if (!victoryUI)
            victoryUI = GameObject.Find("Victory UI");

        if (!gameOverUI)
            gameOverUI = GameObject.Find("Game Over UI");

        if (victoryUI)
            victoryUI.SetActive(false);

        if (gameOverUI)
            gameOverUI.SetActive(false);

        if (!pauseUI)
            pauseUI = GameObject.Find("PauseMenu");

        if (pauseUI)
            pauseUI.SetActive(false);

        if (!pauseCanvasGroup && pauseUI)
            pauseCanvasGroup = pauseUI.GetComponent<CanvasGroup>();

    }

    private void ShowVictoryUI()
    {
        timerRunning = false;

        if (victoryTimerText)
            victoryTimerText.text = "Time: " + FormatTime(runTimer);

        if (victoryUI)
            victoryUI.SetActive(true);
    }

    private void ShowGameOverUI()
    {
        timerRunning = false;

        if (gameOverTimerText)
            gameOverTimerText.text = "Time Survived:\n" + FormatTime(runTimer);

        if (vignette != null)
        {
            StartCoroutine(LerpVignette(0.45f, 0.5f));
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = -1.2f;
        }

        if (gameOverFade)
        {
            StartCoroutine(gameOverFade.GameOverFadeSequence(1f, 1f, 1f));
        }
        else if (gameOverUI)
        {
            gameOverUI.SetActive(true);
        }
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        return $"{minutes:00}:{seconds:00}";
    }

    public void TogglePause()
    {
        if (gameEnded) return;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void LoadSelectedCharacterScene()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (!characterSelector)
            characterSelector = FindFirstObjectByType<SelectCharacter>();

        if (!characterSelector)
        {
            Debug.LogWarning("No SelectCharacter found in scene.");
            return;
        }

        int selectedIndex = characterSelector.number;

        if (selectedIndex < 0 || selectedIndex >= gameplayScenes.Length)
        {
            Debug.LogWarning("Selected character index is outside gameplayScenes array.");
            return;
        }

        SceneManager.LoadScene(gameplayScenes[selectedIndex]);
    }

    public void PauseGame()
    {
        if (isPaused) return;

        isPaused = true;

        if (pauseUI)
            pauseUI.SetActive(true);

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = -0.5f;
        }

        if (musicSource)
            musicSource.Pause();

        Time.timeScale = 0f;
        Time.fixedDeltaTime = 0.02f;

        if (pauseFadeRoutine != null)
            StopCoroutine(pauseFadeRoutine);

        pauseFadeRoutine = StartCoroutine(FadePauseUI(0f, 1f));
    }

    public void ResumeGame()
    {
        if (!isPaused) return;

        isPaused = false;

        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (colorAdjustments != null)
        {
            colorAdjustments.postExposure.value = 0f;
        }

        if (vignette != null)
        {
            StartCoroutine(LerpVignette(0f, 0.3f));
        }

        if (musicSource)
            musicSource.UnPause();

        if (pauseFadeRoutine != null)
            StopCoroutine(pauseFadeRoutine);

        pauseFadeRoutine = StartCoroutine(FadePauseUI(1f, 0f));
    }

    private IEnumerator FadePauseUI(float startAlpha, float targetAlpha)
    {
        if (!pauseCanvasGroup)
            yield break;

        float elapsed = 0f;

        pauseCanvasGroup.interactable = targetAlpha > 0f;
        pauseCanvasGroup.blocksRaycasts = targetAlpha > 0f;

        while (elapsed < pauseFadeDuration)
        {
            float t = elapsed / pauseFadeDuration;
            pauseCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        pauseCanvasGroup.alpha = targetAlpha;

        if (Mathf.Approximately(targetAlpha, 0f) && pauseUI)
            pauseUI.SetActive(false);
    }

    private IEnumerator LerpVignette(float target, float duration)
    {
        if (vignette == null) yield break;

        float start = vignette.intensity.value;
        float t = 0f;

        while (t < duration)
        {
            vignette.intensity.value = Mathf.Lerp(start, target, t / duration);
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        vignette.intensity.value = target;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;

        if (musicSource)
            musicSource.Stop();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
