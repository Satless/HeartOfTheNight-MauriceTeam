# Examples — Unity 2D Platformer Controller

Complete, copy-pasteable component examples that follow the SOLID + State + Observer + ScriptableObject architecture from `SKILL.md`.

## 1. `PlayerStatsSO` (ScriptableObject)

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Stats", fileName = "PlayerStats")]
public class PlayerStatsSO : ScriptableObject
{
    [Header("Move")]
    public float moveSpeed     = 8f;
    public float groundAccel   = 60f;
    public float groundDecel   = 60f;
    public float airAccel      = 30f;
    public float airDecel      = 20f;

    [Header("Jump")]
    public float jumpForce         = 16f;
    public float coyoteTime        = 0.1f;
    public float jumpBuffer        = 0.1f;
    public float jumpCutMultiplier = 0.5f;
    public float fallGravityMult   = 2f;
    public float maxFallSpeed      = 25f;

    [Header("Wall")]
    public float wallSlideSpeed = 2f;
    public Vector2 wallJumpForce = new(12f, 16f);
    public float wallJumpLockTime = 0.15f;
}
```

## 2. `PlayerInputHandler` (Observer source)

```csharp
using System;
using UnityEngine;

public class PlayerInputHandler : MonoBehaviour
{
    public float HorizontalInput { get; private set; }
    public event Action JumpPressed;
    public event Action JumpReleased;

    void Update()
    {
        HorizontalInput = Input.GetAxisRaw("Horizontal");
        if (Input.GetButtonDown("Jump")) JumpPressed?.Invoke();
        if (Input.GetButtonUp("Jump"))   JumpReleased?.Invoke();
    }
}
```

Swap to Unity's new Input System by changing only this class — the rest of the controller is unchanged.

## 3. `GroundChecker`

```csharp
using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    [SerializeField] private Transform groundPoint;
    [SerializeField] private Transform wallPoint;
    [SerializeField] private Vector2 groundSize = new(0.5f, 0.05f);
    [SerializeField] private Vector2 wallSize   = new(0.05f, 0.5f);
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask wallLayer;

    public bool IsGrounded   => Physics2D.OverlapBox(groundPoint.position, groundSize, 0f, groundLayer);
    public bool IsTouchingWall => Physics2D.OverlapBox(wallPoint.position, wallSize, 0f, wallLayer);

    void OnDrawGizmosSelected()
    {
        if (groundPoint) { Gizmos.color = Color.green; Gizmos.DrawWireCube(groundPoint.position, groundSize); }
        if (wallPoint)   { Gizmos.color = Color.cyan;  Gizmos.DrawWireCube(wallPoint.position,   wallSize); }
    }
}
```

## 4. `PlayerEvents` (static Observer hub)

```csharp
using System;

public static class PlayerEvents
{
    public static event Action<int>   OnHealthChanged;
    public static event Action        OnJumped;
    public static event Action        OnLanded;
    public static event Action        OnDied;

