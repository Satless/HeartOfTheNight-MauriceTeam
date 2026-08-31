using HeartOfTheNight.Hung;
using HeartOfTheNight.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Bấm ESC trong màn chơi để hiện Pause Prefab, dừng game.
/// Tự tạo overlay (DontDestroyOnLoad) nên không cần kéo Prefab vào từng scene.
/// </summary>
public class PauseUI : MonoBehaviour
{
    private const string ResourcesBootstrapPath = "UI/PauseMenu";
    private const string HomeSceneFallback = "mainMenu";

    public static PauseUI Instance { get; private set; }
    public static bool IsPaused { get; private set; }

    [Header("Pause Prefab")]
    [SerializeField] private GameObject pausePanelPrefab;

    [Header("Scenes")]
    [SerializeField] private string homeSceneName = HomeSceneFallback;

    [Header("Optional")]
    [Tooltip("Kéo Prefab SettingPanel vào đây. PauseUI sẽ tự Instantiate khi bấm SETTING.")]
    [SerializeField] private GameObject settingsPanel;

    private GameObject _panel;
    private GameObject _settingsInstance;
    private Canvas _canvas;
    private bool _buttonsWired;
    private bool _levelTimerWasPaused;

    private static readonly string[] MenuScenes =
    {
        "mainMenu", "SelectLevel", "AuthScene", "LevelComplete",
        "SceneStory", "stoty", "deadscreen", "MenuDat", "SelectLvDat", "Pause"
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static PauseUI EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var existing = FindFirstObjectByType<PauseUI>();
        if (existing != null)
            return existing;

        var prefab = Resources.Load<GameObject>(ResourcesBootstrapPath);
        if (prefab != null)
        {
            var spawned = Instantiate(prefab);
            spawned.name = "PauseMenu";
            if (Instance != null)
                return Instance;
        }

        var go = new GameObject("PauseMenu");
        return go.AddComponent<PauseUI>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;
        EnsureCanvas();
        EnsurePanel();
        EnsureEventSystem();
        HideImmediate();
        HideStrayPauseOverlays();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
        {
            if (IsPaused)
                ApplyResume(restoreGameplay: false);
            Instance = null;
            IsPaused = false;
        }
    }

    private void Update()
    {
        if (!WasPausePressed())
            return;

        if (IsPaused)
        {
            if (IsSettingsOpen())
            {
                CloseSettings();
                return;
            }

            Resume();
            return;
        }

        if (CanOpenPause())
            Pause();
    }

    //public void Pause()
    //{
    //    if (IsPaused || !CanOpenPause())
    //        return;

    //    EnsureCanvas();
    //    EnsurePanel();
    //    EnsureEventSystem();
    //    if (_panel == null)
    //        return;

    //    IsPaused = true;
    //    _panel.SetActive(true);
    //    _panel.transform.SetAsLastSibling();
    //    CloseSettings();

    //    _levelTimerWasPaused = DataManager.Instance != null && DataManager.Instance.IsLevelTimerPaused;
    //    if (DataManager.Instance != null)
    //        DataManager.Instance.PauseLevelTimer();

    //    Time.timeScale = 0f;
    //    AudioListener.pause = true;
    //    FreezeGameplay();
    //}

    public void Pause()
    {
        if (IsPaused || !CanOpenPause())
            return;

        EnsureCanvas();
        EnsurePanel();
        EnsureEventSystem();
        if (_panel == null)
            return;

        IsPaused = true;
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        CloseSettings();

        _levelTimerWasPaused = DataManager.Instance != null && DataManager.Instance.IsLevelTimerPaused;
        if (DataManager.Instance != null)
            DataManager.Instance.PauseLevelTimer();

        Time.timeScale = 0f;

        // Kích hoạt dừng toàn bộ AudioListener để ngắt âm thanh SFX quái và môi trường
        AudioListener.pause = true;

        FreezeGameplay();
    }

    public void Resume()
    {
        if (!IsPaused)
            return;

        ApplyResume(restoreGameplay: true);
    }

    /// <summary>
    /// Đóng Pause khi chết / DeadScreen — không Resume đồng hồ (DataManager đang treo vì chết).
    /// </summary>
    public void DismissForExternalFlow()
    {
        if (!IsPaused)
        {
            HideImmediate();
            return;
        }

        IsPaused = false;
        HideImmediate();
        AudioListener.pause = false;
        UnfreezeGameplay();
    }

    public void GoHome()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.SaveBeforeLeaveLevel();

        ApplyResume(restoreGameplay: false);
        string scene = string.IsNullOrEmpty(homeSceneName) ? HomeSceneFallback : homeSceneName;

