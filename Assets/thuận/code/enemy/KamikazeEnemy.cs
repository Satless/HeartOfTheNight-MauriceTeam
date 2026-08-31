using System.Collections;
using UnityEngine;
using HeartOfTheNight.Common; // 🔥 QUAN TRỌNG: Gọi thư viện chứa IDamageable

public class KamikazeEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float explodeRange = 1.5f;
    [Tooltip("Tầm vụ nổ sát thương thực tế. Nên to hơn Explode Range một xíu để Player khó né")]
    [SerializeField] private float blastRadius = 2.5f; // 🔥 MỚI THÊM

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

    private float moveSoundTimer;

    private void Awake()
    {
        currentHP = maxHP;

        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("Player");

        if (obj != null)
            player = obj.transform;
    }

    private void Update()
    {
        if (dead || exploding || player == null)
            return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Player đi vào vùng phát hiện
        if (!chasing && distance <= detectionRange)
        {
            chasing = true;
        }

        if (!chasing)
            return;

        // Di chuyển tới Player
        if (distance > explodeRange)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                moveSpeed * Time.deltaTime);

            moveSoundTimer -= Time.fixedDeltaTime;
            if (moveSoundTimer <= 0f)
            {
                AudioEvents.TriggerSound3D("Enemy", "Kamikaze", "Move", transform.position);
                moveSoundTimer = 0.5f;
            }

            // Lật hướng
            if (player.position.x > transform.position.x)
                transform.localScale = new Vector3(1, 1, 1);
            else
                transform.localScale = new Vector3(-1, 1, 1);
        }
        else
        {
            StartCoroutine(Explode());
        }
    }

    IEnumerator Explode()
    {
        exploding = true;

        AudioEvents.TriggerSound3D("Enemy", "Kamikaze", "Attack", transform.position);

        // Dừng di chuyển và nhấp nháy trắng
        float timer = 0f;
        bool visible = true;

        while (timer < explodeDelay)
        {
            if (spriteRenderer != null)
            {
                if (visible)
                    spriteRenderer.color = Color.red;                 // Hiện bình thường
                else
                    spriteRenderer.color = new Color(1, 1, 1, 0.2f);    // Mờ trắng
            }

            visible = !visible;

            yield return new WaitForSeconds(flashInterval);

            timer += flashInterval;
        }

        // Trả sprite về bình thường
        if (spriteRenderer != null)
            spriteRenderer.color = Color.white;

        // Phát animation Attack
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        // Chờ animation Attack chạy (0.3s)
        yield return new WaitForSeconds(0.3f);

        // 🔥 SỬA LỖI SÁT THƯƠNG: Dùng sóng xung kích quét toàn bộ Player nằm trong vùng nổ
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, blastRadius);
        foreach (Collider2D hit in hits)
        {
            // Kiểm tra xem có trúng Player không
            if (hit.CompareTag("Player") || hit.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                // Gọi IDamageable chuẩn như mấy con Boss
                IDamageable target = hit.GetComponent<IDamageable>();
                if (target == null) target = hit.GetComponentInParent<IDamageable>();

                if (target != null)
                {
                    target.TakeDamage(damage);
                }
            }
        }

        Destroy(gameObject);
    }

    public void TakeDamage(int damageTaken)
    {
        if (dead)
            return;

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

        // Vòng đỏ là vòng bắt đầu đếm ngược kích nổ
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explodeRange);

        // Vòng tím (mới) là vòng sát thương thực tế khi nổ cái bùm
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, blastRadius);
    }
}