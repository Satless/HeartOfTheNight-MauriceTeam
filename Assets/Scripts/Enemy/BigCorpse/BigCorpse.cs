using UnityEngine;

public class BigCorpse : MonoBehaviour
{
    public float moveSpeed = 3f;
    public int attackDamage = 10;

    public float detectionRange = 7f; // Khoảng cách nhìn thấy người chơi
    public float attackRange = 1.2f;  // Khoảng cách để chém

    public float platformHeightDiff = 1.5f; // Chênh lệch độ cao để quái biết là phải Teleport
    public float teleportDelay = 0.5f; // Đợi nửa giây rồi mới Teleport
    public float attackCooldown = 1.5f; // Nghỉ 1.5 giây giữa mỗi lần chém

    private Transform player;
    private float nextAttackTime = 0f;
    private float teleportTimer = 0f;

    void Start()
    {
        // Tự động tìm nhân vật Player trong game
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // TẮT VA CHẠM GIỮA QUÁI VÀ PLAYER (CHO PHÉP ĐI XUYÊN NHAU)
        Collider2D quaiCollider = GetComponent<Collider2D>();
        Collider2D playerCollider = player.GetComponent<Collider2D>();
    }

    void Update()
    {
        if (player == null) return;

        // Tính khoảng cách giữa Quái và Player
        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);
        float totalDistance = Vector2.Distance(transform.position, player.position);

        // Nếu Player đi vào tầm nhìn của quái
        if (totalDistance <= detectionRange)
        {
            // TRƯỜNG HỢP 1: Player đang ở quá cao hoặc quá thấp (nhảy / khác platform) -> Dịch chuyển
            if (distanceY > platformHeightDiff)
            {
                teleportTimer += Time.deltaTime;
                if (teleportTimer >= teleportDelay)
                {
                    Teleport();
                    teleportTimer = 0f; // Reset thời gian chờ
                }
            }
            // TRƯỜNG HỢP 2: Đang ở cùng mặt phẳng với Player
            else
            {
                teleportTimer = 0f; // Không cần dịch chuyển

                // Nếu chưa tới đủ gần để chém -> Chạy lại gần
                if (distanceX > attackRange)
                {
                    Move();
                }
                // Nếu đủ gần và đã hồi chiêu xong -> Chém
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
        // Đi từ từ về phía Player (chỉ đi ngang)
        Vector2 targetPosition = new Vector2(player.position.x, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    void Teleport()
    {
        // Dịch chuyển ra đứng ngay đằng sau Player
        float standBehind = (player.localScale.x > 0) ? -1f : 1f;
        transform.position = new Vector2(player.position.x + standBehind, player.position.y);
        LookAtPlayer();
    }

    void LookAtPlayer()
    {
        // Lật mặt quái quay về hướng Player
        Vector3 scale = transform.localScale;
        if (player.position.x > transform.position.x) scale.x = Mathf.Abs(scale.x); // Quay phải
        else scale.x = -Mathf.Abs(scale.x); // Quay trái

        transform.localScale = scale;
    }

    void Attack()
    {
        // Kéo script máu của Player ra và trừ máu
        player.GetComponent<PlayerHealth>().TakeDamage(attackDamage);
    }
}