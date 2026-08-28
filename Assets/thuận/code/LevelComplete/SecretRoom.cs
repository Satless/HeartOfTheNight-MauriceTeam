using UnityEngine;

/// <summary>
/// Đánh dấu phòng bí mật. Kéo vào collider phòng (Is Trigger), hoặc bật Is Secret Room trên RoomCameraPriority.
/// Player bước vào là tính 1 secret đã tìm.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SecretRoom : MonoBehaviour
{
    [SerializeField] private string secretId;
    [SerializeField] private string playerTag = "Player";

    public string SecretId
    {
        get
        {
            if (!string.IsNullOrEmpty(secretId))
                return secretId;
            return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_" + gameObject.name;
        }
    }

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsPlayer(other))
            return;

        LevelStatsTracker.DiscoverSecret(SecretId);
    }

    private bool IsPlayer(Collider2D other)
    {
        if (other == null)
            return false;
        if (other.CompareTag(playerTag))
            return true;
        return other.transform.root.CompareTag(playerTag);
    }
}
