using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    /// <summary>
    /// Quy tac friendly-fire: dan va don cua quai chi gay damage len Player.
    /// </summary>
    public static class EnemyCombatRules
    {
        public static bool IsEnemyCollider(Collider2D other)
        {
            if (other == null) return false;
            if (other.CompareTag("Enemy")) return true;
            if (other.transform.root.CompareTag("Enemy")) return true;

            return other.GetComponentInParent<Cultist>() != null
                || other.GetComponentInParent<Inquisitor>() != null
                || other.GetComponentInParent<BruteMage>() != null;
        }

        public static bool IsPlayerCollider(Collider2D other)
        {
            if (other == null) return false;
            if (other.CompareTag("Player")) return true;
            return other.transform.root.CompareTag("Player");
        }

        public static bool TryGetPlayerDamageable(Collider2D other, out IDamageable damageable)
        {
            damageable = null;
            if (IsEnemyCollider(other)) return false;
            if (!IsPlayerCollider(other)) return false;

            damageable = other.GetComponentInParent<IDamageable>();
            return damageable != null;
        }
    }
}
