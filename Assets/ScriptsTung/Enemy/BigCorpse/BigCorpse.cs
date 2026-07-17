using UnityEngine;
using System.Collections;

public class BigCorpse : MonoBehaviour
{
    [Header("Hoạt ảnh & Hitbox")]
    public Animator anim;
    public GameObject attackHitbox; // Kéo cái GameObject AttackHitbox vào đây

    [Header("Tầm nhìn & Di chuyển")]
    public float moveSpeed = 5f;
    public float detectionRangeX = 12f;
    public float detectionRangeY = 3f;
    public float attackRange = 1.2f;

    [Header("Dịch chuyển an toàn")]
    public float platformHeightDiff = 1.5f;
    public float teleportDelay = 0.5f;
    public float postTeleportDelay = 0.5f;

    [Header("Sát thương")]
    public int attackDamage = 10; // Giờ nó sẽ truyền sang cho Hitbox
    public float attackCooldown = 1.5f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D myCol;
    private EnemyHitbox hitboxScript;

    private float nextAttackTime = 0f;
    private float teleportTimer = 0f;
    private bool isBusy = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();

        if (anim == null) anim = GetComponent<Animator>();

        // Truyền sát thương sang cho script Hitbox
        if (attackHitbox != null)
        {
            hitboxScript = attackHitbox.GetComponent<EnemyHitbox>();
            if (hitboxScript != null) hitboxScript.attackDamage = attackDamage;

            attackHitbox.SetActive(false); // Chắc chắn hitbox đang tắt khi mới bắt đầu
        }
    }

    void Update()
    {
        // Cập nhật thông số Tốc độ cho Animator (để chạy Animation đi bộ/chạy)
        if (anim != null && !isBusy)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }

        if (player == null || isBusy) return;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);

        if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
        {
            if (distanceY > platformHeightDiff)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
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
                float hitDistance = 999f;
                Collider2D playerCol = player.GetComponent<Collider2D>();
                if (myCol != null && playerCol != null)
                    hitDistance = Physics2D.Distance(myCol, playerCol).distance;

                if (hitDistance > attackRange)
                {
                    Move();
                }
                else
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
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
        }
    }

    void Move()
    {
        LookAtPlayer();
        float dir = (player.position.x > transform.position.x) ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    IEnumerator AttackRoutine()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;
        LookAtPlayer();

        // Kích hoạt Animation Đánh
        if (anim != null) anim.SetTrigger("Attack");

        // Đợi một khoảng thời gian vừa đủ để hoàn thành hoạt ảnh đánh
        // (Bạn có thể điều chỉnh 0.8f này cho khớp với độ dài thực tế clip animation của bạn)
        yield return new WaitForSeconds(0.8f);

        // Đảm bảo hitbox đã được tắt (phòng hờ Event bị miss do giật lag)
        DisableHitbox();

        nextAttackTime = Time.time + attackCooldown;
        isBusy = false;
    }

    IEnumerator ThucHienTeleportAnToan()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;

        // Kích hoạt Animation dịch chuyển (tan biến đi)
        if (anim != null) anim.SetTrigger("Teleport");

        // Đợi cho hoạt ảnh biến mất chiếu xong (tùy chỉnh thời gian)
        yield return new WaitForSeconds(0.3f);

        float dirSauLung = (player.localScale.x > 0) ? -1f : 1f;
        Vector2 viTriSauLung = new Vector2(player.position.x + (dirSauLung * 1.2f), player.position.y + 1f);

        RaycastHit2D hit = Physics2D.Raycast(viTriSauLung, Vector2.down, 3f);

        if (hit.collider != null && !hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
            transform.position = new Vector2(viTriSauLung.x, player.position.y);
        else
            transform.position = player.position;

        LookAtPlayer();

        // Đợi một chút sau khi xuất hiện ở vị trí mới
        yield return new WaitForSeconds(postTeleportDelay);
        isBusy = false;
    }

    void LookAtPlayer()
    {
        Vector3 scale = transform.localScale;
        scale.x = (player.position.x > transform.position.x) ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    // =======================================================
    // 2 HÀM NÀY SẼ ĐƯỢC GỌI TỪ ANIMATION EVENT TRONG UNITY
    // =======================================================
    public void EnableHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(true);
        }
    }

    public void DisableHitbox()
    {
        if (attackHitbox != null)
        {
            attackHitbox.SetActive(false);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}