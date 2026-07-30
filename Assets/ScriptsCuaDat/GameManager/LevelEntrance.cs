using UnityEngine;

public class LevelEntrance : MonoBehaviour
{
    [Header("ID Cửa (Phải khớp với spawnIDInNextScene ở Scene trước)")]
    public string entranceID;

    private void Start()
    {
        // Kiểm tra xem DataManager có mang theo ID khớp với cửa này không
        if (DataManager.Instance != null && DataManager.Instance.Data.targetSpawnID == entranceID)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                // Dịch chuyển Player đến đúng vị trí của object này
                player.transform.position = transform.position;

                // Dịch chuyển luôn Camera theo Player (nếu chưa dùng Cinemachine)
                Camera.main.transform.position = new Vector3(transform.position.x, transform.position.y, Camera.main.transform.position.z);
            }

            // Xóa ID đi để tránh lỗi nếu load lại màn
            DataManager.Instance.Data.targetSpawnID = "";
        }
    }
}