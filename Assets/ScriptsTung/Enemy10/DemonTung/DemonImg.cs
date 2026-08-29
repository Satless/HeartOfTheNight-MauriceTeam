using UnityEngine;
using System.Collections;
using HeartOfTheNight.Common;

public class DemonImg : MonoBehaviour, IDamageable
{
    [Header("Chỉ số Sinh tồn")]
    public int maxHealth = 100;
    public int currentHealth;
    public bool isDead = false;

    [Header("Hoạt ảnh & Đồ họa")]
    public Animator anim;
    public SpriteRenderer sr;

    [Header("Đổi Màu Khi Cast Chiêu")]
    public Color castColor = Color.red;
    private Color originalColor;

    [Header("Tầm nhìn & Tuần tra")]
    public float detectionRange = 10f;
    public float patrolSpeed = 2f;
    public float patrolDistance = 5f;
    private float startX;

    [Header("Di chuyển Bay (Thả diều)")]
    public float runSpeed = 4f;
    public float minimumDistance = 5f;

    [Header("Kiểm tra Vật cản (Tường)")]
    public Transform wallCheck;
    public float wallCheckDistance = 0.5f;
    public LayerMask obstacleLayer;

    [Header("Kỹ năng Cột Lửa (Demon Skill Laser)")]
    public float attackCooldown = 3f;
    public float chargeTime = 1.5f;
    [Tooltip("Thời gian đứng thở sau khi bay trước khi được tung chiêu")]
    public float delayAfterMove = 0.5f;
    public GameObject fireHazardPrefab;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D myCol;
    private float nextAttackTime = 0f;
    private bool isCasting = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();
        if (anim == null) anim = GetComponent<Animator>();
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        currentHealth = maxHealth;

        if (sr != null) originalColor = sr.color;
        if (rb != null) rb.gravityScale = 0f;
    }

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        startX = transform.position.x;
        SetupXuyenThau();
    }

    void SetupXuyenThau()
    {
        if (myCol == null) return;
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
        if (isDead || player == null) return;

        if (isCasting)
        {
            StopMoving();
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        float distanceX = Mathf.Abs(player.position.x - transform.position.x);

        if (distanceToPlayer <= detectionRange)
        {
            if (distanceX < minimumDistance)
            {
                FleeFromPlayer();
            }
            else
            {
                StopMoving();
                LookAtPlayer();

                // KHÓA MÕM NGAY LẬP TỨC: Đưa bộ đếm lên đầu để tránh spam đúp
                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + chargeTime + attackCooldown;
                    StartCoroutine(CastFireRoutine());
                }
            }
        }
        else
        {
            Patrol();
        }
    }

    void Patrol()
    {
        float distanceFromStart = Mathf.Abs(transform.position.x - startX);
        float currentDir = Mathf.Sign(transform.localScale.x);
        float dirToStart = Mathf.Sign(startX - transform.position.x);

        if (IsHittingWall(currentDir) || (distanceFromStart >= patrolDistance && currentDir != dirToStart))
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
            currentDir = Mathf.Sign(transform.localScale.x);
        }

        rb.linearVelocity = new Vector2(currentDir * patrolSpeed, 0f);

        // THÊM: Đang đi tuần mà thấy mặt sếp thì cũng phải giật mình "thở" 0.5s mới bắn
        nextAttackTime = Mathf.Max(nextAttackTime, Time.time + delayAfterMove);
    }

    bool IsHittingWall(float direction)
    {
        if (wallCheck == null) return false;
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, Vector2.right * direction, wallCheckDistance, obstacleLayer);
        return hit.collider != null && !hit.collider.isTrigger;
    }

    void FleeFromPlayer()
    {
        float fleeDir = (player.position.x > transform.position.x) ? -1f : 1f;

        if (IsHittingWall(fleeDir))
        {
            StopMoving();
            LookAtPlayer();

            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + chargeTime + attackCooldown;
                StartCoroutine(CastFireRoutine());
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(fleeDir * runSpeed, 0f);
            transform.localScale = new Vector3(fleeDir * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);

            // THÊM: Vừa chạy xong phải mất 0.5s mới được vận nội công
            nextAttackTime = Mathf.Max(nextAttackTime, Time.time + delayAfterMove);
        }
    }

    void StopMoving()
    {
        rb.linearVelocity = Vector2.zero;
    }

    void LookAtPlayer()
    {
        float dir = player.position.x > transform.position.x ? 1f : -1f;
        transform.localScale = new Vector3(dir * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }

    // ==========================================
    // KỸ NĂNG: GỌI LỬA DƯỚI ĐẤT
    // ==========================================
    IEnumerator CastFireRoutine()
    {
        isCasting = true;
        StopMoving();

        if (sr != null) sr.color = castColor;
        if (anim != null) anim.SetTrigger("Attack");

        Vector3 targetPosition = player.position;
        Vector2 rayStart = player.position;

        Collider2D pCol = player.GetComponent<Collider2D>();
        if (pCol == null) pCol = player.GetComponentInChildren<Collider2D>();

        if (pCol != null)
        {
            rayStart.y = pCol.bounds.center.y;
            targetPosition.y = pCol.bounds.min.y;
        }

        RaycastHit2D[] hits = Physics2D.RaycastAll(rayStart, Vector2.down, 20f);

        foreach (var hit in hits)
        {
            if (hit.collider.isTrigger) continue;
            if (hit.collider.transform.root == player.root) continue;
            if (hit.collider.CompareTag("Enemy") || hit.collider.CompareTag("Player")) continue;

            targetPosition.y = hit.point.y;
            break;
        }

        if (fireHazardPrefab != null)
        {
            Instantiate(fireHazardPrefab, targetPosition, Quaternion.identity);
        }

        yield return new WaitForSeconds(chargeTime);

        if (sr != null) sr.color = originalColor;
        isCasting = false; // Xong xuôi hết rồi mới nhả cờ Casting
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minimumDistance);
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        isCasting = false;
        StopMoving();

        if (sr != null) sr.color = originalColor;
        if (rb != null) rb.simulated = false;

        gameObject.tag = "Untagged";
        if (myCol != null) myCol.enabled = false;

        if (anim != null)
        {
            anim.enabled = true;
            anim.SetTrigger("Dead");
        }

        Destroy(gameObject, 2f);
    }
}