using System.Collections.Generic;
using UnityEngine;

namespace HeartOfTheNight.Common
{
    /// <summary>
    /// Moves a platform through a set of waypoints and carries any rider (player/enemy)
    /// standing on top. Uses a Kinematic Rigidbody2D + MovePosition in FixedUpdate so
    /// movement is smooth and physics-correct. Riders are carried by applying the
    /// platform's per-step displacement directly to their Rigidbody2D (reliable even
    /// when the rider's own controller overwrites its velocity).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public class MovingPlatform : MonoBehaviour
    {
        public enum LoopMode { PingPong, Loop }

        [Header("Path")]
        [Tooltip("World-space points the platform travels through, in order. Need at least 2.")]
        [SerializeField] private Transform[] waypoints;
        [SerializeField] private LoopMode loopMode = LoopMode.PingPong;

        [Header("Motion")]
        [SerializeField] private float moveSpeed = 2.5f;
        [Tooltip("Seconds to pause when reaching a waypoint.")]
        [SerializeField] private float waitTime = 0.5f;

        [Header("Riders")]
        [Tooltip("Layers that get carried by the platform (e.g. Player, Enemy).")]
        [SerializeField] private LayerMask riderLayers = ~0;
        [Tooltip("How far below the platform top a rider's feet may be and still be carried.")]
        [SerializeField] private float onTopTolerance = 0.3f;

        private Rigidbody2D rb;
        private Collider2D  platformCol;
        private int   targetIndex;
        private int   direction = 1;
        private float waitCounter;

        private readonly HashSet<Collider2D> contacts = new();
        private readonly HashSet<Rigidbody2D> carriedThisStep = new();

        private void Awake()
        {
            rb          = GetComponent<Rigidbody2D>();
            platformCol = GetComponent<Collider2D>();
            rb.bodyType      = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            // Frictionless so the physics solver doesn't also drag the rider; carrying
            // is done manually so the rider moves at exactly the platform's speed.
            platformCol.sharedMaterial = new PhysicsMaterial2D("MovingPlatform_NoFriction")
            {
                friction   = 0f,
                bounciness = 0f
            };

            if (waypoints != null && waypoints.Length > 0 && waypoints[0] != null)
                rb.position = waypoints[0].position;
            targetIndex = waypoints != null && waypoints.Length > 1 ? 1 : 0;
        }

        private void FixedUpdate()
        {
            if (waypoints == null || waypoints.Length < 2) return;

            if (waitCounter > 0f)
            {
                waitCounter -= Time.fixedDeltaTime;
                return;
            }

            Vector2 prev   = rb.position;
            Vector2 target = waypoints[targetIndex].position;
            Vector2 next    = Vector2.MoveTowards(prev, target, moveSpeed * Time.fixedDeltaTime);

            rb.MovePosition(next);

            Vector2 delta = next - prev;
            if (delta.sqrMagnitude > 0f) CarryRiders(delta);

            if (Vector2.Distance(next, target) <= 0.0001f)
            {
                waitCounter = waitTime;
                AdvanceTarget();
            }
        }

        private void CarryRiders(Vector2 delta)
        {
            carriedThisStep.Clear();
            float platformTop = platformCol.bounds.max.y;

            // Horizontal is always carried manually. Vertical is only carried when the
            // platform descends; while ascending the collision already lifts the rider,
            // so adding delta.y too would move them twice as fast.
            Vector2 carry = new Vector2(delta.x, delta.y < 0f ? delta.y : 0f);

            foreach (var col in contacts)
            {
                if (col == null) continue;
                var riderRb = col.attachedRigidbody;
                if (riderRb == null || carriedThisStep.Contains(riderRb)) continue;

                // Only carry riders standing on top (their feet near/above the surface).
                if (col.bounds.min.y < platformTop - onTopTolerance) continue;

                riderRb.position += carry;
                carriedThisStep.Add(riderRb);
            }
        }

        private void AdvanceTarget()
        {
            if (loopMode == LoopMode.Loop)
            {
                targetIndex = (targetIndex + 1) % waypoints.Length;
                return;
            }

            if (targetIndex >= waypoints.Length - 1) direction = -1;
            else if (targetIndex <= 0)               direction = 1;
            targetIndex += direction;
            targetIndex = Mathf.Clamp(targetIndex, 0, waypoints.Length - 1);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (IsRider(collision.collider.gameObject))
                contacts.Add(collision.collider);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            contacts.Remove(collision.collider);
        }

        private bool IsRider(GameObject obj)
        {
            return (riderLayers.value & (1 << obj.layer)) != 0;
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Length < 2) return;

            Gizmos.color = Color.cyan;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] == null) continue;
                Gizmos.DrawWireSphere(waypoints[i].position, 0.15f);

                int nextIdx = i + 1;
                if (nextIdx >= waypoints.Length)
                {
                    if (loopMode == LoopMode.Loop) nextIdx = 0;
                    else break;
                }
                if (waypoints[nextIdx] != null)
                    Gizmos.DrawLine(waypoints[i].position, waypoints[nextIdx].position);
            }
        }
    }
}
