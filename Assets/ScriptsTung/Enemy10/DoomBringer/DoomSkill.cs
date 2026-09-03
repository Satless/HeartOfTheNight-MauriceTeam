using UnityEngine;
using HeartOfTheNight.Common;
using HeartOfTheNight.Enemy;

public class DoomSkill : MonoBehaviour
{
    [Header("Cài đặt Sát thương")]
    public int damage = 8;

    [Header("Dọn rác")]
    public float lifeTime = 5f;

    [Header("Hiệu ứng (Tùy chọn)")]
    public GameObject hitEffect;

    [Header("Chống lọt hurtbox (sweep)")]
    [SerializeField] private float sweepRadius = 0.45f;

    private Vector2 lastPos;
    private bool consumed;
    private int playerMask;
    private static readonly RaycastHit2D[] SweepHits = new RaycastHit2D[12];

    void Awake()
    {
        playerMask = LayerMask.GetMask("Player");
    }

    void Start()
    {
        lastPos = transform.position;
        Destroy(gameObject, lifeTime);
    }

    void FixedUpdate()
    {
        if (consumed) return;

        Vector2 now = transform.position;
        Vector2 delta = now - lastPos;
        float dist = delta.magnitude;
        if (dist > 0.001f)
        {
            int count = Physics2D.CircleCastNonAlloc(
                lastPos, sweepRadius, delta / dist, SweepHits, dist, playerMask);

            if (TryHitPlayerInSweep(count))
                return;
        }

        lastPos = now;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        TryHitPlayer(collision);
    }

    private bool TryHitPlayerInSweep(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (TryHitPlayer(SweepHits[i].collider))
                return true;
        }

        return false;
    }

    private bool TryHitPlayer(Collider2D collision)
    {
        if (consumed || collision == null) return false;
        if (EnemyCombatRules.IsEnemyCollider(collision)) return false;
        if (!EnemyCombatRules.TryGetPlayerDamageable(collision, out IDamageable target))
            return false;

        target.TakeDamage(damage);
        TuHuy();
        return true;
    }

    void TuHuy()
    {
        if (consumed) return;
        consumed = true;

        if (hitEffect != null)
            Instantiate(hitEffect, transform.position, Quaternion.identity);

        Destroy(gameObject);
    }
}
