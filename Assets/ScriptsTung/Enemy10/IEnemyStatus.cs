namespace HeartOfTheNight.Enemy
{
    /// <summary>
    /// Interface chung cho tất cả các loại Enemy để chuẩn hóa việc kiểm tra trạng thái (vd: Đã chết chưa).
    /// Giúp EnemyLootDrop và các hệ thống khác không cần dùng Reflection (Zero-GC).
    /// </summary>
    public interface IEnemyStatus
    {
        /// <summary>
        /// Trả về true nếu quái vật đã chết.
        /// </summary>
        bool IsDead { get; }
    }
}
