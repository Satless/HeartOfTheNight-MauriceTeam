using UnityEngine;

[CreateAssetMenu(fileName = "NewItemData", menuName = "Data/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Item Information")]
    public string itemName;
    
    [Tooltip("Giá trị của vật phẩm (ví dụ: lượng máu hồi, hoặc lượng vàng cộng thêm)")]
    public int value;

    [Header("Magnet Settings")]
    [Tooltip("Khoảng cách đủ gần để người chơi thực sự nhặt được vật phẩm và biến mất.")]
    public float collectDistance = 0.5f;
}