        if (ScreenFader.Instance != null)
            ScreenFader.Instance.LoadSceneWithLoading(scene);
        else
            SceneManager.LoadScene(scene);
    }

    public void ExitGame()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.SaveBeforeLeaveLevel();

        ApplyResume(restoreGameplay: false);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void OpenSettings()
    {
        EnsureSettingsPanel();
        if (_settingsInstance == null)
        {
            Debug.LogWarning("[PauseUI] Chưa gán SettingPanel. Kéo Prefab SettingPanel vào field Settings Panel trên PauseMenu.");
            return;
        }

        _settingsInstance.SetActive(true);
        _settingsInstance.transform.SetAsLastSibling();
    }

    public void CloseSettings()
    {
        if (_settingsInstance != null)
            _settingsInstance.SetActive(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsPaused)
            ApplyResume(restoreGameplay: false);

        HideImmediate();
        EnsureEventSystem();
        HideStrayPauseOverlays();
    }

    private bool CanOpenPause()
    {
        if (!IsGameplayScene())
            return false;

        var dead = FindFirstObjectByType<DeadScreenUI>(FindObjectsInactive.Include);
        if (dead != null && dead.gameObject.activeInHierarchy)
            return false;

        if (LevelCompleteUI.IsShowing)
            return false;

        var player = FindFirstObjectByType<PlayerHealth>();
        if (player != null && player.IsDead)
            return false;

        return true;
    }

    private static bool IsGameplayScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        for (int i = 0; i < MenuScenes.Length; i++)
        {
            if (string.Equals(sceneName, MenuScenes[i], System.StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }

    private static bool WasPausePressed()
    {
        if (Keyboard.current != null)
            return Keyboard.current.escapeKey.wasPressedThisFrame;

        return Input.GetKeyDown(KeyCode.Escape);
    }

    private void ApplyResume(bool restoreGameplay)
    {
        IsPaused = false;
        HideImmediate();

        // Mở lại AudioListener khi bỏ Pause
        AudioListener.pause = false;
        Time.timeScale = 1f;

        if (restoreGameplay
            && DataManager.Instance != null
            && !_levelTimerWasPaused)
        {
            DataManager.Instance.ResumeLevelTimer();
        }

        UnfreezeGameplay();
    }

    private bool IsSettingsOpen()
    {
        return _settingsInstance != null && _settingsInstance.activeSelf;
    }

    private void HideImmediate()
    {
        if (_panel != null)
            _panel.SetActive(false);
        CloseSettings();
    }

    private void FreezeGameplay()
    {
        GameplayEvents.SetGameplayInputEnabled(false);
    }

    private void UnfreezeGameplay()
    {
        GameplayEvents.SetGameplayInputEnabled(true);
    }

    private void EnsureCanvas()
    {
        if (_canvas != null)
            return;

        var canvasGo = new GameObject("PauseCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 500;
        _canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
    }

    private void EnsurePanel()
    {
        if (_panel != null)
            return;

        var prefab = pausePanelPrefab;
        if (prefab == null)
            prefab = Resources.Load<GameObject>("UI/Pause");
        if (prefab == null)
        {
            Debug.LogError("[PauseUI] Chưa gán Pause Prefab. Kéo Assets/thuận/Prefabs/Pause vào PauseMenu.");
            return;
        }

        _panel = Instantiate(prefab, _canvas.transform, false);
        _panel.name = "Pause";
        StretchFull(_panel.GetComponent<RectTransform>());
        WireButtons(_panel);
        _panel.SetActive(false);
        EnsureSettingsPanel();
    }

    private void EnsureSettingsPanel()
    {
        if (_settingsInstance != null || settingsPanel == null || _canvas == null)
            return;

        if (settingsPanel.scene.IsValid())
        {
            _settingsInstance = settingsPanel;
            _settingsInstance.transform.SetParent(_canvas.transform, false);
        }
        else
        {
            _settingsInstance = Instantiate(settingsPanel, _canvas.transform, false);
            _settingsInstance.name = "SettingPanel";
        }

        _settingsInstance.SetActive(false);
        WireSettingsCloseButton(_settingsInstance);
    }

    private void WireButtons(GameObject root)
    {
        if (_buttonsWired || root == null)
            return;

        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            string name = button.gameObject.name.Trim();
            EnsureButtonHitArea(button);

            if (name.Equals("CONTINUE", System.StringComparison.OrdinalIgnoreCase))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(Resume);
            }
            else if (name.Equals("HOME", System.StringComparison.OrdinalIgnoreCase))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(GoHome);
            }
            else if (name.Equals("EXIT GAME", System.StringComparison.OrdinalIgnoreCase)
                     || name.Equals("EXITGAME", System.StringComparison.OrdinalIgnoreCase))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(ExitGame);
            }
            else if (name.Equals("SETTING", System.StringComparison.OrdinalIgnoreCase)
                     || name.Equals("SETTINGS", System.StringComparison.OrdinalIgnoreCase))
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OpenSettings);
            }
        }

        _buttonsWired = true;
    }

    private void WireSettingsCloseButton(GameObject root)
    {
        var buttons = root.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            var button = buttons[i];
            if (!button.gameObject.name.Equals("CloseButton", System.StringComparison.OrdinalIgnoreCase))
                continue;

            EnsureButtonHitArea(button);
            MutePersistentClicks(button);
            button.onClick.AddListener(CloseSettings);
        }
    }

    private static void EnsureButtonHitArea(Button button)
    {
        var image = button.targetGraphic as Image;
        if (image == null)
            image = button.GetComponent<Image>();
        if (image == null)
            return;

        image.enabled = true;
        Color color = image.color;
        color.a = 0f;
        image.color = color;
        image.raycastTarget = true;
    }

    private static void MutePersistentClicks(Button button)
    {
        button.onClick.RemoveAllListeners();
        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
    }

    private static void StretchFull(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void HideStrayPauseOverlays()
    {
        if (!IsGameplayScene())
            return;

        var images = FindObjectsByType<Image>(FindObjectsSortMode.None);
        for (int i = 0; i < images.Length; i++)
        {
            var go = images[i].gameObject;
            if (go == _panel || go.name != "Pause")
                continue;
            if (go.transform.Find("Khung") == null)
                continue;

            go.SetActive(false);
        }
    }

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }
}
