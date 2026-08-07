// --- HeartOfTheNightStatsSO.cs ---
using System.Collections.Generic;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [CreateAssetMenu(menuName = "Enemy/Heart Of The Night Stats", fileName = "HeartOfTheNightStats")]
    public class HeartOfTheNightStatsSO : ScriptableObject
    {
        [System.Serializable]
        public class SummonEntry
        {
            public GameObject prefab;
            public int count = 1;
        }

        [Header("Health / Enrage")]
        public int maxHealth = 800;
        [Range(0.05f, 1f)] public float enrageHealthFraction = 0.5f;
        [Range(0.2f, 1f)] public float enrageSpeedMultiplier = 0.6f;

        [Header("Targeting")]
        public float detectRange = 0f;

        [Header("Attack Loop")]
        public float timeBetweenAttacks = 1.25f;
        [Tooltip("Thoi diem trong HeartAttack_Start de xả skill (khop Animation Event ~0.78s). Khong phu thuoc Event.")]
        public float skillCastDelay = 0.78f;
        [Tooltip("Thoi gian cho clip HeartAttack_End chay xong truoc khi tinh timeBetweenAttacks.")]
        public float attackEndDuration = 0.6f;
        public float[] attackWeights = { 1f, 1f, 1f, 0.6f };

        [Header("State 1 - Barrage (Na dan)")]
        public GameObject bulletPrefab; // Đổi từ HeartOfTheNightBullet sang GameObject
        public int barrageBulletCount = 6;
        public float barrageBetweenShots = 0.18f;
        public float barrageSpreadAngle = 18f;
        public float bulletSpeed = 11f;
        public int bulletDamage = 12;
        public float bulletLifetime = 5f;
        public float barrageCooldown = 3f;

        [Header("State 2 & 3 - Prefabs")]
        // Đổi từ HeartOfTheNightLaser sang GameObject
        public GameObject laserPrefab;

        // Đổi từ HeartOfTheNightTelegraph sang GameObject
        public GameObject telegraphPrefab;

        [Header("State 2 - 8 Direction Laser")]
        public int laserDirections = 8;
        public int laserVolleys = 1;
        public float laserVolleyRotationStep = 22.5f;
        public float laserAngleOffset = 0f;
        public bool laserSafeGapTowardPlayer = true;
        public float laserWarnTime = 0.6f;
        public float laserFireTime = 0.35f;
        public float laserLength = 30f;
        public float laserWidth = 0.35f;
        public int laserDamage = 16;
        public float eightDirCooldown = 5f;

        [Header("State 3 - Fire Pillar (Cot lua)")]
        public float pillarCooldown = 3f;
        public float pillarChargeTime = 1.4f;
        public bool pillarFollowPlayer = true;
        public float pillarLockLeadTime = 0.45f;
        public float pillarFireTime = 0.6f;
        public float pillarWidth = 1.4f;
        public float pillarHeight = 18f;
        public int pillarDamage = 22;
        public float telegraphRadius = 1.1f;
        public float telegraphSpinStart = 120f;
        public float telegraphSpinEnd = 1440f;

        [Header("State 4 - Summon")]
        public List<SummonEntry> summons = new();
        public int maxActiveSummons = 6;
        public float summonScatterRadius = 4f;
        public float summonCooldown = 12f;
        [Tooltip("Hieu ung spawn giong luc vao phong (RoomSpawnController.spawnVfxPrefab).")]
        public GameObject summonSpawnVfxPrefab;

        [Header("Ground Detection (cho cot lua)")]
        public LayerMask groundLayer;
        public float groundProbeDistance = 30f;
    }
}