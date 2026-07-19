using UnityEngine;
using System.Collections;

public class BurningCorpse : MonoBehaviour
{
    [Header("Hoạt ảnh & Hitbox")]
    public Animator anim;
    public GameObject attackHitbox;

    [Header("Tầm nhìn & Di chuyển")]
    public float detectionRangeX = 12f;
    public float detectionRangeY = 3f;
    public float moveSpeed = 4f;
    public float attackRange = 2f;

    [Header("Dịch chuyển")]
    public float platformHeightDiff = 0.8f;
    public float teleportDelay = 1f;
    public float postTeleportDelay = 0.5f;

    [Header("Sát thương & Hiệu ứng Cháy")]
    public int attackDamage = 10;
    public float attackCooldown = 2f;
    public int burnDamagePerTick = 2;
    public int burnTicks = 3;
    public float timeBetweenTicks = 1f;

    [Header("Cảm biến dập lửa")]
    public float dashSpeedThreshold = 12f;

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

        if (anim == null) anim = GetComponent<Animator>();

        if (attackHitbox != null)
        {
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
    }

    void Update()
    {
        if (anim != null && !isBusy)
        {
            anim.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        }

        if (player == null || isBusy || myCol == null) return;

        Collider2D playerCol = player.GetComponent<Collider2D>();
        if (playerCol == null) return;

        float myFeetY = myCol.bounds.min.y;
        float playerFeetY = playerCol.bounds.min.y;

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

        if (anim != null) anim.SetTrigger("Attack");

        yield return new WaitForSeconds(0.8f);
        DisableHitbox();
        nextAttackTime = Time.time + attackCooldown;
        isBusy = false;
    }

    IEnumerator ThucHienTeleport()
    {
        isBusy = true;
        rb.linearVelocity = Vector2.zero;

        if (anim != null) anim.SetTrigger("Teleport");
        yield return new WaitForSeconds(0.3f);

        float standBehind = (player.localScale.x > 0) ? -1f : 1f;
        Vector2 viTriMoi = new Vector2(player.position.x + standBehind, player.position.y + 1f);

        float pivotToFeetOffset = transform.position.y - myCol.bounds.min.y;

        RaycastHit2D hit = Physics2D.Raycast(viTriMoi, Vector2.down, 3f);
        if (hit.collider != null && !hit.collider.CompareTag("Player") && !hit.collider.isTrigger)
        {
            transform.position = new Vector2(viTriMoi.x, hit.point.y + pivotToFeetOffset + 0.05f);
        }
        else
        {
            float playerFeet = player.GetComponent<Collider2D>().bounds.min.y;
            transform.position = new Vector2(viTriMoi.x, playerFeet + pivotToFeetOffset);
        }

        LookAtPlayer();
        yield return new WaitForSeconds(postTeleportDelay);
        isBusy = false;
    }

    public void DealDamageAndBurn(PlayerHealth pHealth)
    {
        pHealth.TakeDamage(attackDamage);
        StartCoroutine(GayHieuUngChay(pHealth));
    }

    IEnumerator GayHieuUngChay(PlayerHealth pHealth)
    {
        Rigidbody2D playerRb = pHealth.GetComponent<Rigidbody2D>();
        for (int i = 0; i < burnTicks; i++)
        {
            float thoiGianDaCho = 0f;
            while (thoiGianDaCho < timeBetweenTicks)
            {
                if (playerRb != null && Mathf.Abs(playerRb.linearVelocity.x) >= dashSpeedThreshold) yield break;
                thoiGianDaCho += Time.deltaTime;
                yield return null;
            }
            if (pHealth != null) pHealth.TakeDamage(burnDamagePerTick);
            else yield break;
        }
    }

    void LookAtPlayer()
    {
        transform.localScale = new Vector3((player.position.x > transform.position.x ? 1 : -1) * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }

    public void EnableHitbox()
    {
        if (attackHitbox != null) attackHitbox.SetActive(true);
    }

    public void DisableHitbox()
    {
        if (attackHitbox != null) attackHitbox.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}