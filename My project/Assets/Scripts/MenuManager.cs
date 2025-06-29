using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static bool GameIsPaused = false;

    public GameObject MenuPanel;
    public GameObject SettingsPanel;

    public Slider musicSlider;
    public Slider sfxSlider;

    public AudioSource musicSource;
    public List<AudioSource> sfxSources = new List<AudioSource>();
    
    private void Start()
    {
        SettingsPanel.SetActive(false);
        MenuPanel.SetActive(true);

        float defaultVolume = 10f; // Slider range is 0–10

        // If no saved values, set and store defaults
        if (!PlayerPrefs.HasKey("MusicVolume"))
            PlayerPrefs.SetFloat("MusicVolume", defaultVolume);

        if (!PlayerPrefs.HasKey("SFXVolume"))
            PlayerPrefs.SetFloat("SFXVolume", defaultVolume);

        float musicVolume = PlayerPrefs.GetFloat("MusicVolume", defaultVolume);
        float sfxVolume = PlayerPrefs.GetFloat("SFXVolume", defaultVolume);

        musicSlider.value = musicVolume;
        sfxSlider.value = sfxVolume;

        musicSlider.onValueChanged.AddListener(UpdateMusicVolume);
        sfxSlider.onValueChanged.AddListener(UpdateSFXVolume);

        UpdateMusicVolume(musicVolume);
        UpdateSFXVolume(sfxVolume);
        

    }

    public void play()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Level1");
    }

    public void Settings()
    {
        MenuPanel.SetActive(false);
        SettingsPanel.SetActive(true);
    }

    public void BackToMenu()
    {
        SettingsPanel.SetActive(false);
        MenuPanel.SetActive(true);
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
        float normalizedVolume = sliderValue / 10f; // Convert 0–10 → 0–1
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
