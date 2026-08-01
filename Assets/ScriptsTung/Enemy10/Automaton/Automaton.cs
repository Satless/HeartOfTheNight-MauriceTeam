using UnityEngine;
using System.Collections;

public class Automaton : MonoBehaviour
{
    [Header("Hoạt ảnh & Hình ảnh")]
    public Animator anim;
    public SpriteRenderer sr;
<<<<<<< HEAD:Assets/ScriptsTung/Enemy10/Automaton/Automaton.cs
    public Sprite dashAttackSprite;/////
=======
    [Tooltip("Ảnh hiển thị khi lướt trúng người")]
    public Sprite dashAttackSprite;
>>>>>>> 1c33f729c40d0dca5d358e60c0fedca93ec1ebb8:Assets/ScriptsTung/Enemy/Automaton/Automaton.cs

    // ĐÃ XÓA SẠCH CÁC BIẾN HITBOX LẰNG NHẰNG Ở ĐÂY

    [Header("Tầm nhìn & Di chuyển")]
    public float moveSpeed = 4f;
    public float detectionRangeX = 12f;
    public float detectionRangeY = 3f;
    public float dashRange = 5.5f;
    public float attackRange = 2f;

    [Header("Dịch chuyển an toàn")]
    public float platformHeightDiff = 0.8f;
    public float teleportDelay = 0.5f;
    public float postTeleportDelay = 0.6f;
    public float teleportYOffset = 0f;

    [Header("Sát thương")]
    public int meleeDamage = 10; // Giờ sẽ dùng trực tiếp biến này luôn!
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
        if (anim != null && !dangBanRaDon && anim.enabled)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }

        if (player == null || dangBanRaDon || myCol == null) return;

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

    // ==========================================
    // CHIẾN ĐẬU
    // ==========================================

    IEnumerator ThucHienDanhThuong()
    {
        dangBanRaDon = true;
        StopMoving();
        LookAtPlayer();

        // CHÌA KHÓA Ở ĐÂY: Khóa cứng trục X để quái đứng im phăng phắc, không bị trượt hay bị đẩy
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        if (anim != null) anim.SetTrigger("Attack");

        // Ngồi chờ cho clip chiếu xong
        yield return new WaitForSeconds(2f);

        // TRẢ LẠI TỰ DO: Mở khóa trục X để quái có thể đi bộ đuổi theo Player tiếp
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
                player.GetComponent<PlayerHealth>()?.TakeDamage(dashDamage);

                if (anim != null) anim.enabled = false;
                if (sr != null && dashAttackSprite != null) sr.sprite = dashAttackSprite;
            }

            thoiGianDaLuot += Time.deltaTime;
            yield return null;
        }

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

    // =======================================================
    // ANIMATION EVENT: XỬ LÝ CHÉM THƯỜNG TRỰC TIẾP BẰNG CODE
    // =======================================================

    // Gọi hàm này trong bảng Animation ở frame mà kiếm vung xuống
    public void DealMeleeDamage()
    {
        if (player == null) return;

        // Nếu Player đứng trong tầm chém lúc tay vung xuống -> Trừ máu!
        if (Mathf.Abs(player.position.x - transform.position.x) <= attackRange + 0.5f)
        {
            player.GetComponent<PlayerHealth>()?.TakeDamage(meleeDamage);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, dashRange);
    }
}