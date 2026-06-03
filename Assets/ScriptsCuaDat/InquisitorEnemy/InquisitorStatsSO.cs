using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [CreateAssetMenu(menuName = "Enemy/Inquisitor Stats", fileName = "InquisitorStats")]
    public class InquisitorStatsSO : ScriptableObject
    {
        [Header("Detection")]
        public float detectRange          = 12f;
        [Tooltip("Player gan hon muc nay -> chay lui (Dead Cells style).")]
        public float panicDistance        = 4f;
        public float hysteresis           = 0.5f;
        public float panicReactionDelay   = 0.15f;

        [Header("Movement")]
        public float chaseSpeed   = 4f;
        public float retreatSpeed = 5.5f;
        public float groundAccel  = 28f;

        [Header("Shooting")]
        public float fireCooldown   = 1.4f;
        public float bulletSpeed    = 10f;
        public int   bulletDamage   = 10;
        public float bulletLifetime = 4f;

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
