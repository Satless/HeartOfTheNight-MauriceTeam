using UnityEngine;
using System.Collections;
using HeartOfTheNight.Common;

public class Wrath : MonoBehaviour, IDamageable
{
    [Header("Chỉ số Sinh tồn")]
    public int maxHealth = 150;
    public int currentHealth;
    public bool isDead = false;

    [Header("Hoạt ảnh")]
    public Animator anim;

    [Header("Tầm nhìn & Tầm Húc")]
    public float moveSpeed = 5f;
    public float patrolSpeed = 2f;
    public float patrolDistance = 5f;
    private float startX;

    public float detectionRangeX = 12f;
    public float detectionRangeY = 2.5f;
    public float attackRange = 4.5f;
    public float attackRangeY = 1.5f;

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

    [Header("Sát thương & Cú Húc (Charge)")]
    public int attackDamage = 15;
    public float attackCooldown = 2f;
    public float antiHealDuration = 6f;

    [Space]
    public float chargeWindupTime = 0.6f;
    public float chargeSpeed = 18f;
    public float chargeDuration = 0.35f;
    public float hitRadius = 1.5f;
    public float recoveryTime = 1.5f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D myCol;
    private float nextAttackTime = 0f;
    private bool isBusy = false;

    private float idleSoundTimer;
    private float moveSoundTimer;

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
        if (flipTimer > 0) flipTimer -= Time.deltaTime;

        if (player != null)
        {
            float distanceX = Mathf.Abs(player.position.x - transform.position.x);
            float distanceY = Mathf.Abs(player.position.y - transform.position.y);

            if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
            {
                if (distanceX <= attackRange && distanceY <= attackRangeY)
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    if (anim != null) anim.SetFloat("Speed", 0f);

                    if (Time.time >= nextAttackTime) StartCoroutine(ThucHienHucRoutine());
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
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
                currentDir = Mathf.Sign(transform.localScale.x);

                flipTimer = flipCooldown;
                startX = transform.position.x; // FIX
            }
            else if (distanceFromStart >= patrolDistance && currentDir != dirToStart)
            {
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
                currentDir = Mathf.Sign(transform.localScale.x);

                flipTimer = flipCooldown;
            }
        }

        rb.linearVelocity = new Vector2(currentDir * patrolSpeed, rb.linearVelocity.y);
        if (anim != null) anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));

        idleSoundTimer -= Time.fixedDeltaTime;
        if (idleSoundTimer <= 0f)
        {
            AudioEvents.TriggerSound3D("Enemy", "Wrath", "Idle", transform.position);
            idleSoundTimer = 10f;
        }
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

            moveSoundTimer -= Time.fixedDeltaTime;
            if (moveSoundTimer <= 0f)
            {
                AudioEvents.TriggerSound3D("Enemy", "Wrath", "Move", transform.position);
                moveSoundTimer = 0.2f;
            }
        }
    }

    void LookAtPlayer()
    {
        if (flipTimer > 0) return;
        Vector3 scale = transform.localScale;
        float expectedSign = player.position.x > transform.position.x ? 1f : -1f;

        if (Mathf.Sign(scale.x) != expectedSign)
        {
            scale.x = expectedSign * Mathf.Abs(scale.x);
            transform.localScale = scale;
            flipTimer = flipCooldown;
        }
    }

    IEnumerator ThucHienHucRoutine()
    {
        isBusy = true;
        AudioEvents.TriggerSound3D("Enemy", "Wrath", "Attack", transform.position);

        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetFloat("Speed", 0f);
        LookAtPlayer();

        if (anim != null) anim.SetTrigger("Roar");
        yield return new WaitForSeconds(chargeWindupTime);

        if (isDead) yield break;

        if (anim != null) anim.SetTrigger("Rush");

        float huongHuc = Mathf.Sign(transform.localScale.x);
        float thoiGianDaHuc = 0f;
        bool daTrungDon = false;

        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

        while (thoiGianDaHuc < chargeDuration)
        {
            if (isDead || IsNearEdge() || IsHittingWall()) break;

            rb.linearVelocity = new Vector2(huongHuc * chargeSpeed, 0f);

            if (!daTrungDon)
            {
                Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(transform.position, hitRadius);
                foreach (Collider2D p in hitPlayers)
                {
                    if (p.CompareTag("Enemy") || !p.isTrigger) continue;
                    if (p.CompareTag("Player") || p.gameObject.layer == LayerMask.NameToLayer("Player"))
                    {
                        daTrungDon = true;

                        IDamageable target = p.GetComponent<IDamageable>();
                        GameObject targetObj = p.gameObject;

                        if (target == null)
                        {
                            target = p.GetComponentInParent<IDamageable>();
                            if (target != null) targetObj = p.transform.parent.gameObject;
                        }

                        if (target != null)
                        {
                            target.TakeDamage(attackDamage);
                            ApplyAntiHeal(targetObj);
                        }
                    }
                }
            }

            thoiGianDaHuc += Time.deltaTime;
            yield return null;
        }

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = Vector2.zero;

        if (anim != null)
        {
            anim.SetFloat("Speed", 0f);
            anim.Play("Idle");
        }

        yield return new WaitForSeconds(recoveryTime);

        nextAttackTime = Time.time + attackCooldown;
        isBusy = false;
    }

    void ApplyAntiHeal(GameObject targetObj)
    {
        if (targetObj == null) return;
        AntiHeal anti = targetObj.GetComponent<AntiHeal>();
        if (anti == null) anti = targetObj.AddComponent<AntiHeal>();
        anti.thoiGianConLai = antiHealDuration;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        AudioEvents.TriggerSound3D("Enemy", "Wrath", "Hurt", transform.position);
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        AudioEvents.TriggerSound3D("Enemy", "Wrath", "Die", transform.position);

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