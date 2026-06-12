using UnityEngine;

public class AntiHeal : MonoBehaviour
{
    public float thoiGianConLai = 0f;

    void Update()
    {
        if (thoiGianConLai > 0)
        {
            thoiGianConLai -= Time.deltaTime; // Trừ dần thời gian
        }
        else
        {
            // Hết 6 giây, tự động hủy lá bùa khỏi người Player
            Destroy(this);
        }
    }
}