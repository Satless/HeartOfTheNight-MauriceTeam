using HeartOfTheNight.Common;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class Cultist : MonoBehaviour, IDamageable
    {
      
        private enum State { Idle, Chase, Attack, Retreat }
        [Header("Chase Settings")]
[SerializeField] private float chaseRangeOffset = 4f; // Khoảng cách cộng thêm ngoài vòng vàng để đuổi theo

        [Header("Data")]
        [SerializeField] private CultistStatsSO stats;

        [Header("References")]
        [SerializeField] private Transform player;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Transform groundCheck;
        [SerializeField] private CultistBullet bulletPrefab;
        [SerializeField] private Animator anim;


        [Header("Layers")]
        [SerializeField] private LayerMask groundLayer;

        [Header("Health")]
        [SerializeField] private int maxHealth = 30;

        [Header("Debug")]
        [SerializeField] private bool debugLogs = false;

        private Rigidbody2D rb;
        private SpriteRenderer sprite;
        private EnemyStrengthModifier strengthMod;
        private State current = State.Idle;
        private float fireTimer;
        private float closeRangeTimer;
        private int   health;
        private int   facing = 1;

        private void Awake()
        {
            rb          = GetComponent<Rigidbody2D>();
            sprite      = GetComponentInChildren<SpriteRenderer>();
            strengthMod = GetComponent<EnemyStrengthModifier>();
            health      = maxHealth;
            EnemySeparation.Ensure(gameObject);

            if (player == null)
            {
                var found = GameObject.FindGameObjectWithTag("Player");
                if (found != null) player = found.transform;
            }

            ValidateSetup();
            if (anim == null) anim = GetComponentInChildren<Animator>();
        }

        private void ValidateSetup()
        {
            if (stats == null)
                Debug.LogError($"[{name}] CultistStatsSO chua duoc gan trong Inspector.", this);
            if (player == null)
                Debug.LogError($"[{name}] Khong tim thay Player. Hay gan truc tiep hoac dat Tag 'Player' cho GameObject Player.", this);
            if (groundLayer.value == 0)
                Debug.LogError($"[{name}] Ground Layer chua duoc tick! Cultist se khong the lui vi khong detect duoc nen dat. Vao Inspector cua Cultist va tick layer Ground.", this);
            if (bulletPrefab == null)
                Debug.LogWarning($"[{name}] Bullet Prefab chua duoc gan - Cultist se khong ban duoc.", this);
            if (groundCheck == null)
                Debug.LogWarning($"[{name}] GroundCheck Transform chua duoc gan - dang fallback ve transform.position. Hay tao child empty 'GroundCheck' o ngay duoi chan Cultist va keo vao field.", this);
            else
            {
                var col = GetComponent<Collider2D>();
                if (col != null)
                {
                    float feetY    = col.bounds.min.y;
                    float gcY      = groundCheck.position.y;
                    float distAbove = gcY - feetY;
                    if (distAbove > 0.15f)
                    {
                        Debug.LogError(
                            $"[{name}] GroundCheck dang o CAO HON chan Cultist {distAbove:F2} unit! " +
                            $"GroundCheck.y={gcY:F2}, chan Cultist.y={feetY:F2}. " +
                            $"Vao Hierarchy chon child GroundCheck, set Local Position Y xuong ~{(feetY - transform.position.y):F2} (tuc {-(col.bounds.extents.y):F2} so voi Cultist parent). " +
                            $"Day gan nhu chac chan la ly do Cultist khong lui duoc.", this);
                    }
                }
            }
            if (rb.bodyType != RigidbodyType2D.Dynamic)
                Debug.LogWarning($"[{name}] Rigidbody2D nen la Dynamic de lui duoc. Hien tai: {rb.bodyType}.", this);
            if (rb.constraints.HasFlag(RigidbodyConstraints2D.FreezePositionX))
                Debug.LogError($"[{name}] Rigidbody2D dang khoa FreezePositionX - Cultist khong the di chuyen ngang.", this);
        }

        private void Update()
{
    if (player == null || stats == null) return;

    // FIX: Tính khoảng cách hình tròn 2D thực tế thay vì chỉ trục X
    float distance = Vector2.Distance(transform.position, player.position);
    facing = (player.position.x - transform.position.x) >= 0 ? 1 : -1;
    FaceTarget();

    DecideState(distance);

            switch (current)
            {
                case State.Idle:
                case State.Chase:
                case State.Retreat:
                    fireTimer = stats.fireCooldown; // Khóa súng khi đang đi/đứng chơi
                    if (anim != null) anim.ResetTrigger("Attack"); // Xóa lệnh đánh bị kẹt
                    break;
                case State.Attack:
                    TickAttack(distance);
                    break;
            }

            // FIX: Kích hoạt animation đi bộ mượt mà dựa trên vận tốc thực tế
            if (anim != null)
    {
        bool isMoving = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        anim.SetBool("isMoving", isMoving);
    }
}
        

       private void FixedUpdate()
{
    if (stats == null) return;

    if (current == State.Retreat) 
        ApplyVelocity(-facing); // Lùi ngược hướng nhìn
    else if (current == State.Chase) 
        ApplyVelocity(facing);  // Tiến theo hướng nhìn
    else 
        Decelerate();           // Đứng im (Idle hoặc Attack)
}
       private void DecideState(float distance)

{
    if (distance <= stats.minSafeDistance)
    {
        // Xử lý delay lùi
        closeRangeTimer += Time.deltaTime;
        if (closeRangeTimer >= stats.retreatReactionDelay) 
            current = State.Retreat;
    }
    else
    {
        closeRangeTimer = 0f;
        
        if (distance <= stats.detectRange) 
            current = State.Attack; // Trong vòng vàng -> Đứng lại ngắm bắn
        else if (distance <= stats.detectRange + chaseRangeOffset) 
            current = State.Chase; // Ngoài vòng vàng nhưng trong tầm nhìn -> Đuổi theo
        else 
            current = State.Idle; // Quá xa -> Đứng chơi
    }
}
private void TickAttack(float distance)
{
    fireTimer -= Time.deltaTime;
    if (fireTimer <= 0f)
    {
        Fire();
        fireTimer = stats.fireCooldown;
    }
}
private void ApplyVelocity(int dir)
{
    bool hasGround = HasGroundAhead(dir);
    bool hasWall   = IsWallAhead(dir);

    if (!hasGround || hasWall)
    {
        Decelerate();
        return;
    }

    float target = dir * EffectiveMoveSpeed;
    float newX   = Mathf.MoveTowards(rb.linearVelocity.x, target, stats.groundAccel * Time.fixedDeltaTime);
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
            bool hit = Physics2D.OverlapBox(origin, stats.groundCheckSize, 0f, groundLayer);

            if (debugLogs && !hit) DiagnoseMissedHit("GroundAhead", origin, stats.groundCheckSize);
            return hit;
        }

        private bool IsWallAhead(int dir)
        {
            Vector2 origin = (Vector2)GroundCheckBase
                           + new Vector2(stats.edgeCheckForward * dir, stats.wallCheckHeight);
            return Physics2D.OverlapBox(origin, stats.wallCheckSize, 0f, groundLayer);
        }

        private void DiagnoseMissedHit(string label, Vector2 origin, Vector2 size)
        {
            var hits = Physics2D.OverlapBoxAll(origin, size, 0f);
            if (hits.Length == 0)
            {
                Debug.Log($"[{name}] {label} miss: KHONG co collider nao tai {origin} (size {size}). " +
                          $"=> Platform khong vuon toi day, hoac vi tri GroundCheck dat sai. " +
                          $"GroundCheckBase={GroundCheckBase}, dir-offset={origin - (Vector2)GroundCheckBase}.", this);
                return;
            }

            var names = new string[hits.Length];
            for (int i = 0; i < hits.Length; i++)
            {
                var go = hits[i].gameObject;
                names[i] = $"'{go.name}'(layer:{LayerMask.LayerToName(go.layer)})";
            }
            Debug.Log($"[{name}] {label} miss: Tim thay {hits.Length} collider tai {origin}: " +
                      $"[{string.Join(", ", names)}] " +
                      $"NHUNG khong khop mask Ground Layer (mask value = {groundLayer.value}). " +
                      $"=> Vao Inspector Cultist tick dung layer cua platform.", this);
        }

       private void FaceTarget()
{
    // Xoay 180 độ theo trục Y để mang theo tất cả child objects (FirePoint, GroundCheck)
    transform.rotation = facing < 0 ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
}
// Sửa đổi hàm Fire cũ: Giờ chỉ dùng để kích hoạt Animation đánh
private void Fire()
{
    if (anim != null) anim.SetTrigger("Attack");
}
// Thêm hàm public này để Animation Event gọi vào đúng frame vung gậy
        public void ExecuteFire()
{
    if (bulletPrefab == null || firePoint == null || player == null) return;

    Vector2 dir = ((Vector2)player.position - (Vector2)firePoint.position).normalized;
    var bullet  = Instantiate(bulletPrefab, firePoint.position, Quaternion.identity);
    bullet.Launch(dir, stats.bulletSpeed, EffectiveBulletDamage, stats.bulletLifetime);
}

        private float EffectiveMoveSpeed =>
            stats.moveSpeed * (strengthMod != null ? strengthMod.MoveSpeedMultiplier : 1f);

        private int EffectiveBulletDamage =>
            Mathf.Max(1, Mathf.RoundToInt(stats.bulletDamage *
                (strengthMod != null ? strengthMod.DamageMultiplier : 1f)));

       public void TakeDamage(int amount)
{
    if (health <= 0) return; // Tránh dính đòn nhiều lần khi đang chết

    health -= amount;
    if (health <= 0) 
    {
        if (anim != null) anim.SetTrigger("Die");
        
        // Vô hiệu hóa vật lý và logic để quái không di chuyển/bắn nữa
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        GetComponent<Collider2D>().enabled = false;
        this.enabled = false;

        // Cho delay 1 khoản thời gian để chạy hết animation rồi mới Destroy
        Destroy(gameObject, 1f); 
    }
}
        private void OnDrawGizmosSelected()
        {
            if (stats == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, stats.detectRange + chaseRangeOffset);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, stats.detectRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, stats.minSafeDistance);
        }

        private void OnDrawGizmos()
        {
            if (stats == null) return;

            int dir = facing == 0 ? -1 : -facing;
            Vector3 baseP = GroundCheckBase;

            Vector3 groundOrigin = baseP + new Vector3(stats.edgeCheckForward * dir, 0f, 0f);
            bool hasGround = Application.isPlaying
                && Physics2D.OverlapBox(groundOrigin, stats.groundCheckSize, 0f, groundLayer);
            Gizmos.color = hasGround ? Color.green : Color.red;
            Gizmos.DrawWireCube(groundOrigin, stats.groundCheckSize);

            Vector3 wallOrigin = baseP + new Vector3(stats.edgeCheckForward * dir,
                                                    stats.wallCheckHeight, 0f);
            bool hasWall = Application.isPlaying
                && Physics2D.OverlapBox(wallOrigin, stats.wallCheckSize, 0f, groundLayer);
            Gizmos.color = hasWall ? Color.red : new Color(0f, 1f, 1f, 0.4f);
            Gizmos.DrawWireCube(wallOrigin, stats.wallCheckSize);
        }
    }
}
