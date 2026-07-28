using UnityEngine;

public class RoomDoor : MonoBehaviour
{
    [SerializeField] private Animator anim;

    [Tooltip("Collider vật lý chắn không cho đi qua khi đóng")]
    [SerializeField] private Collider2D blockerCollider;

    [Tooltip("Collider trigger để chuyển phòng khi đi vào (chỉ bật khi cửa mở)")]
    [SerializeField] private Collider2D transitionTrigger;

    public void Open()
    {
        anim.SetTrigger("Open");
        if (blockerCollider != null) blockerCollider.enabled = false;
        if (transitionTrigger != null) transitionTrigger.enabled = true;
    }

    public void Close()
    {
        anim.SetTrigger("Close");
        if (blockerCollider != null) blockerCollider.enabled = true;
        if (transitionTrigger != null) transitionTrigger.enabled = false;
    }
}