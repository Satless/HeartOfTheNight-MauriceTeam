using System.Collections;
using UnityEngine;
using HeartOfTheNight.Common;

public class KamikazeEnemy : MonoBehaviour, IDamageable
{
    [Header("Movement & Avoidance")]
    [SerializeField] private float moveSpeed = 6f;
    [Tooltip("Layer chứa các vật cản (Đất, Tường...)")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("Khoảng cách quái bắt đầu phát hiện tường để né")]
    [SerializeField] private float avoidDistance = 1.5f;
    [Tooltip("Độ mượt khi bẻ lái (Càng thấp cua càng gắt)")]
    [SerializeField] private float turnSpeed = 5f;

    [Header("Detection")]
    [Tooltip("Layer của mục tiêu (Player) để quái quét tìm")]
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float explodeRange = 1.5f;
    [SerializeField] private float blastRadius = 2.5f;

    [Header("Explosion")]
    [SerializeField] private float explodeDelay = 1.2f;
    [SerializeField] private float flashInterval = 0.1f;

    [Header("Stats")]
    [SerializeField] private int maxHP = 1;
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
        // Vẫn tìm Player sẵn để phòng trường hợp được Boss đẻ ra là có mục tiêu rượt luôn
        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null) player = obj.transform;
    }

    // Boss gọi hàm này để ép con quái rượt ngay lập tức
    public void ActivateBossMode()
    {
        isSpawnedByBoss = true;
        chasing = true;
    }

    private void Update()
    {
        if (dead || exploding) return;

        // 🔥 LOGIC: QUÉT VÙNG XUNG QUANH TÌM TAG "Player" HOẶC LAYER PLAYER
        if (!chasing)
        {
            if (isSpawnedByBoss)
            {
                chasing = true; // Boss đẻ ra thì rượt thẳng
            }
            else
            {
                // Lấy TẤT CẢ các vật thể lọt vào trong vòng tròn quét
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, detectionRange);

                foreach (Collider2D hit in hits)
                {
                    // Kiểm tra: Nếu mang Tag "Player" HOẶC nằm trong Layer Player
                    if (hit.CompareTag("Player") || ((1 << hit.gameObject.layer) & playerLayer) != 0)
                    {
                        chasing = true;
                        player = hit.transform; // Chốt mục tiêu
                        break; // Tìm thấy Player rồi thì dừng quét luôn
                    }
                }
            }
        }

        // Nếu chưa phát hiện ai hoặc mục tiêu đã chết/biến mất thì đứng im
        if (!chasing || player == null) return;

        // Bắt đầu tính khoảng cách để di chuyển hoặc nổ
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);

        if (distanceToPlayer > explodeRange)
        {
            MoveWithObstacleAvoidance();
            PlayMoveSound();
            FlipSprite();
        }
        else
        {
            StartCoroutine(Explode());
        }
    }

    private void MoveWithObstacleAvoidance()
    {
        Vector2 targetDir = (player.position - transform.position).normalized;
        Vector2 optimalDir = targetDir;

        // Bắn tia cảm biến thẳng
        RaycastHit2D hitFront = Physics2D.Raycast(transform.position, targetDir, avoidDistance, obstacleLayer);

        if (hitFront.collider != null)
        {
            // Tường chắn phía trước -> Quét góc trên và dưới
            Vector2 upDir = Quaternion.Euler(0, 0, 45) * targetDir;
            RaycastHit2D hitUp = Physics2D.Raycast(transform.position, upDir, avoidDistance, obstacleLayer);

            Vector2 downDir = Quaternion.Euler(0, 0, -45) * targetDir;
            RaycastHit2D hitDown = Physics2D.Raycast(transform.position, downDir, avoidDistance, obstacleLayer);

            if (hitUp.collider == null) optimalDir = upDir;
            else if (hitDown.collider == null) optimalDir = downDir;
            else optimalDir = Vector2.Reflect(targetDir, hitFront.normal);
        }

        // Làm mượt (Lerp) góc xoay để lách mượt mà qua tường
        currentMoveDirection = Vector2.Lerp(currentMoveDirection, optimalDir, turnSpeed * Time.deltaTime).normalized;
        transform.position += (Vector3)currentMoveDirection * moveSpeed * Time.deltaTime;
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
            // Nổ trúng Player bằng Tag hoặc Layer
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

        // Hiển thị râu cảm biến trong Editor kể cả khi ở chế độ Prefab Mode
        Vector2 targetDir = Vector2.right;
        if (Application.isPlaying && player != null) targetDir = (player.position - transform.position).normalized;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)targetDir * avoidDistance);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(Quaternion.Euler(0, 0, 45) * targetDir) * avoidDistance);
        Gizmos.DrawLine(transform.position, transform.position + (Vector3)(Quaternion.Euler(0, 0, -45) * targetDir) * avoidDistance);
    }
}