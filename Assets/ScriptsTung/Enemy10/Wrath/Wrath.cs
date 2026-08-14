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

    [Header("Kiểm tra Mặt đất (Dùng Layer)")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public bool isGrounded;

    [Header("Tầm nhìn (Quét Ngang & Dọc)")]
    public float moveSpeed = 5f;
    public float detectionRangeX = 12f;
    public float detectionRangeY = 2.5f;

    [Tooltip("Khoảng cách bắt đầu gầm để lấy đà húc")]
    public float attackRange = 4.5f;

    [Header("Dịch chuyển an toàn")]
    public float platformHeightDiff = 1.5f;
    public float teleportDelay = 0.5f;
    public float postTeleportDelay = 0.5f;

    [Header("Sát thương & Cú Húc (Charge)")]
    public int attackDamage = 15;
    public float attackCooldown = 2f;
    public float antiHealDuration = 6f;

    [Space]
    public float chargeWindupTime = 0.6f; // Gầm lấy đà
    public float chargeSpeed = 18f;       // Tốc độ lướt
    public float chargeDuration = 0.35f;  // Thời gian lướt
    public float hitRadius = 1.5f;        // Bán kính trúng đòn

    [Tooltip("Thời gian đứng im nghỉ mệt (thở dốc) sau khi húc xong")]
    public float recoveryTime = 1.5f;     // <--- BIẾN MỚI ĐỂ SẾP CHỈNH THỜI GIAN ĐỨNG CHỜ

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D myCol;
    private float nextAttackTime = 0f;
    private float teleportTimer = 0f;
    private bool isBusy = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        currentHealth = maxHealth;
        SetupXuyenThau();
    }

    void SetupXuyenThau()
    {
        if (myCol == null || player == null) return;
        Collider2D[] pCols = player.GetComponentsInChildren<Collider2D>();
        foreach (Collider2D pC in pCols)
        {
            if (!pC.isTrigger) Physics2D.IgnoreCollision(myCol, pC, true);
        }
    }

    void Update()
    {
        if (player == null || isBusy || isDead) return;

        CheckGroundStatus();

        if (!isGrounded)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetFloat("Speed", 0f);
            return;
        }

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);

        if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
        {
            if (distanceY > platformHeightDiff)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                if (anim != null) anim.SetFloat("Speed", 0f);

                teleportTimer += Time.deltaTime;
                if (teleportTimer >= teleportDelay)
                {
                    StartCoroutine(ThucHienTeleportAnToan());
                    teleportTimer = 0f;
                }
            }
            else
            {
                teleportTimer = 0f;

                if (distanceX <= attackRange)
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    if (anim != null) anim.SetFloat("Speed", 0f);

                    if (Time.time >= nextAttackTime)
                    {
                        StartCoroutine(ThucHienHucRoutine());
                    }
                }
                else
                {
                    Move();
                }
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetFloat("Speed", 0f);
        }
    }

    void CheckGroundStatus()
    {
        if (groundCheck == null) return;
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void Move()
    {
        LookAtPlayer();
        float dir = (player.position.x > transform.position.x) ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);

        if (anim != null) anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
    }

    IEnumerator ThucHienHucRoutine()
    {
        isBusy = true; // Khóa toàn bộ các hoạt động khác
        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetFloat("Speed", 0f);
        LookAtPlayer();

        // 1. GẦM BÁO HIỆU
        if (anim != null) anim.SetTrigger("Roar");
        yield return new WaitForSeconds(chargeWindupTime);

        if (isDead) yield break;

        // 2. PHÓNG ĐI HÚC
        if (anim != null) anim.SetTrigger("Rush");

        float huongHuc = Mathf.Sign(transform.localScale.x);
        float thoiGianDaHuc = 0f;
        bool daTrungDon = false;

        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

        while (thoiGianDaHuc < chargeDuration)
        {
            if (isDead) break;

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
                        if (target == null) target = p.GetComponentInParent<IDamageable>();

                        if (target != null)
                        {
                            target.TakeDamage(attackDamage);
                            ApplyAntiHeal();
                        }
                    }
                }
            }

            thoiGianDaHuc += Time.deltaTime;
            yield return null;
        }

        // 3. DỪNG LẠI KHI HÚC XONG
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = Vector2.zero;

        if (anim != null)
        {
            anim.SetFloat("Speed", 0f);
            anim.Play("Idle");
        }

        // ==========================================
        // 4. NGHỈ MỆT BẮT BUỘC DÙ TRÚNG HAY TRƯỢT
        // ==========================================
        Debug.Log("Wrath: Húc xong mệt quá, đứng thở " + recoveryTime + " giây...");
        yield return new WaitForSeconds(recoveryTime);

        // Nghỉ xong mới mở khóa cho quái hoạt động tiếp
        nextAttackTime = Time.time + attackCooldown;
        isBusy = false;
    }

    void ApplyAntiHeal()
    {
        if (player == null) return;

        AntiHeal anti = player.GetComponent<AntiHeal>();
        if (anti == null)
        {
            anti = player.gameObject.AddComponent<AntiHeal>();
        }
        anti.thoiGianConLai = antiHealDuration;
    }

    IEnumerator ThucHienTeleportAnToan()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;
        if (anim != null) anim.SetFloat("Speed", 0f);

        if (anim != null) anim.SetTrigger("Jump");

        yield return new WaitForSeconds(0.3f);

        float dirSauLung = (player.localScale.x > 0) ? -1f : 1f;
        Vector2 viTriSauLung = new Vector2(player.position.x + (dirSauLung * 1.2f), player.position.y + 1f);

        RaycastHit2D hit = Physics2D.Raycast(viTriSauLung, Vector2.down, 3f);

        if (hit.collider != null && !hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
            transform.position = new Vector2(viTriSauLung.x, player.position.y);
        else
            transform.position = player.position;

        LookAtPlayer();

        if (anim != null) anim.Play("Idle");

        yield return new WaitForSeconds(postTeleportDelay);
        isBusy = false;
    }

    void LookAtPlayer()
    {
        Vector3 scale = transform.localScale;
        if (player.position.x > transform.position.x) scale.x = Mathf.Abs(scale.x);
        else scale.x = -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

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

        if (anim != null)
        {
            anim.enabled = true;
            anim.SetTrigger("Dead");
        }

        Destroy(gameObject, 1.5f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hitRadius);

        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}