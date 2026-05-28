using UnityEngine;

public class BurningCorpse : MonoBehaviour
{
    public float moveSpeed = 4f; // Nhanh hơn con BigCorpse một chút
    public int attackDamage = 10;

    public float detectionRange = 7f;
    public float attackRange = 2f;

    public float platformHeightDiff = 0.8f;
    public float teleportDelay = 1f;
    public float attackCooldown = 2f;

    private Transform player;
    private float nextAttackTime = 0f;
    private float teleportTimer = 0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        Collider2D quaiCollider = GetComponent<Collider2D>();
        Collider2D playerCollider = player.GetComponent<Collider2D>();
    }

    void Update()
    {
        if (player == null) return;

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
                    Teleport();
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

    void Teleport()
    {
        float standBehind = (player.localScale.x > 0) ? -1f : 1f;
        transform.position = new Vector2(player.position.x + standBehind, player.position.y);
        LookAtPlayer();
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

        // 1. Gây sát thương trực tiếp từ đòn đánh
        pHealth.TakeDamage(attackDamage);

        // 2. KÍCH HOẠT HIỆU ỨNG THIÊU ĐỐT NGƯỜI CHƠI
        pHealth.StartBurning();
    }
}