using UnityEngine;

public class RoomDoor : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private Collider2D blockerCollider;
    [SerializeField] private Collider2D transitionTrigger;

    private bool isOpen = false;

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;

        if (anim != null) anim.SetTrigger("Open");

        // Luôn ép trạng thái vật lý
        if (blockerCollider != null) blockerCollider.enabled = false;
        if (transitionTrigger != null) transitionTrigger.enabled = true;
    }

    public void Close()
    {
        // Gỡ bỏ dòng early return để đảm bảo lệnh đóng cửa (vật lý) luôn được thi hành ở lần đầu tiên
        if (isOpen)
        {
            if (anim != null) anim.SetTrigger("Close");
        }

        isOpen = false;

        // Luôn ép trạng thái vật lý, bất chấp cửa trước đó đang mở hay đóng
        if (blockerCollider != null) blockerCollider.enabled = true;
        if (transitionTrigger != null) transitionTrigger.enabled = false;
    }
}