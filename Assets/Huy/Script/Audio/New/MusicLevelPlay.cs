using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class MusicLevelPlay : MonoBehaviour
{
    public static MusicLevelPlay Instance { get; private set; }

    [Header("Audio Mixer Settings")]
    [SerializeField] private UnityEngine.Audio.AudioMixer audioMixer;

    [System.Serializable]
    public class SceneMusicData
    {
        [Tooltip("Tên chính xác của Scene (VD: Level 1, mainMenu, Story1...)")]
        public string sceneName;

        [Tooltip("Tên track trùng khớp với trackName trong MusicLibrary_New")]
        public string trackName;

        [Tooltip("Thời gian chuyển bài (Fade in/out)")]
        public float fadeDuration = 0.5f;
    }

    [Header("Default Music Settings")]
    [SerializeField] private string defaultTrackName = "DefaultBGM";
    [SerializeField] private float defaultFadeDuration = 0.5f;

    [Header("Scene Music Configs")]
    [SerializeField]
    private List<SceneMusicData> sceneMusicList = new List<SceneMusicData>
    {
        //menu
        new SceneMusicData { sceneName = "mainMenu", trackName = "MainMenuBGM" },
        new SceneMusicData { sceneName = "SelectLevel", trackName = "SelectLevelBGM" },
        new SceneMusicData { sceneName = "LevelComplete", trackName = "WinBGM" },
    };

    private string currentPlayingTrack = "";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. Áp dụng ngay âm lượng từ PlayerPrefs vào AudioMixer khi Scene vừa load
        ApplySavedVolumes();

        // 2. Phát nhạc cho Scene
        PlayMusicForScene(scene.name);
    }

    /// <summary>
    /// Đọc PlayerPrefs và ép giá trị trực tiếp vào AudioMixer
    /// </summary>
    public void ApplySavedVolumes()
    {
        StartCoroutine(ApplySavedVolumesRoutine());
    }

    private System.Collections.IEnumerator ApplySavedVolumesRoutine()
    {
        // Chờ 0.05 giây (dùng unscaledTime để không bị ảnh hưởng bởi Pause/Time.timeScale = 0)
        yield return new WaitForSecondsRealtime(0.05f);

        if (audioMixer == null)
        {
            audioMixer = Resources.Load<UnityEngine.Audio.AudioMixer>("Settings");
        }

        if (audioMixer != null)
        {
            float master = PlayerPrefs.HasKey("Master") ? PlayerPrefs.GetFloat("Master") : 1f;
            float music = PlayerPrefs.HasKey("MusicVolume") ? PlayerPrefs.GetFloat("MusicVolume") : 1f;
            float sfx = PlayerPrefs.HasKey("SFXVolume") ? PlayerPrefs.GetFloat("SFXVolume") : 1f;

            audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Max(0.0001f, master)) * 20);
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, music)) * 20);
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, sfx)) * 20);
        }
    }

    private void PlayMusicForScene(string sceneName)
    {
        SceneMusicData data = sceneMusicList.Find(x => string.Equals(x.sceneName, sceneName, System.StringComparison.OrdinalIgnoreCase));

        if (data != null)
        {
            if (!string.IsNullOrEmpty(data.trackName) && data.trackName != currentPlayingTrack)
            {
                TriggerMusic(data.trackName, data.fadeDuration);
                currentPlayingTrack = data.trackName;
            }
        }
        else
        {
            // Nếu Scene hiện tại KHÔNG có trong sceneMusicList -> Tự động phát nhạc Default
            if (!string.IsNullOrEmpty(defaultTrackName) && defaultTrackName != currentPlayingTrack)
            {
                TriggerMusic(defaultTrackName, defaultFadeDuration);
                currentPlayingTrack = defaultTrackName;
            }
        }
    }

    private void TriggerMusic(string trackName, float fadeDuration)
    {
        // Ưu tiên gọi qua MusicManager_New Instance hoặc AudioEvents
        if (MusicManager_New.Instance != null)
        {
            MusicManager_New.Instance.PlayMusic(trackName, fadeDuration);
        }
        else
        {
            // Nếu dùng Event Delegate thì gọi thông qua AudioEvents
            AudioEvents.TriggerMusic(trackName, fadeDuration);
        }
    }
}