using UnityEngine;
using System.Collections;
using HeartOfTheNight.Common;

public class BigCorpseImg : MonoBehaviour, IDamageable
{
    [Header("Chỉ số Sinh tồn")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;
    public float deathYOffset = 0f;

    [Header("Hoạt ảnh & Vị trí chém")]
    public Animator anim;
    public GameObject attackHitbox;
    public Vector2 attackOffset = new Vector2(0f, 1f);

    [Header("Tầm nhìn & Tầm Đánh")]
    public float moveSpeed = 3f;
    public float patrolSpeed = 1.5f;
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

    [Header("Tấn công")]
    public int attackDamage = 15;
    public float attackCooldown = 2.5f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D myCol;
    private float nextAttackTime = 0f;
    private bool isBusy = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        startX = transform.position.x;
    }

    void Update()
    {
        if (isBusy || isDead) return;

        CheckGroundStatus();

        if (player != null)
        {
            float distanceX = Mathf.Abs(player.position.x - transform.position.x);
            float distanceY = Mathf.Abs(player.position.y - transform.position.y);

            if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
            {
                // Ép phải cùng độ cao Y mới được chém
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
        if (edgeCheck == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(edgeCheck.position, Vector2.down, edgeCheckDistance, groundLayer);
        return hit.collider == null;
    }

    bool IsHittingWall()
    {
        if (wallCheck == null) return false;
        float dir = Mathf.Sign(transform.localScale.x);
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, Vector2.right * dir, wallCheckDistance, groundLayer);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    void Patrol()
    {
        float distanceFromStart = Mathf.Abs(transform.position.x - startX);
        float currentDir = Mathf.Sign(transform.localScale.x);
        float dirToStart = Mathf.Sign(startX - transform.position.x);

        if (IsNearEdge() || IsHittingWall() || (distanceFromStart >= patrolDistance && currentDir != dirToStart))
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }

        float dir = Mathf.Sign(transform.localScale.x);
        rb.linearVelocity = new Vector2(dir * patrolSpeed, rb.linearVelocity.y);
        if (anim != null) anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
    }

    void ChasePlayer()
    {
        LookAtPlayer();

        if (IsNearEdge() || IsHittingWall())
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetFloat("Speed", 0);
        }
        else
        {
            float dir = (player.position.x > transform.position.x) ? 1 : -1;
            rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
            if (anim != null) anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }
    }

    void LookAtPlayer()
    {
        float dir = (player.position.x > transform.position.x) ? 1 : -1;
        transform.localScale = new Vector3(dir * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }

    IEnumerator AttackRoutine()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;
        LookAtPlayer();

        if (anim != null) anim.SetTrigger("Attack");
        yield return new WaitForSeconds(attackCooldown);

        nextAttackTime = Time.time + attackCooldown;
        isBusy = false;
    }

    public void EnableHitbox()
    {
        if (isDead || attackHitbox == null) return;
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

    public void DisableHitbox() { }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
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
        Destroy(gameObject, 0.5f);
    }
}