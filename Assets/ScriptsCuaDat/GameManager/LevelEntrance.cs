using System.Collections;
using UnityEngine;

/// <summary>
/// Điểm spawn khi vào scene. entranceID phải khớp spawnIDInNextScene của RoomTransition scene trước.
/// PendingSpawnID (static) không bị Firebase LoadGame ghi đè.
/// </summary>
public class LevelEntrance : MonoBehaviour
{
    [Header("ID Cửa (Phải khớp với spawnIDInNextScene ở Scene trước)")]
    public string entranceID;

    [Header("Cửa tại vị trí này")]
    public RoomDoor entranceDoor;

    /// <summary>Set bởi RoomTransition trước LoadScene — sống qua cloud load overwrite.</summary>
    public static string PendingSpawnID { get; set; }

    private void Start()
    {
        StartCoroutine(ApplyWhenReady());
    }

    private IEnumerator ApplyWhenReady()
    {
        // Vài frame: player + DataManager kịp sẵn; ScreenFader cũng gọi TryApplyAllPending sau load.
        for (int i = 0; i < 8; i++)
        {
            if (TryApplySpawn())
                yield break;
            yield return null;
        }
    }

    /// <summary>Gọi sau LoadSceneAsync (từ ScreenFader) để chắc spawn đúng điểm.</summary>
    public static void TryApplyAllPending()
    {
        if (!HasPendingSpawn()) return;

        var entrances = Object.FindObjectsByType<LevelEntrance>(FindObjectsSortMode.None);
        for (int i = 0; i < entrances.Length; i++)
        {
            if (entrances[i] != null && entrances[i].TryApplySpawn())
                return;
        }
    }

    public static void SetPendingSpawn(string id)
    {
        PendingSpawnID = id ?? "";
        if (HeartOfTheNight.Hung.DataManager.Instance?.Data != null)
            HeartOfTheNight.Hung.DataManager.Instance.Data.targetSpawnID = PendingSpawnID;
    }

    public static void ClearPendingSpawn()
    {
        PendingSpawnID = "";
        if (HeartOfTheNight.Hung.DataManager.Instance?.Data != null)
            HeartOfTheNight.Hung.DataManager.Instance.Data.targetSpawnID = "";
    }

    private static bool HasPendingSpawn()
    {
        if (!string.IsNullOrEmpty(PendingSpawnID)) return true;
        var dm = HeartOfTheNight.Hung.DataManager.Instance;
        return dm?.Data != null && !string.IsNullOrEmpty(dm.Data.targetSpawnID);
    }

    private bool MatchesPending()
    {
        if (string.IsNullOrEmpty(entranceID)) return false;
        if (!string.IsNullOrEmpty(PendingSpawnID) && PendingSpawnID == entranceID)
            return true;
        var dm = HeartOfTheNight.Hung.DataManager.Instance;
        return dm?.Data != null && dm.Data.targetSpawnID == entranceID;
    }

    public bool TryApplySpawn()
    {
        if (!MatchesPending()) return false;

        GameObject player = FindPlayerRoot();
        if (player == null) return false;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        player.transform.position = transform.position;

        if (Camera.main != null)
        {
            Vector3 cam = Camera.main.transform.position;
            Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, cam.z);
        }

        var hp = player.GetComponent<HeartOfTheNight.Player.PlayerHealth>();
        if (hp != null) hp.SyncHealthFromSave();

        if (rb != null) rb.simulated = true;

        if (entranceDoor != null) entranceDoor.Open(instant: true);

        ClearPendingSpawn();
        return true;
    }

    /// <summary>
    /// Prefab player có Hurtbox cũng tag Player — phải lấy object có Rigidbody2D (root).
    /// </summary>
    public static GameObject FindPlayerRoot()
    {
        GameObject[] tagged = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < tagged.Length; i++)
        {
            if (tagged[i] != null && tagged[i].GetComponent<Rigidbody2D>() != null)
                return tagged[i];
        }

        for (int i = 0; i < tagged.Length; i++)
        {
            if (tagged[i] == null) continue;
            Transform root = tagged[i].transform.root;
            if (root.GetComponent<Rigidbody2D>() != null)
                return root.gameObject;
            return root.gameObject;
        }

        return null;
    }
}
