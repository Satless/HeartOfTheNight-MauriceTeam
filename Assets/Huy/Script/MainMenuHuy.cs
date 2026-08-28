using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
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
        // 1. Load giá trị từ PlayerPrefs lên Slider trước
        LoadVolume();

        // 2. Đăng ký listener SAU KHI đã Load giá trị để tránh gọi trùng lặp
        masterSlider.onValueChanged.AddListener(UpdateMaster);
        musicSlider.onValueChanged.AddListener(UpdateMusicVolume);
        sfxSlider.onValueChanged.AddListener(UpdateSoundVolume);

        // 3. Bật nhạc nền bằng Observer Pattern mới
        PlayMenuMusic();

        AddStopDragSound(masterSlider);
        AddStopDragSound(musicSlider);
        AddStopDragSound(sfxSlider);
    }

    private void Awake()
    {
        if (audioMixer == null)
        {
            audioMixer = Resources.Load<AudioMixer>("Settings"); // Đặt file AudioMixer vào thư mục Resources
        }
    }

    private void AddStopDragSound(Slider slider)
    {
        if (slider == null) return;

        EventTrigger trigger = slider.gameObject.GetComponent<EventTrigger>();
        if (trigger == null) trigger = slider.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerUp
        };
        entry.callback.AddListener((data) =>
        {
            if (SoundManager_New.Instance != null)
            {
                AudioEvents.TriggerSound2D("UI", "Slider", "StopDrag");
            }
        });

        trigger.triggers.Add(entry);
    }

    private void PlayMenuMusic()
    {
        // Phát nhạc nền "MainMenu" thông qua Event System
        AudioEvents.TriggerMusic("MainMenu", 0.5f);
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void UpdateMaster(float volume)
    {
        audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20);
        SaveVolume();

        //float db = Mathf.Log10(Mathf.Max(0.0001f, volume)) * 20;
        //bool result = audioMixer.SetFloat("Master", db);
        //Debug.Log($"Master Vol: {volume} -> dB: {db} | Thanh cong: {result}");
        //SaveVolume();
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
        masterSlider.value = PlayerPrefs.GetFloat("Master", 1f);
        musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
        sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);

        // Áp dụng ngay giá trị âm lượng vào AudioMixer
        UpdateMaster(masterSlider.value);
        UpdateMusicVolume(musicSlider.value);
        UpdateSoundVolume(sfxSlider.value);
    }
}