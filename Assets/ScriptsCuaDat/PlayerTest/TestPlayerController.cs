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

    [Header("Jump")]
    [SerializeField] private float jumpForce     = 14f;
    [SerializeField] private float coyoteTime    = 0.1f;
    [SerializeField] private float jumpBuffer    = 0.1f;
    [SerializeField] private float fallGravityMult = 2f;
    [SerializeField] private float lowJumpMult    = 2f;

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
    private bool  jumpHeld;
    private bool  isDropping;

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
        jumpHeld = Input.GetButton("Jump");

        coyoteCounter = IsGrounded()
            ? coyoteTime
            : coyoteCounter - Time.deltaTime;

        if (Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBuffer;
        else                              jumpBufferCounter -= Time.deltaTime;

        bool holdingDown = Input.GetAxisRaw("Vertical") <= downThreshold;

        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            if (holdingDown && !isDropping && TryGetOneWayPlatformBelow(out var platform))
            {
                StartCoroutine(DropThrough(platform));
                jumpBufferCounter = 0f;
                coyoteCounter     = 0f;
            }
            else
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                jumpBufferCounter = 0f;
                coyoteCounter     = 0f;
            }
        }

        if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);

        if (sprite != null && Mathf.Abs(inputX) > 0.01f)
            sprite.flipX = inputX < 0f;
    }

    private void FixedUpdate()
    {
        float targetSpeed = inputX * moveSpeed;
        float accelRate   = IsGrounded() ? groundAccel : airAccel;
        float newX        = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed,
                                              accelRate * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);

        if (rb.linearVelocity.y < 0f)
            rb.gravityScale = baseGravity * fallGravityMult;
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
            rb.gravityScale = baseGravity * lowJumpMult;
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
