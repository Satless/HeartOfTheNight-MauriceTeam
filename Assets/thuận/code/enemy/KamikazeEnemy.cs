using System.Collections;
using UnityEngine;
using HeartOfTheNight.Common;

public class KamikazeEnemy : MonoBehaviour, IDamageable
{
    private float escapeTimer = 0f;

    [Header("Movement & Avoidance")]
    [SerializeField] private float moveSpeed = 6f;
    [Tooltip("Layer chứa các vật cản (Đất, Tường...)")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("Khoảng cách tia quét tìm tường")]
    [SerializeField] private float avoidDistance = 1.5f;
    [Tooltip("Độ to của thân con quái (Để tính toán lách cho khỏi kẹt mép)")]
    [SerializeField] private float enemyRadius = 0.4f;
    [Tooltip("Độ mượt khi lượn (Càng thấp cua càng gắt)")]
    [SerializeField] private float turnSpeed = 5f;

    [Header("Detection")]
    [Tooltip("Layer của mục tiêu (Player) để quái quét tìm")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float detectionRange = 7.5f;
    [SerializeField] private float explodeRange = 1.5f;
    [SerializeField] private float blastRadius = 2.5f;

    [Header("Explosion")]
    [SerializeField] private float explodeDelay = 0.85f;
    [SerializeField] private float flashInterval = 0.1f;

    [Header("Stats")]
    [SerializeField] private int maxHP = 20;
    [SerializeField] private int damage = 30;

    private int currentHP;

    private Transform player;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    private bool chasing;
    private bool exploding;
    private bool dead;
    private bool isSpawnedByBoss = false;

    private float moveSoundTimer;
    private Vector2 currentMoveDirection;

    private void Awake()
    {
        currentHP = maxHP;
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null) player = obj.transform;
    }

    public void ActivateBossMode()
    {
        isSpawnedByBoss = true;
        chasing = true;
    }

