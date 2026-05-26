# Reference — Advanced 2D Physics & Movement

Deeper material referenced by `SKILL.md`. Read sections on demand.

## Rigidbody2D Body Types

| Body Type | Use for | Notes |
|-----------|---------|-------|
| **Dynamic** | Default player, enemies | Full physics, gravity, forces |
| **Kinematic** | Custom character controllers, moving platforms | No forces; you set `linearVelocity` or call `MovePosition` |
| **Static** | Immovable world geometry without a Tilemap | Never moves; cheap |

For platformers, **Dynamic Rigidbody2D with `gravityScale = 1` and manually tuned gravity multipliers** is the most common choice. Use Kinematic only if you need pixel-perfect non-physics movement.

## Continuous vs Discrete Collision Detection

- Fast-moving players (dashes, projectiles) → `Collision Detection = Continuous`.
- Tunneling through thin platforms is almost always a CCD issue, not a physics-step issue.

## Raycast vs OverlapBox vs OverlapCircle

| Method | Best for | Pitfalls |
|--------|----------|----------|
| `Raycast2D` | Wall-checks, slope normal sampling | Single line — misses corners |
| `BoxCast` | Movement prediction, ground prediction | Returns first hit — can miss complex geometry |
| `OverlapBox` | Ground/wall presence checks | No surface normal returned |
| `OverlapCircle` | Generic proximity checks | Curved corners — softer detection |

Rule of thumb: **OverlapBox for state queries (am I grounded?), Raycast for measurement (what's the slope angle?)**.

## Slope Handling

```csharp
RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, slopeRayLength, groundLayer);
if (hit)
{
    float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
    if (slopeAngle <= maxSlopeAngle)
    {
        Vector2 slopeDir = new(hit.normal.y, -hit.normal.x);
        rb.linearVelocity = -slopeDir * stats.moveSpeed * inputX;
    }
}
```

To stop the player from sliding on slopes when idle:
- Switch `Rigidbody2D` to a `PhysicsMaterial2D` with high friction *only* when input is zero, or
- Set `linearVelocity` to zero manually when grounded and `inputX == 0`.

## Moving Platforms

Two common approaches:

1. **Parent the player to the platform on contact**, unparent on exit. Simple but can cause scale jitter.
2. **Apply platform delta velocity** to the player each `FixedUpdate` while standing on it. More robust.

```csharp
public class PlatformRider : MonoBehaviour
{
    private Vector2 lastPlatformPos;
    private Rigidbody2D platform;

    public void Attach(Rigidbody2D rb)
    {
        platform = rb;
        lastPlatformPos = rb.position;
    }

    public Vector2 ConsumeDelta()
    {
        if (platform == null) return Vector2.zero;
        Vector2 delta = platform.position - lastPlatformPos;
        lastPlatformPos = platform.position;
        return delta;
    }
}
```

Apply the delta with `rb.MovePosition(rb.position + delta)` in `FixedUpdate`.

## One-Way Platforms — Drop Through

```csharp
[SerializeField] private float dropDuration = 0.3f;
[SerializeField] private LayerMask oneWayLayer;

IEnumerator DropThrough()
{
    int playerLayer = gameObject.layer;
    int oneWay      = (int)Mathf.Log(oneWayLayer.value, 2);
    Physics2D.IgnoreLayerCollision(playerLayer, oneWay, true);
    yield return new WaitForSeconds(dropDuration);
    Physics2D.IgnoreLayerCollision(playerLayer, oneWay, false);
}
```

Trigger with `Down + Jump` for the classic platformer feel.

## Dash

```csharp
public IEnumerator Dash(Vector2 dir, float force, float duration)
{
    float originalGravity = rb.gravityScale;
    rb.gravityScale = 0f;
    rb.linearVelocity = dir.normalized * force;
    yield return new WaitForSeconds(duration);
    rb.gravityScale = originalGravity;
}
```

Lock other movement inputs during the dash. Reset dash availability when grounded.

## ContactFilter2D for Multi-Layer Queries

When you need both a `LayerMask` and a normal-angle filter:

```csharp
ContactFilter2D filter = new()
{
    useLayerMask  = true,
    layerMask     = groundLayer,
    useNormalAngle = true,
    minNormalAngle = 45f,
    maxNormalAngle = 135f,
};

Collider2D[] results = new Collider2D[4];
int count = Physics2D.OverlapBox(point, size, 0f, filter, results);
```

## Debugging Movement Issues

| Symptom | Likely cause |
|---------|--------------|
| Player jitters on platforms | `Interpolation = None`. Set to `Interpolate`. |
| Jump feels floaty / mushy | Missing fall gravity multiplier or jump cut. |
| Double jumps appear randomly | Counters not reset after a successful jump. |
| Sticking to walls mid-fall | No wall jump horizontal lock, or friction material on collider. |
| Catches on tile seams | Use a **CapsuleCollider2D** or composite tilemap collider. |
| Sinking into ground at high fall speeds | Set Collision Detection to `Continuous`. |
| Ground check flickers near edges | OverlapBox is too thin — widen to match feet collider. |

## Performance Notes

- Cache `Physics2D.queriesHitTriggers` and your `LayerMask` field — do not `LayerMask.GetMask("Ground")` inside `FixedUpdate`.
- Allocation-free overload: use `Physics2D.OverlapBoxNonAlloc` / `RaycastNonAlloc` for hot paths.
- Avoid `GameObject.FindWithTag` per frame; wire references via `[SerializeField]` or DI.

## Project Settings Checklist

- **Physics2D → Queries Start In Colliders**: `false`
- **Physics2D → Auto Sync Transforms**: `false` (default) unless you need it for editor scripts
- **Time → Fixed Timestep**: `0.02` (50 Hz) or `0.0166` (60 Hz) for tighter feel
- **Layer Collision Matrix**: turn off `Player ↔ Player`, `Hazard ↔ Hazard`, etc.

## When NOT to Use Rigidbody2D

If you need frame-perfect, deterministic, or replay-friendly platforming (speedrun games, rollback netcode), implement a **manual swept-AABB controller** with `Physics2D.BoxCastAll`. Rigidbody2D's solver can introduce tiny non-determinism between platforms.
