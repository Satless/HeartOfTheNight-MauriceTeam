---
name: unity-2d-platformer-controller
description: >-
  Expert in Unity 2D physics, collision handling, raycasts, and smooth
  platformer Character Controllers. Covers coyote time, jump buffering,
  variable jump height, acceleration/deceleration smoothing, ground and wall
  detection via Raycast2D / OverlapBox / OverlapCircle, one-way platforms,
  Rigidbody2D vs Kinematic controllers, layer masking, and ContactFilter2D.
  Use when implementing player movement, enemy patrol physics, jump
  mechanics, wall jumps, dashes, or any 2D platformer character controller
  in Unity.
---

# Unity 2D Platformer Controller

Specialist guidance for building responsive, juicy 2D platformer movement in Unity. Follow the project's design pattern rules: SOLID, State Pattern (character/enemy logic), Observer Pattern (events for UI), Singleton (managers), ScriptableObject (tunable data).

## When to Apply

- Implementing player or enemy 2D movement, jumping, falling, wall sliding
- Ground / wall / ceiling detection
- Coyote time, jump buffering, variable jump height
- Smooth horizontal movement (accel/decel, air control)
- One-way platforms, slopes, moving platforms
- Filtering collisions with `LayerMask` / `ContactFilter2D`
- Choosing between Dynamic vs Kinematic Rigidbody2D

## Required Architecture (SOLID)

Split responsibilities. Never put input, physics, animation, and state all in one MonoBehaviour.

| Component | Responsibility | Pattern |
|-----------|----------------|---------|
| `PlayerInputHandler` | Read input, expose `Action`/`event` signals | Observer |
| `PlayerMovement` | Apply velocity to `Rigidbody2D` | SRP |
| `GroundChecker` | Raycast / OverlapBox ground & wall detection | SRP |
| `PlayerStateMachine` | Switch between `Idle`, `Run`, `Jump`, `Fall`, `WallSlide`, `Dash` | State |
| `PlayerStatsSO` (ScriptableObject) | Tunable values: moveSpeed, jumpForce, coyoteTime, etc. | Strategy/Data |
| `PlayerEvents` (static `Action`s) | Health changed, landed, jumped, died | Observer |

Managers (`GameManager`, `InputManager`, `AudioManager`) stay as Singletons. Avoid letting the controller reach into managers directly — fire events instead.

## Physics Fundamentals (non-negotiable)

1. **Read input in `Update`**, **apply physics in `FixedUpdate`**. Cache input into fields between frames.
2. Use `Rigidbody2D.linearVelocity` (Unity 6+) / `velocity` (older) for movement — **never** `transform.Translate` on a Rigidbody2D.
3. Set `Rigidbody2D.interpolation = Interpolate` for the player so visuals stay smooth.
4. Use `LayerMask` + dedicated layers (`Ground`, `Wall`, `OneWayPlatform`, `Hazard`). Never compare tags for physics.
5. Prefer `Physics2D.OverlapBox` / `OverlapCircle` over `Raycast` for ground checks — they are tolerant to corner cases.
6. Use `Physics2D.queriesStartInColliders = false` in Project Settings → Physics2D so the player's own collider does not register self-hits.

## Ground Detection (OverlapBox recipe)

```csharp
[SerializeField] private Transform groundCheck;
[SerializeField] private Vector2 groundCheckSize = new(0.5f, 0.05f);
[SerializeField] private LayerMask groundLayer;

public bool IsGrounded =>
    Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
```

Place `groundCheck` as a child Transform at the player's feet. Visualize with `OnDrawGizmosSelected` using `Gizmos.DrawWireCube`.

## Coyote Time

Allow jumping for a short window *after* leaving a ledge.

```csharp
[SerializeField] private float coyoteTime = 0.1f;
private float coyoteCounter;

void Update()
{
    coyoteCounter = IsGrounded ? coyoteTime : coyoteCounter - Time.deltaTime;
}

public bool CanCoyoteJump => coyoteCounter > 0f;
```

## Jump Buffer

Remember the jump press for a short window *before* landing.

