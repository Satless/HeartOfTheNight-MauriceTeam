using UnityEngine;

namespace HeartOfTheNight.Common
{
    //fffff
    //dsads
    //ádsad
    /// <summary>
    /// Marks a platform as one-way: the player can jump up through it and stand on top.
    /// Auto-configures the Collider2D + PlatformEffector2D for upward pass-through.
    /// Drop-through (holding Down + Jump) is driven by the player controller, which
    /// temporarily ignores collision with this platform's <see cref="Collider"/>.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(PlatformEffector2D))]
    public class OneWayPlatform : MonoBehaviour
    {
        public Collider2D Collider { get; private set; }

        private void Awake()
        {
            Collider = GetComponent<Collider2D>();
            Collider.usedByEffector = true;

            var effector = GetComponent<PlatformEffector2D>();
            effector.useOneWay = true;
            effector.useColliderMask = false;
        }
    }
}
