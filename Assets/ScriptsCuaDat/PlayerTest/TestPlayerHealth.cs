using HeartOfTheNight.Common;
using UnityEngine;

public class TestPlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int hp = 100;
    public void TakeDamage(int amount)
    {
        hp -= amount;
        Debug.Log($"Player HP: {hp}");
        if (hp <= 0) Destroy(gameObject);
    }
}