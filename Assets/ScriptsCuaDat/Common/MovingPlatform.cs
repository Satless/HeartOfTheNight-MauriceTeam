using System.Collections.Generic;
using UnityEngine;

namespace HeartOfTheNight.Common
{
    /// <summary>
    /// Moves a platform through a set of waypoints and carries any rider (player/enemy)
    /// standing on top. Uses a Kinematic Rigidbody2D + MovePosition in FixedUpdate so
    /// movement is smooth and physics-correct.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
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

        private Rigidbody2D rb;
        private int   targetIndex;
        private int   direction = 1;
        private float waitCounter;

        private readonly Dictionary<Transform, Transform> riders = new();

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType      = RigidbodyType2D.Kinematic;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            if (waypoints != null && waypoints.Length > 0)
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

            Vector2 target = waypoints[targetIndex].position;
            Vector2 next    = Vector2.MoveTowards(rb.position, target,
                                                  moveSpeed * Time.fixedDeltaTime);
            rb.MovePosition(next);

            if (Vector2.Distance(next, target) <= 0.0001f)
            {
                waitCounter = waitTime;
                AdvanceTarget();
            }
        }

        private void AdvanceTarget()
        {
            if (loopMode == LoopMode.Loop)
            {
                targetIndex = (targetIndex + 1) % waypoints.Length;
                return;
            }

            // PingPong
            if (targetIndex >= waypoints.Length - 1) direction = -1;
            else if (targetIndex <= 0)               direction = 1;
            targetIndex += direction;
            targetIndex = Mathf.Clamp(targetIndex, 0, waypoints.Length - 1);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            var t = collision.transform;
            if (!IsRider(collision.collider.gameObject) || riders.ContainsKey(t)) return;

            riders[t] = t.parent;
            t.SetParent(transform, true);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            var t = collision.transform;
            if (!riders.TryGetValue(t, out var originalParent)) return;

            t.SetParent(originalParent, true);
            riders.Remove(t);
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
