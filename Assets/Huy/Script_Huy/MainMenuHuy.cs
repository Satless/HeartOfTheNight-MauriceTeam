using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class MainMenuHuy : MonoBehaviour
{
    public AudioMixer audioMixer;

    [Header("Sliders")]
    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Start()
    {
        LoadVolume();
        MusicManager.Instance.PlayMusic("MainMenu");
    }

    public void Play()
    {
        //LevelManager.Instance.LoadScene("Game", "CrossFade");
        //MusicManager.Instance.PlayMusic("Game");
    }

    public void Quit()
    {
        Application.Quit();
    }

    // update volume

    public void UpdateMaster(float volume)
    {
        // log10 makes changing volume slider smoother
        audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20);
    }

    public void UpdateMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20);
    }

    public void UpdateSoundVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20);
    }

    // save and downloaded file

    public void SaveVolume()
    {
        //save value of Slider (0 -> 1)
        PlayerPrefs.SetFloat("Master", masterSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);
        PlayerPrefs.Save();
    }

    public void LoadVolume()
    {
        // take saved value (1f if open game for first time)
        masterSlider.value = PlayerPrefs.GetFloat("Master", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // apply value into the mixer
        UpdateMaster(masterSlider.value);
        UpdateMusicVolume(musicSlider.value);
        UpdateSoundVolume(sfxSlider.value);
    }
}