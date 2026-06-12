using UnityEngine;
using System.Collections;

public class Wrath : MonoBehaviour
{
    [Header("Tầm nhìn (Quét Ngang & Dọc)")]
    public float moveSpeed = 7f;
    public float detectionRangeX = 12f;
    public float detectionRangeY = 2.5f;

    [Tooltip("HÃY SET TỪ 1.5 ĐẾN 2.0 ĐỂ NÓ DỪNG LẠI CHÉM TRƯỚC KHI ĐỤNG BỤNG VÀO BẠN")]
    public float attackRange = 1.8f;

    [Header("Dịch chuyển an toàn")]
    public float platformHeightDiff = 1.5f;
    public float teleportDelay = 0.5f;
    public float postTeleportDelay = 0.5f;

    [Header("Sát thương & Combo")]
    public int attackDamage = 15;
    public float attackCooldown = 2f;
    public float timeBetweenHits = 0.3f;
    public float antiHealDuration = 6f;

    private Transform player;
    private Rigidbody2D rb;
    private float nextAttackTime = 0f;
    private float teleportTimer = 0f;
    private bool isBusy = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
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

                // Nếu lọt vào tầm đánh (khoảng cách 1.8m) -> PHANH GẤP VÀ CHÉM
                if (distanceX <= attackRange)
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Khóa vận tốc lập tức

                    if (Time.time >= nextAttackTime)
                    {
                        StartCoroutine(AttackCombo());
                    }
                }
                else
                {
                    // Vẫn ở xa thì rượt đuổi
                    Move();
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

    IEnumerator AttackCombo()
    {
        isBusy = true; // Khóa di chuyển hoàn toàn
        rb.linearVelocity = Vector2.zero;
        LookAtPlayer();

        yield return new WaitForSeconds(0.1f); // Khựng lại một nhịp nhỏ như Dead Cells

        for (int i = 0; i < 3; i++)
        {
            rb.linearVelocity = Vector2.zero; // Ép dừng mọi frame khi chém
            AttackHit();
            yield return new WaitForSeconds(timeBetweenHits);
        }

        nextAttackTime = Time.time + attackCooldown;
        isBusy = false;
    }

    void AttackHit()
    {
        // Cộng thêm 0.5m để lưỡi kiếm dài hơn tầm dừng lại một chút
        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        if (distanceX <= attackRange + 0.5f)
        {
            PlayerHealth hp = player.GetComponent<PlayerHealth>();
            if (hp != null) hp.TakeDamage(attackDamage);

            AntiHeal anti = player.GetComponent<AntiHeal>();
            if (anti == null) anti = player.gameObject.AddComponent<AntiHeal>();
            anti.thoiGianConLai = antiHealDuration;
        }
    }

    // --- HỆ THỐNG TELEPORT CHỐNG RỚT VỰC ---
    IEnumerator ThucHienTeleportAnToan()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;

        // Tính vị trí đằng sau lưng 1.2 mét
        float dirSauLung = (player.localScale.x > 0) ? -1f : 1f;
        Vector2 viTriSauLung = new Vector2(player.position.x + (dirSauLung * 1.2f), player.position.y + 1f);

        // Bắn tia raycast xuống kiểm tra xem sau lưng có ĐẤT không
        RaycastHit2D hit = Physics2D.Raycast(viTriSauLung, Vector2.down, 3f);

        // Nếu có chạm trúng bề mặt và không trúng người chơi
        if (hit.collider != null && !hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
        {
            // Có đất an toàn -> Teleport ra sau lưng
            transform.position = new Vector2(viTriSauLung.x, player.position.y);
        }
        else
        {
            // Đằng sau lưng là vực thẳm -> Teleport chèn vào vị trí người chơi luôn cho chắc chắn
            transform.position = player.position;
        }

        LookAtPlayer();
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));

        // Vẽ Vòng Xanh bọc quanh quái để bạn dễ căn chỉnh attackRange
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}