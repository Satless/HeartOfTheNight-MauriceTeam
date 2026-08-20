using System.Reflection; // 🔥 THƯ VIỆN NỘI SOI CODE CỰC MẠNH
using UnityEngine;
using UnityEngine.UI;
using HeartOfTheNight.Common;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("Cài đặt UI")]
    public Image fillImage;
    public Canvas canvas;
    private Quaternion startRotation;

    private Component enemyScript;
    private FieldInfo currentHealthField;
    private FieldInfo maxHealthField;

    void Start()
    {
        if (canvas != null) startRotation = canvas.transform.rotation;

        // 1. Tự động quét lên trên để tìm xem con quái xài file code tên gì
        enemyScript = GetComponentInParent<IDamageable>() as Component;

        if (enemyScript != null)
        {
            // 2. DÙNG NỘI SOI: Xuyên thủng private để tìm biến máu
            System.Type type = enemyScript.GetType();

            // TÌM ĐÚNG 2 BIẾN TÊN LÀ "currentHealth" VÀ "maxHealth"
            currentHealthField = type.GetField("currentHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            maxHealthField = type.GetField("maxHealth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (currentHealthField == null) Debug.LogWarning("Không tìm thấy biến currentHealth trong " + enemyScript.name);
        }
    }

    void Update()
    {
        // 3. Liên tục đọc trộm số máu hiện tại của quái để cập nhật lên UI
        if (enemyScript != null && currentHealthField != null && maxHealthField != null)
        {
            // Móc số máu thực tế ra
            int curHp = (int)currentHealthField.GetValue(enemyScript);
            int maxHp = (int)maxHealthField.GetValue(enemyScript);

            // Cập nhật thanh đỏ
            fillImage.fillAmount = (float)curHp / maxHp;

            // Nếu máu bằng 0 thì tự giấu cái thanh máu đi cho đỡ vướng
            if (canvas != null) canvas.gameObject.SetActive(curHp > 0);
        }
    }

    void LateUpdate()
    {
        if (canvas != null)
        {
            // 1. Giữ cho thanh máu không bị xoay nghiêng ngả
            canvas.transform.rotation = startRotation;

            // 2. ÉP THANH MÁU KHÔNG BỊ "SOI GƯƠNG"
            // Lấy kích thước hiện tại của thanh máu
            Vector3 fixScale = canvas.transform.localScale;

            // Kiểm tra xem thằng cha (con quái) có đang bị lật mặt (Scale X âm) không?
            if (transform.parent != null && transform.parent.localScale.x < 0)
            {
                // Thằng cha âm, thì ép thằng con cũng âm để trừ với trừ thành cộng (chuẩn chiều)
                fixScale.x = -Mathf.Abs(fixScale.x);
            }
            else
            {
                // Thằng cha dương (bình thường), thì ép thằng con dương
                fixScale.x = Mathf.Abs(fixScale.x);
            }

            // Áp dụng lại kích thước đã sửa vào Canvas
            canvas.transform.localScale = fixScale;
        }
    }
}