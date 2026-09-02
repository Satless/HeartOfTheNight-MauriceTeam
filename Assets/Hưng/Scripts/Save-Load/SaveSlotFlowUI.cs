using System;
using System.Globalization;
using System.IO;
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
        private static readonly Color TextMeta = new Color(1f, 1f, 1f, 0.7f);
        private static readonly Color RowOccupied = Color.white;
        private static readonly Color RowEmpty = new Color(1f, 1f, 1f, 0.62f);

        // Bố cục hàng 100px: title trên-trái, empty/ngày dưới title, new/delete giữa dọc bên phải.
        private const float TitlePosX = 32f;
        private const float TitlePosY = 12f;
        private const float MetaPosY = -20f;
        private const float ActionPosY = 0f;
        private const float SelectPosX = -148f;
        private const float DeletePosX = -18f;

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
        private TMP_Text[] _metaLabels;

        private string[] _confirmMessageTemplates;
        private TMP_Text[] _confirmMessageTargets;
        private int _pendingDeleteSlot = -1;

        private void Awake()
        {
            if (slotsRoot == null)
                slotsRoot = transform.Find("SLOT");

            AutoWireSlotsIfNeeded();
            WireSlotRowButtons();
            EnsureActionButtons();
            EnsureMetaLabels();
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
            var dm = DataManager.EnsureExists();
            if (dm == null)
                return;

            dm.RefreshCloudSlotIndex(() =>
            {
                if (this == null || !isActiveAndEnabled)
                    return;
                RefreshSlotLabels();
            });
        }

        public void OnSlotClicked(int slotIndex)
        {
            var dm = DataManager.EnsureExists();
            if (dm == null)
            {
                Debug.LogError("[SaveSlotFlow] Không tạo được DataManager.");
                return;
            }

            if (dm.IsWaitingForCloudSlots)
            {
                Debug.Log("[SaveSlotFlow] Đang đồng bộ slot cloud, đợi xong đã.");
                return;
            }

            if (dm.IsDeletingSave)
                return;

            if (IsDeleteConfirmOpen())
                return;

            Debug.Log($"[SaveSlotFlow] Selected Slot {slotIndex} | HasSave={DataManager.HasSave(slotIndex)}");
            dm.SelectSlotAndEnter(slotIndex);
        }

        public void OnDeleteClicked(int slotIndex)
        {
            var dm = DataManager.Instance;
            if (dm != null && dm.IsWaitingForCloudSlots)
                return;
            if (dm != null && dm.IsDeletingSave)
                return;
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
            EnsureMetaLabels();

            int activeSlot = DataManager.GetActiveSlotIndex();
            var dm = DataManager.Instance;
            bool waitingCloud = dm != null && dm.IsWaitingForCloudSlots;
            bool deleting = dm != null && dm.IsDeletingSave;
            bool blockActions = waitingCloud || deleting;

            for (int i = 0; i < DataManager.SlotCount; i++)
            {
                int slot = i + 1;
                bool hasSave = DataManager.HasSave(slot);
                bool isCurrent = hasSave && slot == activeSlot;
                DataManager.TryPeekSlot(slot, out GameData peek);

                if (slotLabels != null && i < slotLabels.Length && slotLabels[i] != null)
                {
                    slotLabels[i].text = SlotTitle(slot);
                    slotLabels[i].color = isCurrent ? TextSelected : (hasSave ? TextNormal : TextMuted);
                }

                if (_metaLabels != null && i < _metaLabels.Length && _metaLabels[i] != null)
                {
                    if (waitingCloud && !hasSave)
                    {
                        _metaLabels[i].text = "syncing...";
                        _metaLabels[i].color = TextMuted;
                    }
                    else
                    {
                        _metaLabels[i].text = hasSave ? FormatOccupiedMeta(peek, slot) : "empty";
                        _metaLabels[i].color = hasSave ? TextMeta : TextMuted;
                    }
                }

                if (_selectLabels != null && i < _selectLabels.Length && _selectLabels[i] != null)
                {
                    if (blockActions)
                        _selectLabels[i].text = "wait";
                    else if (isCurrent)
                        _selectLabels[i].text = "selected";
                    else
                        _selectLabels[i].text = hasSave ? "select" : "new";
                    _selectLabels[i].color = isCurrent ? TextSelected : TextNormal;
                }

                if (selectButtons != null && i < selectButtons.Length && selectButtons[i] != null)
                    selectButtons[i].interactable = !blockActions;

                if (deleteButtons != null && i < deleteButtons.Length && deleteButtons[i] != null)
                    deleteButtons[i].interactable = hasSave && !blockActions;

                if (_deleteLabels != null && i < _deleteLabels.Length && _deleteLabels[i] != null)
                    _deleteLabels[i].color = hasSave ? TextNormal : TextMuted;

                if (_slotRows != null && i < _slotRows.Length && _slotRows[i] != null)
                {
                    var rowImage = _slotRows[i].GetComponent<Image>();
                    if (rowImage != null)
                        rowImage.color = hasSave ? RowOccupied : RowEmpty;
                }
            }
        }

        private static string SlotTitle(int slotIndex)
        {
            int i = Mathf.Clamp(slotIndex, 1, RomanNumerals.Length) - 1;
            return $"Slot {RomanNumerals[i]}";
        }

        private void EnsureMetaLabels()
        {
            if (_metaLabels == null || _metaLabels.Length < DataManager.SlotCount)
                _metaLabels = new TMP_Text[DataManager.SlotCount];

            for (int i = 0; i < DataManager.SlotCount; i++)
            {
                Transform slot = _slotRows != null && i < _slotRows.Length ? _slotRows[i] : null;
                if (slot == null)
                    continue;

                TMP_Text title = (slotLabels != null && i < slotLabels.Length) ? slotLabels[i] : null;
                if (title != null)
                    ApplyTitleLayout(title);

                if (_metaLabels[i] == null)
                    _metaLabels[i] = FindOrCreateMetaLabel(slot, title);
                else
                    ApplyMetaLayout(_metaLabels[i], title);
            }
        }

        private static void ApplyTitleLayout(TMP_Text title)
        {
            var rt = title.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(TitlePosX, TitlePosY);
            rt.sizeDelta = new Vector2(280f, 40f);
            title.margin = Vector4.zero;
            title.enableWordWrapping = false;
            title.overflowMode = TextOverflowModes.Overflow;
            title.alignment = TextAlignmentOptions.MidlineLeft;
        }

        private static void ApplyMetaLayout(TMP_Text meta, TMP_Text title)
        {
            var rt = meta.rectTransform;
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchoredPosition = new Vector2(TitlePosX, MetaPosY);
            rt.sizeDelta = new Vector2(280f, 24f);
            meta.margin = Vector4.zero;
            meta.alignment = TextAlignmentOptions.MidlineLeft;
            meta.enableWordWrapping = false;
            meta.overflowMode = TextOverflowModes.Overflow;
        }

        private static TMP_Text FindOrCreateMetaLabel(Transform slot, TMP_Text fontSource)
        {
            Transform existing = slot.Find("Meta");
            if (existing != null)
            {
                var existingTmp = existing.GetComponent<TMP_Text>();
                if (existingTmp != null)
                {
                    ApplyMetaLayout(existingTmp, fontSource);
                    return existingTmp;
                }
            }

            var go = new GameObject("Meta", typeof(RectTransform));
            go.transform.SetParent(slot, false);
            go.layer = slot.gameObject.layer;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            ApplyMetaLayout(tmp, fontSource);
            tmp.text = "empty";
            tmp.fontSize = 16f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = TextMuted;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            CopyFont(fontSource, tmp);
            return tmp;
        }

        private static string FormatOccupiedMeta(GameData data, int slotIndex)
        {
            string playTime = FormatPlayTime(data != null ? data.totalPlayTimeSeconds : 0f);
            string date = FormatLastPlayed(data != null ? data.lastPlayedAtUtc : null)
                ?? FormatFileWriteDate(slotIndex);

            if (!string.IsNullOrEmpty(playTime) && !string.IsNullOrEmpty(date))
                return $"{playTime}  ·  {date}";
            if (!string.IsNullOrEmpty(playTime))
                return playTime;
            if (!string.IsNullOrEmpty(date))
                return date;
            return "saved";
        }

        private static string FormatFileWriteDate(int slotIndex)
        {
            string path = DataManager.GetSlotSavePath(slotIndex);
            if (!File.Exists(path) && slotIndex == 1)
                path = Path.Combine(Application.persistentDataPath, "save_data.json");
            if (!File.Exists(path))
                return null;

            DateTime local = File.GetLastWriteTime(path);
            return local.Year == DateTime.Now.Year
                ? local.ToString("dd/MM")
                : local.ToString("dd/MM/yy");
        }

        private static string FormatPlayTime(float seconds)
        {
            if (seconds < 60f)
                return "<1m";

            int totalMinutes = Mathf.FloorToInt(seconds / 60f);
            int hours = totalMinutes / 60;
            int minutes = totalMinutes % 60;
            if (hours <= 0)
                return $"{minutes}m";

            int days = hours / 24;
            hours %= 24;
            if (days > 0)
                return hours > 0 ? $"{days}d {hours}h" : $"{days}d";

            return minutes > 0 ? $"{hours}h {minutes}m" : $"{hours}h";
        }

        private static string FormatLastPlayed(string utc)
        {
            if (string.IsNullOrEmpty(utc))
                return null;

            if (!DateTime.TryParse(utc, null, DateTimeStyles.RoundtripKind, out DateTime parsed))
                return null;

            DateTime local = parsed.Kind == DateTimeKind.Utc ? parsed.ToLocalTime() : parsed;
            return local.Year == DateTime.Now.Year
                ? local.ToString("dd/MM")
                : local.ToString("dd/MM/yy");
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

        private void WireSlotRowButtons()
        {
            if (_slotRows == null)
                return;

            if (slotButtons == null || slotButtons.Length < DataManager.SlotCount)
                slotButtons = new Button[DataManager.SlotCount];

            for (int i = 0; i < _slotRows.Length; i++)
            {
                if (_slotRows[i] == null)
                    continue;

                int slot = i + 1;
                var rowButton = _slotRows[i].GetComponent<Button>();
                if (rowButton == null)
                    rowButton = _slotRows[i].gameObject.AddComponent<Button>();

                var graphic = _slotRows[i].GetComponent<Graphic>();
                if (graphic != null)
                    rowButton.targetGraphic = graphic;

                rowButton.enabled = true;
                rowButton.interactable = true;
                rowButton.transition = Selectable.Transition.ColorTint;
                rowButton.onClick.RemoveAllListeners();
                rowButton.onClick.AddListener(() => OnSlotClicked(slot));
                slotButtons[i] = rowButton;
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
            if (_confirmMessageTargets == null || _confirmMessageTargets.Length == 0)
                CacheConfirmMessageTemplate();

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

            if (deleteConfirmPopup == null)
                return;

            deleteConfirmPopup.transform.SetAsLastSibling();
            deleteConfirmPopup.SetActive(true);
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

            dm.DeleteSave(slot, _ =>
            {
                if (this == null)
                    return;
                RefreshSlotLabels();
            });
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
                if (n == "Selected" || n == "Delete" || n == "Meta")
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

                Vector2 selectPos = new Vector2(SelectPosX, ActionPosY);
                Vector2 selectSize = new Vector2(150f, 50f);
                Vector2 deletePos = new Vector2(DeletePosX, ActionPosY);
                Vector2 deleteSize = new Vector2(120f, 50f);

                if (selectButtons[i] == null)
                {
                    selectButtons[i] = FindOrCreateActionButton(
                        slot, "Selected", "select",
                        selectPos, selectSize,
                        fontSource, TextAlignmentOptions.MidlineRight);
                }

                if (deleteButtons[i] == null)
                {
                    deleteButtons[i] = FindOrCreateActionButton(
                        slot, "Delete", "delete",
                        deletePos, deleteSize,
                        fontSource, TextAlignmentOptions.MidlineRight);
                }

                ApplyActionButtonLayout(
                    selectButtons[i], selectPos, selectSize,
                    TextAlignmentOptions.MidlineRight);
                ApplyActionButtonLayout(
                    deleteButtons[i], deletePos, deleteSize,
                    TextAlignmentOptions.MidlineRight);

                _selectLabels[i] = selectButtons[i].GetComponent<TMP_Text>();
                _deleteLabels[i] = deleteButtons[i].GetComponent<TMP_Text>();
            }
        }

        private static void ApplyActionButtonLayout(
            Button button, Vector2 anchoredPos, Vector2 size, TextAlignmentOptions align)
        {
            if (button == null)
                return;

            var rt = button.transform as RectTransform;
            if (rt == null)
                return;

            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var tmp = button.GetComponent<TMP_Text>();
            if (tmp != null)
            {
                tmp.alignment = align;
                tmp.enableWordWrapping = false;
                tmp.overflowMode = TextOverflowModes.Truncate;
                tmp.margin = Vector4.zero;
            }
        }

        private static Button FindOrCreateActionButton(
            Transform slot, string objectName, string label,
            Vector2 anchoredPos, Vector2 size, TMP_Text fontSource, TextAlignmentOptions align)
        {
            Transform existing = slot.Find(objectName);
            if (existing != null)
            {
                var existingButton = existing.GetComponent<Button>();
                if (existingButton != null)
                {
                    ApplyActionButtonLayout(existingButton, anchoredPos, size, align);
                    return existingButton;
                }
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
            tmp.alignment = align;
            tmp.color = TextNormal;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Truncate;
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

        private bool IsDeleteConfirmOpen()
        {
            return deleteConfirmPopup != null && deleteConfirmPopup.activeSelf;
        }

        private void ResolveDeleteConfirmRefs()
        {
            if (deleteConfirmPopup == null)
            {
                var found = transform.Find("DeleteConfirmPopup");
                if (found == null)
                    found = FindDeep(transform, "DeleteConfirmPopup");
                if (found != null)
                    deleteConfirmPopup = found.gameObject;
            }

            if (deleteConfirmPopup == null)
                BuildRuntimeDeleteConfirm();

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

        private void BuildRuntimeDeleteConfirm()
        {
            TMP_Text fontSource = (slotLabels != null && slotLabels.Length > 0) ? slotLabels[0] : null;
            Sprite windowSprite = null;
            Image.Type windowType = Image.Type.Simple;
            var panelImage = GetComponent<Image>();
            if (panelImage != null)
            {
                windowSprite = panelImage.sprite;
                windowType = panelImage.type;
            }

            Transform canvasRoot = transform;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                canvasRoot = canvas.transform;

            var overlay = new GameObject("DeleteConfirmPopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlay.layer = gameObject.layer;
            overlay.transform.SetParent(canvasRoot, false);
            StretchFull(overlay.GetComponent<RectTransform>());
            var dim = overlay.GetComponent<Image>();
            dim.color = new Color(0.02f, 0f, 0f, 0.72f);
            dim.raycastTarget = true;

            var panel = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.layer = gameObject.layer;
            panel.transform.SetParent(overlay.transform, false);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(560f, 280f);
            panelRt.anchoredPosition = Vector2.zero;
            var window = panel.GetComponent<Image>();
            window.raycastTarget = true;
            if (windowSprite != null)
            {
                window.sprite = windowSprite;
                window.color = Color.white;
                window.type = windowType;
            }
            else
            {
                window.color = new Color(0.12f, 0.04f, 0.04f, 0.98f);
            }

            deleteConfirmMessage = CreatePopupLabel(
                panel.transform, "Message",
                "Xóa {slot}?\n\nSave sẽ mất vĩnh viễn.",
                new Vector2(0f, 36f), new Vector2(480f, 120f), 26f, fontSource);

            deleteConfirmYesButton = CreatePopupButton(
                panel.transform, "ButtonYes", "XÓA",
                new Vector2(-110f, -78f), new Color(0.62f, 0.12f, 0.12f, 0.95f), fontSource);
            deleteConfirmNoButton = CreatePopupButton(
                panel.transform, "ButtonNo", "HỦY",
                new Vector2(110f, -78f), new Color(0.18f, 0.16f, 0.16f, 0.95f), fontSource);
            deleteConfirmCloseButton = CreatePopupButton(
                panel.transform, "CloseButton", "X",
                new Vector2(236f, 112f), new Color(0.35f, 0.1f, 0.1f, 0.9f), fontSource);
            var closeRt = deleteConfirmCloseButton.transform as RectTransform;
            if (closeRt != null)
                closeRt.sizeDelta = new Vector2(44f, 36f);

            overlay.SetActive(false);
            deleteConfirmPopup = overlay;
        }

        private static TMP_Text CreatePopupLabel(
            Transform parent, string objectName, string text,
            Vector2 pos, Vector2 size, float fontSize, TMP_Text fontSource)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(1f, 0.92f, 0.88f, 0.95f);
            tmp.enableWordWrapping = true;
            tmp.raycastTarget = false;
            CopyFont(fontSource, tmp);
            return tmp;
        }

        private static Button CreatePopupButton(
            Transform parent, string objectName, string label,
            Vector2 pos, Color bg, TMP_Text fontSource)
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.layer = parent.gameObject.layer;
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(180f, 52f);

            var image = go.GetComponent<Image>();
            image.color = bg;
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.layer = go.layer;
            textGo.transform.SetParent(go.transform, false);
            StretchFull(textGo.GetComponent<RectTransform>());
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 24f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            CopyFont(fontSource, tmp);
            return button;
        }

        private static Transform FindDeep(Transform root, string objectName)
        {
            if (root == null)
                return null;
            var all = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].name == objectName)
                    return all[i];
            }
            return null;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
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
