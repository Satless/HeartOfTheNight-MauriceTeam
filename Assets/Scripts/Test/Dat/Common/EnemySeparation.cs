using System.Collections.Generic;
using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    /// <summary>
    /// Xu ly va cham vat ly cho quai:
    /// - Quai van CHAN Player nhung khong bi Player xo (khoi luong lon = bat dong).
    ///   (Hoac bat ignorePlayerCollision de cho Player di xuyen qua.)
    /// - Cac quai khong day nhau khi dung sat (bo qua va cham giua cac quai).
    /// - Steering tach nhe de giu khoang cach giua cac quai (khong giat).
    /// Component nay tu duoc cac controller quai gan vao khi chay, hoac co the
    /// gan san vao prefab de tinh chinh trong Inspector.
    /// Chay sau cac controller (execution order cao) de cong them luc tach
    /// sau khi controller da set van toc.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemySeparation : MonoBehaviour
    {
        [Header("Quan he voi Player")]
        [Tooltip("Quai van chan Player nhung khong bi Player xo (tang khoi luong de bat dong).")]
        [SerializeField] private bool blockPlayerButImmovable = true;

        [Tooltip("Khoi luong lon de Player khong day duoc quai. Cang lon cang 'cung' (bat dong).")]
        [SerializeField] private float immovableMass = 1000f;

        [Tooltip("Thay vi chan, cho Player di xuyen qua quai (bo qua va cham hoan toan voi Player).")]
        [SerializeField] private bool ignorePlayerCollision = false;

        [Header("Bo qua va cham giua quai")]
        [Tooltip("Bo qua va cham vat ly giua cac quai => quai khong day nhau.")]
        [SerializeField] private bool ignoreEnemyCollision = true;

        [Header("Giu khoang cach giua cac quai")]
        [Tooltip("Bat logic steering giu khoang cach.")]
        [SerializeField] private bool enableSeparation = true;

        [Tooltip("Khoang cach ngang mong muon giua tam hai quai.")]
        [SerializeField] private float desiredSpacing = 1.5f;

        [Tooltip("Toc do toi da cua luc tach (units/s).")]
        [SerializeField] private float separationSpeed = 2.5f;

        [Tooltip("Chi tach voi quai co chenh lech do cao nho hon nguong nay (cung 1 nen dat).")]
        [SerializeField] private float verticalTolerance = 1.0f;

        private Rigidbody2D rb;
        private Collider2D[] solidColliders;
        private bool playerCollisionIgnored;

        private static readonly List<EnemySeparation> Active = new();

        /// <summary>Dam bao GameObject co component nay (goi tu Awake cua controller).</summary>
        public static EnemySeparation Ensure(GameObject go)
        {
            if (go == null) return null;
            var sep = go.GetComponent<EnemySeparation>();
            if (sep == null) sep = go.AddComponent<EnemySeparation>();
            return sep;
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            solidColliders = CollectSolidColliders(transform);

            // Khoi luong lon => contact solver gan nhu khong dich chuyen quai khi
            // Player xo vao, nhung van chan duoc Player. Khong anh huong di chuyen
            // (controller set van toc truc tiep) hay trong luc (theo gia toc).
            if (blockPlayerButImmovable && !ignorePlayerCollision)
            {
                rb.mass = immovableMass;
            }
        }

        private void OnEnable()
        {
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
        }

        private void Start()
        {
            if (ignoreEnemyCollision) IgnoreOtherEnemies();
            if (ignorePlayerCollision) TryIgnorePlayer();
        }

        private void FixedUpdate()
        {
            // Player co the duoc spawn sau quai => thu lai cho den khi bo qua duoc.
            if (ignorePlayerCollision && !playerCollisionIgnored) TryIgnorePlayer();

            if (enableSeparation) ApplySeparation();
        }

        private void IgnoreOtherEnemies()
        {
            for (int i = 0; i < Active.Count; i++)
            {
                var other = Active[i];
                if (other == null || other == this) continue;
                IgnoreBetween(solidColliders, other.solidColliders);
            }
        }

        private void TryIgnorePlayer()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo == null) return;

            var playerColliders = CollectSolidColliders(playerGo.transform);
            IgnoreBetween(solidColliders, playerColliders);
            playerCollisionIgnored = true;
        }

        private void ApplySeparation()
        {
            if (rb == null) return;

            float pushX = 0f;
            Vector2 myPos = rb.position;

            for (int i = 0; i < Active.Count; i++)
            {
                var other = Active[i];
                if (other == null || other == this || other.rb == null) continue;

                Vector2 otherPos = other.rb.position;
                if (Mathf.Abs(myPos.y - otherPos.y) > verticalTolerance) continue;

                float dx = myPos.x - otherPos.x;
                float dist = Mathf.Abs(dx);
                if (dist >= desiredSpacing) continue;

                float dir;
                if (dist > 0.0001f)
                {
                    dir = dx / dist;
                }
                else
                {
                    // Trung vi tri: tie-break on dinh theo InstanceID.
                    dir = GetInstanceID() < other.GetInstanceID() ? -1f : 1f;
                }

                float strength = 1f - (dist / desiredSpacing); // 0..1
                pushX += dir * strength;
            }

            if (Mathf.Abs(pushX) <= 0.0001f) return;

            float sepVel = Mathf.Clamp(pushX, -1f, 1f) * separationSpeed;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x + sepVel, rb.linearVelocity.y);
        }

        private static void IgnoreBetween(Collider2D[] a, Collider2D[] b)
        {
            if (a == null || b == null) return;
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] == null) continue;
                for (int j = 0; j < b.Length; j++)
                {
                    if (b[j] == null) continue;
                    Physics2D.IgnoreCollision(a[i], b[j], true);
                }
            }
        }

        private static Collider2D[] CollectSolidColliders(Transform root)
        {
            var all = root.GetComponentsInChildren<Collider2D>(true);
            var list = new List<Collider2D>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && !all[i].isTrigger) list.Add(all[i]);
            }
            return list.ToArray();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, desiredSpacing);
        }
    }
}
