using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int health = 100;
    public int maxHealth = 100;

    // Hàm bị quái đánh trúng
    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Player bị đánh! Máu còn: " + health);

        if (health <= 0)
        {
            Debug.Log("Player chết!");
            Destroy(gameObject); // Xoá player
        }
    }

    // Hàm hồi máu (khi ăn bình máu)
    public void Heal(int amount)
    {
        health += amount;

        // Không cho hồi vượt quá max máu
        if (health > maxHealth)
        {
            health = maxHealth;
        }

        Debug.Log("Player được hồi máu! Máu hiện tại: " + health);
    }
}