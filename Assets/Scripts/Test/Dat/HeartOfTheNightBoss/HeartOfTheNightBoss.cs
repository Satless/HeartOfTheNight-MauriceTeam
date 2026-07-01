using System.Collections;
using System.Collections.Generic;
using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    /// <summary>
    /// Boss "Heart Of The Night".
    /// - Dung yen giua tran cua can phong.
    /// - Duoi nua mau (enrage) thi tan cong nhanh hon.
    /// State 1: Na dan ve phia player.
    /// State 2: Ban laze 8 huong.
    /// State 3: Cot lua thang dung duoi chan player (cooldown ~3s), co vong tron mau quay nhanh dan canh bao truoc.
    /// State 4: Trieu hoi Cultist / Brute Mage / Inquisitor / Eye Of The Night.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class HeartOfTheNightBoss : MonoBehaviour, IDamageable
    {
        private enum Attack { Barrage = 0, EightDirLaser = 1, FirePillar = 2, Summon = 3 }

        [Header("Data")]
        [SerializeField] private HeartOfTheNightStatsSO stats;

        [Header("References")]
        [SerializeField] private Transform player;
        [Tooltip("Diem ban dan/laze. De trong = dung tam boss.")]
        [SerializeField] private Transform firePoint;
        [Tooltip("Cac diem dat san de trieu hoi quai. De trong = sinh quanh player tren mat dat.")]
        [SerializeField] private Transform[] summonPoints;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;
        [SerializeField] private bool showStateColor = true;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color enrageColor = new(1f, 0.5f, 0.5f, 1f);

        private Rigidbody2D rb;
        private Collider2D col;
        private SpriteRenderer sprite;
        private int health;
        private bool enraged;
        private bool dead;

        private readonly Dictionary<Attack, float> nextReadyTime = new();
        private readonly List<GameObject> activeSummons = new();

        private float SpeedMul => enraged ? stats.enrageSpeedMultiplier : 1f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();

            ConfigureBody();

            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                if (found != null) player = found.transform;
            }

            ValidateSetup();
        }

        private void OnEnable()
        {
            if (stats == null) return;
            health = stats.maxHealth;
            enraged = false;
            dead = false;
            ApplyEnrageVisual();
            StopAllCoroutines();
            StartCoroutine(AttackLoop());
        }

        private void ConfigureBody()
        {
            // Boss dung yen giua tran -> khong roi, khong bi day.
            rb.gravityScale = 0f;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        private void ValidateSetup()
        {
            if (stats == null) Debug.LogError($"[{name}] HeartOfTheNightStatsSO chua duoc gan.", this);
            if (player == null) Debug.LogError($"[{name}] Khong tim thay Player (tag Player).", this);
            if (stats != null && stats.bulletPrefab == null)
                Debug.LogWarning($"[{name}] Bullet prefab chua duoc gan - State 1 (na dan) se khong ban.", this);
            if (stats != null && stats.groundLayer.value == 0)
                Debug.LogWarning($"[{name}] Ground Layer chua tick - cot lua se dung vi tri player thay vi mat dat.", this);
        }

        private IEnumerator AttackLoop()
        {
            // Cho mot nhip dau de moi thu init xong.
            yield return null;

            while (!dead && stats != null)
            {
                if (player == null || !PlayerInRange())
                {
                    yield return new WaitForSeconds(0.25f);
                    continue;
                }

                Attack? choice = PickAttack();
                if (choice == null)
                {
                    yield return new WaitForSeconds(0.2f);
                    continue;
                }

                yield return StartCoroutine(Execute(choice.Value));
                yield return new WaitForSeconds(stats.timeBetweenAttacks * SpeedMul);
            }
        }

        private bool PlayerInRange()
        {
            if (stats.detectRange <= 0f) return true;
            return Vector2.Distance(transform.position, player.position) <= stats.detectRange;
        }

        private Attack? PickAttack()
        {
            float now = Time.time;
            float[] weights = stats.attackWeights;

            var ready = new List<Attack>();
            var readyWeights = new List<float>();
            float total = 0f;

            foreach (Attack a in System.Enum.GetValues(typeof(Attack)))
            {
                int idx = (int)a;
                if (nextReadyTime.TryGetValue(a, out float t) && now < t) continue;
                if (a == Attack.Summon && !HasSummonsConfigured()) continue;
                if (a == Attack.Barrage && stats.bulletPrefab == null) continue;

                float w = (weights != null && idx < weights.Length) ? Mathf.Max(0f, weights[idx]) : 1f;
                if (w <= 0f) continue;

                ready.Add(a);
                readyWeights.Add(w);
                total += w;
            }

            if (ready.Count == 0 || total <= 0f) return null;

            float roll = Random.value * total;
            for (int i = 0; i < ready.Count; i++)
            {
                roll -= readyWeights[i];
                if (roll <= 0f) return ready[i];
            }
            return ready[ready.Count - 1];
        }

        private bool HasSummonsConfigured()
        {
            if (stats.summons == null) return false;
            for (int i = 0; i < stats.summons.Count; i++)
                if (stats.summons[i] != null && stats.summons[i].prefab != null) return true;
            return false;
        }

        private IEnumerator Execute(Attack attack)
        {
            FacePlayer();
            if (debugLogs) Debug.Log($"[{name}] Attack: {attack} (enraged={enraged})", this);

            switch (attack)
            {
                case Attack.Barrage:
                    yield return StartCoroutine(DoBarrage());
                    SetCooldown(attack, stats.barrageCooldown);
                    break;
                case Attack.EightDirLaser:
                    yield return StartCoroutine(DoEightDirLaser());
                    SetCooldown(attack, stats.eightDirCooldown);
                    break;
                case Attack.FirePillar:
                    yield return StartCoroutine(DoFirePillar());
                    SetCooldown(attack, stats.pillarCooldown);
                    break;
                case Attack.Summon:
                    DoSummon();
                    SetCooldown(attack, stats.summonCooldown);
                    break;
            }
        }

        private void SetCooldown(Attack attack, float baseCooldown)
        {
            nextReadyTime[attack] = Time.time + baseCooldown * SpeedMul;
        }

        // ---------------- STATE 1: Na dan ----------------
        private IEnumerator DoBarrage()
        {
            int count = Mathf.Max(1, stats.barrageBulletCount);
            float spread = stats.barrageSpreadAngle;
            float interval = stats.barrageBetweenShots * SpeedMul;

            for (int i = 0; i < count; i++)
            {
                if (player == null) yield break;

                Vector2 origin = FireOrigin;
                Vector2 baseDir = ((Vector2)player.position - origin).normalized;

                float angle = count > 1
                    ? Mathf.Lerp(-spread, spread, i / (float)(count - 1))
                    : 0f;
                Vector2 dir = Rotate(baseDir, angle);

                var bullet = Instantiate(stats.bulletPrefab, origin, Quaternion.identity);
                bullet.Launch(dir, stats.bulletSpeed, stats.bulletDamage, stats.bulletLifetime);

                if (interval > 0f && i < count - 1) yield return new WaitForSeconds(interval);
            }
        }

        // ---------------- STATE 2: Laze 8 huong ----------------
        private IEnumerator DoEightDirLaser()
        {
            int dirs = Mathf.Max(2, stats.laserDirections);
            int volleys = Mathf.Max(1, stats.laserVolleys);
            float warn = stats.laserWarnTime * SpeedMul;
            float fire = stats.laserFireTime;

            float gap = 360f / dirs;

            for (int v = 0; v < volleys; v++)
            {
                float baseOffset = stats.laserAngleOffset + v * stats.laserVolleyRotationStep;
                Vector2 origin = FireOrigin;

                // Xoay ca chum sao cho player nam chinh giua khe 2 tia -> luon co duong ne.
                if (stats.laserSafeGapTowardPlayer && player != null)
                {
                    Vector2 toPlayer = (Vector2)player.position - origin;
                    if (toPlayer.sqrMagnitude > 0.0001f)
                    {
                        float angleToPlayer = Mathf.Atan2(toPlayer.y, toPlayer.x) * Mathf.Rad2Deg;
                        float rel = Mathf.Repeat(angleToPlayer - baseOffset, gap);
                        baseOffset += (gap * 0.5f) - rel;
                    }
                }

                for (int i = 0; i < dirs; i++)
                {
                    float deg = baseOffset + gap * i;
                    Vector2 dir = Rotate(Vector2.right, deg);
                    SpawnLaser(origin, dir, stats.laserLength, stats.laserWidth, stats.laserDamage, warn, fire);
                }

                yield return new WaitForSeconds(warn + fire + 0.1f);
            }
        }

        // ---------------- STATE 3: Cot lua duoi chan player ----------------
        private IEnumerator DoFirePillar()
        {
            if (player == null) yield break;

            float charge = stats.pillarChargeTime * SpeedMul;
            // Ngung bam theo player o cuoi qua trinh charge -> player co cua so de chay ra khoi vung lua.
            float lockLead = Mathf.Clamp(stats.pillarLockLeadTime, 0f, charge * 0.9f);
            float followUntil = charge - lockLead;

            Vector2 spot = player.position;
            var telegraphGo = new GameObject("HotN_Telegraph");
            telegraphGo.transform.position = GroundUnder(spot);
            var telegraph = telegraphGo.AddComponent<HeartOfTheNightTelegraph>();
            telegraph.Configure(stats.telegraphRadius, charge, stats.telegraphSpinStart, stats.telegraphSpinEnd);

            float timer = 0f;
            while (timer < charge)
            {
                timer += Time.deltaTime;
                if (telegraphGo == null) break;

                // Chi bam theo player trong giai doan dau; sau followUntil thi khoa vi tri (vong tron dung yen, quay nhanh nhat).
                if (stats.pillarFollowPlayer && player != null && timer < followUntil)
                    telegraphGo.transform.position = GroundUnder(player.position);

                yield return null;
            }

            Vector2 pillarBase = telegraphGo != null
                ? (Vector2)telegraphGo.transform.position
                : GroundUnder(spot);

            if (telegraphGo != null) Destroy(telegraphGo);

            SpawnLaser(pillarBase, Vector2.up, stats.pillarHeight, stats.pillarWidth,
                       stats.pillarDamage, 0f, stats.pillarFireTime, 0.1f);

            yield return new WaitForSeconds(stats.pillarFireTime);
        }

        // ---------------- STATE 4: Trieu hoi ----------------
        private void DoSummon()
        {
            PruneSummons();
            if (stats.summons == null) return;

            int slots = stats.maxActiveSummons - activeSummons.Count;
            if (slots <= 0)
            {
                if (debugLogs) Debug.Log($"[{name}] Summon bo qua - da dat toi da ({stats.maxActiveSummons}).", this);
                return;
            }

            int pointIndex = 0;
            foreach (var entry in stats.summons)
            {
                if (entry == null || entry.prefab == null) continue;

                for (int c = 0; c < Mathf.Max(1, entry.count); c++)
                {
                    if (slots <= 0) return;

                    Vector3 pos = NextSummonPosition(ref pointIndex);
                    var go = Instantiate(entry.prefab, pos, Quaternion.identity);
                    activeSummons.Add(go);
                    slots--;
                }
            }
        }

        private Vector3 NextSummonPosition(ref int pointIndex)
        {
            if (summonPoints != null && summonPoints.Length > 0)
            {
                for (int tries = 0; tries < summonPoints.Length; tries++)
                {
                    var p = summonPoints[pointIndex % summonPoints.Length];
                    pointIndex++;
                    if (p != null) return p.position;
                }
            }

            float centerX = player != null ? player.position.x : transform.position.x;
            float offsetX = Random.Range(-stats.summonScatterRadius, stats.summonScatterRadius);
            return GroundUnder(new Vector2(centerX + offsetX, transform.position.y));
        }

        private void PruneSummons()
        {
            for (int i = activeSummons.Count - 1; i >= 0; i--)
                if (activeSummons[i] == null) activeSummons.RemoveAt(i);
        }

        // ---------------- Helpers ----------------
        private void SpawnLaser(Vector2 origin, Vector2 dir, float length, float width,
                                int damage, float warn, float fire, float tick = 0.12f)
        {
            var go = new GameObject("HotN_Laser");
            var laser = go.AddComponent<HeartOfTheNightLaser>();
            laser.Configure(origin, dir, length, width, damage, warn, fire, tick);
        }

        private Vector2 GroundUnder(Vector2 from)
        {
            if (stats.groundLayer.value == 0) return from;

            Vector2 rayStart = from + Vector2.up * 0.5f;
            RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, stats.groundProbeDistance, stats.groundLayer);
            return hit.collider != null ? hit.point : from;
        }

        private Vector2 FireOrigin => firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float rad = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        private void FacePlayer()
        {
            if (sprite == null || player == null) return;
            sprite.flipX = player.position.x < transform.position.x;
        }

        private void CheckEnrage()
        {
            if (enraged || stats == null) return;
            if (health <= stats.maxHealth * stats.enrageHealthFraction)
            {
                enraged = true;
                ApplyEnrageVisual();
                if (debugLogs) Debug.Log($"[{name}] ENRAGE! Tan cong nhanh hon.", this);
            }
        }

        private void ApplyEnrageVisual()
        {
            if (!showStateColor || sprite == null) return;
            sprite.color = enraged ? enrageColor : normalColor;
        }

        public void TakeDamage(int amount)
        {
            if (dead) return;

            health -= amount;
            CheckEnrage();

            if (health <= 0)
            {
                dead = true;
                StopAllCoroutines();
                Destroy(gameObject);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (stats != null && stats.detectRange > 0f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(transform.position, stats.detectRange);
            }

            if (summonPoints != null)
            {
                Gizmos.color = Color.magenta;
                foreach (var p in summonPoints)
                {
                    if (p == null) continue;
                    Gizmos.DrawWireSphere(p.position, 0.4f);
                    Gizmos.DrawLine(transform.position, p.position);
                }
            }
        }
    }
}
