using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 10f;

    [Header("Cài đặt Dash (Lướt)")]
    public float dashForce = 15f; // Tốc độ lướt
    public float dashDuration = 0.2f; // Thời gian lướt (0.2s là mượt)
    public float dashCooldown = 1f; // Đợi 1s mới được Dash tiếp

    private Rigidbody2D rb;
    private bool isGrounded;

    // Biến cho Dash
    private bool isDashing;
    private float dashTimeLeft;
    private float lastDashTime;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        int playerLayer = gameObject.layer;
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        if (enemyLayer != -1)
        {
            Physics2D.IgnoreLayerCollision(playerLayer, enemyLayer, true);
        }
    }

    void Update()
    {
        // 1. Kích hoạt Dash khi bấm Left Shift
        if (Input.GetKeyDown(KeyCode.LeftShift) && Time.time >= lastDashTime + dashCooldown)
        {
            PerformDash();
        }

        // 2. Nếu đang trong trạng thái Dash thì bỏ qua việc đi lại bình thường
        if (isDashing)
        {
            dashTimeLeft -= Time.deltaTime;
            if (dashTimeLeft <= 0)
            {
                isDashing = false; // Hết 0.2s thì dừng lướt
            }
            return; // Chặn code bên dưới chạy
        }

        // 3. Đi trái phải bình thường
        float moveInput = Input.GetAxisRaw("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * moveSpeed, rb.linearVelocity.y);

        if (moveInput != 0)
        {
            transform.localScale = new Vector3(Mathf.Sign(moveInput), 1, 1);
        }

        // 4. Nhảy
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }
    }

    // Hàm xử lý lướt
    void PerformDash()
    {
        isDashing = true;
        dashTimeLeft = dashDuration;
        lastDashTime = Time.time;

        // Bắn nhân vật về phía trước
        rb.linearVelocity = new Vector2(transform.localScale.x * dashForce, 0);

        // GỌI HÀM DẬP LỬA BÊN SCRIPT PLAYER HEALTH
        GetComponent<PlayerHealth>().CureBurn();
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