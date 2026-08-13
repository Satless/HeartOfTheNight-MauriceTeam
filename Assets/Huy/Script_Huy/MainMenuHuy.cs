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

        // automatically adjust and save value
        masterSlider.onValueChanged.AddListener(UpdateMaster);
        musicSlider.onValueChanged.AddListener(UpdateMusicVolume);
        sfxSlider.onValueChanged.AddListener(UpdateSoundVolume);

        PlayMenuMusic();
    }

    private void PlayMenuMusic()
    {
        MusicManager.Instance.PlayMusic("MainMenu");
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void UpdateMaster(float volume)
    {
        audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20);
        SaveVolume();
    }

    public void UpdateMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20);
        SaveVolume();
    }

    public void UpdateSoundVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20);
        SaveVolume();
    }

    public void SaveVolume()
    {
        PlayerPrefs.SetFloat("Master", masterSlider.value);
        PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);
        PlayerPrefs.Save();
    }

    public void LoadVolume()
    {
        //take saved value, 1f as default if there is none
        masterSlider.value = PlayerPrefs.GetFloat("Master", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);



        //update SFX into audiomixer right when loading
        UpdateMaster(masterSlider.value);
        UpdateMusicVolume(musicSlider.value);
        UpdateSoundVolume(sfxSlider.value);
    }
}