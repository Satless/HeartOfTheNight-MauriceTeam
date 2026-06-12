using UnityEngine;
using System.Collections;

public class BigCorpse : MonoBehaviour
{
    [Header("Tầm nhìn (Quét Ngang & Dọc)")]
    public float moveSpeed = 3f;
    public float detectionRangeX = 7f;
    public float detectionRangeY = 2.5f;
    public float attackRange = 1.2f;

    [Header("Dịch chuyển")]
    public float platformHeightDiff = 1.5f;
    public float teleportDelay = 0.5f;
    public float postTeleportDelay = 0.5f;

    [Header("Sát thương Cận chiến")]
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;

    private Transform player;
    private float nextAttackTime = 0f;
    private float teleportTimer = 0f;
    private bool isBusy = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null || isBusy) return;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);

        // ĐÃ ĐỔI SANG TẦM NHÌN CHỮ NHẬT
        if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
        {
            if (distanceY > platformHeightDiff)
            {
                teleportTimer += Time.deltaTime;
                if (teleportTimer >= teleportDelay)
                {
                    StartCoroutine(ThucHienTeleport());
                    teleportTimer = 0f;
                }
            }
            else
            {
                teleportTimer = 0f;

                if (distanceX > attackRange)
                {
                    Move();
                }
                else if (Time.time >= nextAttackTime)
                {
                    Attack();
                    nextAttackTime = Time.time + attackCooldown;
                }
            }
        }
    }

    void Move()
    {
        LookAtPlayer();
        Vector2 targetPosition = new Vector2(player.position.x, transform.position.y);
        transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
    }

    IEnumerator ThucHienTeleport()
    {
        isBusy = true;

        float standBehind = (player.localScale.x > 0) ? -1f : 1f;
        transform.position = new Vector2(player.position.x + standBehind, player.position.y);
        LookAtPlayer();

        yield return new WaitForSeconds(postTeleportDelay);

        isBusy = false;
    }

    void LookAtPlayer()
    {
        Vector3 scale = transform.localScale;
        if (player.position.x > transform.position.x) scale.x = Mathf.Abs(scale.x);
        else scale.x = -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    void Attack()
    {
        if (player.GetComponent<PlayerHealth>() != null)
        {
            player.GetComponent<PlayerHealth>().TakeDamage(attackDamage);
        }
    }

    // ================= VẼ TẦM NHÌN TRONG EDITOR =================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}