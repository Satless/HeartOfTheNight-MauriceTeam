using UnityEngine;

/// <summary>
/// Interface định nghĩa cho mọi đối tượng có thể bị đẩy lùi (Quái, Boss, Vật thể...).
/// Tuân thủ nguyên tắc "Dependency Inversion" giống NhanSatThuong:
/// Đạn không cần biết nó đang bắn trúng cái gì, chỉ cần biết đối tượng đó có cài "INhanKnockback".
///
/// Cách dùng cho team:
/// - Muốn plug-and-play → Gắn Component "KnockbackReceiver" lên quái (tự xử lý hết).
/// - Muốn custom (Boss chống knockback, AI phức tạp) → Tự implement interface này.
/// </summary>
public interface INhanKnockback
{
    /// <summary>
    /// Áp lực đẩy lùi lên đối tượng.
    /// </summary>
    /// <param name="direction">Hướng đẩy (đã normalized).</param>
    /// <param name="force">Lực đẩy (đơn vị Unity). 0 = không đẩy.</param>
    void ApplyKnockback(Vector2 direction, float force);
}
