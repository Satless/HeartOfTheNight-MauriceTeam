using UnityEngine;
using HeartOfTheNight.Common;
using HeartOfTheNight.Enemy;

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
        if (EnemyCombatRules.TryGetPlayerDamageable(other, out var hp))
        {
            hp.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (other.CompareTag("Ground"))
        {
            Destroy(gameObject);
        }
    }
}