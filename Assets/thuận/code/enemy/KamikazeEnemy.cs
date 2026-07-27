using System.Collections;
using UnityEngine;

public class KamikazeEnemy : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;

    [Header("Detection")]
    [SerializeField] private float detectionRange = 6f;
    [SerializeField] private float explodeRange = 1.5f;

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

        // Phát animation Attack
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        float timer = 0;

        while (timer < explodeDelay)
        {
            if (spriteRenderer != null)
                spriteRenderer.color = Color.red;

            yield return new WaitForSeconds(flashInterval);

            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;

            yield return new WaitForSeconds(flashInterval);

            timer += flashInterval * 2f;
        }

        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= explodeRange)
            {
                PlayerHealth hp = player.GetComponent<PlayerHealth>();

                if (hp != null)
                {
                    hp.TakeDamage(damage);
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

        if (currentHP <= 0)
        {
            dead = true;

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
    }
}