using UnityEngine;

public class LevelEntrance : MonoBehaviour
{
    [Header("ID Cửa (Phải khớp với spawnIDInNextScene ở Scene trước)")]
    public string entranceID;

    // THÊM BIẾN NÀY: Kéo object cửa ở Scene mới vào đây
    [Header("Cửa tại vị trí này")]
    public RoomDoor entranceDoor;

    private void Start()
    {
        if (DataManager.Instance != null && DataManager.Instance.Data.targetSpawnID == entranceID)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = transform.position;
                Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z);
            }

            // THÊM LỆNH NÀY: Mở cửa ngay khi ném Player tới
            if (entranceDoor != null)
            {
                entranceDoor.Open();
            }

            DataManager.Instance.Data.targetSpawnID = "";
        }
    }
}