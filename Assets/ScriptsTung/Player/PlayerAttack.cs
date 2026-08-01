using UnityEngine;

public class PlayerAttack2 : MonoBehaviour
{
    [Header("Cài đặt Đòn đánh")]
    public Transform attackPoint;
    public float attackRange = 1f;
    public int attackDamage = 100;
    public float attackRate = 1f;
    public LayerMask enemyLayers;

    private float nextAttackTime = 0f;

    void Update()
    {
        if (Time.time >= nextAttackTime)
        {
            // Bấm Chuột trái hoặc phím J để chém
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J))
            {
                Attack();
                nextAttackTime = Time.time + 1f / attackRate;
            }
        }
    }

    void Attack()
    {
        // Tạo vòng tròn quét trúng quái
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);

        foreach (Collider2D enemyHit in hitEnemies)
        {
            // 1. BỎ QUA nếu chém trúng cái Hitbox (isTrigger) của quái, chỉ chém vào thân chính
            if (enemyHit.isTrigger) continue;

            // 2. CHẶN SÁT THƯƠNG KHIÊN: Bỏ qua nếu quái không mang Tag Enemy hoặc Boss
            // (Lúc có khiên, Mắt Đêm đã lột Tag đi nên lệnh này sẽ đá văng nhát chém)
            if (!enemyHit.CompareTag("Enemy") && !enemyHit.CompareTag("Boss")) continue;

            Debug.Log("Player chém trúng: " + enemyHit.name);

            // 3. Gây sát thương nếu thỏa mãn mọi điều kiện
            enemyHit.SendMessageUpwards("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
        }
    }

    // Vẽ vòng tầm đánh trong Editor
    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}