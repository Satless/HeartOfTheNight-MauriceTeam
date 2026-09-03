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
        public float chaseSpeed   = 4.5f;
        public float retreatSpeed = 6f;
        public float groundAccel  = 30f;

        [Header("Shooting")]
        public float fireCooldown     = 1.6f;
        public float bulletSpeed      = 11f;
        public int   bulletDamage     = 10;
        public float bulletLifetime   = 4f;
        [Tooltip("Độ bám (độ/giây). ~85 bám kịp đi bộ (~11), không kịp lướt (35 trong 0.15s). 0 = bay thẳng.")]
        public float homingTurnRate   = 85f;
        [Tooltip("Gần hơn mức này thì khóa bay thẳng — cửa sổ để lướt né.")]
        public float homingStopDistance = 3.2f;
        [Tooltip("Vận tốc player vượt mức này (lướt) thì đạn khóa thẳng. Walk ~11, Dash 35.")]
        public float homingLockPlayerSpeed = 22f;

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
