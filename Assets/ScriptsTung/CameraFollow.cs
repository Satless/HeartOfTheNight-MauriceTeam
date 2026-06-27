using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Mục tiêu theo dõi")]
    public Transform target; // Kéo Player vào đây

    [Header("Cài đặt Camera")]
    [Tooltip("Thời gian trễ để camera đuổi kịp (Càng lớn càng mượt nhưng chậm)")]
    public float smoothTime = 0.15f;
    public Vector3 offset = new Vector3(0f, 1f, -10f); // -10 ở trục Z là BẮT BUỘC để nhìn thấy game 2D

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;
        //
        // Tính toán vị trí đích đến
        Vector3 targetPosition = target.position + offset;

        // Hàm SmoothDamp giúp camera lướt đi cực kỳ êm ái, không bị giật cục
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}