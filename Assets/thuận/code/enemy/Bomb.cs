using UnityEngine;

public class Bomb : MonoBehaviour
{
    public int damage = 50;
    public float speed = 8f;
    public float destroyTime = 5f;

    void Start()
    {
        Destroy(gameObject, destroyTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerHealth1 hp = other.GetComponent<PlayerHealth1>();

            if (hp != null)
                hp.TakeDamage(damage);

            Destroy(gameObject);
        }

        if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}