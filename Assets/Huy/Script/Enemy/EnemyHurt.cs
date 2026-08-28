using UnityEngine;

public class EnemyHurt : MonoBehaviour
{
    [SerializeField] private EnemyHealthPoint healthPoint;
    [SerializeField] private float damageToSelf = 10f;
    [SerializeField] private float damageCooldown = 1.0f; 

    private float lastDamageTime;

    private void Awake()
    {
        if (healthPoint == null)
            healthPoint = GetComponentInParent<EnemyHealthPoint>();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (Time.time - lastDamageTime >= damageCooldown)
            {
                if (healthPoint != null)
                {
                    healthPoint.TakeDamage(damageToSelf);
                    lastDamageTime = Time.time; 
                }
            }
        }
    }
}