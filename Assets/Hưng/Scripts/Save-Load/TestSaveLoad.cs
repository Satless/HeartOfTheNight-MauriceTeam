using UnityEngine;

namespace HeartOfTheNight.Hung
{
    public class TestSaveLoad : MonoBehaviour
    {
        private void Update()
        {
            // Nhấn F5 để Lưu vị trí
            if (Input.GetKeyDown(KeyCode.F5))
            {
                if (DataManager.Instance == null)
                {
                    Debug.LogError("[TestSaveLoad] Không tìm thấy DataManager. Bạn đã kéo DataManager.cs vào Scene chưa?");
                    return;
                }

                // Cập nhật máu mới nhất từ PlayerHealth vào DataManager TRƯỚC KHI LƯU
                var hp = GetComponent<HeartOfTheNight.Player.PlayerHealth>();
                if (hp != null)
                {
                    DataManager.Instance.Data.playerHealth = hp.GetCurrentHealth();
                }
                else
                {
                    Debug.LogWarning("[TestSaveLoad - LƯU] Không tìm thấy script PlayerHealth trên Object này! Bạn có chắc đã gắn TestSaveLoad vào đúng nhân vật Player chưa?");
                }

                DataManager.Instance.Data.playerPosition = transform.position;
                DataManager.Instance.SaveGame();
                Debug.Log($"[TestSaveLoad] Đã LƯU vị trí Player tại: {transform.position}, Máu: {DataManager.Instance.Data.playerHealth}");
            }

            // Nhấn F9 để Tải lại vị trí
            if (Input.GetKeyDown(KeyCode.F9))
            {
                if (DataManager.Instance == null)
                {
                    Debug.LogError("[TestSaveLoad] Không tìm thấy DataManager. Bạn đã kéo DataManager.cs vào Scene chưa?");
                    return;
                }

                // Gọi LoadGame và truyền một hàm Callback (Action) vào
                // Hàm này chỉ chạy sau khi DataManager tải xong dữ liệu từ Firebase hoặc Local
                DataManager.Instance.LoadGame(() => 
                {
                    // Nếu là game mới (tọa độ 0,0,0) thì bỏ qua để tránh rớt khỏi map
                    if (DataManager.Instance.Data.playerPosition != Vector3.zero)
                    {
                        transform.position = DataManager.Instance.Data.playerPosition;
                        Debug.Log($"[TestSaveLoad] Đã TẢI vị trí Player về: {transform.position}");
                    }
                    else
                    {
                        Debug.Log("[TestSaveLoad] Không tìm thấy dữ liệu vị trí cũ (có thể là game mới), không dịch chuyển.");
                    }

                    // Ép file PlayerHealth cập nhật lại thanh máu ngay lập tức
                    var hp = GetComponent<HeartOfTheNight.Player.PlayerHealth>();
                    if (hp != null) 
                    {
                        hp.SyncHealthFromSave();
                        Debug.Log($"[TestSaveLoad - LOAD] Đã ép PlayerHealth cập nhật máu thành: {hp.GetCurrentHealth()}");
                    }
                    else
                    {
                        Debug.LogWarning("[TestSaveLoad - LOAD] Không tìm thấy script PlayerHealth!");
                    }
                });
            }
        }
    }
}
