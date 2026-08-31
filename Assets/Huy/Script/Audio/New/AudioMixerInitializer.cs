using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class AudioMixerInitializer : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    private void Awake()
    {
        if (audioMixer == null)
        {
            audioMixer = Resources.Load<AudioMixer>("Settings");
        }
    }

    private void OnEnable()
    {
        // Chờ 1 frame để đảm bảo AudioMixer đã load xong hệ thống exposed parameters
        StartCoroutine(ApplySavedVolumesNextFrame());
    }

    private IEnumerator ApplySavedVolumesNextFrame()
    {
        yield return null;
        ApplySavedVolumes();
    }

    public void ApplySavedVolumes()
    {
        if (audioMixer == null) return;

        float master = PlayerPrefs.GetFloat("Master", 1f);
        float music = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfx = PlayerPrefs.GetFloat("SFXVolume", 1f);

        audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Max(0.0001f, master)) * 20);
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, music)) * 20);
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, sfx)) * 20);
    }
}