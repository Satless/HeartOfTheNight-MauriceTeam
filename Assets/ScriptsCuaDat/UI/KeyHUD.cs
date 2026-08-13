using HeartOfTheNight.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// Điều khiển HUD chìa trên Canvas có sẵn trong Editor.
    /// Gán Image + Text trong Inspector — không tự tạo UI lúc Play.
    /// Layout khuyến nghị: [BlueIcon][BlueCount]  [RedIcon][RedCount] góc phải.
    /// </summary>
    public class KeyHUD : MonoBehaviour
    {
        public static KeyHUD Instance { get; private set; }

        [Header("Blue Key")]
        [SerializeField] private Image blueKeyImage;
        [SerializeField] private Text blueCountText;

        [Header("Red Key")]
        [SerializeField] private Image redKeyImage;
        [SerializeField] private Text redCountText;

        [Header("Look")]
        [SerializeField] private Color ownedColor = Color.white;
        [SerializeField] private Color emptyColor = new Color(0.45f, 0.45f, 0.45f, 0.9f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (blueKeyImage == null || redKeyImage == null || blueCountText == null || redCountText == null)
            {
                Debug.LogWarning(
                    "[KeyHUD] Chua gan du Image/Text trong Inspector. " +
                    "Mo prefab KeyHUD va keo BlueKeyIcon, BlueCount, RedKeyIcon, RedCount vao.",
                    this);
            }
        }

        private void OnEnable()
        {
            PlayerKeyInventory.OnKeysChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            PlayerKeyInventory.OnKeysChanged -= Refresh;
        }

        private void Start()
        {
            Refresh();
            // Save Firebase load async
            Invoke(nameof(Refresh), 0.5f);
            Invoke(nameof(Refresh), 1.5f);
            Invoke(nameof(Refresh), 3f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Refresh()
        {
            ApplySlot(blueKeyImage, blueCountText, PlayerKeyInventory.GetCount(KeyType.Blue));
            ApplySlot(redKeyImage, redCountText, PlayerKeyInventory.GetCount(KeyType.Red));
        }

        private void ApplySlot(Image image, Text label, int count)
        {
            Color color = count > 0 ? ownedColor : emptyColor;

            if (image != null)
            {
                image.enabled = true;
                image.color = color;
            }

            if (label != null)
            {
                label.text = count.ToString();
                label.color = color;
            }
        }
    }
}
