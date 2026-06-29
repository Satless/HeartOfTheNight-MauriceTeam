using UnityEngine;

public class FlyingMass : MonoBehaviour
{
    [Header("Stats")]
    // Máu của Flying Mass
    public int hp = 100;

    [Header("Movement")]
    // Tốc độ bay
    public float speed = 3f;

    // Nếu Player gần hơn khoảng này thì Flying Mass sẽ bay lùi
    public float minDistance = 5f;

    // Nếu Player xa hơn khoảng này thì Flying Mass sẽ bay lại gần
    public float maxDistance = 7f;

    [Header("Attack")]
    // Prefab quả bom
    public GameObject bombPrefab;

    // Vị trí tạo bom
    public Transform firePoint;

    // Thời gian hồi chiêu
    public float attackCooldown = 4f;

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
        // Tìm Player
        player = GameObject.FindGameObjectWithTag("Player").transform;

        // Lấy Rigidbody2D
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

        //------------------------
        // AI di chuyển
        //------------------------

        // Nếu Player quá gần
        if (distance < minDistance)
        {
            // Bay lùi
            rb.linearVelocity = -dir * speed;
        }
        // Nếu Player quá xa
        else if (distance > maxDistance)
        {
            // Bay lại gần
            rb.linearVelocity = dir * speed;
        }
        else
        {
            // Giữ vị trí
            rb.linearVelocity = Vector2.zero;

            // Hết hồi chiêu thì ném bom
            if (cooldown <= 0)
            {
                ThrowBomb();

                cooldown = attackCooldown;
            }
        }

        //------------------------
        // Quay mặt về Player
        //------------------------

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
        if (bombPrefab == null || firePoint == null)
            return;

        // Sinh bom
        GameObject bomb = Instantiate(
            bombPrefab,
            firePoint.position,
            Quaternion.identity);

        Rigidbody2D bombRB = bomb.GetComponent<Rigidbody2D>();

        if (bombRB != null)
        {
            // Hướng về Player
            Vector2 dir =
                (player.position - firePoint.position).normalized;

            // Cho bom bay
            bombRB.linearVelocity = dir * bombSpeed;
        }
    }

    //=========================
    // Nhận sát thương
    //=========================

    public void TakeDamage(int damage)
    {
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
        // Vùng quá gần -> bay lùi
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minDistance);

        // Vùng đứng yên và ném bom
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, (minDistance + maxDistance) / 2f);

        // Vùng quá xa -> bay lại gần
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxDistance);

        // Hiển thị FirePoint
        if (firePoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(firePoint.position, 0.2f);
        }
    }
}