using System.Collections.Generic;
using UnityEngine;
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
        new SceneMusicData { sceneName = "mainMenu", trackName = "MainMenuBGM" },
        new SceneMusicData { sceneName = "SelectLevel", trackName = "SelectLevelBGM" },
        new SceneMusicData { sceneName = "LevelComplete", trackName = "WinBGM" },
    };

    private string currentPlayingTrack = "";
    private bool _isDuplicate;
    private Coroutine _volumeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            _isDuplicate = true;
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        if (_isDuplicate)
            return;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (_isDuplicate)
            return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        if (_isDuplicate)
            return;
        if (string.IsNullOrEmpty(currentPlayingTrack))
            PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySavedVolumes();
        PlayMusicForScene(scene.name);
    }

    public void ApplySavedVolumes()
    {
        if (_isDuplicate)
            return;
        if (_volumeRoutine != null)
            StopCoroutine(_volumeRoutine);
        _volumeRoutine = StartCoroutine(ApplySavedVolumesRoutine());
    }

    private System.Collections.IEnumerator ApplySavedVolumesRoutine()
    {
        yield return new WaitForSecondsRealtime(0.05f);

        if (audioMixer == null)
            audioMixer = Resources.Load<UnityEngine.Audio.AudioMixer>("Settings");

        if (audioMixer != null)
        {
            float master = PlayerPrefs.HasKey("Master") ? PlayerPrefs.GetFloat("Master") : 1f;
            float music = PlayerPrefs.HasKey("MusicVolume") ? PlayerPrefs.GetFloat("MusicVolume") : 1f;
            float sfx = PlayerPrefs.HasKey("SFXVolume") ? PlayerPrefs.GetFloat("SFXVolume") : 1f;

            audioMixer.SetFloat("Master", Mathf.Log10(Mathf.Max(0.0001f, master)) * 20);
            audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Max(0.0001f, music)) * 20);
            audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Max(0.0001f, sfx)) * 20);
        }

        _volumeRoutine = null;
    }

    private void PlayMusicForScene(string sceneName)
    {
        SceneMusicData data = sceneMusicList.Find(x => string.Equals(x.sceneName, sceneName, System.StringComparison.OrdinalIgnoreCase));

        if (data != null)
        {
            if (!string.IsNullOrEmpty(data.trackName) && data.trackName != currentPlayingTrack)
                TryPlaySceneTrack(data.trackName, data.fadeDuration);
        }
        else if (!string.IsNullOrEmpty(defaultTrackName) && defaultTrackName != currentPlayingTrack)
        {
            TryPlaySceneTrack(defaultTrackName, defaultFadeDuration);
        }
    }

    private void TryPlaySceneTrack(string trackName, float fadeDuration)
    {
        if (TriggerMusic(trackName, fadeDuration))
            currentPlayingTrack = trackName;
    }

    private bool TriggerMusic(string trackName, float fadeDuration)
    {
        if (MusicManager_New.Instance != null)
            return MusicManager_New.Instance.PlayMusic(trackName, fadeDuration);

        AudioEvents.TriggerMusic(trackName, fadeDuration);
        return true;
    }
}
