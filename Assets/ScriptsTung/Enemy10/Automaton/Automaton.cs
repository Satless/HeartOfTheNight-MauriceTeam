using UnityEngine;
using System.Collections;
using HeartOfTheNight.Common;
using HeartOfTheNight.Enemy;

public class Automaton : MonoBehaviour, IDamageable, IKnockbackGate
{
    [Header("Chỉ số Sinh tồn")]
    public int maxHealth = 550;
    public int currentHealth;
    public bool isDead = false;

    [Header("Hoạt ảnh & Hình ảnh")]
    public Animator anim;
    public SpriteRenderer sr;
    public Sprite dashAttackSprite;

    [Header("Tầm nhìn & Di chuyển")]
    public float moveSpeed = 4.5f;
    public float detectionRangeX = 12f;
    public float detectionRangeY = 8f;
    public float dashRange = 5.5f;
    public float attackRange = 2f;
    public Vector2 attackOffset = new Vector2(0f, 1f);

    [Header("Tuần tra (Patrol)")]
    public float patrolSpeed = 2f;
    public float patrolDistance = 5f;
    private float startX;
    private float flipCooldown = 0.5f;
    private float flipTimer = 0f;

    [Header("Kiểm tra Mặt đất (Dùng Layer)")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public bool isGrounded;

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
    public float meleeCooldown = 1.6f;

    [Header("Hệ thống chống kẹt")]
    public float stuckTimeLimit = 1.2f;
    private float stuckTimer = 0f;
    private float lastXPos = 0f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D myCol;
    private Collider2D playerBodyCol;
    private IDamageable playerDamageable;

    private bool dangBanRaDon = false;
    private float nextDashTime = 0f;
    private float nextMeleeTime = 0f;
    private float teleportTimer = 0f;


    private float idleTimer;
    private float _footstepTimer;
    private KnockbackReceiver knockback;

    public bool CanReceiveKnockback => !isDead && !dangBanRaDon;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        knockback = GetComponent<KnockbackReceiver>();
        myCol = GetComponent<Collider2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;
    }

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform.root;

        if (currentHealth <= 0 || currentHealth > maxHealth)
            currentHealth = maxHealth;

        startX = transform.position.x;

