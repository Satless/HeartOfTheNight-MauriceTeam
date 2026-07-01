using System.Collections.Generic;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    /// <summary>
    /// Cau hinh cho boss "Heart Of The Night".
    /// Boss dung yen giua tran phong, doi state lien tuc va danh nhanh hon khi con duoi nua mau.
    /// </summary>
    [CreateAssetMenu(menuName = "Enemy/Heart Of The Night Stats", fileName = "HeartOfTheNightStats")]
    public class HeartOfTheNightStatsSO : ScriptableObject
    {
        [System.Serializable]
        public class SummonEntry
        {
            [Tooltip("Prefab quai duoc trieu hoi (Cultist / Brute Mage / Inquisitor / Eye Of The Night...).")]
            public GameObject prefab;

            [Tooltip("So luong sinh ra moi lan dung chieu trieu hoi.")]
            public int count = 1;
        }

        [Header("Health / Enrage")]
        public int maxHealth = 800;

        [Tooltip("Khi mau tut xuong duoi ti le nay (0.5 = nua mau) -> boss buoc vao trang thai cuong no.")]
        [Range(0.05f, 1f)] public float enrageHealthFraction = 0.5f;

        [Tooltip("Khi cuong no, moi thoi gian hoi/cho duoc nhan voi he so nay (<1 = danh nhanh hon).")]
        [Range(0.2f, 1f)] public float enrageSpeedMultiplier = 0.6f;

        [Header("Targeting")]
        [Tooltip("Khoang cach toi da de boss bat dau danh player. <=0 = luon danh.")]
        public float detectRange = 0f;

        [Header("Attack Loop")]
        [Tooltip("Thoi gian nghi giua hai chieu lien tiep (giay).")]
        public float timeBetweenAttacks = 1.25f;

        [Tooltip("Trong so chon ngau nhien tung chieu. Theo thu tu: [0]=Na dan, [1]=Laze 8 huong, [2]=Cot lua, [3]=Trieu hoi.")]
        public float[] attackWeights = { 1f, 1f, 1f, 0.6f };

        // ----- STATE 1: Na dan ve phia player -----
        [Header("State 1 - Barrage (Na dan)")]
        public HeartOfTheNightBullet bulletPrefab;
        [Tooltip("So vien ban ra trong mot loat.")]
        public int barrageBulletCount = 6;
        [Tooltip("Thoi gian giua moi vien trong loat (giay). 0 = ban dong loat.")]
        public float barrageBetweenShots = 0.18f;
        [Tooltip("Goc loe (degrees) quanh huong ngam player. 0 = ban thang.")]
        public float barrageSpreadAngle = 18f;
        public float bulletSpeed = 11f;
        public int bulletDamage = 12;
        public float bulletLifetime = 5f;
        public float barrageCooldown = 3f;

        // ----- STATE 2: Laze 8 huong -----
        [Header("State 2 - 8 Direction Laser")]
        [Tooltip("So huong ban laze (mac dinh 8).")]
        public int laserDirections = 8;
        [Tooltip("So lan ban lap lai trong mot chieu (moi lan se xoay them goc offset).")]
        public int laserVolleys = 1;
        [Tooltip("Goc xoay them cho moi lan ban lap (degrees).")]
        public float laserVolleyRotationStep = 22.5f;
        [Tooltip("Goc lech ban dau cho ca chum laze (degrees).")]
        public float laserAngleOffset = 0f;
        [Tooltip("Tu dong xoay ca chum sao cho player nam giua khe 2 tia luc canh bao (de player luon co duong ne).")]
        public bool laserSafeGapTowardPlayer = true;
        [Tooltip("Thoi gian canh bao (tia mo) truoc khi laze gay sat thuong.")]
        public float laserWarnTime = 0.6f;
        [Tooltip("Thoi gian laze gay sat thuong.")]
        public float laserFireTime = 0.35f;
        public float laserLength = 30f;
        public float laserWidth = 0.35f;
        public int laserDamage = 16;
        public float eightDirCooldown = 5f;

        // ----- STATE 3: Cot lua duoi chan player -----
        [Header("State 3 - Fire Pillar (Cot lua)")]
        [Tooltip("Cooldown rieng cua chieu cot lua (~3s theo yeu cau).")]
        public float pillarCooldown = 3f;
        [Tooltip("Thoi gian vong tron mau quay (cang ve cuoi quay cang nhanh) truoc khi lua xuat hien.")]
        public float pillarChargeTime = 1.4f;
        [Tooltip("Vong tron mau co bam theo chan player trong luc quay khong. Tat = khoa vi tri ngay tu dau.")]
        public bool pillarFollowPlayer = true;
        [Tooltip("Khoa vi tri vong tron ssom bao nhieu giay TRUOC khi lua xuat hien -> tao cua so de player ne ra.")]
        public float pillarLockLeadTime = 0.45f;
        [Tooltip("Cot lua keo dai bao lau (giay) sau khi xuat hien.")]
        public float pillarFireTime = 0.6f;
        [Tooltip("Be rong vung sat thuong cua cot lua.")]
        public float pillarWidth = 1.4f;
        [Tooltip("Chieu cao cot lua (huong thang len tu mat dat).")]
        public float pillarHeight = 18f;
        public int pillarDamage = 22;
        [Tooltip("Ban kinh vong tron mau canh bao.")]
        public float telegraphRadius = 1.1f;
        [Tooltip("Toc do quay luc bat dau (do/giay).")]
        public float telegraphSpinStart = 120f;
        [Tooltip("Toc do quay luc gan xuat hien lua (do/giay).")]
        public float telegraphSpinEnd = 1440f;

        // ----- STATE 4: Trieu hoi -----
        [Header("State 4 - Summon")]
        public List<SummonEntry> summons = new();
        [Tooltip("Tong so summon toi da con song; vuot qua se khong trieu them.")]
        public int maxActiveSummons = 6;
        [Tooltip("Ban kinh vong tron sinh quai quanh diem trieu hoi (neu khong gan spawn point rieng).")]
        public float summonScatterRadius = 4f;
        public float summonCooldown = 12f;

        [Header("Ground Detection (cho cot lua)")]
        [Tooltip("Layer dung de tim mat dat duoi chan player cho cot lua dung len.")]
        public LayerMask groundLayer;
        [Tooltip("Khoang cach raycast xuong tim mat dat tu vi tri player.")]
        public float groundProbeDistance = 30f;
    }
}
