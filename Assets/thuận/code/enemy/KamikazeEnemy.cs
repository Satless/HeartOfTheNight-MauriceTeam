using System.Collections;
using UnityEngine;

public class KamikazeEnemy : MonoBehaviour
{
    public float speed = 4f;
    public float detectionRange = 8f;
    public float explodeRange = 1.5f;

    public int damage = 30;
    public int hp = 1;

    private Transform player;
    private bool exploding;

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (player == null || exploding)
            return;

        float distance =
            Vector2.Distance(transform.position,
                             player.position);

        if (distance <= detectionRange)
        {
            transform.position =
                Vector2.MoveTowards(
                    transform.position,
                    player.position,
                    speed * Time.deltaTime);
        }

        if (distance <= explodeRange)
        {
            StartCoroutine(Explode());
        }
    }

    IEnumerator Explode()
    {
        exploding = true;

        Debug.Log("Sắp nổ");

        yield return new WaitForSeconds(1f);

        Debug.Log("BOOM");

        float distance =
            Vector2.Distance(transform.position,
                             player.position);

        if (distance <= explodeRange)
        {
            PlayerHealth health =
                player.GetComponent<PlayerHealth>();

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
}