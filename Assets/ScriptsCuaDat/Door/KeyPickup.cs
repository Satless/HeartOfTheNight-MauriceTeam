using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeartOfTheNight.Rooms
{
    /// <summary>
    /// Gan len prefab BlueKey / RedKey. Player cham trigger la nhat chia.
    /// Gan pickupId rieng neu scene co nhieu key cung mau (khuyen nghi).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KeyPickup : MonoBehaviour
    {
        [SerializeField] private KeyType keyType = KeyType.Blue;
        [Tooltip("Id duy nhat tren map. De trong = SceneName_GameObjectName.")]
        [SerializeField] private string pickupId;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool destroyOnPickup = true;

        private bool collected;
        private string resolvedPickupId;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void Awake()
        {
            var col = GetComponent<Collider2D>();
            if (col != null && !col.isTrigger)
                Debug.LogWarning($"[{name}] Collider2D cua KeyPickup nen bat Is Trigger.", this);

            resolvedPickupId = ResolvePickupId();
        }

        private void Start()
        {
            // DataManager co the load async — thu an neu da nhat; retry nhe.
            if (TryHideIfAlreadyCollected())
                return;

            Invoke(nameof(RetryHideIfAlreadyCollected), 0.5f);
            Invoke(nameof(RetryHideIfAlreadyCollected), 1.5f);
        }

        private void RetryHideIfAlreadyCollected()
        {
            TryHideIfAlreadyCollected();
        }

        private bool TryHideIfAlreadyCollected()
        {
            if (collected) return true;
            if (string.IsNullOrEmpty(resolvedPickupId)) return false;

            if (!PlayerKeyInventory.IsPickupCollected(resolvedPickupId))
                return false;

            collected = true;
            if (destroyOnPickup)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
            return true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;
            if (keyType == KeyType.None) return;
            if (!IsPlayer(other)) return;

            if (string.IsNullOrEmpty(resolvedPickupId))
            {
                Debug.LogWarning($"[{name}] pickupId trong — van cho nhat nhung khong persist object tren map.", this);
            }
            else if (PlayerKeyInventory.IsPickupCollected(resolvedPickupId))
            {
                collected = true;
                if (destroyOnPickup) Destroy(gameObject);
                else gameObject.SetActive(false);
                return;
            }

            collected = true;
            PlayerKeyInventory.Add(keyType, resolvedPickupId);

            if (destroyOnPickup)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

        private string ResolvePickupId()
        {
            string sceneName = gameObject.scene.name;
            if (string.IsNullOrEmpty(sceneName))
                sceneName = SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(sceneName))
                sceneName = "UnknownScene";

            if (!string.IsNullOrWhiteSpace(pickupId))
            {
                // Id gõ tay phải gắn scene: id trần ("Blue2") thì scene khác trùng theo,
                // và chơi lại màn cũng không reset được vì ClearSceneLocalProgress lọc theo prefix scene.
                string manualId = pickupId.Trim();
                return HeartOfTheNight.Hung.GameData.IdBelongsToScene(manualId, sceneName)
                    ? manualId
                    : $"{sceneName}_{manualId}";
            }

            string autoId = $"{sceneName}_{gameObject.name}";
            Debug.LogWarning(
                $"[{name}] pickupId trong Inspector trong. Dung fallback '{autoId}'. " +
                "Nen gan id rieng neu scene co nhieu key.",
                this);
            return autoId;
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other == null) return false;
            if (other.CompareTag(playerTag)) return true;
            return other.transform.root.CompareTag(playerTag);
        }
    }
}
