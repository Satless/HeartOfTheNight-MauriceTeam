using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class BruteMage : MonoBehaviour, IDamageable
    {
        private enum State
        {
            Aggressive,
            Kite
        }

        [Header("Data")]
        [SerializeField] private BruteMageStatsSO stats;

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform meleePoint;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private BruteMageBullet bulletPrefab;

        [Header("Layers")]
        [SerializeField] private LayerMask groundLayer;

        [Header("Health")]
        [SerializeField] private int maxHealth = 60;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        [Header("State Visual")]
        [SerializeField] private bool showStateColor = true;
        [SerializeField] private Color state1Color = new(1f, 0.55f, 0.55f, 1f);
        [SerializeField] private Color state2Color = new(0.6f, 0.75f, 1f, 1f);

        private Rigidbody2D rb;
        private Collider2D col;
        private SpriteRenderer sprite;
        private State current = State.Aggressive;
        private float fireTimer;
        private float meleeTimer;
        private float teleportTimer;
        private float nextStateSwitchTime;
        private int health;
        private int facing = 1;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();
            sprite = GetComponentInChildren<SpriteRenderer>();
            health = maxHealth;
            current = State.Aggressive;
            ScheduleNextStateSwitch();
            LogCurrentState("Initial");
            ApplyStateVisual();

            // Prevent a common setup issue where X is locked in prefab.
            if (rb.constraints.HasFlag(RigidbodyConstraints2D.FreezePositionX))
            {
                rb.constraints &= ~RigidbodyConstraints2D.FreezePositionX;
            }

            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                if (found != null) player = found.transform;
            }

            ValidateSetup();
        }

        private void Update()
        {
            if (player == null || stats == null) return;

            fireTimer -= Time.deltaTime;
            meleeTimer -= Time.deltaTime;
            teleportTimer -= Time.deltaTime;

            float dx = player.position.x - transform.position.x;
            float distance = Mathf.Abs(dx);
            facing = dx >= 0f ? 1 : -1;
            FaceTarget();

            if (current == State.Aggressive && stats.canTeleportToPlayerPlatform) TryTeleportToPlayerPlatform();
            UpdateStateCycle();

            if (current == State.Aggressive)
            {
                TickAggressive(distance);
                return;
            }

            TickKite(distance);
        }

        private void FixedUpdate()
        {
            if (stats == null || player == null) return;
            float chaseSpeed = stats.chaseSpeed > 0f ? stats.chaseSpeed : 3.25f;
            float retreatSpeed = stats.retreatSpeed > 0f ? stats.retreatSpeed : 4f;
            float minKiteDistance = stats.kiteMinDistance > 0f ? stats.kiteMinDistance : 3.5f;

            if (current == State.Aggressive)
            {
                ApplyHorizontalMove(facing, chaseSpeed);
                return;
            }

            float distance = Mathf.Abs(player.position.x - transform.position.x);
            if (distance < minKiteDistance)
            {
                ApplyHorizontalMove(-facing, retreatSpeed);
            }
            else
            {
                Decelerate();
            }
        }

        private void UpdateStateCycle()
        {
            if (Time.time < nextStateSwitchTime) return;
            ChangeState(PickRandomState());
        }

        private void ChangeState(State next)
        {
            if (current == next) return;
            State prev = current;
            current = next;
            ScheduleNextStateSwitch();
            if (debugLogs) Debug.Log($"[{name}] State: {GetStateLabel(prev)} -> {GetStateLabel(current)}", this);
            LogCurrentState("Enter");
            ApplyStateVisual();
        }

        private void ScheduleNextStateSwitch()
        {
            float aggressiveDuration = 1.5f;
            float kiteDuration = 1.5f;
            if (stats != null)
            {
                aggressiveDuration = stats.aggressiveMinDuration > 0f ? stats.aggressiveMinDuration : 1.5f;
                kiteDuration = stats.kiteDuration > 0f ? stats.kiteDuration : 1.5f;
            }

            float duration = current == State.Aggressive ? aggressiveDuration : kiteDuration;
            nextStateSwitchTime = Time.time + duration;
        }

        private State PickRandomState()
        {
            // 40% Aggressive (State1) / 60% Kite (State2)
            return Random.value < 0.4f ? State.Aggressive : State.Kite;
        }

        private string GetStateLabel(State state)
        {
            return state == State.Aggressive
                ? "STATE 1 (Can chien)"
                : "STATE 2 (Ban/Rut lui)";
        }

        private void LogCurrentState(string phase)
        {
            if (!debugLogs) return;
            Debug.Log($"[{name}] {phase}: {GetStateLabel(current)}", this);
        }

        private void ApplyStateVisual()
        {
            if (!showStateColor || sprite == null) return;
            sprite.color = current == State.Aggressive ? state1Color : state2Color;
        }

        private void TickAggressive(float distance)
        {
            if (distance > stats.detectRange) return;
            TryMelee();
        }

        private void TickKite(float distance)
        {
            if (distance > stats.detectRange) return;

            if (fireTimer <= 0f)
            {
                Fire();
                fireTimer = stats.fireCooldown;
            }
        }

        private void TryMelee()
        {
            if (meleeTimer > 0f || player == null) return;

            Vector2 origin = GetMeleeOrigin();
            Collider2D[] hits = Physics2D.OverlapCircleAll(origin, stats.meleeRange, ~0);
            if (hits == null || hits.Length == 0) return;

            IDamageable selected = null;
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null) continue;

                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable == null || damageable == this) continue;

                // Prioritize the player to avoid hitting unrelated targets first.
                if (hit.CompareTag("Player") || hit.transform.root.CompareTag("Player"))
                {
                    selected = damageable;
                    break;
                }

                if (selected == null) selected = damageable;
            }

            if (selected == null) return;
            selected.TakeDamage(stats.meleeDamage);
            meleeTimer = stats.meleeCooldown;
        }

        private Vector2 GetMeleeOrigin()
        {
            if (meleePoint == null)
            {
                float forward = Mathf.Max(0.2f, stats.meleeRange * 0.6f);
                return (Vector2)transform.position + Vector2.right * (facing * forward);
            }

            Vector2 local = meleePoint.localPosition;
            local.x = Mathf.Abs(local.x) * facing;
            return (Vector2)transform.TransformPoint(local);
        }

        private void Fire()
        {
            if (bulletPrefab == null || firePoint == null || player == null) return;

            Vector2 dir = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
            var bullet = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
            bullet.Launch(this, dir, stats.bulletSpeed, stats.bulletDamage, stats.bulletLifetime);
        }

        private void ApplyHorizontalMove(int moveDir, float speed)
        {
            speed = speed > 0f ? speed : 3f;

            float target = moveDir * speed;
            float accel = stats.groundAccel > 0f ? stats.groundAccel : 28f;
            float newX = Mathf.MoveTowards(rb.linearVelocity.x, target, accel * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        private void Decelerate()
        {
            float accel = stats.groundAccel > 0f ? stats.groundAccel : 28f;
            float newX = Mathf.MoveTowards(rb.linearVelocity.x, 0f, accel * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
        }

        private Vector3 GroundCheckBase => groundCheck != null ? groundCheck.position : transform.position;

        private bool HasGroundAhead(int dir)
        {
            Vector2 origin = (Vector2)GroundCheckBase + new Vector2(stats.edgeCheckForward * dir, 0f);
            return Physics2D.OverlapBox(origin, stats.groundCheckSize, 0f, groundLayer);
        }

        private bool IsWallAhead(int dir)
        {
            Vector2 origin = (Vector2)GroundCheckBase
                             + new Vector2(stats.edgeCheckForward * dir, stats.wallCheckHeight);
            return Physics2D.OverlapBox(origin, stats.wallCheckSize, 0f, groundLayer);
        }

        private void TryTeleportToPlayerPlatform()
        {
            if (teleportTimer > 0f || player == null) return;

            var myGround = ProbeGround(transform.position);
            var playerGround = ProbeGround(player.position);
            if (myGround.collider == null || playerGround.collider == null) return;
            if (myGround.collider == playerGround.collider) return;

            int sideDir = facing == 0 ? 1 : -facing;
            float sideOffset = stats.teleportSideOffset > 0f ? stats.teleportSideOffset : 1.25f;
            float minDistance = stats.teleportMinDistanceFromPlayer > 0f ? stats.teleportMinDistanceFromPlayer : 2.25f;
            float desiredOffset = Mathf.Max(sideOffset, minDistance);

            // In melee state, teleport with a clear gap so the enemy has to walk in before attacking.
            Vector2 probeFrom = (Vector2)player.position
                                + new Vector2(desiredOffset * sideDir, stats.teleportProbeHeight);
            RaycastHit2D landing = Physics2D.Raycast(probeFrom, Vector2.down, stats.teleportProbeHeight * 3f, groundLayer);
            if (!landing.collider) return;

            float halfHeight = col != null ? col.bounds.extents.y : 0.5f;
            Vector3 targetPos = new(
                probeFrom.x,
                landing.point.y + halfHeight + stats.teleportYPadding,
                transform.position.z);

            float dxToPlayer = Mathf.Abs(targetPos.x - player.position.x);
            if (dxToPlayer < minDistance)
            {
                float pushDir = targetPos.x >= player.position.x ? 1f : -1f;
                targetPos.x = player.position.x + pushDir * minDistance;
            }

            transform.position = targetPos;
            rb.linearVelocity = Vector2.zero;
            teleportTimer = stats.teleportCooldown;

            if (debugLogs) Debug.Log($"[{name}] Teleport to player platform.", this);
        }

        private RaycastHit2D ProbeGround(Vector2 worldPos)
        {
            Vector2 from = worldPos + Vector2.up * 0.25f;
            return Physics2D.Raycast(from, Vector2.down, 2.5f, groundLayer);
        }

        private void FaceTarget()
        {
            if (sprite != null) sprite.flipX = facing < 0;
        }

        private void ValidateSetup()
        {
            if (stats == null) Debug.LogError($"[{name}] BruteMageStatsSO chua duoc gan.", this);
            if (player == null) Debug.LogError($"[{name}] Khong tim thay Player (tag Player).", this);
            if (groundLayer.value == 0) Debug.LogWarning($"[{name}] Ground Layer chua duoc tick.", this);
            if (bulletPrefab == null) Debug.LogWarning($"[{name}] Bullet prefab chua duoc gan.", this);
            if (firePoint == null) Debug.LogWarning($"[{name}] FirePoint chua duoc gan.", this);
            if (meleePoint == null) Debug.LogWarning($"[{name}] MeleePoint chua duoc gan, se dung transform.position.", this);
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

            Vector3 meleeOrigin = meleePoint != null ? meleePoint.position : transform.position;
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(meleeOrigin, stats.meleeRange);

            Vector3 groundOrigin = GroundCheckBase + new Vector3(stats.edgeCheckForward * facing, 0f, 0f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundOrigin, stats.groundCheckSize);
        }
    }
}
