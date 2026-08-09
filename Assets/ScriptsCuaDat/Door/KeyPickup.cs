using UnityEngine;

namespace HeartOfTheNight.Rooms
{
    /// <summary>
    /// Gan len prefab BlueKey / RedKey. Player cham trigger la nhat chia.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KeyPickup : MonoBehaviour
    {
        [SerializeField] private KeyType keyType = KeyType.Blue;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool destroyOnPickup = true;

        private bool collected;

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
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;
            if (keyType == KeyType.None) return;
            if (!IsPlayer(other)) return;

            collected = true;
            PlayerKeyInventory.Add(keyType);

            if (destroyOnPickup)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
        }

        private bool IsPlayer(Collider2D other)
        {
            if (other == null) return false;
            if (other.CompareTag(playerTag)) return true;
            return other.transform.root.CompareTag(playerTag);
        }
    }
}
