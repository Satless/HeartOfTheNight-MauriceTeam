using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [CreateAssetMenu(menuName = "Enemy/Brute Mage Stats", fileName = "BruteMageStats")]
    public class BruteMageStatsSO : ScriptableObject
    {
        [Header("Detection / State")]
        public float detectRange = 14f;
        public float kiteMinDistance = 3.5f;
        public float kiteExitDistance = 5f;
        public float aggressiveMinDuration = 0.6f;
        public float kiteDuration = 1.75f;

        [Header("Movement")]
        public float chaseSpeed = 3.25f;
        public float retreatSpeed = 4f;
        public float groundAccel = 28f;

        [Header("Melee")]
        public float meleeRange = 1.4f;
        public float meleeCooldown = 1.2f;
        public int meleeDamage = 15;

        [Header("Shooting")]
        public float fireCooldown = 1.8f;
        public float bulletSpeed = 9f;
        public int bulletDamage = 10;
        public float bulletLifetime = 4f;

        [Header("Teleport To Player Platform")]
        public bool canTeleportToPlayerPlatform = true;
        public float teleportCooldown = 3f;
        public float teleportSideOffset = 1.25f;
        public float teleportProbeHeight = 3f;
        public float teleportYPadding = 0.05f;

        [Header("Ground / Wall Check (relative to GroundCheck)")]
        public Vector2 groundCheckSize = new(0.35f, 0.1f);
        public float edgeCheckForward = 0.6f;
        public float wallCheckHeight = 0.5f;
        public Vector2 wallCheckSize = new(0.25f, 0.45f);
    }
}
