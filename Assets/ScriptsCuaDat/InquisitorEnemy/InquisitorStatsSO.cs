using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [CreateAssetMenu(menuName = "Enemy/Inquisitor Stats", fileName = "InquisitorStats")]
    public class InquisitorStatsSO : ScriptableObject
    {
        [Header("Detection")]
        public float detectRange        = 12f;
        [Tooltip("Dung duoi day: chi nhin/ban, khong duoi ngang nua.")]
        public float chaseStopDistance  = 6f;
        [Tooltip("Gan hon muc nay 1s -> chay lui.")]
        public float panicDistance      = 4f;
        [Tooltip("Phai xa hon panicDistance them khoang nay moi het Retreat (tranh giat).")]
        public float retreatHysteresis  = 2f;
        public float panicReactionDelay = 1f;

        [Header("Movement")]
        public float chaseSpeed   = 4f;
        public float retreatSpeed = 5.5f;
        public float groundAccel  = 28f;

        [Header("Shooting")]
        public float fireCooldown     = 1.4f;
        public float bulletSpeed      = 10f;
        public int   bulletDamage     = 10;
        public float bulletLifetime   = 4f;
        public float homingTurnRate   = 4f;
        [Tooltip("Gan player hon muc nay thi dan khong bam nua, bay thang.")]
        public float homingStopDistance = 2.5f;

        [Header("Room Buff")]
        [Tooltip("Cong them vao suc manh (0.5 = +50% damage & move speed cho quai trong phong).")]
        public float roomBuffBonus = 0.5f;
        public float buffRadius    = 18f;

        [Header("Ground / Edge Check")]
        public Vector2 groundCheckSize  = new(0.35f, 0.1f);
        public float   edgeCheckForward = 0.6f;
        public float   wallCheckHeight  = 0.5f;
        public Vector2 wallCheckSize    = new(0.2f, 0.4f);
    }
}