    private void Update()
    {
        if (dead || exploding) return;

        if (!chasing)
        {
            if (isSpawnedByBoss)
            {
                chasing = true;
            }
            else
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange);
                foreach (Collider2D hit in hits)
                {
                    if (hit.CompareTag("Player") || ((1 << hit.gameObject.layer) & playerLayer) != 0)
                    {
                        chasing = true;
                        player = hit.transform;
                        break;
                    }
                }
            }
        }

        if (!chasing || player == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > explodeRange)
        {
            MoveWithSmartAvoidance(); // 🔥 DÙNG HÀM TÌM ĐƯỜNG MỚI
            PlayMoveSound();
            FlipSprite();
        }
        else
        {
            StartCoroutine(Explode());
        }
    }

    // 🔥 HỆ THỐNG AI MỚI: QUÉT RẺ QUẠT VÀ TÌM ĐƯỜNG RỘNG NHẤT
    // 🔥 HỆ THỐNG AI MỚI: CÓ CHỨC NĂNG CHỐNG KẸT TƯỜNG (ANTI-STUCK)
    // 🔥 HỆ THỐNG AI MỚI NHẤT: CÓ TRÍ NHỚ VƯỢT TƯỜNG (ESCAPE TIMER)
    private void MoveWithSmartAvoidance()
    {
        // 0. TRẠNG THÁI VƯỢT TƯỜNG KHẨN CẤP (IGNORE RADAR)
        if (escapeTimer > 0f)
        {
            escapeTimer -= Time.deltaTime;

            // 🔥 FIX LỖI "CÀ TƯỜNG": Bay LÊN TRỜI và HƠI LÙI LẠI để tách khỏi mặt thùng
            float dirX = Mathf.Sign(player.position.x - transform.position.x);
            Vector2 escapeDir = new Vector2(-dirX * 0.3f, 1f).normalized;

            // Ép bẻ lái cực gắt (nhân 10) để thắng lực quán tính
            currentMoveDirection = Vector2.Lerp(currentMoveDirection, escapeDir, turnSpeed * 10f * Time.deltaTime).normalized;
            transform.position += (Vector3)currentMoveDirection * moveSpeed * Time.deltaTime;
            return;
        }

        // 1. KIỂM TRA LÚN (SPAWN TRAP)
        Collider2D insideWall = Physics2D.OverlapCircle(transform.position, enemyRadius, obstacleLayer);
        if (insideWall != null)
        {
            escapeTimer = 0.6f; // Bật chế độ dội tường 0.6 giây
            return;
        }

        // 2. RADAR TÌM ĐƯỜNG CHÍNH
        Vector2 targetDir = (player.position - transform.position).normalized;
        Vector2 bestDir = Vector2.up;
        float bestScore = -Mathf.Infinity;
        bool pathFound = false;

        float[] checkAngles = { 0f, 30f, -30f, 60f, -60f, 90f, -90f };

        foreach (float angle in checkAngles)
        {
            Vector2 checkDir = Quaternion.Euler(0, 0, angle) * targetDir;
            RaycastHit2D hit = Physics2D.CircleCast(transform.position, enemyRadius, checkDir, avoidDistance, obstacleLayer);

            if (hit.collider == null)
            {
                float score = Vector2.Dot(targetDir, checkDir);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestDir = checkDir;
                    pathFound = true;
                }
            }
        }

        // 🔥 3. FIX LỖI RADAR "NHÌN XUỐNG ĐẤT"
        // Nếu quét về phía Player mà vướng hết, bắt buộc phải ngẩng đầu nhìn lên trời!
        if (!pathFound)
        {
            // Quét 3 hướng cố định: Thẳng đứng, Chéo lên Trái, Chéo lên Phải
            Vector2[] skyDirs = { Vector2.up, new Vector2(-1, 1).normalized, new Vector2(1, 1).normalized };
            foreach (Vector2 skyDir in skyDirs)
            {
                RaycastHit2D skyHit = Physics2D.CircleCast(transform.position, enemyRadius, skyDir, avoidDistance, obstacleLayer);
                if (skyHit.collider == null)
                {
                    bestDir = skyDir;
                    pathFound = true;
                    break;
                }
            }
        }

        // 4. QUYẾT ĐỊNH CUỐI CÙNG
        if (!pathFound)
        {
            // Tứ phía (kể cả trên trời) đều bị chặn (ví dụ kẹt trong 1 cái hộp kín) -> Dội tường!
            escapeTimer = 0.6f;
        }
        else
        {
            currentMoveDirection = Vector2.Lerp(currentMoveDirection, bestDir, turnSpeed * Time.deltaTime).normalized;
            transform.position += (Vector3)currentMoveDirection * moveSpeed * Time.deltaTime;
        }
    }

    private void PlayMoveSound()
    {
        moveSoundTimer -= Time.deltaTime;
        if (moveSoundTimer <= 0f)
        {
            AudioEvents.TriggerSound3D("Enemy", "Kamikaze", "Move", transform.position);
            moveSoundTimer = 0.5f;
        }
    }

    private void FlipSprite()
    {
        if (currentMoveDirection.x > 0) transform.localScale = new Vector3(1, 1, 1);
        else if (currentMoveDirection.x < 0) transform.localScale = new Vector3(-1, 1, 1);
    }

    IEnumerator Explode()
    {
        exploding = true;
        AudioEvents.TriggerSound3D("Enemy", "Kamikaze", "Attack", transform.position);

        float timer = 0f;
        bool visible = true;

        while (timer < explodeDelay)
        {
            if (spriteRenderer != null) spriteRenderer.color = visible ? Color.red : new Color(1, 1, 1, 0.2f);
            visible = !visible;
            yield return new WaitForSeconds(flashInterval);
            timer += flashInterval;
        }

        if (spriteRenderer != null) spriteRenderer.color = Color.white;
        if (animator != null) animator.SetTrigger("Attack");

        yield return new WaitForSeconds(0.3f);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, blastRadius);
        foreach (Collider2D hit in hits)
        {
            if (hit.CompareTag("Player") || ((1 << hit.gameObject.layer) & playerLayer) != 0)
            {
                IDamageable target = hit.GetComponent<IDamageable>() ?? hit.GetComponentInParent<IDamageable>();
                if (target != null) target.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }

    public void TakeDamage(int damageTaken)
    {
        if (dead) return;

        currentHP -= damageTaken;
        AudioEvents.TriggerSound3D("Enemy", "Kamikaze", "Hurt", transform.position);

        if (currentHP <= 0)
        {
            dead = true;
            AudioEvents.TriggerSound3D("Enemy", "Kamikaze", "Die", transform.position);
            StopAllCoroutines();
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explodeRange);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, blastRadius);

        Vector2 targetDir = Vector2.right;
        if (Application.isPlaying && player != null) targetDir = (player.position - transform.position).normalized;

        Gizmos.color = Color.cyan;
        float[] checkAngles = { 0f, 30f, -30f, 60f, -60f, 90f, -90f };

        foreach (float angle in checkAngles)
        {
            Vector2 checkDir = Quaternion.Euler(0, 0, angle) * targetDir;
            // Vẽ đường quét
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)checkDir * avoidDistance);
            // Vẽ 1 vòng tròn ở đuôi để bạn hình dung độ bự của CircleCast
            Gizmos.DrawWireSphere(transform.position + (Vector3)checkDir * avoidDistance, enemyRadius);
        }
    }
}