using System.Collections.Generic;
using HeartOfTheNight.Hung;
using HeartOfTheNight.Player;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Overlay Level Complete (DontDestroyOnLoad). Hiện khi qua cửa Next Level, điền ENEMIES / TIME / SECRET.
/// </summary>
public class LevelCompleteUI : MonoBehaviour
{
    private const string ResourcesBootstrapPath = "UI/LevelCompleteMenu";
    private const string ResourcesPanelPath = "UI/LevelComplete";
    private const string HomeSceneFallback = "mainMenu";

    public static LevelCompleteUI Instance { get; private set; }
    public static bool IsShowing { get; private set; }

    [Header("Prefab")]
    [SerializeField] private GameObject panelPrefab;

    [Header("Scenes")]
    [SerializeField] private string homeSceneName = HomeSceneFallback;

    private GameObject _panel;
    private Canvas _canvas;
    private LevelCompletePending _pending;
    private bool _hasPending;
    private bool _buttonsWired;
    private readonly List<Behaviour> _disabledOnShow = new List<Behaviour>(8);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        EnsureExists();
    }

    public static LevelCompleteUI EnsureExists()
    {
        if (Instance != null)
            return Instance;

        var existing = FindFirstObjectByType<LevelCompleteUI>();
        if (existing != null)
            return existing;

        var prefab = Resources.Load<GameObject>(ResourcesBootstrapPath);
        if (prefab != null)
        {
            var spawned = Instantiate(prefab);
            spawned.name = "LevelCompleteMenu";
            if (Instance != null)
                return Instance;
        }

        var go = new GameObject("LevelCompleteMenu");
        return go.AddComponent<LevelCompleteUI>();
    }

    public bool HasPending => _hasPending;

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
        HideImmediate();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
        {
            Instance = null;
            IsShowing = false;
        }
    }

    /// <summary>
    /// Hiện overlay trên màn vừa xong. Trả false nếu không load được prefab — caller load scene luôn.
    /// </summary>
    public static bool TryShow(LevelCompletePending pending)
    {
        var ui = EnsureExists();
        return ui.Show(pending);
    }

    public bool Show(LevelCompletePending pending)
    {
        EnsureCanvas();
        EnsurePanel();
        EnsureEventSystem();
        if (_panel == null)
        {
            Debug.LogError("[LevelCompleteUI] Không tìm thấy Prefab LevelComplete. Đặt vào Resources/UI/LevelComplete hoặc gán Panel Prefab.");
            return false;
        }

        _pending = pending;
        _hasPending = pending != null && !string.IsNullOrEmpty(pending.nextSceneName);

        var stats = LevelStatsTracker.CaptureSnapshot();
        var manager = _panel.GetComponent<LevelCompleteManager>();
        if (manager == null)
            manager = _panel.AddComponent<LevelCompleteManager>();
        manager.ApplyStats(stats);
        WireButtons(_panel);

        if (PauseUI.Instance != null)
            PauseUI.Instance.DismissForExternalFlow();

        if (DataManager.Instance != null)
            DataManager.Instance.PauseLevelTimer();

        IsShowing = true;
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        Time.timeScale = 0f;
        FreezeGameplay();
        return true;
    }

    public void ConfirmNextLevel()
    {
        if (!IsShowing)
            return;

        string next = _hasPending ? _pending.nextSceneName : "";
        if (string.IsNullOrEmpty(next) && DataManager.Instance != null)
            next = DataManager.Instance.Data != null ? DataManager.Instance.Data.currentScene : "";

        if (string.IsNullOrEmpty(next))
        {
            Debug.LogError("[LevelCompleteUI] Chưa có scene level tiếp theo.");
            return;
        }

        float fade = _hasPending ? _pending.fadeDuration : -1f;
        float delay = _hasPending ? _pending.delayBeforeFadeIn : -1f;
        string spawnId = _hasPending ? _pending.spawnIDInNextScene : "";
        int nextLevelIndex = _hasPending ? _pending.nextLevelIndex : 0;
        bool saveCheckpoint = _hasPending && _pending.saveAsCheckpoint;
        int health = _hasPending ? _pending.playerHealth : -1;

        _hasPending = false;
        BeginLeaveOverlay();

        if (DataManager.Instance != null)
        {
            if (nextLevelIndex > DataManager.Instance.Data.maxUnlockedLevel)
                DataManager.Instance.Data.maxUnlockedLevel = nextLevelIndex;

            DataManager.Instance.Data.currentScene = next;
            DataManager.Instance.PrepareForNewScene();

            if (saveCheckpoint)
                DataManager.Instance.SaveCheckpoint(next, spawnId, Vector3.zero, health);
        }

        LevelEntrance.SetPendingSpawn(spawnId);

        if (ScreenFader.Instance != null)
            ScreenFader.Instance.LoadSceneWithLoading(next, fade, delay);
        else
            SceneManager.LoadScene(next);
    }

    public void GoHome()
    {
        if (DataManager.Instance != null)
            DataManager.Instance.SaveBeforeLeaveLevel();

        LevelEntrance.ClearPendingSpawn();
        _hasPending = false;
        BeginLeaveOverlay();

        string scene = string.IsNullOrEmpty(homeSceneName) ? HomeSceneFallback : homeSceneName;
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.LoadSceneWithLoading(scene);
        else
            SceneManager.LoadScene(scene);
    }

    private void BeginLeaveOverlay()
    {
        IsShowing = false;
        HideImmediate();
        UnfreezeGameplay();
        Time.timeScale = 1f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsShowing)
            HideImmediate();

        UnfreezeGameplay();
        if (Time.timeScale == 0f && !PauseUI.IsPaused)
            Time.timeScale = 1f;
    }

    private void HideImmediate()
    {
        if (_panel != null)
            _panel.SetActive(false);
    }

    private void FreezeGameplay()
    {
        _disabledOnShow.Clear();
        DisableIfEnabled(FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None));
        DisableIfEnabled(FindObjectsByType<PlayerAttack>(FindObjectsSortMode.None));
    }

    private void DisableIfEnabled(Behaviour[] behaviours)
    {
        for (int i = 0; i < behaviours.Length; i++)
        {
            var behaviour = behaviours[i];
            if (behaviour == null || !behaviour.enabled)
                continue;

            behaviour.enabled = false;
            _disabledOnShow.Add(behaviour);
        }
    }

    private void UnfreezeGameplay()
    {
        for (int i = 0; i < _disabledOnShow.Count; i++)
        {
            if (_disabledOnShow[i] != null)
                _disabledOnShow[i].enabled = true;
        }

        _disabledOnShow.Clear();
    }

    private void EnsureCanvas()
    {
        if (_canvas != null)
            return;

        var canvasGo = new GameObject("LevelCompleteCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);

        _canvas = canvasGo.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 800;
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

        var prefab = panelPrefab;
        if (prefab == null)
            prefab = Resources.Load<GameObject>(ResourcesPanelPath);
        if (prefab == null)
            return;

        _panel = Instantiate(prefab, _canvas.transform, false);
        _panel.name = "LevelComplete";
        StretchFull(_panel.GetComponent<RectTransform>());
        _panel.SetActive(false);
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

            if (name.Equals("NEXT LEVEL", System.StringComparison.OrdinalIgnoreCase)
                || name.Equals("NEXTLEVEL", System.StringComparison.OrdinalIgnoreCase))
            {
                MutePersistentClicks(button);
                button.onClick.AddListener(ConfirmNextLevel);
            }
            else if (name.Equals("BACK TO HOME", System.StringComparison.OrdinalIgnoreCase)
                     || name.Equals("BACKTOHOME", System.StringComparison.OrdinalIgnoreCase)
                     || name.Equals("HOME", System.StringComparison.OrdinalIgnoreCase))
            {
                MutePersistentClicks(button);
                button.onClick.AddListener(GoHome);
            }
        }

        _buttonsWired = true;
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

    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<InputSystemUIInputModule>();
    }
}

public class LevelCompletePending
{
    public string nextSceneName;
    public string spawnIDInNextScene;
    public float fadeDuration = -1f;
    public float delayBeforeFadeIn = -1f;
    public int nextLevelIndex;
    public bool saveAsCheckpoint;
    public int playerHealth = -1;
}
