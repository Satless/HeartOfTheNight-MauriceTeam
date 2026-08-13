using UnityEngine;
using System.Collections;
using HeartOfTheNight.Common; // 1. THÊM THƯ VIỆN CHỨA BỘ LUẬT CỦA TEAM

// 2. KẾT NỐI VỚI INTERFACE IDamageable
public class BurningCorpseImg : MonoBehaviour, IDamageable
{
    [Header("Chỉ số Sinh tồn")]
    public int maxHealth = 60;
    public int currentHealth;
    public bool isDead = false;

    [Header("Hoạt ảnh & Vị trí chém (Cục atk)")]
    public Animator anim;
    public GameObject attackHitbox;
    public Vector2 attackOffset = new Vector2(0f, 1f); // Dùng để nâng tâm chém lên cao (trục Y)

    [Header("Tầm nhìn & Di chuyển")]
    public float detectionRangeX = 12f;
    public float detectionRangeY = 3f;
    public float moveSpeed = 4f;
    public float attackRange = 2f;
    public float attackRadius = 1.2f;

    [Header("Dịch chuyển & Cảm biến kẹt")]
    public float platformHeightDiff = 0.8f;
    public float teleportDelay = 1f;
    public float postTeleportDelay = 0.5f;
    public float timeToDetectStuck = 0.5f;

    [Header("Sát thương & Hiệu ứng Cháy")]
    public int attackDamage = 10;
    public float attackCooldown = 2f;
    public int burnDamagePerTick = 2;
    public int burnTicks = 3;
    public float timeBetweenTicks = 1f;
    public float dashSpeedThreshold = 12f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D myCol;
    private float nextAttackTime = 0f;
    private float teleportTimer = 0f;
    private bool isBusy = false;

    private float lastXPos = 0f;
    private float stuckTimer = 0f;


    private float _dmgEffectTimer;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();

        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (anim != null && anim.gameObject != this.gameObject)
        {
            if (anim.GetComponent<HitboxEventForwarder>() == null)
            {
                anim.gameObject.AddComponent<HitboxEventForwarder>();
            }
        }

