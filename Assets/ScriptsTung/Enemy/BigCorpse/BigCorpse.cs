using UnityEngine;
using System.Collections; // Bắt buộc thêm thư viện này để dùng Coroutine

public class BigCorpse : MonoBehaviour
{
    public float moveSpeed = 3f;
    public int attackDamage = 10;

    public float detectionRange = 7f;
    public float attackRange = 1.2f;

    public float platformHeightDiff = 1.5f;
    public float teleportDelay = 0.5f;

    [Header("Thời gian chờ sau khi Teleport")]
    public float postTeleportDelay = 0.5f; // Thời gian cho Player phản ứng (Nửa giây)

    public float attackCooldown = 1.5f;

    private Transform player;
    private float nextAttackTime = 0f;
    private float teleportTimer = 0f;

    private bool isBusy = false; // Biến khóa hành động

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;

        Collider2D quaiCollider = GetComponent<Collider2D>();
        Collider2D playerCollider = player.GetComponent<Collider2D>();
    }

    void Update()
    {
        // Nếu Player chết hoặc Quái đang bận dịch chuyển -> Không làm gì cả
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
                    StartCoroutine(ThucHienTeleport()); // Đổi sang gọi Coroutine
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

    // ĐÃ CHUYỂN THÀNH COROUTINE ĐỂ CÓ THỜI GIAN NGHỈ
    IEnumerator ThucHienTeleport()
    {
        isBusy = true; // Khóa các hành động khác (không cho chém ngay)

        float standBehind = (player.localScale.x > 0) ? -1f : 1f;
        transform.position = new Vector2(player.position.x + standBehind, player.position.y);
        LookAtPlayer();

        // Đứng im tại chỗ chờ người chơi phản ứng
        yield return new WaitForSeconds(postTeleportDelay);

        isBusy = false; // Mở khóa lại để quái có thể tấn công
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
        player.GetComponent<PlayerHealth>().TakeDamage(attackDamage);
    }
}