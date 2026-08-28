using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeartOfTheNight.Hung
{
    /// <summary>
    /// Popup hỏi Continue / Bỏ khi vào Select Level mà slot còn màn đang chơi dở (hasCheckpoint).
    /// </summary>
    public class ContinueInProgressUI : MonoBehaviour
    {
        [SerializeField] private GameObject popupRoot;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button abandonButton;

        private bool _resolved;

        public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

        private void Awake()
        {
            if (popupRoot == null)
                BuildRuntimePopup();

            BindButtons();
            Hide();
        }

        public void TryShowIfInProgress()
        {
            var dm = DataManager.EnsureExists();
            if (dm == null || !dm.HasInProgress())
            {
                Hide();
                return;
            }

            _resolved = false;
            if (messageText != null)
            {
                string scene = dm.Data.checkpointScene;
                messageText.text =
                    "Bạn đang chơi dở một màn.\n\n" +
                    $"Checkpoint: {scene}\n\n" +
                    "Tiếp tục từ checkpoint hay bỏ màn đó?";
            }

            if (popupRoot != null)
            {
                popupRoot.transform.SetAsLastSibling();
                popupRoot.SetActive(true);
            }
        }

        public void OnContinue()
        {
            if (_resolved) return;
            _resolved = true;

            var dm = DataManager.Instance;
            if (dm == null)
            {
                Hide();
                return;
            }

            Hide();
            dm.ContinueFromCheckpoint();
        }

        public void OnAbandon()
        {
            if (_resolved) return;
            _resolved = true;

            var dm = DataManager.Instance;
            if (dm != null)
                dm.AbandonInProgress();

            Hide();
            Debug.Log("[ContinueInProgress] Đã bỏ màn đang chơi dở — ở lại Select Level.");
        }

        public void Hide()
        {
            if (popupRoot != null)
                popupRoot.SetActive(false);
        }

        private void BindButtons()
        {
            if (continueButton != null)
            {
                continueButton.onClick.RemoveAllListeners();
                continueButton.onClick.AddListener(OnContinue);
            }

            if (abandonButton != null)
            {
                abandonButton.onClick.RemoveAllListeners();
                abandonButton.onClick.AddListener(OnAbandon);
            }
        }

        private void BuildRuntimePopup()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[ContinueInProgress] Không thấy Canvas trong scene.");
                return;
            }

            popupRoot = new GameObject("ContinueInProgressPopup", typeof(RectTransform), typeof(Image));
            popupRoot.transform.SetParent(canvas.transform, false);

            var rootRt = popupRoot.GetComponent<RectTransform>();
            StretchFull(rootRt);
            var dim = popupRoot.GetComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            var panel = CreatePanel(popupRoot.transform);
            messageText = CreateLabel(panel.transform,
                "Bạn đang chơi dở một màn.\nTiếp tục hay bỏ?",
                new Vector2(0f, 40f), new Vector2(520f, 140f), 28);

            continueButton = CreateButton(panel.transform, "TIẾP TỤC", new Vector2(-130f, -90f),
                new Color(0.2f, 0.55f, 0.28f, 0.95f));
            abandonButton = CreateButton(panel.transform, "BỎ MÀN", new Vector2(130f, -90f),
                new Color(0.55f, 0.15f, 0.15f, 0.95f));
        }

        private static GameObject CreatePanel(Transform parent)
        {
            var panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600f, 320f);
            rt.anchoredPosition = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.08f, 0.06f, 0.06f, 0.96f);
            return panel;
        }

        private static TMP_Text CreateLabel(Transform parent, string text, Vector2 pos, Vector2 size, float fontSize)
        {
            var go = new GameObject("Message", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.92f, 0.88f, 0.82f, 1f);
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private static Button CreateButton(Transform parent, string label, Vector2 pos, Color bg)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(200f, 56f);
            rt.anchoredPosition = pos;

            var image = go.GetComponent<Image>();
            image.color = bg;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            StretchFull(textGo.GetComponent<RectTransform>());
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 26;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return button;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
