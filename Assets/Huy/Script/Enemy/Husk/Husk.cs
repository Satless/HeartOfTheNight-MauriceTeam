using UnityEngine;
using System.Collections;
using HeartOfTheNight.Common;

public class Husk : MonoBehaviour, IDamageable, IKnockbackGate
{
    [Header("Chỉ số Sinh tồn")]
    public int maxHealth = 50;
    public int currentHealth;
    public bool isDead = false;
    public float deathYOffset = 0f;

    [Header("Hoạt ảnh & Vị trí chém")]
    public Animator anim;
    public GameObject attackHitbox;
    public Vector2 attackOffset = new Vector2(0f, 1f);

    [Header("Tầm nhìn & Tầm Đánh")]
    public float moveSpeed = 3.8f;
    public float patrolSpeed = 2.5f;
    public float patrolDistance = 5f;
    private float startX;

    public float detectionRangeX = 10f;
    public float detectionRangeY = 3f;
    public float attackRange = 2.5f;
    public float attackRangeY = 1.5f; // Chống lỗi chém không khí
    public float attackRadius = 1.5f;

    [Header("Kiểm tra Mặt đất & Mép vực")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public bool isGrounded;

    [Space]
    public Transform edgeCheck;
    public Transform wallCheck;
    public float edgeCheckDistance = 1.5f;
    public float wallCheckDistance = 0.5f;
    public float wallCheckHeight = 1.5f; // 🔥 THÊM: Check tường theo trục Y

    [Header("Fix Bug Lật Liên Tục")]
    public float flipCooldown = 0.5f; // Thời gian cấm lật lại
    private float flipTimer = 0f;

    [Header("Tấn công")]
    public int attackDamage = 15;
    public float attackCooldown = 2f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D myCol;
    private float nextAttackTime = 0f;
    private bool isBusy = false;
    private KnockbackReceiver knockback;

    private float idleSoundTimer;
    private float moveSoundTimer;

    public bool CanReceiveKnockback => !isDead && !isBusy;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        knockback = GetComponent<KnockbackReceiver>();
        myCol = GetComponent<Collider2D>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        startX = transform.position.x;
    }

    void Update()
    {
        if (isBusy || isDead) return;
        if (knockback != null && knockback.IsKnockedBack) return;

        CheckGroundStatus();

        if (flipTimer > 0) flipTimer -= Time.deltaTime;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (player != null)
        {
            float distanceX = Mathf.Abs(player.position.x - transform.position.x);
            float distanceY = Mathf.Abs(player.position.y - transform.position.y);

            if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
            {
                if (distanceX <= attackRange && distanceY <= attackRangeY)
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    if (anim != null) anim.SetFloat("Speed", 0);

                    if (Time.time >= nextAttackTime) StartCoroutine(AttackRoutine());
                }
                else
                {
                    ChasePlayer();
                }
                return;
            }
        }

