using HeartOfTheNight.Rooms;
using UnityEngine;
using UnityEngine.UI;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// HUD chìa: 2 skull trên nền.
    /// Hết chìa = SkullHubSkullInLine (xám). Có chìa = SkullKey (xanh/đỏ đúng art).
    /// </summary>
    public class KeyHUD : MonoBehaviour
    {
        public static KeyHUD Instance { get; private set; }

        [Header("Blue Key (skull trái)")]
        [SerializeField] private Image blueKeyImage;
        [SerializeField] private Text blueCountText;
        [SerializeField] private Sprite emptyBlueSprite;
        [SerializeField] private Sprite ownedBlueSprite;

        [Header("Red Key (skull phải)")]
        [SerializeField] private Image redKeyImage;
        [SerializeField] private Text redCountText;
        [SerializeField] private Sprite emptyRedSprite;
        [SerializeField] private Sprite ownedRedSprite;

        [Header("Look")]
        [SerializeField] private Color emptyLabelColor = Color.white;
        [SerializeField] private Color blueLabelColor = new Color(0.45f, 0.82f, 1f, 1f);
        [SerializeField] private Color redLabelColor = new Color(1f, 0.45f, 0.28f, 1f);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (blueKeyImage == null || redKeyImage == null)
            {
                Debug.LogWarning(
                    "[KeyHUD] Chua gan BlueKeyIcon / RedKeyIcon trong Inspector.",
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
            ApplySlot(
                blueKeyImage,
                blueCountText,
                PlayerKeyInventory.GetCount(KeyType.Blue),
                emptyBlueSprite,
                ownedBlueSprite,
                blueLabelColor);

            ApplySlot(
                redKeyImage,
                redCountText,
                PlayerKeyInventory.GetCount(KeyType.Red),
                emptyRedSprite,
                ownedRedSprite,
                redLabelColor);
        }

        private void ApplySlot(
            Image image,
            Text label,
            int count,
            Sprite emptySprite,
            Sprite ownedSprite,
            Color ownedLabelColor)
        {
            bool owned = count > 0;

            if (image != null)
            {
                image.enabled = true;
                image.color = Color.white;
                Sprite sprite = owned ? ownedSprite : emptySprite;
                if (sprite != null)
                    image.sprite = sprite;
            }

            if (label != null)
            {
                label.gameObject.SetActive(true);
                label.enabled = true;
                label.transform.SetAsLastSibling();
                label.text = count.ToString();
                label.color = owned ? ownedLabelColor : emptyLabelColor;
            }
        }
    }
}
