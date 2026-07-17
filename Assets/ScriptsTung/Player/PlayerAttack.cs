using UnityEngine;

public class PlayerAttackk : MonoBehaviour
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

        foreach (Collider2D enemy in hitEnemies)
        {
            Debug.Log("Player chém trúng: " + enemy.name);

            // 1. Dành cho quái thường
            EnemyHealth enemyHp = enemy.GetComponent<EnemyHealth>();
            if (enemyHp != null)
            {
                enemyHp.TakeDamage(attackDamage);
            }

            // 2. Dành cho Boss Doom Bringer
            DoomBringer boss = enemy.GetComponent<DoomBringer>();
            if (boss != null)
            {
                boss.TakeDamage(attackDamage);
            }
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