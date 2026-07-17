using UnityEngine;

/// <summary>
/// Data-Driven: File cấu hình chỉ số riêng cho Enemy (tương tự PlayerData).
/// Không hardcode máu hay tốc độ trong code logic.
/// </summary>
[CreateAssetMenu(fileName = "NewMauTestEnemyData", menuName = "Data/Enemy/Mau Test Enemy Data")]
public class MauTestEnemyData : ScriptableObject
{
    [Header("Health Stats")]
    [Tooltip("Lượng máu tối đa của quái")]
    public int maxHealth = 100;

    [Header("Visual Feedback")]
    [Tooltip("Màu hiển thị khi bị trúng đạn")]
    public Color damageColor = Color.red;
    
    [Tooltip("Thời gian nháy đỏ")]
    public float damageFlashDuration = 0.1f;
}
