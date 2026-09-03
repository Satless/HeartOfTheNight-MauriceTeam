using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    public AudioMixer mixer;

    public Slider masterSlider;
    public Slider musicSlider;
    public Slider sfxSlider;

    private void Start()
    {
        if (masterSlider != null)
        {
            masterSlider.value = PlayerPrefs.GetFloat("Master", 1f);
            SetMaster(masterSlider.value);
            masterSlider.onValueChanged.AddListener(SetMaster);
        }

        if (musicSlider != null)
        {
            musicSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            SetMusic(musicSlider.value);
            musicSlider.onValueChanged.AddListener(SetMusic);
        }

        if (sfxSlider != null)
        {
            sfxSlider.value = PlayerPrefs.GetFloat("SFXVolume", 1f);
            SetSFX(sfxSlider.value);
            sfxSlider.onValueChanged.AddListener(SetSFX);
        }
    }

    public void SetMaster(float value)
    {
        if (mixer == null) return;
        value = Mathf.Clamp(value, 0.0001f, 1f);
        mixer.SetFloat("Master", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("Master", value);
        PlayerPrefs.Save();
    }

    public void SetMusic(float value)
    {
        if (mixer == null) return;
        value = Mathf.Clamp(value, 0.0001f, 1f);
        mixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("MusicVolume", value);
        PlayerPrefs.Save();
    }

    public void SetSFX(float value)
    {
        if (mixer == null) return;
        value = Mathf.Clamp(value, 0.0001f, 1f);
        mixer.SetFloat("SFXVolume", Mathf.Log10(value) * 20);
        PlayerPrefs.SetFloat("SFXVolume", value);
        PlayerPrefs.Save();
    }
}
