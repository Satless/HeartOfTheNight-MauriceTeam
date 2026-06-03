using System.Collections.Generic;
using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    /// <summary>
    /// Dead Cells style: duoi player de ban, player ap sat thi chay lui. Dan bay thang.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Inquisitor : MonoBehaviour, IDamageable
    {
        private enum State { Chase, Retreat }

        [Header("Data")]
        [SerializeField] private InquisitorStatsSO stats;

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private InquisitorBullet bulletPrefab;

        [Header("Layers")]
        [SerializeField] private LayerMask groundLayer;

        [Header("Health")]
        [SerializeField] private int maxHealth = 35;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private Rigidbody2D rb;
        private SpriteRenderer sprite;
        private State current = State.Chase;
        private float fireTimer;
        private float panicTimer;
        private int   health;
        private int   facing = 1;
        private readonly List<EnemyStrengthModifier> buffedAllies = new();

        private void Awake()
        {
            rb     = GetComponent<Rigidbody2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            health = maxHealth;

            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                if (found != null) player = found.transform;
            }

            ValidateSetup();
        }

        private void OnDestroy()
        {
            ClearAllRoomBuffs();
        }

        private void ValidateSetup()
        {
            if (stats == null)
                Debug.LogError($"[{name}] InquisitorStatsSO chua duoc gan trong Inspector.", this);
            if (player == null)
                Debug.LogError($"[{name}] Khong tim thay Player. Gan truc tiep hoac dat Tag 'Player'.", this);
            if (groundLayer.value == 0)
                Debug.LogError($"[{name}] Ground Layer chua duoc tick.", this);
            if (bulletPrefab == null)
                Debug.LogWarning($"[{name}] Bullet Prefab chua duoc gan.", this);
            if (rb.constraints.HasFlag(RigidbodyConstraints2D.FreezePositionX))
                Debug.LogError($"[{name}] Rigidbody2D dang khoa FreezePositionX.", this);
        }

        private void Update()
        {
            if (player == null || stats == null) return;

            float dx       = player.position.x - transform.position.x;
            float distance = Mathf.Abs(dx);
            facing         = dx >= 0 ? 1 : -1;
            FaceTarget();

            ApplyRoomBuffToAllies();
            DecideState(distance);

            if (current == State.Chase)
                TickChase(distance);
            else
                TickRetreat();
        }

        private void FixedUpdate()
        {
            if (stats == null) return;

            if (current == State.Retreat)
                ApplyHorizontalMove(-facing, stats.retreatSpeed);
            else if (PlayerInChaseRange())
                ApplyHorizontalMove(facing, stats.chaseSpeed);
            else
                Decelerate();
        }

        private bool PlayerInChaseRange()
        {
            if (player == null) return false;
            float distance = Mathf.Abs(player.position.x - transform.position.x);
            return distance <= stats.detectRange;
        }

        private void DecideState(float distance)
        {
            State prev = current;

            if (current == State.Chase)
            {
                if (distance < stats.panicDistance)
                {
                    panicTimer += Time.deltaTime;
                    if (panicTimer >= stats.panicReactionDelay)
                    {
                        current    = State.Retreat;
                        panicTimer = 0f;
                    }
                }
                else
                {
                    panicTimer = 0f;
                }
            }
            else if (current == State.Retreat &&
                     distance > stats.panicDistance + stats.hysteresis)
            {
                current    = State.Chase;
                panicTimer = 0f;
            }

            if (debugLogs && prev != current)
                Debug.Log($"[{name}] State: {prev} -> {current} (distance={distance:F2})", this);
        }

        private void TickChase(float distance)
        {
            fireTimer -= Time.deltaTime;
            if (distance > stats.detectRange) return;
            if (fireTimer > 0f) return;

            Fire();
            fireTimer = stats.fireCooldown;
        }

        private void TickRetreat()
        {
            fireTimer = stats.fireCooldown * 0.5f;
        }

        private void ApplyRoomBuffToAllies()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, stats.buffRadius);
            var inRange = new HashSet<EnemyStrengthModifier>();

            for (int i = 0; i < hits.Length; i++)
            {
                var mod = hits[i].GetComponentInParent<EnemyStrengthModifier>();
                if (mod == null || mod.gameObject == gameObject) continue;

                inRange.Add(mod);
                mod.SetRoomBuff(stats.roomBuffBonus);

                if (!buffedAllies.Contains(mod))
                    buffedAllies.Add(mod);
            }

            for (int i = buffedAllies.Count - 1; i >= 0; i--)
            {
                var mod = buffedAllies[i];
                if (mod == null)
                {
                    buffedAllies.RemoveAt(i);
                    continue;
                }

                if (!inRange.Contains(mod))
                {
                    mod.ClearBuff();
                    buffedAllies.RemoveAt(i);
                }
            }
        }

        private void ClearAllRoomBuffs()
        {
            for (int i = 0; i < buffedAllies.Count; i++)
            {
                if (buffedAllies[i] != null)
                    buffedAllies[i].ClearBuff();
            }
            buffedAllies.Clear();
        }

        private void ApplyHorizontalMove(int moveDir, float speed)
        {
            if (!HasGroundAhead(moveDir) || IsWallAhead(moveDir))
            {
                Decelerate();
                return;
            }

            float target = moveDir * speed;
            float newX   = Mathf.MoveTowards(rb.linearVelocity.x, target,
                                             stats.groundAccel * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        private void Decelerate()
        {
            float newX = Mathf.MoveTowards(rb.linearVelocity.x, 0f,
                                           stats.groundAccel * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        private Vector3 GroundCheckBase =>
            groundCheck != null ? groundCheck.position : transform.position;

        private bool HasGroundAhead(int dir)
        {
            Vector2 origin = (Vector2)GroundCheckBase
                           + new Vector2(stats.edgeCheckForward * dir, 0f);
            return Physics2D.OverlapBox(origin, stats.groundCheckSize, 0f, groundLayer);
        }

        private bool IsWallAhead(int dir)
        {
            Vector2 origin = (Vector2)GroundCheckBase
                           + new Vector2(stats.edgeCheckForward * dir, stats.wallCheckHeight);
            return Physics2D.OverlapBox(origin, stats.wallCheckSize, 0f, groundLayer);
        }

        private void FaceTarget()
        {
            if (sprite != null) sprite.flipX = facing < 0;
        }

        private void Fire()
        {
            if (bulletPrefab == null || firePoint == null || player == null) return;

            Vector2 dir = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
            var bullet  = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            bullet.Launch(dir, stats.bulletSpeed, stats.bulletDamage, stats.bulletLifetime);
        }

        public void TakeDamage(int amount)
        {
            health -= amount;
            if (health <= 0) Destroy(gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            if (stats == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stats.detectRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stats.panicDistance);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, stats.buffRadius);
        }
    }
}
