using UnityEngine;
using System.Collections;

public class BurningCorpse : MonoBehaviour
{
    [Header("Tầm nhìn & Di chuyển")]
    public float moveSpeed = 4f;
    public float detectionRange = 7f;
    public float attackRange = 2f;

    [Header("Dịch chuyển")]
    public float platformHeightDiff = 0.8f;
    public float teleportDelay = 1f;
    public float postTeleportDelay = 0.5f;

    [Header("Sát thương Cận chiến")]
    public int attackDamage = 10;
    public float attackCooldown = 2f;

    [Header("Hiệu ứng Thiêu Đốt (DoT)")]
    public int burnDamagePerTick = 2;  // Mỗi lần đốt mất bao nhiêu máu?
    public int burnTicks = 3;          // Đốt tổng cộng mấy lần? (Ví dụ: 3 lần)
    public float timeBetweenTicks = 1f; // Khoảng cách giữa các lần đốt (Ví dụ: 1 giây đốt 1 lần)

    [Header("Cảm biến dập lửa")]
    public float dashSpeedThreshold = 12f; // Tốc độ lướt của Player (Nếu lướt chậm hơn số này thì hạ xuống)

    private Transform player;
    private float nextAttackTime = 0f;
    private float teleportTimer = 0f;

    private bool isBusy = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null || isBusy) return;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);
        float totalDistance = Vector2.Distance(transform.position, player.position);

        if (totalDistance <= detectionRange)
        {
            if (distanceY > platformHeightDiff)
            {
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

                if (distanceX > attackRange)
                {
                    Move();
                }
                else if (Time.time >= nextAttackTime)
                {
                    Attack();
                    nextAttackTime = Time.time + attackCooldown;
                }
            }
        }
    }

    void Move()
    {
        LookAtPlayer();
        Vector2 targetPosition = new Vector2(player.position.x, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    IEnumerator ThucHienTeleport()
    {
        isBusy = true;

        float standBehind = (player.localScale.x > 0) ? -1f : 1f;
        transform.position = new Vector2(player.position.x + standBehind, player.position.y);
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

    void Attack()
    {
        PlayerHealth pHealth = player.GetComponent<PlayerHealth>();
        if (pHealth != null)
        {
            // 1. Chém một cái mất máu gốc luôn
            pHealth.TakeDamage(attackDamage);

            // 2. Kích hoạt bùa chú đốt máu đằng sau hậu trường
            StartCoroutine(GayHieuUngChay(pHealth));
        }
    }

    // --- COROUTINE ĐỐT MÁU (CÓ CẢM BIẾN LƯỚT) ---
    IEnumerator GayHieuUngChay(PlayerHealth pHealth)
    {
        // Lấy Rigidbody của Player để đo tốc độ
        Rigidbody2D playerRb = pHealth.GetComponent<Rigidbody2D>();

        // Chạy vòng lặp đúng số lần quy định (Ví dụ 3 lần)
        for (int i = 0; i < burnTicks; i++)
        {
            // Thay vì đợi 1 cục 1 giây (làm quái bị mù thông tin), ta chia nhỏ ra soi từng frame
            float thoiGianDaCho = 0f;
            while (thoiGianDaCho < timeBetweenTicks)
            {
                // Nếu lấy được Rigidbody VÀ tốc độ di chuyển ngang (trục X) vọt lên lớn hơn mức quy định
                if (playerRb != null && Mathf.Abs(playerRb.linearVelocity.x) >= dashSpeedThreshold)
                {
                    Debug.Log("Phát hiện Player lướt! Dập lửa thành công!");
                    yield break; // Lệnh thần thánh: Ngay lập tức kết thúc/hủy bỏ toàn bộ Coroutine đốt máu này!
                }

                thoiGianDaCho += Time.deltaTime; // Cộng dồn thời gian
                yield return null; // Nghỉ 1 frame rồi soi tiếp
            }

            // Nếu soi hết 1 giây mà Player vẫn không lướt -> Trừ máu!
            if (pHealth != null)
            {
                pHealth.TakeDamage(burnDamagePerTick);
            }
            else
            {
                yield break; // Player chết rồi thì ngừng đốt
            }
        }
    }
}