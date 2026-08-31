using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicLevelPlay : MonoBehaviour
{
    public static MusicLevelPlay Instance { get; private set; }

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

        //floor1
        new SceneMusicData { sceneName = "LevelComplete", trackName = "Tutorial" }
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
        PlayMusicForScene(scene.name);
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