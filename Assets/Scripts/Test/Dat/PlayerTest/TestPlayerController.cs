using System.Collections;
using UnityEngine;
using HeartOfTheNight.Common;

[RequireComponent(typeof(Rigidbody2D))]
public class TestPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed     = 7f;
    [SerializeField] private float groundAccel   = 60f;
    [SerializeField] private float airAccel      = 60f;
    [Tooltip("Tốc độ ngang tối đa khi đang trên không, theo tỉ lệ của moveSpeed. 1 = bằng dưới đất.")]
    [Range(0f, 1f)]
    [SerializeField] private float airMoveMultiplier = 1f;

    [Header("Jump")]
    [SerializeField] private float jumpForce     = 13f;
    [SerializeField] private float coyoteTime    = 0.1f;
    [SerializeField] private float jumpBuffer    = 0.1f;

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
    private bool  isDropping;
    private bool  wasHoldingDown;

    private void Awake()
    {
        rb          = GetComponent<Rigidbody2D>();
        sprite      = GetComponentInChildren<SpriteRenderer>();
        colliders   = GetComponentsInChildren<Collider2D>();
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
            && TryGetOneWayPlatformBelow(out var dropCollider))
        {
            StartCoroutine(DropThrough(dropCollider));
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
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    private bool TryGetOneWayPlatformBelow(out Collider2D platform)
    {
        platform = null;
        if (groundCheck == null) return false;

        var hits = Physics2D.OverlapBoxAll(groundCheck.position, groundCheckSize, 0f, groundLayer);
        foreach (var hit in hits)
        {
            if (hit == null) continue;
            if (IsOneWay(hit))
            {
                platform = hit;
                return true;
            }
        }
        return false;
    }

    private static bool IsOneWay(Collider2D col)
    {
        if (col.GetComponentInParent<OneWayPlatform>() != null) return true;
        var effector = col.GetComponent<PlatformEffector2D>();
        return effector != null && effector.useOneWay;
    }

    private IEnumerator DropThrough(Collider2D platform)
    {
        isDropping = true;
        SetIgnore(platform, true);

        // Wait until the player has actually fallen below the platform, so collision
        // never re-enables while still overlapping (which would pop the player back up).
        float timeout = Mathf.Max(dropThroughTime, 1f);
        while (timeout > 0f)
        {
            timeout -= Time.deltaTime;
            if (GetPlayerTopY() < platform.bounds.min.y) break;
            yield return null;
        }

        yield return new WaitForSeconds(0.05f);

        SetIgnore(platform, false);
        isDropping = false;
    }

    private void SetIgnore(Collider2D platform, bool ignore)
    {
        foreach (var col in colliders)
            if (col != null) Physics2D.IgnoreCollision(col, platform, ignore);
    }

    private float GetPlayerTopY()
    {
        float top   = float.NegativeInfinity;
        bool  found  = false;
        foreach (var col in colliders)
        {
            if (col == null || col.isTrigger) continue;
            top   = Mathf.Max(top, col.bounds.max.y);
            found = true;
        }
        return found ? top : transform.position.y;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
    }
}
