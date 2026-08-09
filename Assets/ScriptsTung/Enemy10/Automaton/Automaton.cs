using UnityEngine;
using System.Collections;
using HeartOfTheNight.Common; // 1. GỌI THƯ VIỆN CHỨA BỘ LUẬT CỦA TEAM

// 2. THAY NhanSatThuong THÀNH IDamageable
public class Automaton : MonoBehaviour, IDamageable
{
    [Header("Chỉ số Sinh tồn")]
    public int maxHealth = 150;
    public int currentHealth;
    public bool isDead = false;

    [Header("Hoạt ảnh & Hình ảnh")]
    public Animator anim;
    public SpriteRenderer sr;

    [Tooltip("Ảnh hiển thị khi lướt trúng người")]
    public Sprite dashAttackSprite;

    [Header("Tầm nhìn & Di chuyển")]
    public float moveSpeed = 4f;
    public float detectionRangeX = 12f;
    public float detectionRangeY = 3f;
    public float dashRange = 5.5f;
    public float attackRange = 2f;
    public Vector2 attackOffset = new Vector2(0f, 1f);

    [Header("Dịch chuyển an toàn")]
    public float platformHeightDiff = 0.8f;
    public float teleportDelay = 0.5f;
    public float postTeleportDelay = 0.6f;
    public float teleportYOffset = 0f;

    [Header("Sát thương")]
    public int meleeDamage = 10;
    public int dashDamage = 20;

    [Header("Chỉ số Lướt (Dash)")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.35f;
    public float dashCooldown = 4f;
    public float meleeCooldown = 2f;

    [Header("Hệ thống chống kẹt")]
    public float stuckTimeLimit = 1.2f;
    private float stuckTimer = 0f;
    private float lastXPos = 0f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D myCol;

    private bool dangBanRaDon = false;
    private float nextDashTime = 0f;
    private float nextMeleeTime = 0f;
    private float teleportTimer = 0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();

        if (anim == null) anim = GetComponent<Animator>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();

        currentHealth = maxHealth;
        SetupXuyenThau();
    }

