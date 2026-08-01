using UnityEngine;
using System.Collections;

public class PlayerMovement1 : MonoBehaviour
{
    [Header("Di chuyển cơ bản")]
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Kỹ năng Lướt (Dash)")]
    public float dashSpeed = 20f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private Rigidbody2D rb;
    private bool isGrounded;

    private bool isDashing;
    private bool canDash = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Bỏ qua va chạm vật lý giữa 2 layer Player và Enemy
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
    }

    void Update()
    {
        // 1. NẾU ĐANG LƯỚT THÌ KHÔNG CHO ĐI BỘ HAY NHẢY
        if (isDashing)
        {
            return;
        }

        // Nhấn Left Shift để lướt
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartCoroutine(Dash());
            // CHÌA KHÓA: Dừng Update để lệnh đi bộ bên dưới không đè lên lực lướt
            return;
        }

        // Nhấn A/D hoặc Mũi tên trái phải để đi
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        // Lật mặt nhân vật quay sang trái/phải
        if (moveInput != 0)
        {
            transform.localScale = new Vector3(moveInput, 1, 1);
        }

        // Nhấn Space để nhảy (chỉ nhảy được khi chạm đất)
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    // --- LOGIC LƯỚT (DASH) ---
    private IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;

        // Reset vận tốc về 0 trước khi lướt
        rb.linearVelocity = Vector2.zero;

        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;

        // Lấy hướng chuẩn xác
        float dashDir = Mathf.Sign(transform.localScale.x);
        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);

        yield return new WaitForSeconds(dashDuration);

        rb.gravityScale = originalGravity;
        isDashing = false;

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) isGrounded = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground")) isGrounded = false;
    }
}