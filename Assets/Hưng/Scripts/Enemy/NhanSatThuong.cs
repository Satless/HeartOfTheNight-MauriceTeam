using UnityEngine;

/// <summary>
/// Interface định nghĩa cho mọi đối tượng có thể nhận sát thương (Quái, Boss, Vật thể phá hủy được).
/// Tuân thủ nguyên tắc "Dependency Inversion": Đạn không cần biết nó đang bắn trúng con quái nào,
/// chỉ cần biết đối tượng đó có cài interface "NhanSatThuong".
/// </summary>
public interface NhanSatThuong
{
    /// <summary>
    /// Hàm xử lý khi bị nhận sát thương.
    /// </summary>
    /// <param name="damage">Lượng sát thương nhận vào.</param>
    void TakeDamage(int damage);
}