    void SetupXuyenThau()
    {
        if (myCol == null) return;

        if (player != null)
        {
            Collider2D pCol = player.GetComponent<Collider2D>();
            if (pCol != null) Physics2D.IgnoreCollision(myCol, pCol, true);
        }

        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in allEnemies)
        {
            Collider2D enemyCol = enemyObj.GetComponent<Collider2D>();
            if (enemyCol != null && enemyCol != myCol)
            {
                Physics2D.IgnoreCollision(myCol, enemyCol, true);
            }
        }
    }

    void Update()
    {
        if (isDead || player == null || myCol == null) return;

        if (anim != null && !dangBanRaDon && anim.enabled)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }

        if (dangBanRaDon) return;

        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol == null) return;

        float myFeetY = myCol.bounds.min.y;
        float playerFeetY = playerCol.bounds.min.y;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(playerFeetY - myFeetY);

        if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
        {
            if (distanceY > platformHeightDiff || (distanceX <= attackRange && distanceY > 0.8f))
            {
                StopMoving();
                teleportTimer += Time.deltaTime;
                if (teleportTimer >= teleportDelay)
                {
                    StartCoroutine(ThucHienTeleport());
                    teleportTimer = 0f;
                }
            }
            else
            {
                teleportTimer = 0f;

                if (distanceX <= attackRange)
                {
                    StopMoving();
                    if (Time.time >= nextMeleeTime)
                    {
                        StartCoroutine(ThucHienDanhThuong());
                    }
                }
                else if (distanceX <= dashRange && distanceX > attackRange && Time.time >= nextDashTime)
                {
                    StopMoving();
                    StartCoroutine(ThucHienLuot());
                }
                else
                {
                    Move();

                    if (Mathf.Abs(transform.position.x - lastXPos) < 0.01f)
                    {
                        stuckTimer += Time.deltaTime;
                        if (stuckTimer >= stuckTimeLimit)
                        {
                            StartCoroutine(ThucHienTeleport());
                            stuckTimer = 0f;
                        }
                    }
                    else
                    {
                        stuckTimer = 0f;
                    }
                    lastXPos = transform.position.x;
                }
            }
        }
        else
        {
            StopMoving();
        }
    }

    // 3. HÀM NÀY TỰ ĐỘNG KHỚP VỚI KHUÔN MẪU CỦA IDamageable
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        Debug.Log("Automaton nhận " + damage + " sát thương! Máu: " + currentHealth + "/" + maxHealth);

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        dangBanRaDon = true;

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

    void Move()
    {
        LookAtPlayer();
        float huong = (player.position.x > transform.position.x) ? 1f : -1f;
        rb.linearVelocity = new Vector2(huong * moveSpeed, rb.linearVelocity.y);
    }

    void StopMoving()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    void LookAtPlayer()
    {
        transform.localScale = new Vector3((player.position.x > transform.position.x ? 1 : -1) * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }

    IEnumerator ThucHienTeleport()
    {
        dangBanRaDon = true;
        StopMoving();

        if (anim != null) anim.SetTrigger("Teleport");
        yield return new WaitForSeconds(0.3f);

        if (isDead) yield break;

        transform.position = TimViTriTeleport();
        LookAtPlayer();

        yield return new WaitForSeconds(postTeleportDelay);
        dangBanRaDon = false;
    }

    Vector2 TimViTriTeleport()
    {
        float standBehind = (player.localScale.x > 0) ? -1f : 1f;
        float targetX = player.position.x + standBehind;
        if (ThuTimDat(targetX, out float groundY)) return new Vector2(targetX, groundY);
        float standFront = -standBehind;
        float targetX_Front = player.position.x + standFront;
        if (ThuTimDat(targetX_Front, out float groundY_Front)) return new Vector2(targetX_Front, groundY_Front);
        return new Vector2(player.position.x, player.position.y);
    }

    bool ThuTimDat(float xPos, out float groundY)
    {
        groundY = 0f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(xPos, player.position.y + 2f), Vector2.down, 5f);
        foreach (RaycastHit2D hit in hits)
        {
            if (!hit.collider.CompareTag("Player") && hit.collider.gameObject.layer != LayerMask.NameToLayer("Enemy") && !hit.collider.isTrigger)
            {
                groundY = hit.point.y + GetComponent<Collider2D>().bounds.extents.y + teleportYOffset;
                return true;
            }
        }
        return false;
    }

    IEnumerator ThucHienDanhThuong()
    {
        dangBanRaDon = true;
        StopMoving();
        LookAtPlayer();

        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        if (anim != null) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(1.5f);

        if (isDead) yield break;

        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        nextMeleeTime = Time.time + meleeCooldown;
        dangBanRaDon = false;
    }

    IEnumerator ThucHienLuot()
    {
        dangBanRaDon = true;
        LookAtPlayer();

        float huongLuot = Mathf.Sign(transform.localScale.x);
        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = new Vector2(huongLuot * dashSpeed, 0f);

        if (anim != null) anim.SetFloat("Speed", dashSpeed);

        float thoiGianDaLuot = 0f;
        bool daTrungDon = false;
        float doRongQuai = myCol.bounds.extents.x;

        while (thoiGianDaLuot < dashDuration)
        {
            if (isDead) break;

            Vector2 origin = myCol.bounds.center;
            Vector2 bottomFront = new Vector2(origin.x + (huongLuot * doRongQuai), myCol.bounds.min.y);

            RaycastHit2D wallHit = Physics2D.Raycast(origin, Vector2.right * huongLuot, doRongQuai + 0.5f);
            RaycastHit2D pitHit = Physics2D.Raycast(bottomFront + new Vector2(huongLuot * 0.2f, 0), Vector2.down, 1.5f);

            bool thayTuong = (wallHit.collider != null && !wallHit.collider.isTrigger && !wallHit.collider.CompareTag("Player") && !wallHit.collider.CompareTag("Enemy"));
            bool truotChan = (pitHit.collider == null);

            if (thayTuong || truotChan) break;

            if (!daTrungDon && Mathf.Abs(player.position.x - transform.position.x) <= attackRange)
            {
                daTrungDon = true;

                Collider2D pCol = player.GetComponent<Collider2D>();
                if (pCol != null && pCol.CompareTag("Player"))
                {
                    IDamageable target = pCol.GetComponent<IDamageable>();
                    if (target != null)
                    {
                        target.TakeDamage(dashDamage);
                    }
                }

                if (anim != null) anim.enabled = false;
                if (sr != null && dashAttackSprite != null) sr.sprite = dashAttackSprite;
            }

            thoiGianDaLuot += Time.deltaTime;
            yield return null;
        }

        if (!isDead)
        {
            rb.linearVelocity = Vector2.zero;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (anim != null)
            {
                anim.enabled = true;
                anim.SetFloat("Speed", 0f);
            }

            nextDashTime = Time.time + dashCooldown;
            dangBanRaDon = false;
        }
    }

    public void DealMeleeDamage()
    {
        if (player == null || isDead) return;

        float facingDirection = Mathf.Sign(transform.localScale.x);
        Vector2 adjustedOffset = new Vector2(attackOffset.x * facingDirection, attackOffset.y);

        Vector2 finalAttackPos = (Vector2)transform.position + adjustedOffset;
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(finalAttackPos, attackRange);

        foreach (Collider2D p in hitPlayers)
        {
            if (p.CompareTag("Enemy")) continue;

            if (p.CompareTag("Player"))
            {
                IDamageable target = p.GetComponent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(meleeDamage);
                    Debug.Log("Automaton chém thường trúng Player qua IDamageable!");
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));

        Gizmos.color = Color.red;
        float facingDirection = Mathf.Sign(transform.localScale.x);
        Vector2 adjustedOffset = new Vector2(attackOffset.x * facingDirection, attackOffset.y);

        Vector2 finalAttackPos = (Vector2)transform.position + adjustedOffset;
        Gizmos.DrawWireSphere(finalAttackPos, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, dashRange);
    }
}