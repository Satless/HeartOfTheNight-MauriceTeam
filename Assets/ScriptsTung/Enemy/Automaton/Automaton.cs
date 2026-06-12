using UnityEngine;
using System.Collections;

public class Automaton : MonoBehaviour
{
    [Header("Tầm nhìn (Quét Ngang & Dọc)")]
    public float moveSpeed = 3f;
    public float detectionRangeX = 12f; // Nhìn xa theo chiều ngang
    public float detectionRangeY = 2.5f; // Giới hạn chiều cao
    public float dashRange = 5.5f;
    public float meleeRange = 2f;

    [Header("Dịch chuyển (Chuẩn BigCorpse)")]
    public float platformHeightDiff = 0.8f;
    public float teleportDelay = 0.5f;
    public float postTeleportDelay = 0.6f;
    public float teleportYOffset = 0f;

    private float teleportTimer = 0f;

    [Header("Sát thương")]
    public int meleeDamage = 10;
    public int dashDamage = 20;

    [Header("Chỉ số Lướt (Dash)")]
    public float dashSpeed = 25f;
    public float chargeTime = 0.6f;
    public float dashDuration = 0.35f;
    public float dashCooldown = 4f;

    [Header("Thời gian nghỉ chém")]
    public float meleeCooldown = 2f;

    private Transform player;
    private Rigidbody2D rb;
    private bool dangBanRaDon = false;

    private float nextDashTime = 0f;
    private float nextMeleeTime = 0f;

    private Vector2 viTriCu;
    private float thoiGianKet = 0f;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        rb = GetComponent<Rigidbody2D>();
        viTriCu = transform.position;
    }

    void Update()
    {
        if (player == null || dangBanRaDon) return;

        float distanceX = Mathf.Abs(player.position.x - transform.position.x);
        float distanceY = Mathf.Abs(player.position.y - transform.position.y);

        // ĐÃ ĐỔI SANG TẦM NHÌN CHỮ NHẬT
        if (distanceX <= detectionRangeX && distanceY <= detectionRangeY)
        {
            if (distanceY > platformHeightDiff || (distanceX <= meleeRange && distanceY > 0.8f))
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

                if (distanceX <= meleeRange)
                {
                    StopMoving();
                    if (Time.time >= nextMeleeTime)
                    {
                        StartCoroutine(ChemLienHoan());
                        nextMeleeTime = Time.time + meleeCooldown;
                    }
                }
                else if (distanceX <= dashRange && distanceX > meleeRange && Time.time >= nextDashTime)
                {
                    StopMoving();
                    StartCoroutine(GongVaLuot());
                    nextDashTime = Time.time + dashCooldown;
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

        if (Mathf.Abs(transform.position.x - viTriCu.x) < 0.005f)
        {
            thoiGianKet += Time.deltaTime;
            if (thoiGianKet >= 0.3f)
            {
                StartCoroutine(ThucHienTeleport());
                thoiGianKet = 0f;
            }
        }
        else
        {
            thoiGianKet = 0f;
        }
        viTriCu = transform.position;
    }

    void StopMoving()
    {
        rb.linearVelocity = new Vector2(0, rb.linearVelocity.y);
        thoiGianKet = 0f;
    }

    void LookAtPlayer()
    {
        Vector3 scale = transform.localScale;
        scale.x = (player.position.x > transform.position.x) ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    IEnumerator ThucHienTeleport()
    {
        dangBanRaDon = true;
        StopMoving();

        transform.position = TimViTriTeleport();
        LookAtPlayer();

        yield return new WaitForSeconds(postTeleportDelay);

        thoiGianKet = 0f;
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
        Vector2 origin = new Vector2(xPos, player.position.y + 2f);

        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 5f);

        foreach (RaycastHit2D hit in hits)
        {
            if (!hit.collider.CompareTag("Player") && hit.collider.gameObject.layer != LayerMask.NameToLayer("Enemy") && !hit.collider.isTrigger)
            {
                float nuaChieuCao = GetComponent<Collider2D>().bounds.extents.y;
                groundY = hit.point.y + nuaChieuCao + teleportYOffset;
                return true;
            }
        }
        return false;
    }

    IEnumerator ChemLienHoan()
    {
        dangBanRaDon = true;

        for (int i = 0; i < 3; i++)
        {
            LookAtPlayer();
            if (Vector2.Distance(transform.position, player.position) <= meleeRange + 0.5f)
            {
                player.GetComponent<PlayerHealth>().TakeDamage(meleeDamage);
            }
            yield return new WaitForSeconds(0.4f);
        }

        dangBanRaDon = false;
    }

    IEnumerator GongVaLuot()
    {
        dangBanRaDon = true;
        LookAtPlayer();

        yield return new WaitForSeconds(chargeTime);

        rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;

        float huongLuot = Mathf.Sign(transform.localScale.x);
        rb.linearVelocity = new Vector2(huongLuot * dashSpeed, 0f);

        bool daGayDam = false;
        float thoiGianDaLuot = 0f;

        while (thoiGianDaLuot < dashDuration)
        {
            if (!daGayDam && Vector2.Distance(transform.position, player.position) <= meleeRange)
            {
                player.GetComponent<PlayerHealth>().TakeDamage(dashDamage);
                daGayDam = true;
            }
            thoiGianDaLuot += Time.deltaTime;
            yield return null;
        }

        rb.linearVelocity = Vector2.zero;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        dangBanRaDon = false;
    }

    // ================= VẼ TẦM NHÌN TRONG EDITOR =================
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(detectionRangeX * 2, detectionRangeY * 2, 0));

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, dashRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}