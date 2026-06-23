using System.Collections;
using UnityEngine;
using HeartOfTheNight.Common;

[RequireComponent(typeof(Rigidbody2D))]
public class TestPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed     = 7f;
    [SerializeField] private float groundAccel   = 60f;
    [SerializeField] private float airAccel      = 30f;
    [Tooltip("Tốc độ ngang tối đa khi đang trên không, theo tỉ lệ của moveSpeed. 1 = như dưới đất, nhỏ hơn = nhảy gần hơn.")]
    [Range(0f, 1f)]
    [SerializeField] private float airMoveMultiplier = 0.6f;

    [Header("Jump")]
    [SerializeField] private float jumpForce     = 14f;
    [SerializeField] private float coyoteTime    = 0.1f;
    [SerializeField] private float jumpBuffer    = 0.1f;
    [SerializeField] private float fallGravityMult = 2f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Vector2   groundCheckSize = new(0.5f, 0.08f);
    [SerializeField] private LayerMask groundLayer;

    [Header("Drop Through (One-Way Platform)")]
    [Tooltip("Vertical input at/below this counts as holding Down.")]
    [SerializeField] private float downThreshold   = -0.5f;
    [Tooltip("How long collision with the platform is disabled while dropping through.")]
    [SerializeField] private float dropThroughTime = 0.35f;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Collider2D[] colliders;
    private float inputX;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float baseGravity;
    private bool  isDropping;
    private bool  wasHoldingDown;

    private void Awake()
    {
        rb          = GetComponent<Rigidbody2D>();
        sprite      = GetComponentInChildren<SpriteRenderer>();
        colliders   = GetComponentsInChildren<Collider2D>();
        baseGravity = rb.gravityScale;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        inputX   = Input.GetAxisRaw("Horizontal");

        coyoteCounter = IsGrounded()
            ? coyoteTime
            : coyoteCounter - Time.deltaTime;

        if (Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBuffer;
        else                              jumpBufferCounter -= Time.deltaTime;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter     = 0f;
        }

        bool holdingDown = Input.GetAxisRaw("Vertical") <= downThreshold;
        if (holdingDown && !wasHoldingDown && !isDropping
            && TryGetOneWayPlatformBelow(out var dropPlatform))
        {
            StartCoroutine(DropThrough(dropPlatform));
        }
        wasHoldingDown = holdingDown;

        if (sprite != null && Mathf.Abs(inputX) > 0.01f)
            sprite.flipX = inputX < 0f;
    }

    private void FixedUpdate()
    {
        bool  grounded    = IsGrounded();
        float targetSpeed = inputX * moveSpeed * (grounded ? 1f : airMoveMultiplier);
        float accelRate   = grounded ? groundAccel : airAccel;
        float newX        = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed,
                                              accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        if (rb.linearVelocity.y < 0f)
            rb.gravityScale = baseGravity * fallGravityMult;
        else
            rb.gravityScale = baseGravity;
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    private bool TryGetOneWayPlatformBelow(out OneWayPlatform platform)
    {
        platform = null;
        if (groundCheck == null) return false;

        Collider2D hit = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
        if (hit == null) return false;

        platform = hit.GetComponent<OneWayPlatform>();
        return platform != null;
    }

    private IEnumerator DropThrough(OneWayPlatform platform)
    {
        isDropping = true;

        foreach (var col in colliders)
            if (col != null) Physics2D.IgnoreCollision(col, platform.Collider, true);

        yield return new WaitForSeconds(dropThroughTime);

        foreach (var col in colliders)
            if (col != null) Physics2D.IgnoreCollision(col, platform.Collider, false);

        isDropping = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