    public static void RaiseHealthChanged(int hp) => OnHealthChanged?.Invoke(hp);
    public static void RaiseJumped()              => OnJumped?.Invoke();
    public static void RaiseLanded()              => OnLanded?.Invoke();
    public static void RaiseDied()                => OnDied?.Invoke();
}
```

## 5. `PlayerMovement` (physics applier)

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private PlayerStatsSO stats;
    [SerializeField] private PlayerInputHandler input;
    [SerializeField] private GroundChecker checker;

    private Rigidbody2D rb;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float wallJumpLockCounter;
    private bool  wasGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        input.JumpPressed  += OnJumpPressed;
        input.JumpReleased += OnJumpReleased;
    }

    void OnDestroy()
    {
        input.JumpPressed  -= OnJumpPressed;
        input.JumpReleased -= OnJumpReleased;
    }

    void Update()
    {
        coyoteCounter       = checker.IsGrounded ? stats.coyoteTime : coyoteCounter - Time.deltaTime;
        jumpBufferCounter  -= Time.deltaTime;
        wallJumpLockCounter -= Time.deltaTime;

        if (!wasGrounded && checker.IsGrounded) PlayerEvents.RaiseLanded();
        wasGrounded = checker.IsGrounded;
    }

    void FixedUpdate()
    {
        ApplyHorizontal();
        ApplyGravityTuning();
        TryJump();
        ApplyWallSlide();
    }

    void ApplyHorizontal()
    {
        if (wallJumpLockCounter > 0f) return;

        float target = input.HorizontalInput * stats.moveSpeed;
        float accel  = checker.IsGrounded
            ? (Mathf.Abs(target) > 0.01f ? stats.groundAccel : stats.groundDecel)
            : (Mathf.Abs(target) > 0.01f ? stats.airAccel    : stats.airDecel);

        float newX = Mathf.MoveTowards(rb.linearVelocity.x, target, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newX, rb.linearVelocity.y);
    }

    void ApplyGravityTuning()
    {
        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (stats.fallGravityMult - 1f) * Time.fixedDeltaTime;

        if (rb.linearVelocity.y < -stats.maxFallSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -stats.maxFallSpeed);
    }

    void OnJumpPressed()  => jumpBufferCounter = stats.jumpBuffer;
    void OnJumpReleased()
    {
        if (rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * stats.jumpCutMultiplier);
    }

    void TryJump()
    {
        if (jumpBufferCounter <= 0f) return;

        if (coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, stats.jumpForce);
            jumpBufferCounter = 0f;
            coyoteCounter     = 0f;
            PlayerEvents.RaiseJumped();
        }
        else if (checker.IsTouchingWall)
        {
            float dir = -Mathf.Sign(input.HorizontalInput == 0 ? transform.localScale.x : input.HorizontalInput);
            rb.linearVelocity = new Vector2(dir * stats.wallJumpForce.x, stats.wallJumpForce.y);
            wallJumpLockCounter = stats.wallJumpLockTime;
            jumpBufferCounter   = 0f;
            PlayerEvents.RaiseJumped();
        }
    }

    void ApplyWallSlide()
    {
        if (!checker.IsTouchingWall || checker.IsGrounded) return;
        if (rb.linearVelocity.y < -stats.wallSlideSpeed)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -stats.wallSlideSpeed);
    }
}
```

## 6. State Machine (optional layered animation/state control)

```csharp
public interface IPlayerState
{
    void Enter();
    void Tick();
    void FixedTick();
    void Exit();
}

public class PlayerStateMachine
{
    public IPlayerState Current { get; private set; }

    public void ChangeState(IPlayerState next)
    {
        if (Current == next) return;
        Current?.Exit();
        Current = next;
        Current.Enter();
    }

    public void Tick()      => Current?.Tick();
    public void FixedTick() => Current?.FixedTick();
}
```

Each concrete state holds a reference to the player context (animator, movement, checker) and decides its own transitions:

```csharp
public class IdleState : IPlayerState
{
    private readonly PlayerContext ctx;
    public IdleState(PlayerContext ctx) { this.ctx = ctx; }

    public void Enter() => ctx.Animator.Play("Idle");
    public void Tick()
    {
        if (Mathf.Abs(ctx.Input.HorizontalInput) > 0.01f) ctx.FSM.ChangeState(ctx.RunState);
        else if (!ctx.Checker.IsGrounded)                 ctx.FSM.ChangeState(ctx.FallState);
    }
    public void FixedTick() { }
    public void Exit() { }
}
```

## 7. UI Listener (Observer consumer)

```csharp
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    void OnEnable()  => PlayerEvents.OnHealthChanged += UpdateBar;
    void OnDisable() => PlayerEvents.OnHealthChanged -= UpdateBar;

    void UpdateBar(int hp) => slider.value = hp;
}
```

The `PlayerMovement` script never knows the HUD exists — events keep them decoupled.
