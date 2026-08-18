using System.Collections.Generic;
using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Inquisitor : MonoBehaviour, IDamageable
    {
        private enum State { Chase, Aim, Retreat }

        // Test thử Tối ưu hiệu năng quét Buff
        
        [SerializeField] private LayerMask enemyLayer;

        [Header("Data")]
        [SerializeField] private InquisitorStatsSO stats;

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private InquisitorBullet bulletPrefab;
        [SerializeField] private Animator anim;

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
        private int health;
        private int facing = 1;
        private bool isDead = false;
        private readonly List<EnemyStrengthModifier> buffedAllies = new();

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            health = maxHealth;
            //EnemySeparation.Ensure(gameObject);

            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                if (found != null) player = found.transform;
            }

            ValidateSetup();
            if (anim == null) anim = GetComponentInChildren<Animator>();
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
                Debug.LogError($"[{name}] Khong tim thay Player.", this);
            if (groundLayer.value == 0)
                Debug.LogError($"[{name}] Ground Layer chua duoc tick.", this);
            if (bulletPrefab == null)
                Debug.LogWarning($"[{name}] Bullet Prefab chua duoc gan.", this);
            if (stats != null && stats.chaseStopDistance <= stats.panicDistance)
                Debug.LogWarning($"[{name}] chaseStopDistance nen lon hon panicDistance de tranh giat state.", this);
        }

        private void Update()
        {
            if (isDead || player == null || stats == null) return;

            float dx = player.position.x - transform.position.x;
            float distance = Mathf.Abs(dx);
            facing = dx >= 0 ? 1 : -1;
            FaceTarget();

            ApplyRoomBuffToAllies();
            DecideState(distance);

            switch (current)
            {
                case State.Retreat:
                    TickRetreat();
                    if (anim != null) anim.ResetTrigger("Attack");
                    break;
                case State.Chase:
                    if (anim != null) anim.ResetTrigger("Attack");
                    break;
            }

            if (PlayerEngaged(distance) && current != State.Chase)
            {
                TickCombat();
            }

            if (anim != null)
            {
                bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
                anim.SetBool("isMoving", isMoving);
            }
        }

        private void FixedUpdate()
        {
            if (isDead || stats == null || player == null) return;

            float distance = Mathf.Abs(player.position.x - transform.position.x);

            switch (current)
            {
                case State.Retreat:
                    ApplyHorizontalMove(-facing, stats.retreatSpeed);
                    break;
                case State.Chase:
                    if (distance > stats.chaseStopDistance && distance <= stats.detectRange)
                        ApplyHorizontalMove(facing, stats.chaseSpeed);
                    else
                        Decelerate();
                    break;
                default:
                    Decelerate();
                    break;
            }
        }

        private bool PlayerEngaged(float distance) => distance <= stats.detectRange;

        private void DecideState(float distance)
        {
            State prev = current;
            float retreatExit = stats.panicDistance + stats.retreatHysteresis;

            if (current == State.Retreat)
            {
                if (distance > retreatExit)
                    current = distance > stats.chaseStopDistance ? State.Chase : State.Aim;
                return;
            }

            if (distance < stats.panicDistance)
            {
                panicTimer += Time.deltaTime;
                if (panicTimer >= stats.panicReactionDelay)
                {
                    current = State.Retreat;
                    panicTimer = 0f;
                }
                else
                    current = State.Aim;

                if (debugLogs && prev != current)
                    Debug.Log($"[{name}] Panic windup {panicTimer:F2}/{stats.panicReactionDelay} -> {current}", this);
                return;
            }

            panicTimer = 0f;

            if (!PlayerEngaged(distance))
            {
                if (current != State.Chase) current = State.Chase;
                return;
            }

            current = distance > stats.chaseStopDistance ? State.Chase : State.Aim;

            if (debugLogs && prev != current)
                Debug.Log($"[{name}] State: {prev} -> {current} (distance={distance:F2})", this);
        }

        private void TickCombat()
        {
            fireTimer -= Time.deltaTime;
            if (fireTimer <= 0f)
            {
                Fire();
                fireTimer = stats.fireCooldown;
            }
        }

        private void TickRetreat()
        {
            fireTimer = Mathf.Max(fireTimer, stats.fireCooldown * 0.5f);
        }

        private void ApplyRoomBuffToAllies()
        {
            //var hits = Physics2D.OverlapCircleAll(transform.position, stats.buffRadius);
            //test thử tối ưu hiệu năng quét buff
            var hits = Physics2D.OverlapCircleAll(transform.position, stats.buffRadius, enemyLayer);
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
            float newX = Mathf.MoveTowards(rb.linearVelocity.x, target,
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
            transform.rotation = facing < 0 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        }

        private void Fire()
        {
            if (anim != null) anim.SetTrigger("Attack");
        }

        public void ExecuteFire()
        {
            if (bulletPrefab == null || firePoint == null || player == null) return;

            Vector2 dir = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
            var bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            bullet.Launch(player, dir, stats.bulletSpeed, stats.homingTurnRate,
                          stats.homingStopDistance, stats.bulletDamage, stats.bulletLifetime);

        }

        public void TakeDamage(int amount)
        {
            if (isDead) return;

            health -= amount;

            if (health <= 0)
            {
                isDead = true;
                if (anim != null) anim.SetTrigger("Die");

                rb.linearVelocity = Vector2.zero;
                rb.simulated = false;
                GetComponent<Collider2D>().enabled = false;
                this.enabled = false;

                Destroy(gameObject, 1.5f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (stats == null) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stats.detectRange);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, stats.chaseStopDistance);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stats.panicDistance);
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, stats.buffRadius);
        }
    }
}