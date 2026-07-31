using UnityEngine;
using System.Collections;

public class EnemyDamage : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float damageCooldown = 1.0f;
    private bool canTakeDamage = true;

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") && canTakeDamage)
        {
            if (collision.gameObject.TryGetComponent<HealthPoint>(out HealthPoint playerHealth))
            {
                playerHealth.TakeDamage(damage);
                StartCoroutine(DamageCooldownRoutine());
            }
        }
    }

    private IEnumerator DamageCooldownRoutine()
    {
        canTakeDamage = false; 
        yield return new WaitForSeconds(damageCooldown); 
        canTakeDamage = true; 
    }
}