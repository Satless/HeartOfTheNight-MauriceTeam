using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [CreateAssetMenu(menuName = "Enemy/Cultist Stats", fileName = "CultistStats")]
    public class CultistStatsSO : ScriptableObject
    {
        [Header("Detection")]
        public float detectRange      = 12f;
        public float minSafeDistance  = 5f;
        public float hysteresis       = 0.5f;

        [Header("Movement")]
        public float moveSpeed        = 3.5f;
        public float groundAccel      = 25f;

        [Header("Shooting")]
        public float fireCooldown     = 2f;
        public float bulletSpeed      = 9f;
        public int   bulletDamage     = 10;
        public float bulletLifetime   = 4f;

        [Header("Ground / Edge Check (offsets are RELATIVE to GroundCheck Transform)")]
        public Vector2 groundCheckSize  = new(0.35f, 0.1f);
        public float   edgeCheckForward = 0.6f;
        public float   wallCheckHeight  = 0.5f;
        public Vector2 wallCheckSize    = new(0.2f, 0.4f);
    }
}