        currentHealth = maxHealth;
        SetupXuyenThau();
    }

    void SetupXuyenThau()
    {
        Collider2D[] myCols = GetComponentsInChildren<Collider2D>();
        if (player != null)
        {
            Collider2D[] pCols = player.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D myC in myCols)
            {
                if (myC.isTrigger) continue;
                foreach (Collider2D pC in pCols)
                    Physics2D.IgnoreCollision(myC, pC, true);
            }
        }

        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in allEnemies)
        {
            Collider2D[] enemyCols = enemyObj.GetComponentsInChildren<Collider2D>();
            foreach (Collider2D myC in myCols)
            {
                if (myC.isTrigger) continue;
                foreach (Collider2D eC in enemyCols)
                {
                    if (eC.isTrigger) continue;
                    if (myC.gameObject != eC.gameObject)
                        Physics2D.IgnoreCollision(myC, eC, true);
                }
            }
        }
    }

    void Update()
    {
        if (player == null || isBusy || myCol == null || isDead) return;

        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol == null) return;

        float myFeetY = myCol.bounds.min.y;
        float playerFeetY = playerCol.bounds.min.y;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(playerFeetY - myFeetY);

        bool isStuck = false;
        if (distanceX > attackRange)
        {
            if (Mathf.Abs(transform.position.x - lastXPos) < 0.05f)
            {
                stuckTimer += Time.deltaTime;
                if (stuckTimer >= timeToDetectStuck) isStuck = true;
            }
            else stuckTimer = 0f;
        }
        else stuckTimer = 0f;

        lastXPos = transform.position.x;

        if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
        {
            if (distanceY > platformHeightDiff || isStuck)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                if (anim != null) anim.SetFloat("Speed", 0);

                teleportTimer += Time.deltaTime;
                if (teleportTimer >= teleportDelay)
                {
                    StartCoroutine(ThucHienTeleport());
                    teleportTimer = 0f;
                    stuckTimer = 0f;
                }
            }
            else
            {
                teleportTimer = 0f;
                if (distanceX > attackRange)
                {
                    Move();
                }
                else
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    if (anim != null) anim.SetFloat("Speed", 0);

                    if (Time.time >= nextAttackTime)
                    {
                        StartCoroutine(AttackRoutine());
                    }
                }
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
            if (anim != null) anim.SetFloat("Speed", 0);
            stuckTimer = 0f;
        }
    }

    // 3. KHỚP VỚI KHUÔN MẪU IDamageable ĐỂ BỊ ĐÁNH MẤT MÁU
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        SoundManager.Instance.PlaySound3D("Enemy", "HurtGeneral", transform.position);

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

        SoundManager.Instance.PlaySound3D("Enemy", "DeathGeneral", transform.position);
        Destroy(gameObject, 1.5f);
    }

    void Move()
    {
        LookAtPlayer();
        float dir = (player.position.x > transform.position.x) ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);

        if (anim != null) anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));

        SoundManager.Instance.PlaySound3D("Enemy", "MoveGeneral", transform.position);
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

    IEnumerator ThucHienTeleport()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;

        if (anim != null) anim.SetTrigger("Teleport");
        yield return new WaitForSeconds(0.3f);

        if (isDead) yield break;

        float pivotToFeetOffset = transform.position.y - myCol.bounds.min.y;
        float distance = 1.5f;
        float standBehind = (player.localScale.x > 0) ? -distance : distance;

        Vector2 viTriSau = new Vector2(player.position.x + standBehind, player.position.y + 1f);
        Vector2 viTriTruoc = new Vector2(player.position.x - standBehind, player.position.y + 1f);

        RaycastHit2D hitSau = Physics2D.Raycast(viTriSau, Vector2.down, 3f);
        RaycastHit2D hitTruoc = Physics2D.Raycast(viTriTruoc, Vector2.down, 3f);

        if (hitSau.collider != null && !hitSau.collider.CompareTag("Player") && !hitSau.collider.isTrigger)
            transform.position = new Vector2(viTriSau.x, hitSau.point.y + pivotToFeetOffset + 0.05f);
        else if (hitTruoc.collider != null && !hitTruoc.collider.CompareTag("Player") && !hitTruoc.collider.isTrigger)
            transform.position = new Vector2(viTriTruoc.x, hitTruoc.point.y + pivotToFeetOffset + 0.05f);
        else
        {
            Vector2 viTriGiua = new Vector2(player.position.x, player.position.y + 1f);
            RaycastHit2D hitGiua = Physics2D.Raycast(viTriGiua, Vector2.down, 3f);
            if (hitGiua.collider != null)
                transform.position = new Vector2(player.position.x, hitGiua.point.y + pivotToFeetOffset + 0.05f);
            else
            {
                float playerFeet = player.GetComponent<Collider2D>().bounds.min.y;
                transform.position = new Vector2(player.position.x, playerFeet + pivotToFeetOffset);
            }
        }

        LookAtPlayer();
        yield return new WaitForSeconds(postTeleportDelay);
        isBusy = false;
    }

    void LookAtPlayer()
    {
        transform.localScale = new Vector3((player.position.x > transform.position.x ? 1 : -1) * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }

    // ==========================================
    // SÁT THƯƠNG QUÉT VÒNG TRÒN (GỌI TỪ EVENT)
    // ==========================================
    public void EnableHitbox()
    {
        if (isDead || attackHitbox == null) return;

        float facingDirection = Mathf.Sign(transform.localScale.x);
        Vector2 adjustedOffset = new Vector2(attackOffset.x * facingDirection, attackOffset.y);

        Vector2 finalAttackPos = (Vector2)attackHitbox.transform.position + adjustedOffset;
        Collider2D[] hitPlayers = Physics2D.OverlapCircleAll(finalAttackPos, attackRadius);

        foreach (Collider2D p in hitPlayers)
        {
            // 4. CHỐNG CHÉM NHẦM PHE MÌNH
            if (p.CompareTag("Enemy")) continue;

            if (p.CompareTag("Player"))
            {
                // 5. CHUẨN HÓA SANG IDamageable
                IDamageable target = p.GetComponent<IDamageable>();
                if (target != null)
                {
                    DealDamageAndBurn(p); // Truyền luôn Collider2D của Player sang hàm Burn
                    Debug.Log("Xác cháy chém trúng Player qua IDamageable!");
                }
            }
        }
    }

    public void DealDamageAndBurn(Collider2D playerCol)
    {
        if (isDead) return;

        IDamageable target = playerCol.GetComponent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(attackDamage);
            StartCoroutine(GayHieuUngChay(playerCol));
        }
    }

    IEnumerator GayHieuUngChay(Collider2D playerCol)
    {
        Rigidbody2D playerRb = playerCol.GetComponent<Rigidbody2D>();
        IDamageable target = playerCol.GetComponent<IDamageable>();

        for (int i = 0; i < burnTicks; i++)
        {
            float thoiGianDaCho = 0f;
            while (thoiGianDaCho < timeBetweenTicks)
            {
                // Giữ nguyên logic dập lửa khi lướt (dash)
                if (playerRb != null && Mathf.Abs(playerRb.linearVelocity.x) >= dashSpeedThreshold) yield break;
                thoiGianDaCho += Time.deltaTime;
                yield return null;
            }

            // Gây sát thương thiêu đốt thông qua IDamageable
            if (target != null) target.TakeDamage(burnDamagePerTick);
            else yield break;

            _dmgEffectTimer -= Time.fixedDeltaTime;
            if (_dmgEffectTimer <= 0f)
            {
                SoundManager.Instance.PlaySound3D("Enemy", "DmgEffectGeneral", transform.position);
                _dmgEffectTimer = 0.2f; 
            }
        }
    }

    public void DisableHitbox() { }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));

        if (attackHitbox != null)
        {
            Gizmos.color = Color.red;
            float facingDirection = Mathf.Sign(transform.localScale.x);
            Vector2 adjustedOffset = new Vector2(attackOffset.x * facingDirection, attackOffset.y);

            Vector2 finalAttackPos = (Vector2)attackHitbox.transform.position + adjustedOffset;
            Gizmos.DrawWireSphere(finalAttackPos, attackRadius);
        }
    }
}

// KHÔNG XÓA CLASS NÀY - Bắt buộc phải có để nhận Animation Event từ Object con
public class HitboxEventForwarder : MonoBehaviour
{
    public void EnableHitbox()
    {
        SendMessageUpwards("EnableHitbox", SendMessageOptions.DontRequireReceiver);
    }

    public void DisableHitbox()
    {
        SendMessageUpwards("DisableHitbox", SendMessageOptions.DontRequireReceiver);
    }
}