using UnityEngine;
using System.Collections;

public class Automaton : MonoBehaviour
{
    [Header("Tầm nhìn & Di chuyển")]
    public float moveSpeed = 3f;
    public float detectionRange = 12f;
    public float dashRange = 5.5f;
    public float meleeRange = 2f;

    [Header("Dịch chuyển (Chuẩn BigCorpse)")]
    public float platformHeightDiff = 0.8f;
    public float teleportDelay = 0.5f;
    public float postTeleportDelay = 0.6f; // Hồi chiêu sau khi bay tới
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
        float totalDistance = Vector2.Distance(transform.position, player.position);

        if (totalDistance <= detectionRange)
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

    // ================= CHIÊU THỨC VÀ HÀNH ĐỘNG =================

    IEnumerator ThucHienTeleport()
    {
        dangBanRaDon = true;
        StopMoving();

        // 1. TÌM VỊ TRÍ ĐÁP ĐẤT AN TOÀN
        transform.position = TimViTriTeleport();
        LookAtPlayer();

        // 2. Đứng chờ
        yield return new WaitForSeconds(postTeleportDelay);

        thoiGianKet = 0f;
        dangBanRaDon = false;
    }

    // --- CÔNG CỤ TÌM MẶT ĐẤT ---
    Vector2 TimViTriTeleport()
    {
        float standBehind = (player.localScale.x > 0) ? -1f : 1f;
        float targetX = player.position.x + standBehind;

        // Ưu tiên 1: Quét xem sau lưng có đất không
        if (ThuTimDat(targetX, out float groundY))
        {
            return new Vector2(targetX, groundY);
        }

        // Ưu tiên 2: Nếu sau lưng là vực, quét thử đằng TRƯỚC mặt
        float standFront = -standBehind;
        float targetX_Front = player.position.x + standFront;
        if (ThuTimDat(targetX_Front, out float groundY_Front))
        {
            return new Vector2(targetX_Front, groundY_Front);
        }

        // Ưu tiên 3: Nếu Player đang bay trên không (cả trước và sau đều không có đất), thì bay thẳng vào tọa độ Player
        return new Vector2(player.position.x, player.position.y);
    }

    // Bắn 1 tia từ trên cao xuống đất để dò địa hình
    bool ThuTimDat(float xPos, out float groundY)
    {
        groundY = 0f;
        Vector2 origin = new Vector2(xPos, player.position.y + 2f); // Bắt đầu quét từ ngang đầu Player

        // Quét xuyên mọi thứ
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 5f);

        foreach (RaycastHit2D hit in hits)
        {
            // Nếu đụng trúng cục gạch (Không phải Player, quái, hay vùng trigger)
            if (!hit.collider.CompareTag("Player") && hit.collider.gameObject.layer != LayerMask.NameToLayer("Enemy") && !hit.collider.isTrigger)
            {
                float nuaChieuCao = GetComponent<Collider2D>().bounds.extents.y;
                groundY = hit.point.y + nuaChieuCao + teleportYOffset; // Tính ra tọa độ Y chuẩn
                return true;
            }
        }
        return false;
    }
    // ----------------------------

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
}