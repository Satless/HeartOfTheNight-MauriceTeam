using UnityEngine;
using System.Collections;

public class Automaton : MonoBehaviour
{
    [Header("Tầm nhìn & Di chuyển")]
    public float moveSpeed = 4f;
    public float detectionRangeX = 12f;
    public float detectionRangeY = 3f;
    public float dashRange = 5.5f;
    public float attackRange = 2f;

    [Header("Dịch chuyển an toàn")]
    public float platformHeightDiff = 0.8f;
    public float teleportDelay = 0.5f;
    public float postTeleportDelay = 0.6f;
    public float teleportYOffset = 0f;

    [Header("Sát thương")]
    public int meleeDamage = 10;
    public int dashDamage = 20;

    [Header("Chỉ số Lướt (Dash)")]
    public float dashSpeed = 25f;
    public float chargeTime = 0.6f;
    public float dashDuration = 0.35f;
    public float dashCooldown = 4f;
    public float meleeCooldown = 2f; // Thời gian NGHỈ sau khi chém xong combo

    private Transform player;
    private Rigidbody2D rb;
    private bool dangBanRaDon = false;
    private float nextDashTime = 0f;
    private float nextMeleeTime = 0f;
    private float teleportTimer = 0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (player == null || dangBanRaDon) return;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);

        if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
        {
            if (distanceY > platformHeightDiff || (distanceX <= attackRange && distanceY > 0.8f))
            {
                StopMoving();
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

                if (distanceX <= attackRange)
                {
                    StopMoving();
                    if (Time.time >= nextMeleeTime)
                    {
                        StartCoroutine(ChemLienHoan());
                        // Đã xóa dòng đếm Cooldown ở đây
                    }
                }
                else if (distanceX <= dashRange && distanceX > attackRange && Time.time >= nextDashTime)
                {
                    StopMoving();
                    StartCoroutine(GongVaLuot());
                    // Đã xóa dòng đếm Cooldown ở đây
                }
                else
                {
                    Move();
                }
            }
        }
        else
        {
            StopMoving();
        }
    }

    void Move()
    {
        LookAtPlayer();
        float huong = (player.position.x > transform.position.x) ? 1f : -1f;
        rb.linearVelocity = new Vector2(huong * moveSpeed, rb.linearVelocity.y);
    }

    void StopMoving()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
    }

    void LookAtPlayer()
    {
        transform.localScale = new Vector3((player.position.x > transform.position.x ? 1 : -1) * Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
    }

    IEnumerator ThucHienTeleport()
    {
        dangBanRaDon = true;
        StopMoving();
        transform.position = TimViTriTeleport();
        LookAtPlayer();
        yield return new WaitForSeconds(postTeleportDelay);
        dangBanRaDon = false;
    }

    Vector2 TimViTriTeleport()
    {
        float standBehind = (player.localScale.x > 0) ? -1f : 1f;
        float targetX = player.position.x + standBehind;
        if (ThuTimDat(targetX, out float groundY)) return new Vector2(targetX, groundY);
        float standFront = -standBehind;
        float targetX_Front = player.position.x + standFront;
        if (ThuTimDat(targetX_Front, out float groundY_Front)) return new Vector2(targetX_Front, groundY_Front);
        return new Vector2(player.position.x, player.position.y);
    }

    bool ThuTimDat(float xPos, out float groundY)
    {
        groundY = 0f;
        RaycastHit2D[] hits = Physics2D.RaycastAll(new Vector2(xPos, player.position.y + 2f), Vector2.down, 5f);
        foreach (RaycastHit2D hit in hits)
        {
            if (!hit.collider.CompareTag("Player") && hit.collider.gameObject.layer != LayerMask.NameToLayer("Enemy") && !hit.collider.isTrigger)
            {
                groundY = hit.point.y + GetComponent<Collider2D>().bounds.extents.y + teleportYOffset;
                return true;
            }
        }
        return false;
    }

    IEnumerator ChemLienHoan()
    {
        dangBanRaDon = true;
        StopMoving(); // Đảm bảo quái khựng lại khi chém

        for (int i = 0; i < 3; i++)
        {
            LookAtPlayer();
            if (Mathf.Abs(player.position.x - transform.position.x) <= attackRange + 0.5f)
                player.GetComponent<PlayerHealth>()?.TakeDamage(meleeDamage);

            yield return new WaitForSeconds(0.4f);
        }

        // CHÌA KHÓA: Bắt đầu tính Cooldown sau khi combo kết thúc
        nextMeleeTime = Time.time + meleeCooldown;
        dangBanRaDon = false;
    }

    IEnumerator GongVaLuot()
    {
        dangBanRaDon = true;
        StopMoving(); // Gồng là phải đứng im
        LookAtPlayer();

        yield return new WaitForSeconds(chargeTime);

        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        rb.linearVelocity = new Vector2(Mathf.Sign(transform.localScale.x) * dashSpeed, 0f);

        bool daGayDam = false;
        float thoiGianDaLuot = 0f;

        while (thoiGianDaLuot < dashDuration)
        {
            if (!daGayDam && Mathf.Abs(player.position.x - transform.position.x) <= attackRange)
            {
                player.GetComponent<PlayerHealth>()?.TakeDamage(dashDamage);
                daGayDam = true;
            }
            thoiGianDaLuot += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // CHÌA KHÓA: Tính Cooldown dash sau khi lướt xong
        nextDashTime = Time.time + dashCooldown;
        dangBanRaDon = false;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.6f, 0f);
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, dashRange);
    }
}