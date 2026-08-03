using UnityEngine;

public class LevelEntrance : MonoBehaviour
{
    [Header("ID Cửa (Phải khớp với spawnIDInNextScene ở Scene trước)")]
    public string entranceID;

    // THÊM BIẾN NÀY: Kéo object cửa ở Scene mới vào đây
    [Header("Cửa tại vị trí này")]
    public RoomDoor entranceDoor;

    // Đổi private void Start() thành dạng IEnumerator:
    private System.Collections.IEnumerator Start()
    {
        // Ép hệ thống chờ 1 khung hình để đảm bảo Player đã xuất hiện trên Scene
        yield return new UnityEngine.WaitForEndOfFrame();

        if (HeartOfTheNight.Hung.DataManager.Instance != null && HeartOfTheNight.Hung.DataManager.Instance.Data.targetSpawnID == entranceID)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.simulated = false;

                player.transform.position = transform.position;
                Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z);

                var hp = player.GetComponent<HeartOfTheNight.Player.PlayerHealth>();
                if (hp != null) hp.SyncHealthFromSave();

                if (rb != null) rb.simulated = true;
            }

            if (entranceDoor != null) entranceDoor.Open();

            HeartOfTheNight.Hung.DataManager.Instance.Data.targetSpawnID = "";
        }
    }
}