using UnityEngine;
using HeartOfTheNight.Common;

public class Enemy : MonoBehaviour, IDamageable
{
    [Header("HP")]
    public int hp = 150;


[Header("Shield")]
    public GameObject shieldPrefab;

    private GameObject shieldObject;
    public bool isProtected;

    [Header("Patrol")]
    public float moveSpeed = 2f;
    public float patrolDistance = 3f;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private bool movingRight = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    private void FixedUpdate()
    {
        Patrol();
    }

    void Patrol()
    {
        if (movingRight)
        {
            rb.linearVelocity =
                new Vector2(moveSpeed, rb.linearVelocity.y);

            if (transform.position.x >=
                startPosition.x + patrolDistance)
            {
                TurnAround();
            }
        }
        else
        {
            rb.linearVelocity =
                new Vector2(-moveSpeed, rb.linearVelocity.y);

            if (transform.position.x <=
                startPosition.x - patrolDistance)
            {
                TurnAround();
            }
        }
    }

    void TurnAround()
    {
        movingRight = !movingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    public void EnableShield()
    {
        isProtected = true;

        if (shieldPrefab == null)
            return;

        if (shieldObject == null)
        {
            shieldObject =
                Instantiate(shieldPrefab, transform);
        }

        shieldObject.transform.localPosition =
            Vector3.zero;
    }

    public void DisableShield()
    {
        isProtected = false;

        if (shieldObject != null)
        {
            Destroy(shieldObject);
            shieldObject = null;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isProtected)
        {
            Debug.Log(gameObject.name +
                      " đang được bảo vệ");
            return;
        }

        hp -= damage;

        Debug.Log(gameObject.name +
                  " mất " + damage);

        if (hp <= 0)
        {
            Destroy(gameObject);
        }
    }


}
