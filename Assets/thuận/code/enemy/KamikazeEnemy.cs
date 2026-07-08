using System.Collections;
using UnityEngine;

public class KamikazeEnemyy : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 6f;

    [Header("Detection Circle")]
    public float detectionRange = 6f;

    [Header("Explosion")]
    public float explodeRange = 1.5f;  //Khoảng cách bắt đầu kích nổ
    public float explodeDelay = 1.5f;  //Thời gian nhấp nháy trước khi nổ
    public float flashInterval = 0.1f;  // Tốc độ nhấp nháy

    public int damage = 30;
    public int hp = 1;

    private Transform player;
    private bool chasing = false;
    private bool exploding = false;

    private void Start()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("Player");

        if (obj != null)
            player = obj.transform;
    }

    private void Update()
    {
        if (player == null || exploding)
            return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Player vào vùng tròn thì bắt đầu đuổi
        if (distance <= detectionRange)
        {
            chasing = true;
        }

        // Bay đuổi Player
        if (chasing)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                speed * Time.deltaTime);

            distance = Vector2.Distance(transform.position, player.position);

            // Đến gần thì bắt đầu nổ
            if (distance <= explodeRange)
            {
                StartCoroutine(Explode());
            }
        }
    }

    IEnumerator Explode()
    {
        exploding = true;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();

        float timer = 0f;

        while (timer < explodeDelay)
        {
            if (sr != null)
                sr.color = Color.red;

            yield return new WaitForSeconds(flashInterval);

            if (sr != null)
                sr.color = Color.white;

            yield return new WaitForSeconds(flashInterval);

            timer += flashInterval * 2f;
        }

        if (Vector2.Distance(transform.position, player.position) <= explodeRange)
        {
            PlayerHealth health = player.GetComponent<PlayerHealth>();

            if (health != null)
            {
                health.TakeDamage(damage);
            }
        }

        Destroy(gameObject);
    }

    public void TakeDamage(int dmg)
    {
        hp -= dmg;

        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Vùng phát hiện
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Vùng nổ
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explodeRange);
    }
}