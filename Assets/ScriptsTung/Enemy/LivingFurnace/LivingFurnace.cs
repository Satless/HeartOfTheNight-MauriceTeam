using System.Collections;
using UnityEngine;

public class KamikazeEnemy : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 4f;

    [Header("Detection")]
    public float detectionRange = 8f;

    [Header("Explosion")]
    public float explodeRange = 1.5f;
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

        // Player vào vùng phát hiện
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

            // Kiểm tra khoảng cách để nổ
            distance = Vector2.Distance(transform.position, player.position);

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
        float explodeTime = 1f;

        while (timer < explodeTime)
        {
            if (sr != null)
                sr.color = Color.red;

            yield return new WaitForSeconds(0.1f);

            if (sr != null)
                sr.color = Color.white;

            yield return new WaitForSeconds(0.1f);

            timer += 0.2f;
        }

        float distance = Vector2.Distance(transform.position, player.position);

        if (distance <= explodeRange)
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
        // Vùng phát hiện (màu cam)
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Vùng nổ (màu đỏ)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explodeRange);
    }
}