using UnityEngine;
using System.Collections;

public class FireDamage : MonoBehaviour
{
    [SerializeField] private float damage = 10f;
    [SerializeField] private float damageCooldown = 1.0f;
    private bool canTakeDamage = true;

    private void OnTriggerStay2D(Collider2D collision)
    {
        Debug.Log("Đang chạm vào: " + collision.name + " với tag: " + collision.tag);
        if (collision.CompareTag("Player") && canTakeDamage)
        {
            if (collision.TryGetComponent<HealthPoint>(out HealthPoint playerHealth))
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