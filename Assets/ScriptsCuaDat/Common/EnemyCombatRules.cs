using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    public static class EnemyCombatRules
    {
        public static bool IsEnemyCollider(Collider2D other)
        {
            if (other == null) return false;
            // Chỉ kiểm tra bằng Tag - Tốc độ nhanh nhất
            return other.CompareTag("Enemy") || other.transform.root.CompareTag("Enemy");
        }

        public static bool IsPlayerCollider(Collider2D other)
        {
            if (other == null) return false;
            return other.CompareTag("Player") || other.transform.root.CompareTag("Player");
        }

        public static bool TryGetPlayerDamageable(Collider2D other, out IDamageable damageable)
        {
            damageable = null;
            if (!IsPlayerCollider(other)) return false;

            // Chỉ gọi hàm tốn kém GetComponent khi chắc chắn đây là Player
            damageable = other.GetComponentInParent<IDamageable>();
            return damageable != null;
        }
    }
}