using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// Hiện đồng hồ của màn đang chơi. Chết thì số đứng lại, hồi sinh chạy tiếp từ chỗ đã dừng.
    /// Không cần kéo tay: tự dựng sẵn một dòng ở giữa trên màn hình. Muốn đặt chỗ khác thì gắn
    /// component này vào TextMeshProUGUI của mình, bản tự dựng sẽ tự ẩn.
    /// </summary>
    public class LevelTimeDisplay : MonoBehaviour
    {
        [Header("Hiển thị")]
        [Tooltip("Để trống = lấy TextMeshProUGUI trên chính GameObject này.")]
        [SerializeField] private TextMeshProUGUI label;
        [Tooltip("Chữ đứng trước số, ví dụ \"TIME \".")]
        [SerializeField] private string prefix = "";
        [Tooltip("Bật = luôn hiện dạng H:MM:SS thay vì MM:SS.")]
        [SerializeField] private bool alwaysShowHours;
        [Tooltip("Bật = tự ẩn khi đang ở menu / màn hình kết thúc.")]
        [SerializeField] private bool hideOutsideLevels = true;

        [Header("Khi đồng hồ bị treo")]
        [Tooltip("Bật = làm mờ số lúc đồng hồ đang dừng, để thấy rõ là đã ngừng đếm.")]
        [SerializeField] private bool dimWhenPaused = true;
        [SerializeField] private Color pausedColor = new Color(1f, 1f, 1f, 0.4f);

        private Color _runningColor = Color.white;
        private int _shownSeconds = -1;
        private bool _shownPaused;

        private void Awake()
        {
            if (label == null)
                label = GetComponent<TextMeshProUGUI>();

            if (label != null)
                _runningColor = label.color;
        }

        private void OnEnable()
        {
            _shownSeconds = -1;
            Refresh();
        }

        private void Update() => Refresh();

        private void Refresh()
        {
            if (label == null)
                return;

            var data = HeartOfTheNight.Hung.DataManager.Instance;
            bool inLevel = data != null && data.HasLevelTimer;

            if (hideOutsideLevels && label.enabled != inLevel)
                label.enabled = inLevel;

            float seconds = inLevel ? data.LevelTimeSeconds : 0f;
            int wholeSeconds = Mathf.FloorToInt(Mathf.Max(0f, seconds));
            if (wholeSeconds != _shownSeconds)
            {
                _shownSeconds = wholeSeconds;
                label.text = prefix + HeartOfTheNight.Hung.DataManager.FormatLevelTime(seconds, alwaysShowHours);
            }

            bool paused = inLevel && data.IsLevelTimerPaused;
            if (dimWhenPaused && paused != _shownPaused)
            {
                _shownPaused = paused;
                label.color = paused ? pausedColor : _runningColor;
            }
        }

        // ─── TỰ DỰNG SẴN TRÊN MÀN HÌNH ──────────────────────────────────────────

        private static LevelTimeDisplay _autoBuilt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            SyncAutoBuilt();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => SyncAutoBuilt();

        private static void SyncAutoBuilt()
        {
            if (FindDesignerPlaced() != null)
            {
                if (_autoBuilt != null)
                    _autoBuilt.gameObject.SetActive(false);
                return;
            }

            if (_autoBuilt == null)
                _autoBuilt = BuildDefault();
            else
                _autoBuilt.gameObject.SetActive(true);
        }

        private static LevelTimeDisplay FindDesignerPlaced()
        {
            var found = FindObjectsByType<LevelTimeDisplay>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < found.Length; i++)
            {
                if (found[i] != _autoBuilt)
                    return found[i];
            }
            return null;
        }

        private static LevelTimeDisplay BuildDefault()
        {
            var root = new GameObject("LevelTimeDisplay (auto)");
            DontDestroyOnLoad(root);

            // Dưới ScreenFader (9999) để lúc fade đen không bị số giờ đè lên.
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(root.transform, false);

            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.sizeDelta = new Vector2(400f, 80f);
            rect.anchoredPosition = new Vector2(0f, -24f);

            var text = textGo.AddComponent<TextMeshProUGUI>();
            text.alignment = TextAlignmentOptions.Top;
            text.fontSize = 44f;
            text.color = Color.white;
            text.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                text.font = TMP_Settings.defaultFontAsset;

            return textGo.AddComponent<LevelTimeDisplay>();
        }
    }
}
