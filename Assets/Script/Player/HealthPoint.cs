using UnityEngine;

public class HealthPoint : MonoBehaviour
{
    [SerializeField] private float startingHealth;
    public float currentHealth { get; private set; }

    private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hurtColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;
    private Color originalColor;

    private bool dead;

    private void Awake()
    {
        currentHealth = startingHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void TakeDamage(float _damage)
    {
        currentHealth = Mathf.Clamp(currentHealth - _damage, 0, startingHealth);

        if (currentHealth > 0)
        {
            StartCoroutine(FlashEffect());
        }
        else
        {
            if (!dead)
            {
                gameObject.SetActive(false);
                dead = true;
            }
        }
    }

    private System.Collections.IEnumerator FlashEffect()
    {
        spriteRenderer.color = hurtColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }


}