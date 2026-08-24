using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeartOfTheNight.Hung
{
    /// <summary>
    /// LoadSavePanel: 4 slot (Slot I–IV) + Selected / Delete.
    /// Selected → vào slot. Delete → popup xác nhận rồi mới xóa.
    /// Thanh slot không vào game.
    /// </summary>
    public class SaveSlotFlowUI : MonoBehaviour
    {
        private static readonly string[] RomanNumerals = { "I", "II", "III", "IV" };
        private static readonly Color TextNormal = new Color(1f, 1f, 1f, 0.95f);
        private static readonly Color TextSelected = new Color(1f, 0.62f, 0.22f, 1f);
        private static readonly Color TextMuted = new Color(1f, 1f, 1f, 0.32f);

        [SerializeField] private Transform slotsRoot;
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private TMP_Text[] slotLabels;
        [SerializeField] private Button[] selectButtons;
        [SerializeField] private Button[] deleteButtons;

        [Header("Delete Confirm — chỉnh trong Hierarchy")]
        [SerializeField] private GameObject deleteConfirmPopup;
        [SerializeField] private TMP_Text deleteConfirmMessage;
        [SerializeField] private Button deleteConfirmYesButton;
        [SerializeField] private Button deleteConfirmNoButton;
        [SerializeField] private Button deleteConfirmCloseButton;

        private Transform[] _slotRows;
        private TMP_Text[] _selectLabels;
        private TMP_Text[] _deleteLabels;

        private string[] _confirmMessageTemplates;
        private TMP_Text[] _confirmMessageTargets;
        private int _pendingDeleteSlot = -1;

        private void Awake()
        {
            if (slotsRoot == null)
                slotsRoot = transform.Find("SLOT");

            AutoWireSlotsIfNeeded();
            DisableSlotRowButtons();
            EnsureActionButtons();
            BindButtons();
            ApplySlotLabelLayout();
            ResolveDeleteConfirmRefs();
            BindDeleteConfirmButtons();
            CacheConfirmMessageTemplate();
            HideDeleteConfirm();
        }

        private void OnEnable()
        {
            HideDeleteConfirm();
            RefreshSlotLabels();
        }

        public void OnSlotClicked(int slotIndex)
        {
            var dm = DataManager.EnsureExists();
            if (dm == null)
            {
                Debug.LogError("[SaveSlotFlow] Không tạo được DataManager.");
                return;
            }

            Debug.Log($"[SaveSlotFlow] Selected Slot {slotIndex} | HasSave={DataManager.HasSave(slotIndex)}");
            dm.SelectSlotAndEnter(slotIndex);
        }

        public void OnDeleteClicked(int slotIndex)
        {
            if (!DataManager.HasSave(slotIndex))
                return;

            ShowDeleteConfirm(slotIndex);
        }

        public void OnSlot1() => OnSlotClicked(1);
        public void OnSlot2() => OnSlotClicked(2);
        public void OnSlot3() => OnSlotClicked(3);
        public void OnSlot4() => OnSlotClicked(4);

        public void RefreshSlotLabels()
        {
            EnsureLabelArray();

            for (int i = 0; i < DataManager.SlotCount; i++)
            {
                int slot = i + 1;
                bool hasSave = DataManager.HasSave(slot);

                if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
                    slotLabels[i].text = SlotTitle(slot);

                if (_selectLabels != null && i < _selectLabels.Length && _selectLabels[i] != null)
                    _selectLabels[i].color = TextNormal;

                if (deleteButtons != null && i < deleteButtons.Length && deleteButtons[i] != null)
                    deleteButtons[i].interactable = hasSave;

                if (_deleteLabels != null && i < _deleteLabels.Length && _deleteLabels[i] != null)
                    _deleteLabels[i].color = hasSave ? TextNormal : TextMuted;
            }
        }

        private static string SlotTitle(int slotIndex)
        {
            int i = Mathf.Clamp(slotIndex, 1, RomanNumerals.Length) - 1;
            return $"Slot {RomanNumerals[i]}";
        }

        private void BindButtons()
        {
            EnsureButtonArray();
            for (int i = 0; i < DataManager.SlotCount; i++)
            {
                int slot = i + 1;

                if (selectButtons != null && i < selectButtons.Length && selectButtons[i] != null)
                {
                    selectButtons[i].onClick.RemoveAllListeners();
                    selectButtons[i].onClick.AddListener(() => OnSlotClicked(slot));
                }

                if (deleteButtons != null && i < deleteButtons.Length && deleteButtons[i] != null)
                {
                    deleteButtons[i].onClick.RemoveAllListeners();
                    deleteButtons[i].onClick.AddListener(() => OnDeleteClicked(slot));
                }
            }
        }

        private void DisableSlotRowButtons()
        {
            if (_slotRows == null)
                return;

            for (int i = 0; i < _slotRows.Length; i++)
            {
                if (_slotRows[i] == null)
                    continue;

                var rowButton = _slotRows[i].GetComponent<Button>();
                if (rowButton != null)
                {
                    rowButton.onClick.RemoveAllListeners();
                    rowButton.enabled = false;
                    rowButton.interactable = false;
                }
            }

            if (slotButtons == null)
                return;

            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] == null)
                    continue;

                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].enabled = false;
                slotButtons[i].interactable = false;
            }
        }

        private void OnDisable()
        {
            RestoreConfirmMessageTemplate();
        }

        private void ShowDeleteConfirm(int slotIndex)
        {
            _pendingDeleteSlot = slotIndex;
            ResolveDeleteConfirmRefs();

            if (_confirmMessageTargets != null)
            {
                for (int i = 0; i < _confirmMessageTargets.Length; i++)
                {
                    if (_confirmMessageTargets[i] == null || string.IsNullOrEmpty(_confirmMessageTemplates[i]))
                        continue;
                    _confirmMessageTargets[i].text =
                        _confirmMessageTemplates[i].Replace("{slot}", SlotTitle(slotIndex));
                }
            }

            if (deleteConfirmPopup != null)
            {
                deleteConfirmPopup.transform.SetAsLastSibling();
                deleteConfirmPopup.SetActive(true);
            }
            else
            {
                Debug.LogError("[SaveSlotFlow] Chưa gán DeleteConfirmPopup trên LoadSavePanel.");
            }
        }

        public void HideDeleteConfirm()
        {
            _pendingDeleteSlot = -1;
            RestoreConfirmMessageTemplate();
            if (deleteConfirmPopup != null)
                deleteConfirmPopup.SetActive(false);
        }

        private void ConfirmDelete()
        {
            int slot = _pendingDeleteSlot;
            HideDeleteConfirm();
            if (slot < 1)
                return;

            var dm = DataManager.EnsureExists();
            if (dm == null)
            {
                Debug.LogError("[SaveSlotFlow] Không tạo được DataManager.");
                return;
            }

            dm.DeleteSave(slot);
            RefreshSlotLabels();
        }

        private void ApplySlotLabelLayout()
        {
            EnsureLabelArray();
            for (int i = 0; i < DataManager.SlotCount; i++)
            {
                if (slotLabels[i] == null)
                    continue;
                slotLabels[i].text = SlotTitle(i + 1);
            }
        }

        private void AutoWireSlotsIfNeeded()
        {
            if (slotsRoot == null)
                return;

            _slotRows = new Transform[DataManager.SlotCount];
            for (int i = 0; i < slotsRoot.childCount; i++)
            {
                var child = slotsRoot.GetChild(i);
                int index = GuessSlotIndex(child.name, i);
                if (index < 0 || index >= DataManager.SlotCount)
                    continue;
                if (_slotRows[index] == null)
                    _slotRows[index] = child;
            }

            bool labelsReady = slotLabels != null && slotLabels.Length >= DataManager.SlotCount
                && slotLabels[0] != null;
            if (!labelsReady)
                slotLabels = new TMP_Text[DataManager.SlotCount];

            slotButtons = new Button[DataManager.SlotCount];

            for (int i = 0; i < DataManager.SlotCount; i++)
            {
                var t = _slotRows[i];
                if (t == null)
                    continue;

                slotButtons[i] = t.GetComponent<Button>();
                if (!labelsReady || slotLabels[i] == null)
                    slotLabels[i] = FindSlotTitleLabel(t);
            }
        }

        private static TMP_Text FindSlotTitleLabel(Transform slot)
        {
            var texts = slot.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                string n = texts[i].gameObject.name;
                if (n == "Selected" || n == "Delete")
                    continue;
                return texts[i];
            }

            return texts.Length > 0 ? texts[0] : null;
        }

        private void EnsureActionButtons()
        {
            if (selectButtons == null || selectButtons.Length < DataManager.SlotCount)
                selectButtons = new Button[DataManager.SlotCount];
            if (deleteButtons == null || deleteButtons.Length < DataManager.SlotCount)
                deleteButtons = new Button[DataManager.SlotCount];

            _selectLabels = new TMP_Text[DataManager.SlotCount];
            _deleteLabels = new TMP_Text[DataManager.SlotCount];

            for (int i = 0; i < DataManager.SlotCount; i++)
            {
                Transform slot = _slotRows != null && i < _slotRows.Length ? _slotRows[i] : null;
                if (slot == null)
                    continue;

                TMP_Text fontSource = (slotLabels != null && i < slotLabels.Length) ? slotLabels[i] : null;

                if (selectButtons[i] == null)
                {
                    selectButtons[i] = FindOrCreateActionButton(
                        slot, "Selected", "selected",
                        new Vector2(-148f, 3f), new Vector2(150f, 50f),
                        fontSource);
                }

                if (deleteButtons[i] == null)
                {
                    deleteButtons[i] = FindOrCreateActionButton(
                        slot, "Delete", "delete",
                        new Vector2(-18f, 3f), new Vector2(120f, 50f),
                        fontSource);
                }

                _selectLabels[i] = selectButtons[i].GetComponent<TMP_Text>();
                _deleteLabels[i] = deleteButtons[i].GetComponent<TMP_Text>();
            }
        }

        private static Button FindOrCreateActionButton(
            Transform slot, string objectName, string label,
            Vector2 anchoredPos, Vector2 size, TMP_Text fontSource)
        {
            Transform existing = slot.Find(objectName);
            if (existing != null)
            {
                var existingButton = existing.GetComponent<Button>();
                if (existingButton != null)
                    return existingButton;
            }

            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(slot, false);
            go.layer = slot.gameObject.layer;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.MidlineRight;
            tmp.color = TextNormal;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = true;
            CopyFont(fontSource, tmp);

            var button = go.AddComponent<Button>();
            button.targetGraphic = tmp;
            button.transition = Selectable.Transition.ColorTint;

            var colors = button.colors;
            colors.normalColor = TextNormal;
            colors.highlightedColor = TextSelected;
            colors.pressedColor = new Color(0.85f, 0.4f, 0.12f, 1f);
            colors.selectedColor = TextNormal;
            colors.disabledColor = TextMuted;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            return button;
        }

        private void ResolveDeleteConfirmRefs()
        {
            if (deleteConfirmPopup == null)
            {
                var found = transform.Find("DeleteConfirmPopup");
                if (found != null)
                    deleteConfirmPopup = found.gameObject;
            }

            if (deleteConfirmPopup == null)
                return;

            if (deleteConfirmMessage == null)
            {
                var message = deleteConfirmPopup.transform.Find("Panel/Message");
                if (message != null)
                    deleteConfirmMessage = message.GetComponent<TMP_Text>();
            }

            if (deleteConfirmYesButton == null)
            {
                var yes = deleteConfirmPopup.transform.Find("Panel/ButtonYes");
                if (yes != null)
                    deleteConfirmYesButton = yes.GetComponent<Button>();
            }

            if (deleteConfirmNoButton == null)
            {
                var no = deleteConfirmPopup.transform.Find("Panel/ButtonNo");
                if (no != null)
                    deleteConfirmNoButton = no.GetComponent<Button>();
            }

            if (deleteConfirmCloseButton == null)
                deleteConfirmCloseButton = FindNamedButton(deleteConfirmPopup.transform, "CloseButton", "ButtonClose", "X");

            DisableDecorativeWindowButtons(deleteConfirmPopup.transform);
        }

        private void BindDeleteConfirmButtons()
        {
            if (deleteConfirmYesButton != null)
            {
                deleteConfirmYesButton.onClick.RemoveAllListeners();
                deleteConfirmYesButton.onClick.AddListener(ConfirmDelete);
            }

            if (deleteConfirmNoButton != null)
            {
                deleteConfirmNoButton.onClick.RemoveAllListeners();
                deleteConfirmNoButton.onClick.AddListener(HideDeleteConfirm);
            }

            if (deleteConfirmCloseButton != null)
            {
                deleteConfirmCloseButton.onClick.RemoveAllListeners();
                deleteConfirmCloseButton.onClick.AddListener(HideDeleteConfirm);
            }
        }

        private static Button FindNamedButton(Transform root, params string[] names)
        {
            if (root == null)
                return null;

            var buttons = root.GetComponentsInChildren<Button>(true);
            for (int n = 0; n < names.Length; n++)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    if (buttons[i] != null && buttons[i].gameObject.name == names[n])
                        return buttons[i];
                }
            }

            return null;
        }

        private static void DisableDecorativeWindowButtons(Transform root)
        {
            if (root == null)
                return;

            var buttons = root.GetComponentsInChildren<Button>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null)
                    continue;

                string n = buttons[i].gameObject.name;
                if (n == "ButtonMin" || n == "ButtonMax" || n == "Minimize" || n == "Maximize"
                    || n == "MinButton" || n == "MaxButton")
                {
                    buttons[i].onClick.RemoveAllListeners();
                    buttons[i].interactable = false;
                    buttons[i].enabled = false;
                }
            }
        }

        private void CacheConfirmMessageTemplate()
        {
            if (deleteConfirmPopup == null)
                return;

            var texts = deleteConfirmPopup.GetComponentsInChildren<TMP_Text>(true);
            int count = 0;
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && !string.IsNullOrEmpty(texts[i].text) && texts[i].text.Contains("{slot}"))
                    count++;
            }

            _confirmMessageTargets = new TMP_Text[count];
            _confirmMessageTemplates = new string[count];
            int w = 0;
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null || string.IsNullOrEmpty(texts[i].text) || !texts[i].text.Contains("{slot}"))
                    continue;
                _confirmMessageTargets[w] = texts[i];
                _confirmMessageTemplates[w] = texts[i].text;
                w++;
            }
        }

        private void RestoreConfirmMessageTemplate()
        {
            if (_confirmMessageTargets == null)
                return;

            for (int i = 0; i < _confirmMessageTargets.Length; i++)
            {
                if (_confirmMessageTargets[i] == null || string.IsNullOrEmpty(_confirmMessageTemplates[i]))
                    continue;
                _confirmMessageTargets[i].text = _confirmMessageTemplates[i];
            }
        }

        private static void CopyFont(TMP_Text source, TMP_Text target)
        {
            if (source == null || target == null)
                return;

            target.font = source.font;
            target.fontSharedMaterial = source.fontSharedMaterial;
        }

        private static int GuessSlotIndex(string name, int fallbackOrder)
        {
            if (string.IsNullOrEmpty(name))
                return fallbackOrder;

            if (name.Contains("(3)")) return 3;
            if (name.Contains("(2)")) return 2;
            if (name.Contains("(1)")) return 1;
            if (name.StartsWith("SLOT")) return 0;
            return fallbackOrder;
        }

        private void EnsureButtonArray()
        {
            if (slotButtons == null)
                slotButtons = new Button[DataManager.SlotCount];
        }

        private void EnsureLabelArray()
        {
            if (slotLabels == null)
                slotLabels = new TMP_Text[DataManager.SlotCount];
        }
    }
}
