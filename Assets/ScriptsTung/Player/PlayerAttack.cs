using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    public Transform attackPoint; // Tạo 1 gameobject rỗng làm con của Player, đặt ở đằng trước
    public float attackRange = 0.5f;
    public int attackDamage = 20;
    public LayerMask enemyLayers; // Chỉnh layer của quái trong Inspector

    void Update()
    {
        if (Input.GetButtonDown("Fire1")) // Chuột trái
        {
            Attack();
        }
    }

    void Attack()
    {
        // Tạo vòng tròn sát thương
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemy in hitEnemies)
        {
            // Gọi script máu của quái
            enemy.GetComponent<EnemyHealth>().TakeDamage(attackDamage);
        }
    }

    // Vẽ vòng tròn đỏ trong Scene để dễ căn chỉnh
    void OnDrawGizmosSelected()
    {
        if (attackPoint != null) Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}