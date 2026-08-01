using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AutoDestroyVFX : MonoBehaviour
{
    [Tooltip("Thời gian bù trừ (giây). Nếu animation bị cắt quá sớm, hãy tăng số này lên một chút (vd: 0.1)")]
    [SerializeField] private float delayBuffer = 0f;

    private void Start()
    {
        Animator anim = GetComponent<Animator>();

        // Lấy thông tin về Animation đang chạy ở Layer 0
        AnimatorStateInfo stateInfo = anim.GetCurrentAnimatorStateInfo(0);

        // Tổng thời gian sống = Độ dài của animation + thời gian bù trừ
        float lifeTime = stateInfo.length + delayBuffer;

        // Lên lịch tự hủy GameObject sau khoảng thời gian lifeTime
        Destroy(gameObject, lifeTime);
    }
}