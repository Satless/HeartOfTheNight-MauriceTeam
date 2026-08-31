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

    private void Awake()
    {
        if (audioMixer == null)
        {
            audioMixer = Resources.Load<AudioMixer>("Settings");
        }
    }

    private void OnEnable()
    {
        // Mỗi khi mở SettingPanel, cập nhật vị trí các thanh Slider đúng với PlayerPrefs
        SyncSlidersFromPrefs();
    }

    private void Start()
    {
        // Load dữ liệu và áp dụng vào Slider + AudioMixer trước
        LoadVolume();

        masterSlider.onValueChanged.RemoveAllListeners();
        musicSlider.onValueChanged.RemoveAllListeners();
        sfxSlider.onValueChanged.RemoveAllListeners();

        masterSlider.onValueChanged.AddListener(UpdateMaster);
        musicSlider.onValueChanged.AddListener(UpdateMusicVolume);
        sfxSlider.onValueChanged.AddListener(UpdateSoundVolume);
    }

    private void SyncSlidersFromPrefs()
    {
        if (masterSlider != null) masterSlider.value = PlayerPrefs.GetFloat("Master", 1f);
        if (musicSlider != null) musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        if (sfxSlider != null) sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void UpdateMaster(float volume)
    {
        audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20);
        PlayerPrefs.SetFloat("Master", volume);
        PlayerPrefs.Save();
        //SaveVolume();
    }

    public void UpdateMusicVolume(float volume)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20);
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
        //SaveVolume();
    }

    public void UpdateSoundVolume(float volume)
    {
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20);
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
        //SaveVolume();
    }

    public void SaveVolume()
    {
        //PlayerPrefs.SetFloat("Master", masterSlider.value);
        //PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        //PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);

        if (masterSlider != null) PlayerPrefs.SetFloat("Master", masterSlider.value);
        if (musicSlider != null) PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        if (sfxSlider != null) PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);

        PlayerPrefs.Save();
    }

    public void LoadVolume()
    {
        SyncSlidersFromPrefs();

        if (masterSlider != null) UpdateMaster(masterSlider.value);
        if (musicSlider != null) UpdateMusicVolume(musicSlider.value);
        if (sfxSlider != null) UpdateSoundVolume(sfxSlider.value);
        //UpdateMaster(masterSlider.value);
        //UpdateMusicVolume(musicSlider.value);
        //UpdateSoundVolume(sfxSlider.value);
    }
}