        CachePlayerRefs();
        SetupXuyenThau();
    }

    void CachePlayerRefs()
    {
        if (player == null) return;
        playerDamageable = player.GetComponent<IDamageable>();
        if (playerDamageable == null)
            playerDamageable = player.GetComponentInChildren<IDamageable>();
        playerBodyCol = ResolvePlayerBodyCollider(player);
    }

    static Collider2D ResolvePlayerBodyCollider(Transform playerRoot)
    {
        Collider2D onRoot = playerRoot.GetComponent<Collider2D>();
        if (onRoot != null && !onRoot.isTrigger) return onRoot;

        Collider2D[] cols = playerRoot.GetComponentsInChildren<Collider2D>();
        Collider2D fallbackTrigger = null;
        for (int i = 0; i < cols.Length; i++)
        {
            Collider2D col = cols[i];
            if (col == null) continue;
            if (!col.isTrigger) return col;
            if (fallbackTrigger == null) fallbackTrigger = col;
        }
        return onRoot != null ? onRoot : fallbackTrigger;
    }

    void SetupXuyenThau()
    {
        if (myCol == null) return;
        if (player != null)
        {
            Collider2D[] playerCols = player.GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < playerCols.Length; i++)
            {
                if (playerCols[i] != null) Physics2D.IgnoreCollision(myCol, playerCols[i], true);
            }
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
        if (isDead || myCol == null) return;

        CheckGroundStatus();
        if (flipTimer > 0) flipTimer -= Time.deltaTime;

        if (anim != null && !dangBanRaDon && anim.enabled)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }

        if (dangBanRaDon) return;
        if (knockback != null && knockback.IsKnockedBack) return;

        // Chỉ khóa AI khi đang rơi. Đứng yên trên tilemap Default / composite outline
        // thì OverlapCircle(Ground) thường miss — đừng vì thế đứng im cả đời.
        bool falling = rb != null && rb.linearVelocity.y < -0.35f;
        if (!isGrounded && falling)
        {
            StopMoving();
            return;
        }

        if (player != null)
        {
            if (playerBodyCol == null) CachePlayerRefs();

            if (playerBodyCol != null)
            {
                float myFeetY = myCol.bounds.min.y;
                float playerFeetY = playerBodyCol.bounds.min.y;
                float distanceX = Mathf.Abs(player.position.x - transform.position.x);
                float distanceY = Mathf.Abs(playerFeetY - myFeetY);

                if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
                {
                    ChasePlayerLogic(distanceX, distanceY);
                    return;
                }
            }
        }

        Patrol();
    }

    void ChasePlayerLogic(float distanceX, float distanceY)
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
                if (Time.time >= nextMeleeTime) StartCoroutine(ThucHienDanhThuong());
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

    void Patrol()
    {
        float distanceFromStart = Mathf.Abs(transform.position.x - startX);
        float currentDir = Mathf.Sign(transform.localScale.x);
        float dirToStart = Mathf.Sign(startX - transform.position.x);

        if (flipTimer <= 0f)
        {
            if (IsHittingWallOrPit() || (distanceFromStart >= patrolDistance && currentDir != dirToStart))
            {
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
                currentDir = Mathf.Sign(transform.localScale.x);

                if (distanceFromStart >= patrolDistance) startX = transform.position.x;
                flipTimer = flipCooldown;
            }
        }

        rb.linearVelocity = new Vector2(currentDir * patrolSpeed, rb.linearVelocity.y);

 
        idleTimer -= Time.deltaTime;
        if (idleTimer <= 0f)
        {
            //SoundManager.Instance.PlaySound3D("Player", "Run", transform.position);

            AudioEvents.TriggerSound3D("Enemy", "Automation", "Idle", transform.position);
            idleTimer = 2f;
        }
    }

    int GroundMask()
    {
        int mask = groundLayer.value;
        mask |= LayerMask.GetMask("Default", "Ground", "Wall");
        return mask == 0 ? Physics2D.AllLayers : mask;
    }

    bool IsSolidHit(RaycastHit2D hit)
    {
        Collider2D col = hit.collider;
        if (col == null || !col.enabled || col.isTrigger) return false;
        if (myCol != null && (col == myCol || col.transform.root == transform.root)) return false;
        if (col.CompareTag("Player") || col.CompareTag("Enemy")) return false;
        return true;
    }

    bool RaycastSolid(Vector2 origin, Vector2 direction, float distance, out RaycastHit2D solidHit)
    {
        solidHit = default;
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, direction, distance, GroundMask());
        for (int i = 0; i < hits.Length; i++)
        {
            if (!IsSolidHit(hits[i])) continue;
            solidHit = hits[i];
            return true;
        }
        return false;
    }

    bool IsHittingWallOrPit()
    {
        float huong = Mathf.Sign(transform.localScale.x);
        Vector2 origin = myCol.bounds.center;
        float doRongQuai = myCol.bounds.extents.x;
        Vector2 bottomFront = new Vector2(origin.x + (huong * doRongQuai), myCol.bounds.min.y);

        bool wall = RaycastSolid(origin, Vector2.right * huong, doRongQuai + 0.5f, out _);
        bool hasFloor = RaycastSolid(bottomFront + new Vector2(huong * 0.2f, 0f), Vector2.down, 1.5f, out _);
        return wall || !hasFloor;
    }

    void CheckGroundStatus()
    {
        Vector2 feet;
        if (groundCheck != null)
            feet = groundCheck.position;
        else if (myCol != null)
            feet = new Vector2(transform.position.x, myCol.bounds.min.y);
        else
            feet = transform.position;

        Vector2 origin = feet + Vector2.up * 0.15f;
        float dist = Mathf.Max(0.4f, groundCheckRadius + 0.25f);
        isGrounded = RaycastSolid(origin, Vector2.down, dist, out _);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        Debug.Log("Automaton nhận " + damage + " sát thương! Máu: " + currentHealth + "/" + maxHealth);
        if (currentHealth <= 0) Die();

        AudioEvents.TriggerSound3D("Enemy", "Automation", "Hurt", transform.position);
    }

    void Die()
    {
        isDead = true;
        dangBanRaDon = true;

        AudioEvents.TriggerSound3D("Enemy", "Automation", "Die", transform.position);

        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (myCol == null) myCol = GetComponent<Collider2D>();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        gameObject.tag = "Untagged";
        if (myCol != null) myCol.enabled = false;

        if (anim != null)
        {
            anim.enabled = true;
            anim.SetTrigger("Dead");
        }
        Destroy(gameObject, 2f);
    }

    void Move()
    {
        LookAtPlayer();
        float huong = (player.position.x > transform.position.x) ? 1f : -1f;
        rb.linearVelocity = new Vector2(huong * moveSpeed, rb.linearVelocity.y);

        _footstepTimer -= Time.deltaTime;
        if (_footstepTimer <= 0f)
        {
            //SoundManager.Instance.PlaySound3D("Player", "Run", transform.position);

            AudioEvents.TriggerSound3D("Enemy", "Automation", "Move", transform.position);
            _footstepTimer = 0.2f;
        }
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
        if (!RaycastSolid(new Vector2(xPos, player.position.y + 2f), Vector2.down, 15f, out RaycastHit2D hit))
            return false;

        float distToFeet = transform.position.y - myCol.bounds.min.y;
        groundY = hit.point.y + distToFeet + teleportYOffset;
        return true;
    }

    IEnumerator ThucHienDanhThuong()
    {
        dangBanRaDon = true;
        StopMoving();
        LookAtPlayer();

        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        if (anim != null) anim.SetTrigger("Attack");

        AudioEvents.TriggerSound3D("Enemy", "Automation", "Attack", transform.position);

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

        // 🔥 ĐÃ FIX 2: Bỏ đóng băng trục Y. Lướt ra mép vực là trọng lực sẽ tự kéo nó rớt xuống!
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        if (anim != null) anim.SetFloat("Speed", dashSpeed);

        AudioEvents.TriggerSound3D("Enemy", "Automation", "Dash", transform.position);

        float thoiGianDaLuot = 0f;
        bool daTrungDon = false;
        float doRongQuai = myCol.bounds.extents.x;

        while (thoiGianDaLuot < dashDuration)
        {
            if (isDead) break;

            // Vẫn cấp lực đẩy ngang, nhưng giữ nguyên vận tốc rơi (Y)
            rb.linearVelocity = new Vector2(huongLuot * dashSpeed, rb.linearVelocity.y);

            Vector2 origin = myCol.bounds.center;

            // 🔥 ĐÃ FIX: Chỉ kiểm tra đụng tường, bỏ cái kiểm tra hố sâu đi để nó tự do rơi
            if (RaycastSolid(origin, Vector2.right * huongLuot, doRongQuai + 0.5f, out _)) break;

            if (!daTrungDon)
            {
                float dashHitRadius = doRongQuai + 0.6f;
                Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(origin, dashHitRadius);

                foreach (Collider2D p in hitPlayers)
                {
                    if (!EnemyCombatRules.TryGetPlayerDamageable(p, out IDamageable target)) continue;

                    daTrungDon = true;
                    target.TakeDamage(dashDamage);

                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);

                    if (anim != null) anim.enabled = false;
                    if (sr != null && dashAttackSprite != null) sr.sprite = dashAttackSprite;

                    break;
                }
            }

            if (daTrungDon) break;

            thoiGianDaLuot += Time.deltaTime;
            yield return null;
        }

        if (daTrungDon && !isDead)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            yield return new WaitForSeconds(0.5f);
        }

        if (!isDead)
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;

            if (anim != null)
            {
                anim.enabled = true;
                anim.SetFloat("Speed", 0f);
                // 🔥 ĐÃ FIX 1: Ép Animator tỉnh lại ngay lập tức, không bị kẹt lướt đứng im
                try { anim.Play("autoIdle", -1, 0f); } catch { }
            }

            nextDashTime = Time.time + dashCooldown;
            dangBanRaDon = false; // Nhả khóa để AI hoạt động tiếp
        }
    }

    public void DealMeleeDamage()
    {
        if (player == null || isDead) return;

        float facingDirection = Mathf.Sign(transform.localScale.x);
        Vector2 adjustedOffset = new Vector2(attackOffset.x * facingDirection, attackOffset.y);

        Vector2 finalAttackPos = (Vector2)transform.position + adjustedOffset;
        float hitRadius = Mathf.Max(0.6f, attackOffset.magnitude + 0.35f);
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(finalAttackPos, hitRadius);

        foreach (Collider2D p in hitPlayers)
        {
            if (!EnemyCombatRules.TryGetPlayerDamageable(p, out IDamageable target)) continue;
            target.TakeDamage(meleeDamage);
            break;
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
        float hitRadius = Mathf.Max(0.6f, attackOffset.magnitude + 0.35f);
        Gizmos.DrawWireSphere(finalAttackPos, hitRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, dashRange);

        if (groundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}