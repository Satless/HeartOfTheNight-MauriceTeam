/*using UnityEngine;
using System.Collections;

public class BurningCorpse : MonoBehaviour
{
    [Header("Tầm nhìn & Di chuyển")]
    public float detectionRangeX = 12f;
    public float detectionRangeY = 3f; // Để 3f cho thoải mái nhảy
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
    private float nextAttackTime = 0f;
    private float teleportTimer = 0f;
    private bool isBusy = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (player == null || isBusy) return;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);

        // PHÁT HIỆN THEO HÌNH CHỮ NHẬT X, Y
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
                    rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Phanh gấp
                    if (Time.time >= nextAttackTime)
                    {
                        Attack();
                        nextAttackTime = Time.time + attackCooldown;
                    }
                }
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(0, rb.linearVelocity.y); // Mất dấu thì đứng im
        }
    }

    void Move()
    {
        LookAtPlayer();
        float dir = (player.position.x > transform.position.x) ? 1 : -1;
        rb.linearVelocity = new Vector2(dir * moveSpeed, rb.linearVelocity.y); // Chống đẩy người chơi
    }

    IEnumerator ThucHienTeleport() 
    { 
        isBusy = true; 
        rb.linearVelocity = Vector2.zero; 
        float standBehind = (player.localScale.x > 0) ? -1f : 1f; 
        transform.position = new Vector2(player.position.x + standBehind, player.position.y); 
        LookAtPlayer(); 
        yield return new WaitForSeconds(postTeleportDelay); 
        isBusy = false; 
    }

    void LookAtPlayer() 
    { 
        transform.localScale = new Vector3((player.position.x > transform.position.x ? 1 : -1) * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1); 
    }

    void Attack() 
    { 
        PlayerHealth pHealth = player.GetComponent<PlayerHealth>(); 
        if (pHealth != null) 
        { 
            pHealth.TakeDamage(attackDamage); 
            StartCoroutine(GayHieuUngChay(pHealth)); 
        } 
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f); // Cam
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}*/