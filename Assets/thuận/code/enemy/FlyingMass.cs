using UnityEngine;

public class FlyingMass : MonoBehaviour
{
    public int hp = 100;

    [Header("Movement")]
    public float speed = 3f;
    public float minDistance = 5f;
    public float maxDistance = 7f;

    [Header("Attack")]
    public GameObject bombPrefab;
    public Transform firePoint;
    public float attackCooldown = 4f;

    private float cooldown;
    private Transform player;
    private Rigidbody2D rb;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();

        rb.gravityScale = 0;
    }

    void Update()
    {
        if (player == null) return;

        cooldown -= Time.deltaTime;

        float distance = Vector2.Distance(transform.position, player.position);

        Vector2 dir = (player.position - transform.position).normalized;

        // Quá gần -> bay lùi
        if (distance < minDistance)
        {
            rb.linearVelocity = -dir * speed;
        }
        // Quá xa -> bay lại
        else if (distance > maxDistance)
        {
            rb.linearVelocity = dir * speed;
        }
        // Đúng khoảng cách -> đứng yên
        else
        {
            rb.linearVelocity = Vector2.zero;

            if (cooldown <= 0)
            {
                ThrowBomb();
                cooldown = attackCooldown;
            }
        }

        // Quay mặt về Player
        if (player.position.x > transform.position.x)
            transform.localScale = Vector3.one;
        else
            transform.localScale = new Vector3(-1, 1, 1);
    }

    void ThrowBomb()
    {
        GameObject bomb = Instantiate(
            bombPrefab,
            firePoint.position,
            Quaternion.identity);

        Rigidbody2D bombRB = bomb.GetComponent<Rigidbody2D>();

        Vector2 dir =
            (player.position - firePoint.position).normalized;

        bombRB.linearVelocity = dir * 8f;
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;

        if (hp <= 0)
            Destroy(gameObject);
    }
}