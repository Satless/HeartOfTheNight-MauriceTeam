using UnityEngine;
using System.Collections;
using HeartOfTheNight.Common;

public class BurningCorpseImg : MonoBehaviour, IDamageable, IKnockbackGate
{
    [Header("Chỉ số Sinh tồn")]
    public int maxHealth = 45;
    public int currentHealth;
    public bool isDead = false;

    [Header("Hoạt ảnh & Vị trí chém")]
    public Animator anim;
    public GameObject attackHitbox;
    public Vector2 attackOffset = new Vector2(0f, 1f);

    [Header("Tầm nhìn & Tầm Đánh")]
    public float moveSpeed = 4.5f;
    public float patrolSpeed = 3.5f;
    public float patrolDistance = 5f;
    private float startX;

    public float detectionRangeX = 12f;
    public float detectionRangeY = 3f;
    public float attackRange = 2f;
    public float attackRangeY = 1.5f;
    public float attackRadius = 1.2f;

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
    public float wallCheckHeight = 1.5f;

    [Header("Fix Bug Lật Liên Tục")]
    public float flipCooldown = 0.5f;
    private float flipTimer = 0f;

    [Header("Hệ thống chống kẹt (Dành cho Tuần tra)")]
    public float stuckTimeLimit = 0.8f;
    private float stuckTimer = 0f;
    private float lastXPos = 0f;

    [Header("Sát thương & Hiệu ứng Cháy")]
    public int attackDamage = 10;
    public float attackCooldown = 1.6f;
    public int burnDamagePerTick = 2;
    public int burnTicks = 3;
    public float timeBetweenTicks = 1f;
    public float dashSpeedThreshold = 12f;

    [Tooltip("Kéo Prefab hình ngọn lửa vào đây để nó bám lên người Player")]
    public GameObject burnEffectPrefab;

    [Tooltip("Điều chỉnh vị trí ngọn lửa (X, Y) bù trừ so với Player. Ví dụ Y=0.5 để đẩy lửa lên cao")]
    public Vector2 burnEffectOffset = new Vector2(0f, 0.5f);

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D myCol;
    private float nextAttackTime = 0f;
    private bool isBusy = false;
    private KnockbackReceiver knockback;
    private float lastBurnHitTime = -999f;

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
        lastXPos = transform.position.x;
    }

    void Update()
    {
        if (isBusy || isDead) return;
        if (knockback != null && knockback.IsKnockedBack) return;

        CheckGroundStatus();
        if (flipTimer > 0) flipTimer -= Time.deltaTime;

        // 1. NẾU THẤY PLAYER -> ƯU TIÊN SỐ 1 LÀ ĐUỔI HOẶC CHÉM
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
                return; // Đã xử lý xong việc đuổi Player, thoát Update() luôn, không chạy xuống Patrol nữa!
            }
        }

        // 2. KHÔNG THẤY PLAYER -> ĐI TUẦN TRA VÀ CHỐNG KẸT
        Patrol();

        // Chống kẹt chỉ áp dụng khi đi tuần tra (Tránh việc đuổi theo ép góc tường bị lật mặt)
        if (Mathf.Abs(transform.position.x - lastXPos) < 0.01f)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer >= stuckTimeLimit)
            {
                if (flipTimer <= 0f) Flip();
                startX = transform.position.x;
                stuckTimer = 0f;
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        lastXPos = transform.position.x;
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

        if (flipTimer <= 0f)
        {
            if (IsNearEdge() || IsHittingWall())
            {
                Flip();
                startX = transform.position.x;
            }
            else if (distanceFromStart >= patrolDistance && currentDir != dirToStart)
            {
                Flip();
            }
        }

        rb.linearVelocity = new Vector2(currentDir * patrolSpeed, rb.linearVelocity.y);
        if (anim != null) anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));

        idleSoundTimer -= Time.deltaTime;
        if (idleSoundTimer <= 0f)
        {
            AudioEvents.TriggerSound3D("Enemy", "BurningCorpse", "Idle", transform.position);
            idleSoundTimer = 1.5f;
        }
    }

    void ChasePlayer()
    {
        LookAtPlayer();

        // 🔥 FIX TRỌNG ĐIỂM: Đang đuổi thì mặc kệ mép vực (lao xuống luôn), chỉ đứng lại khi đụng tường!
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
                AudioEvents.TriggerSound3D("Enemy", "BurningCorpse", "Move", transform.position);
                moveSoundTimer = 0.3f;
            }
        }
    }

    void LookAtPlayer()
    {
        if (flipTimer > 0) return;
        float dir = (player.position.x > transform.position.x) ? 1 : -1;
        if (Mathf.Sign(transform.localScale.x) != dir)
        {
            Flip();
        }
    }

    void Flip()
    {
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
        flipTimer = flipCooldown;
    }

    IEnumerator AttackRoutine()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;
        LookAtPlayer();

        if (anim != null) anim.SetTrigger("Attack");
        AudioEvents.TriggerSound3D("Enemy", "BurningCorpse", "Attack", transform.position);

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
                if (target != null) DealDamageAndBurn(p);
            }
        }
    }

    public void DealDamageAndBurn(Collider2D playerCol)
    {
        if (isDead || playerCol == null) return;
        // Animation event + BurnHB OnTriggerEnter thường bắn cùng 1 nhát — chỉ nhận 1 lần.
        if (Time.time - lastBurnHitTime < 0.15f) return;
        lastBurnHitTime = Time.time;

        IDamageable target = playerCol.GetComponent<IDamageable>();
        if (target == null) target = playerCol.GetComponentInParent<IDamageable>();
        if (target == null) return;

        target.TakeDamage(attackDamage);

        Rigidbody2D playerRb = playerCol.GetComponentInParent<Rigidbody2D>();
        Transform host = playerRb != null ? playerRb.transform : playerCol.transform.root;

        BurnEffectLifetime.ApplyOn(
            host,
            burnEffectPrefab,
            burnEffectOffset,
            target,
            playerRb,
            burnTicks,
            timeBetweenTicks,
            burnDamagePerTick,
            dashSpeedThreshold);
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
        AudioEvents.TriggerSound3D("Enemy", "BurningCorpse", "Hurt", transform.position);
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        AudioEvents.TriggerSound3D("Enemy", "BurningCorpse", "Die", transform.position);

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        gameObject.tag = "Untagged";
        if (myCol != null) myCol.enabled = false;

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
            Vector3 center = wallCheck.position + new Vector3(dir * (wallCheckDistance / 2f), (wallCheckHeight / 2f) + 0.1f, 0);
            Gizmos.DrawWireCube(center, new Vector3(wallCheckDistance, wallCheckHeight, 0));
        }
    }
}