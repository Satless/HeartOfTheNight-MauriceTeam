using HeartOfTheNight.Common;
using UnityEngine;

public class TestPlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Player HP")]
    public int maxHP = 100;

    private int currentHP;

    private void Start()
    {
        currentHP = maxHP;
    }

    public void TakeDamage(int damage)
    {
        // Trừ máu
        currentHP -= damage;

        // Không cho HP âm
        if (currentHP < 0)
            currentHP = 0;

        // Thông báo HP còn lại
        Debug.Log("Player bị mất " + damage + " HP. HP còn lại: " + currentHP + "/" + maxHP);

        // Nếu hết máu
        if (currentHP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Player đã chết!");
        
    }
}