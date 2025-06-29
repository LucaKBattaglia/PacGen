using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuManager : MonoBehaviour
{
    public static bool GameIsPaused = false;

    public GameObject pauseMenuUI;
    public GameObject settingsPanel;

    public Slider musicSlider;
    public Slider sfxSlider;

    public AudioSource musicSource;
    public List<AudioSource> sfxSources = new List<AudioSource>();

    private void Start()
    {
        // Start with settings panel hidden
        settingsPanel.SetActive(false);
        pauseMenuUI.SetActive(false);

        // Load saved volumes or default to 10
        float defaultVolume = 10f;

        if (!PlayerPrefs.HasKey("MusicVolume"))
            PlayerPrefs.SetFloat("MusicVolume", defaultVolume);
        if (!PlayerPrefs.HasKey("SFXVolume"))
            PlayerPrefs.SetFloat("SFXVolume", defaultVolume);

        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultVolume);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", defaultVolume);

        // Apply and connect sliders
        musicSlider.value = musicVolume;
        sfxSlider.value = sfxVolume;

        musicSlider.onValueChanged.AddListener(UpdateMusicVolume);
        sfxSlider.onValueChanged.AddListener(UpdateSFXVolume);

        UpdateMusicVolume(musicVolume);
        UpdateSFXVolume(sfxVolume);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        GameIsPaused = false;
    }

    private void Pause()
    {
        pauseMenuUI.SetActive(true);
        settingsPanel.SetActive(false);
        Time.timeScale = 0f;
        GameIsPaused = true;
    }

    public void OpenSettings()
    {
        pauseMenuUI.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void BackToPauseMenu()
    {
        settingsPanel.SetActive(false);
        pauseMenuUI.SetActive(true);
    }

    public void LoadMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("StartScene");
    }

    public void QuitGame()
    {
        Debug.Log("Quitting game...");
        Application.Quit();
    }

    public void UpdateMusicVolume(float sliderValue)
    {
        float normalizedVolume = sliderValue / 10f;
        if (musicSource != null)
        {
            musicSource.volume = normalizedVolume;
            PlayerPrefs.SetFloat("MusicVolume", sliderValue);
        }
    }

    public void UpdateSFXVolume(float sliderValue)
    {
        float normalizedVolume = sliderValue / 10f;
        foreach (var sfx in sfxSources)
        {
            if (sfx != null)
                sfx.volume = normalizedVolume;
        }
        PlayerPrefs.SetFloat("SFXVolume", sliderValue);
    }
}
