using UnityEngine;

public class RoomDoor : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private Collider2D blockerCollider;
    [SerializeField] private Collider2D transitionTrigger;

    private bool isOpen = false;

    public void Open()
    {
        if (isOpen) return; // Chặn chạy lại animation nếu đã mở
        isOpen = true;

        anim.SetTrigger("Open");
        if (blockerCollider != null) blockerCollider.enabled = false;
        if (transitionTrigger != null) transitionTrigger.enabled = true;
    }

    public void Close()
    {
        if (!isOpen) return; // Chặn chạy lại animation nếu đã đóng
        isOpen = false;

        anim.SetTrigger("Close");
        if (blockerCollider != null) blockerCollider.enabled = true;
        if (transitionTrigger != null) transitionTrigger.enabled = false;
    }
}