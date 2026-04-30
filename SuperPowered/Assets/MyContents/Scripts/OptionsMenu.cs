using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsMenu : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParam = "MasterVolume";
    [SerializeField] private string musicVolumeParam = "MusicVolume";

    [Header("UI")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Toggle fullscreenToggle;

    private const string MasterVolumeKey = "MasterVolume";
    private const string MusicVolumeKey = "MusicVolume";
    private const string FullscreenKey = "Fullscreen";

    [SerializeField] private MainMenuController menuController;
    [SerializeField] private GameObject optionsPanel;

    public void PressBack()
    {
        PlayerPrefs.Save();

        if (menuController && optionsPanel)
        {
            menuController.ToggleObjectWithFade(optionsPanel);
        }
    }

    private void Start()
    {
        float masterVolume = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
        float musicVolume = PlayerPrefs.GetFloat(MusicVolumeKey, 1f);
        bool fullscreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;

        if (masterVolumeSlider)
            masterVolumeSlider.value = masterVolume;

        if (musicVolumeSlider)
            musicVolumeSlider.value = musicVolume;

        if (fullscreenToggle)
            fullscreenToggle.isOn = fullscreen;

        SetMasterVolume(masterVolume);
        SetMusicVolume(musicVolume);
        SetFullscreen(fullscreen);
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);

        if (audioMixer)
            audioMixer.SetFloat(masterVolumeParam, Mathf.Log10(value) * 20f);

        PlayerPrefs.SetFloat(MasterVolumeKey, value);
    }

    public void SetMusicVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);

        if (audioMixer)
            audioMixer.SetFloat(musicVolumeParam, Mathf.Log10(value) * 20f);

        PlayerPrefs.SetFloat(MusicVolumeKey, value);
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
    }

    public void ApplyAndSave()
    {
        PlayerPrefs.Save();
    }
}