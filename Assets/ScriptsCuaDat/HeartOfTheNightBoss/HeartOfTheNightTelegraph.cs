using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    public class HeartOfTheNightTelegraph : MonoBehaviour
    {
        public void Configure(float ringRadius, float charge, float spinStartSpeed, float spinEndSpeed)
        {
            // Chỉ thiết lập kích thước (Scale) duy nhất 1 lần lúc vừa sinh ra dựa trên thông số SO.
            // Toàn bộ hiệu ứng hình ảnh tiếp theo sẽ do Animation của bạn tự chạy.
            transform.localScale = Vector3.one * Mathf.Max(0.1f, ringRadius);
        }
    }
}