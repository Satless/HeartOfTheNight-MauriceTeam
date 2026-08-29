using UnityEngine;

public class AntiHeal : MonoBehaviour
{
    public float thoiGianConLai = 0f;

    void Update()
    {
        if (thoiGianConLai > 0)
        {
            thoiGianConLai -= Time.deltaTime;
        }
        else
        {
            // Hết 6 giây thì bùa tự rớt ra khỏi người Player
            Destroy(this);
        }
    }
}