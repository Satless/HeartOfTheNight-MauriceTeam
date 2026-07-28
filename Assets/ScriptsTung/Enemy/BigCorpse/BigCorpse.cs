/*using UnityEngine;
using System.Collections;

public class BigCorpse : MonoBehaviour
{
<<<<<<< HEAD
=======
    [Header("Hoạt ảnh & Hitbox")]
    public Animator anim;
    public GameObject attackHitbox;

>>>>>>> main
    [Header("Tầm nhìn & Di chuyển")]
    public float moveSpeed = 5f;
    public float detectionRangeX = 12f;
    public float detectionRangeY = 3f;
    public float attackRange = 1.2f;

    [Header("Dịch chuyển an toàn")]
    public float platformHeightDiff = 1.5f;
    public float teleportDelay = 0.5f;
    public float postTeleportDelay = 0.5f;

<<<<<<< HEAD
    [Header("Sát thương & Hiệu ứng cháy")]
=======
    [Header("Sát thương")]
>>>>>>> main
    public int attackDamage = 10;
    public float attackCooldown = 1.5f;

    private Transform player;
    private Rigidbody2D rb;
    private Collider2D myCol;
    private float nextAttackTime = 0f;
    private float teleportTimer = 0f;
    private bool isBusy = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
        myCol = GetComponent<Collider2D>();
<<<<<<< HEAD
=======

        if (anim == null) anim = GetComponent<Animator>();

        if (attackHitbox != null)
        {
            hitboxScript = attackHitbox.GetComponent<EnemyHitbox>();
            if (hitboxScript != null) hitboxScript.attackDamage = attackDamage;
            attackHitbox.SetActive(false);
        }

        // Kích hoạt tính năng xuyên thấu
        SetupXuyenThau();
    }

    void SetupXuyenThau()
    {
        if (myCol == null) return;
        if (player != null)
        {
            Collider2D pCol = player.GetComponent<Collider2D>();
            if (pCol != null) Physics2D.IgnoreCollision(myCol, pCol, true);
        }
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in allEnemies)
        {
            Collider2D enemyCol = enemyObj.GetComponent<Collider2D>();
            if (enemyCol != null && enemyCol != myCol)
            {
                Physics2D.IgnoreCollision(myCol, enemyCol, true);
            }
        }
>>>>>>> main
    }

    void Update()
    {
<<<<<<< HEAD
        if (player == null || isBusy) return;
=======
        if (anim != null && !isBusy)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }

        if (player == null || isBusy || myCol == null) return;
        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol == null) return;

        float myFeetY = myCol.bounds.min.y;
        float playerFeetY = playerCol.bounds.min.y;
>>>>>>> main

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(playerFeetY - myFeetY);

        if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
        {
            if (distanceY > platformHeightDiff)
            {
                rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                teleportTimer += Time.deltaTime;
                if (teleportTimer >= teleportDelay)
                {
                    StartCoroutine(ThucHienTeleportAnToan());
                    teleportTimer = 0f;
                }
            }
            else
            {
                teleportTimer = 0f;

                float hitDistance = 999f;

                if (myCol != null && playerCol != null)
                    hitDistance = Physics2D.Distance(myCol, playerCol).distance;

                if (hitDistance > attackRange)
                {
                    Move();
                }
                else
                {
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
                    if (Time.time >= nextAttackTime)
                    {
                        StartCoroutine(AttackRoutine());
                    }
                }
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        }
    }

    void Move()
    {
        LookAtPlayer();
        float dir = (player.position.x > transform.position.x) ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y);
    }

    IEnumerator AttackRoutine()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;
        LookAtPlayer();

<<<<<<< HEAD
        yield return new WaitForSeconds(0.2f);

        float hitDistance = 999f;
        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (myCol != null && playerCol != null)
            hitDistance = Physics2D.Distance(myCol, playerCol).distance;

        if (hitDistance <= attackRange + 0.5f)
        {
            player.GetComponent<PlayerHealth>()?.TakeDamage(attackDamage);

            // Bạn có thể đổi AntiHeal thành script gây sát thương Thiêu Đốt (Burn) tại đây
            AntiHeal anti = player.GetComponent<AntiHeal>() ?? player.gameObject.AddComponent<AntiHeal>();
            anti.thoiGianConLai = 6f;
        }

        yield return new WaitForSeconds(0.3f);
=======
        if (anim != null) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.8f);
        DisableHitbox();
>>>>>>> main
        nextAttackTime = Time.time + attackCooldown;
        isBusy = false;
    }

    IEnumerator ThucHienTeleportAnToan()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;

<<<<<<< HEAD
=======
        if (anim != null) anim.SetTrigger("Teleport");
        yield return new WaitForSeconds(0.3f);

>>>>>>> main
        float dirSauLung = (player.localScale.x > 0) ? -1f : 1f;
        Vector2 viTriSauLung = new Vector2(player.position.x + (dirSauLung * 1.2f), player.position.y + 1f);

        float pivotToFeetOffset = transform.position.y - myCol.bounds.min.y;
        RaycastHit2D hit = Physics2D.Raycast(viTriSauLung, Vector2.down, 4f);

        if (hit.collider != null && !hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
            transform.position = new Vector2(viTriSauLung.x, hit.point.y + pivotToFeetOffset + 0.05f);
        else
        {
            float playerFeet = player.GetComponent<Collider2D>().bounds.min.y;
            transform.position = new Vector2(viTriSauLung.x, playerFeet + pivotToFeetOffset);
        }

        LookAtPlayer();
        yield return new WaitForSeconds(postTeleportDelay);
        isBusy = false;
    }

    void LookAtPlayer()
    {
        Vector3 scale = transform.localScale;
        scale.x = (player.position.x > transform.position.x) ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

<<<<<<< HEAD
    // ================= VẼ GIZMOS MÀU CAM =================
=======
    public void EnableHitbox()
    {
        if (attackHitbox != null) attackHitbox.SetActive(true);
    }

    public void DisableHitbox()
    {
        if (attackHitbox != null) attackHitbox.SetActive(false);
    }

>>>>>>> main
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f); // Màu Cam (Orange)
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}*/