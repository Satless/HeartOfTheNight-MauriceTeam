using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    public int health = 50;

    void Start()
    {
        // TỰ ĐỘNG TẮT VA CHẠM GIỮA CÁC QUÁI VỚI NHAU
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        // Kiểm tra xem đã có Layer "Enemy" chưa
        if (enemyLayer != -1)
        {
            // Lệnh thần thánh: Cho phép Layer "Enemy" lơ luôn chính Layer "Enemy"
            Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log(gameObject.name + " bị chém! Máu còn: " + health);

        if (health <= 0)
        {
            Destroy(gameObject); // Chết thì xoá quái
        }
    }
}