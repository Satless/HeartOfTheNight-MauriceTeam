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
        public int maxHealth = 1400;
        [Range(0.05f, 1f)] public float enrageHealthFraction = 0.5f;
        [Tooltip("Nho hon = tan cong nhanh hon khi enrage.")]
        [Range(0.2f, 1f)] public float enrageSpeedMultiplier = 0.65f;
        public int enrageExtraBarrageWaves = 1;
        public int enrageExtraLaserVolleys = 0;
        public bool enrageDisableLaserSafeGap = true;
        public int enrageExtraPillars = 0;
        [Min(1f)] public float enrageBulletSpeedMul = 1.15f;

        [Header("Targeting")]
        public float detectRange = 0f;

        [Header("Attack Loop")]
        [Tooltip("Thoi gian dung yen sau khi spawn/vao phong truoc khi bat dau tan cong.")]
        public float fightStartDelay = 1.5f;
        public float timeBetweenAttacks = 1.8f;
        [Tooltip("Thoi diem trong HeartAttack_Start de xả skill (khop Animation Event ~0.78s). Khong phu thuoc Event.")]
        public float skillCastDelay = 0.78f;
        [Tooltip("Thoi gian cho clip HeartAttack_End chay xong truoc khi tinh timeBetweenAttacks.")]
        public float attackEndDuration = 0.6f;
        public float[] attackWeights = { 1f, 1.2f, 1.1f, 0.7f };

        [Header("State 1 - Barrage (Na dan)")]
        public GameObject bulletPrefab; // Đổi từ HeartOfTheNightBullet sang GameObject
        [Tooltip("So dot ban. Moi dot nham lai Player.")]
        public int barrageBulletCount = 5;
        [Tooltip("So vien moi dot (1 giua + le 2 ben).")]
        public int barrageProjectilesPerShot = 3;
        public float barrageBetweenShots = 0.15f;
        public float barrageSpreadAngle = 16f;
        public float bulletSpeed = 11f;
        public int bulletDamage = 11;
        public float bulletLifetime = 5f;
        public float barrageCooldown = 3.2f;

        [Header("State 2 & 3 - Prefabs")]
        // Đổi từ HeartOfTheNightLaser sang GameObject
        public GameObject laserPrefab;

        // Đổi từ HeartOfTheNightTelegraph sang GameObject
        public GameObject telegraphPrefab;

        [Header("State 2 - 8 Direction Laser")]
        public int laserDirections = 8;
        public int laserVolleys = 2;
        public float laserVolleyRotationStep = 22.5f;
        public float laserAngleOffset = 0f;
        [Tooltip("Chi ap dung volley dau. Volley sau xoay lap khe thoat.")]
        public bool laserSafeGapTowardPlayer = true;
        public float laserWarnTime = 1.2f;
        public float laserFireTime = 0.35f;
        public float laserLength = 30f;
        public float laserWidth = 0.8f;
        public int laserDamage = 11;
        public float eightDirCooldown = 5.5f;

        [Header("State 3 - Fire Pillar (Cot lua)")]
        public float pillarCooldown = 4.5f;
        public float pillarChargeTime = 1.35f;
        public bool pillarFollowPlayer = true;
        public float pillarLockLeadTime = 0.5f;
        public float pillarFireTime = 0.65f;
        public float pillarWidth = 1.85f;
        public float pillarHeight = 18f;
        public int pillarDamage = 22;
        public float telegraphRadius = 1.45f;
        public float telegraphSpinStart = 120f;
        public float telegraphSpinEnd = 1440f;

        [Header("State 4 - Summon")]
        public List<SummonEntry> summons = new();
        public int maxActiveSummons = 6;
        public float summonScatterRadius = 4f;
        public float summonCooldown = 10f;
        [Tooltip("Hieu ung spawn giong luc vao phong (RoomSpawnController.spawnVfxPrefab).")]
        public GameObject summonSpawnVfxPrefab;

        [Header("Ground Detection (cho cot lua)")]
        public LayerMask groundLayer;
        public float groundProbeDistance = 30f;
    }
}