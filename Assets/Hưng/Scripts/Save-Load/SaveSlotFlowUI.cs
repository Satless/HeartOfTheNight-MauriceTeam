using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeartOfTheNight.Hung
{
    /// <summary>
    /// Bước 1 sơ đồ Save: chọn slot trên LoadSavePanel.
    /// Trống → tạo save + Level 0-1. Có save → Select Level.
    /// (Popup Continue/Bỏ + hiển thị thời gian chơi làm ở bước sau.)
    /// </summary>
    public class SaveSlotFlowUI : MonoBehaviour
    {
        [SerializeField] private Transform slotsRoot;
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private TMP_Text[] slotLabels;

        private void Awake()
        {
            if (slotsRoot == null)
                slotsRoot = transform.Find("SLOT");

            AutoWireSlotsIfNeeded();
            BindButtons();
        }

        private void OnEnable()
        {
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

            Debug.Log($"[SaveSlotFlow] Chọn Slot {slotIndex} | HasSave={DataManager.HasSave(slotIndex)}");
            dm.SelectSlotAndEnter(slotIndex);
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
                if (slotLabels == null || i >= slotLabels.Length || slotLabels[i] == null)
                    continue;

                if (DataManager.HasSave(slot))
                    slotLabels[i].text = $"Slot {slot} — Đã có save";
                else
                    slotLabels[i].text = $"Slot {slot} — Empty";
            }
        }

        private void BindButtons()
        {
            EnsureButtonArray();
            for (int i = 0; i < slotButtons.Length; i++)
            {
                if (slotButtons[i] == null)
                    continue;

                int slot = i + 1;
                slotButtons[i].onClick.RemoveAllListeners();
                slotButtons[i].onClick.AddListener(() => OnSlotClicked(slot));
            }
        }

        private void AutoWireSlotsIfNeeded()
        {
            if (slotButtons != null && slotButtons.Length >= DataManager.SlotCount)
                return;

            if (slotsRoot == null)
                return;

            // Hierarchy hiện tại: SLOT 1, SLOT 1 (1), SLOT 1 (2), SLOT 1 (3)
            var ordered = new Transform[DataManager.SlotCount];
            for (int i = 0; i < slotsRoot.childCount; i++)
            {
                var child = slotsRoot.GetChild(i);
                int index = GuessSlotIndex(child.name, i);
                if (index < 0 || index >= DataManager.SlotCount)
                    continue;
                if (ordered[index] == null)
                    ordered[index] = child;
            }

            slotButtons = new Button[DataManager.SlotCount];
            slotLabels = new TMP_Text[DataManager.SlotCount];

            for (int i = 0; i < DataManager.SlotCount; i++)
            {
                var t = ordered[i];
                if (t == null)
                    continue;

                var button = t.GetComponent<Button>();
                if (button == null)
                {
                    button = t.gameObject.AddComponent<Button>();
                    var image = t.GetComponent<Image>();
                    if (image != null)
                    {
                        button.targetGraphic = image;
                        image.raycastTarget = true;
                    }
                }

                slotButtons[i] = button;
                slotLabels[i] = t.GetComponentInChildren<TMP_Text>(true);
            }
        }

        private static int GuessSlotIndex(string name, int fallbackOrder)
        {
            if (string.IsNullOrEmpty(name))
                return fallbackOrder;

            // "SLOT 1" → 0, "SLOT 1 (1)" → 1, "SLOT 1 (2)" → 2, "SLOT 1 (3)" → 3
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
