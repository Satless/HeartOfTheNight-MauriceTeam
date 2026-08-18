using System.Collections;
using UnityEngine;

public class FlyingRat : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 4f;
    public float detectionRange = 8f;

    [Header("Attack")]
    public float attackRange = 1.2f;
    public int damage = 15;
    public float attackCooldown = 1.2f;

    [Header("Health")]
    public int hp = 40;

    private Transform player;
    private bool attacking;

    void Start()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("Player");

        if (obj != null)
            player = obj.transform;
    }

    void Update()
    {
        if (player == null)
            return;

        float distance = Vector2.Distance(transform.position, player.position);

        // Bay tới Player
        if (distance <= detectionRange && distance > attackRange)
        {
            transform.position = Vector2.MoveTowards(
                transform.position,
                player.position,
                speed * Time.deltaTime);
        }

        // Tấn công
        if (distance <= attackRange && !attacking)
        {
            StartCoroutine(Attack());
        }

        Flip();
    }

    IEnumerator Attack()
    {
        attacking = true;

        PlayerHealth1 health = player.GetComponent<PlayerHealth1>();

        if (health != null)
        {
            health.TakeDamage(damage);
        }

        yield return new WaitForSeconds(attackCooldown);

        attacking = false;
    }

    void Flip()
    {
        if (player == null)
            return;

        Vector3 scale = transform.localScale;

        if (player.position.x > transform.position.x)
            scale.x = Mathf.Abs(scale.x);
        else
            scale.x = -Mathf.Abs(scale.x);

        transform.localScale = scale;
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
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}