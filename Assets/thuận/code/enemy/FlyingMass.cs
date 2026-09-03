using UnityEngine;
using HeartOfTheNight.Common;

public class FlyingMass : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    // Máu của Flying Mass
    public int hp = 140;

    [Header("Movement")]
    // Tốc độ bay
    public float speed = 3.6f;

    // Vòng đỏ: Player quá gần -> bay lùi
    public float retreatDistance = 3f;

    // Vòng xanh: Đứng yên và tấn công
    public float attackDistance = 5f;

    // Vòng vàng: Phát hiện Player
    public float detectionDistance = 7f;

    [Header("Attack")]
    // Prefab quả bom
    public GameObject bombPrefab;

    // Điểm sinh bom
    public Transform firePoint;

    // Thời gian hồi chiêu
    public float attackCooldown = 3.2f;

    // Tốc độ bay của bom
    public float bombSpeed = 8f;

    // Biến đếm hồi chiêu
    private float cooldown;

    // Player
    private Transform player;

    // Rigidbody
    private Rigidbody2D rb;

    void Start()
    {
        // Tìm Player theo Tag
        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
            player = obj.transform;

        // Lấy Rigidbody
        rb = GetComponent<Rigidbody2D>();

        // Không chịu trọng lực
        rb.gravityScale = 0;
    }

    void Update()
    {
        if (player == null)
            return;

        // Giảm thời gian hồi chiêu
        cooldown -= Time.deltaTime;

        // Khoảng cách tới Player
        float distance = Vector2.Distance(transform.position, player.position);

        // Hướng tới Player
        Vector2 dir = (player.position - transform.position).normalized;

        //---------------------------
        // AI
        //---------------------------

        // Ngoài vòng vàng -> đứng yên
        if (distance > detectionDistance)
        {
            rb.linearVelocity = Vector2.zero;
        }

        // Trong vòng đỏ -> chạy lùi
        else if (distance <= retreatDistance)
        {
            rb.linearVelocity = -dir * speed;
        }

        // Trong vòng xanh -> đứng yên và ném bom
        else if (distance <= attackDistance)
        {
            rb.linearVelocity = Vector2.zero;

            if (cooldown <= 0)
            {
                ThrowBomb();
                cooldown = attackCooldown;
            }
        }

        // Trong vòng vàng -> bay lại gần
        else
        {
            rb.linearVelocity = dir * speed;
        }

        //---------------------------
        // Quay mặt về Player
        //---------------------------

        if (player.position.x > transform.position.x)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }
        else
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
    }

    //=========================
    // Ném bom
    //=========================

    void ThrowBomb()
    {
        if (bombPrefab == null || firePoint == null || player == null)
            return;

        GameObject bomb = Instantiate(bombPrefab, firePoint.position, Quaternion.identity);

        Rigidbody2D bombRB = bomb.GetComponent<Rigidbody2D>();

        if (bombRB == null)
            return;

        // Bật trọng lực
        bombRB.gravityScale = 1f;

        Vector2 start = firePoint.position;
        Vector2 target = player.position;

        // Thời gian để bom bay tới Player
        float flightTime = 1.2f;

        // Gia tốc trọng trường
        float gravity = Mathf.Abs(Physics2D.gravity.y * bombRB.gravityScale);

        // Tính vận tốc theo trục X
        float velocityX = (target.x - start.x) / flightTime;

        // Tính vận tốc theo trục Y
        float velocityY =
            (target.y - start.y + 0.5f * gravity * flightTime * flightTime)
            / flightTime;

        bombRB.linearVelocity = new Vector2(velocityX, velocityY);
    }

    //=========================
    // Nhận sát thương
    //=========================

    public void TakeDamage(int damage)
    {
        if (hp <= 0)
            return;

        hp -= damage;

        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }

    //=========================
    // Hiển thị Gizmos
    //=========================

    private void OnDrawGizmosSelected()
    {
        // Vòng đỏ - Bay lùi
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, retreatDistance);

        // Vòng xanh - Tấn công
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attackDistance);

        // Vòng vàng - Phát hiện
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);

        // Fire Point
        if (firePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
    }
}