        Patrol();
    }

    void CheckGroundStatus()
    {
        if (groundCheck == null) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    bool IsNearEdge()
    {
        if (edgeCheck == null || edgeCheckDistance <= 0f) return false;
        RaycastHit2D hit = Physics2D.Raycast(edgeCheck.position, Vector2.down, edgeCheckDistance, groundLayer);
        return hit.collider == null;
    }

    bool IsHittingWall()
    {
        if (wallCheck == null || wallCheckDistance <= 0f) return false;
        float dir = Mathf.Sign(transform.localScale.x);

        // 🔥 FIX: Nhấc tâm của hộp lên trên để đáy hộp CHẮC CHẮN NÉ MẶT ĐẤT
        Vector2 boxCenter = (Vector2)wallCheck.position + new Vector2(0f, (wallCheckHeight / 2f) + 0.1f);
        Vector2 boxSize = new Vector2(0.1f, wallCheckHeight);

        RaycastHit2D hit = Physics2D.BoxCast(boxCenter, boxSize, 0f, Vector2.right * dir, wallCheckDistance, groundLayer);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    void Patrol()
    {
        float distanceFromStart = Mathf.Abs(transform.position.x - startX);
        float currentDir = Mathf.Sign(transform.localScale.x);
        float dirToStart = Mathf.Sign(startX - transform.position.x);

        idleSoundTimer -= Time.fixedDeltaTime;
        if (idleSoundTimer <= 0f)
        {
            AudioEvents.TriggerSound3D("Enemy", "Husk", "Idle", transform.position);
            idleSoundTimer = 5f;
        }

        // 🔥 FIX: Tách riêng logic check Tường/Vực và check Khoảng cách
        if (flipTimer <= 0f)
        {
            if (IsNearEdge() || IsHittingWall())
            {
                // Đụng tường/mép vực -> Quay đầu VÀ đổi nhà mới
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
                currentDir = Mathf.Sign(transform.localScale.x);

                flipTimer = flipCooldown;
                startX = transform.position.x; // Set tâm mới để chống kẹt logic
            }
            else if (distanceFromStart >= patrolDistance && currentDir != dirToStart)
            {
                // Hết khu tuần tra -> Quay đầu bình thường
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
                currentDir = Mathf.Sign(transform.localScale.x);

                flipTimer = flipCooldown;
            }
        }

        rb.linearVelocity = new Vector2(currentDir * patrolSpeed, rb.linearVelocity.y);
        if (anim != null) anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
    }

    void ChasePlayer()
    {
        LookAtPlayer();

        // Đang đuổi thì chỉ dừng khi đụng tường — không đứng im vì mép vực.
        if (IsHittingWall())
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetFloat("Speed", 0);
        }
        else
        {
            float dir = (player.position.x > transform.position.x) ? 1 : -1;
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
            if (anim != null) anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));

            moveSoundTimer -= Time.deltaTime;
            if (moveSoundTimer <= 0f)
            {
                AudioEvents.TriggerSound3D("Enemy", "Husk", "Move", transform.position);
                moveSoundTimer = 0.2f;
            }
        }
    }

    void LookAtPlayer()
    {
        if (flipTimer > 0) return;
        float dir = (player.position.x > transform.position.x) ? 1 : -1;

        if (Mathf.Sign(transform.localScale.x) != dir)
        {
            transform.localScale = new Vector3(dir * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
            flipTimer = flipCooldown;
        }
    }

    IEnumerator AttackRoutine()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;
        LookAtPlayer();

        if (anim != null) anim.SetTrigger("Attack");
        AudioEvents.TriggerSound3D("Enemy", "Husk", "Attack", transform.position);

        yield return new WaitForSeconds(attackCooldown);

        nextAttackTime = Time.time + attackCooldown;
        isBusy = false;
    }

    public void EnableHitbox()
    {
        if (isDead || attackHitbox == null) return;
        attackHitbox.SetActive(true);
        float facingDirection = Mathf.Sign(transform.localScale.x);
        Vector2 finalAttackPos = (Vector2)attackHitbox.transform.position + new Vector2(attackOffset.x * facingDirection, attackOffset.y);
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(finalAttackPos, attackRadius);

        foreach (Collider2D p in hitPlayers)
        {
            if (p.CompareTag("Enemy") || !p.isTrigger) continue;
            if (p.CompareTag("Player") || p.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                IDamageable target = p.GetComponent<IDamageable>();
                if (target == null) target = p.GetComponentInParent<IDamageable>();
                if (target != null) target.TakeDamage(attackDamage);
            }
        }
    }

    public void DisableHitbox()
    {
        if (attackHitbox != null)
            attackHitbox.SetActive(false);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        AudioEvents.TriggerSound3D("Enemy", "Husk", "Hurt", transform.position);

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        AudioEvents.TriggerSound3D("Enemy", "Husk", "Die", transform.position);

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        gameObject.tag = "Untagged";
        if (myCol != null) myCol.enabled = false;

        transform.position = new Vector3(transform.position.x, transform.position.y + deathYOffset, transform.position.z);
        if (anim != null)
        {
            anim.enabled = true;
            anim.SetTrigger("Dead");
        }
        Destroy(gameObject, 2f);
    }

    void OnDrawGizmosSelected()
    {
        if (wallCheck != null)
        {
            Gizmos.color = Color.cyan;
            float dir = Mathf.Sign(transform.localScale.x);
            // 🔥 Vẽ hộp BoxCast chính xác
            Vector3 center = wallCheck.position + new Vector3(dir * (wallCheckDistance / 2f), (wallCheckHeight / 2f) + 0.1f, 0);
            Gizmos.DrawWireCube(center, new Vector3(wallCheckDistance, wallCheckHeight, 0));
        }
    }
}