```csharp
[SerializeField] private float jumpBuffer = 0.1f;
private float jumpBufferCounter;

void Update()
{
    if (Input.GetButtonDown("Jump")) jumpBufferCounter = jumpBuffer;
    else jumpBufferCounter -= Time.deltaTime;
}

void TryJump()
{
    if (jumpBufferCounter > 0f && CanCoyoteJump)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        jumpBufferCounter = 0f;
        coyoteCounter   = 0f;
        PlayerEvents.Jumped?.Invoke();
    }
}
```

## Variable Jump Height

Cut upward velocity when the player releases the jump button early.

```csharp
if (Input.GetButtonUp("Jump") && rb.linearVelocity.y > 0f)
    rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
```

## Smooth Horizontal Movement

Use separate `acceleration` and `deceleration` so the player feels weighty but responsive. Keep different values for ground vs air.

```csharp
float targetSpeed = inputX * stats.moveSpeed;
float accelRate   = IsGrounded ? stats.groundAccel : stats.airAccel;
float newSpeed    = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accelRate * Time.fixedDeltaTime);
rb.linearVelocity = new Vector2(newSpeed, rb.linearVelocity.y);
```

## Wall Detection & Wall Jump

```csharp
public bool IsTouchingWall =>
    Physics2D.OverlapBox(wallCheck.position, wallCheckSize, 0f, wallLayer);
```

Wall slide: clamp falling speed when touching a wall and not grounded. Wall jump: apply both vertical and horizontal impulse, then **briefly lock horizontal input** (~0.15s) so the player doesn't immediately stick back to the wall.

## One-Way Platforms

- Add `PlatformEffector2D` to the platform with `Use One Way = true`.
- The platform's `Collider2D` must have `Used By Effector = true`.
- For drop-through: temporarily disable the platform's collider or move the player to a layer that ignores the `OneWayPlatform` layer (use `Physics2D.IgnoreLayerCollision`).

## State Pattern Skeleton

```csharp
public interface IPlayerState
{
    void Enter();
    void Tick();        // Update
    void FixedTick();   // FixedUpdate
    void Exit();
}

public class PlayerStateMachine
{
    public IPlayerState Current { get; private set; }
    public void ChangeState(IPlayerState next)
    {
        Current?.Exit();
        Current = next;
        Current.Enter();
    }
}
```

States: `IdleState`, `RunState`, `JumpState`, `FallState`, `WallSlideState`, `DashState`. Each state owns its transition rules — keep transitions inside the state, not in a giant `if/else` block elsewhere.

## ScriptableObject Tuning

```csharp
[CreateAssetMenu(menuName = "Player/Stats")]
public class PlayerStatsSO : ScriptableObject
{
    public float moveSpeed     = 8f;
    public float jumpForce     = 16f;
    public float coyoteTime    = 0.1f;
    public float jumpBuffer    = 0.1f;
    public float groundAccel   = 60f;
    public float airAccel      = 30f;
    public float fallGravityMult = 2f;
}
```

Designers tweak values in the Inspector without recompiling. Multiple `PlayerStatsSO` assets enable preset configurations (e.g. `LightPlayer`, `HeavyPlayer`).

## Observer Events (UI decoupling)

```csharp
public static class PlayerEvents
{
    public static Action<int> OnHealthChanged;
    public static Action      Jumped;
    public static Action      Landed;
    public static Action      Died;
}
```

UI listens in `OnEnable`, unsubscribes in `OnDisable`. The controller never references the HUD directly.

## Common Pitfalls

- ❌ Ground check via `OnCollisionEnter2D` — unreliable at corners and slopes.
- ❌ `transform.position +=` to move a Rigidbody2D — breaks physics, causes jitter.
- ❌ Multiplying input by `Time.deltaTime` inside `FixedUpdate` — use `Time.fixedDeltaTime`.
- ❌ Forgetting to reset `coyoteCounter` and `jumpBufferCounter` after a successful jump — double jumps.
- ❌ Using `Vector2.right * speed` while `Rigidbody2D.gravityScale` is 0 — player floats. Either keep gravity on or apply it manually.
- ❌ Putting state transitions inside `PlayerMovement` — violates SRP. Keep transitions in the state machine.

## Additional Resources

- For full working component code, see [examples.md](examples.md)
- For slopes, moving platforms, dashes, and debugging, see [reference.md](reference.md)
