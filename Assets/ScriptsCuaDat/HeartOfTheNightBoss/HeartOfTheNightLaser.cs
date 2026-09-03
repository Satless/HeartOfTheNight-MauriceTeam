using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    public class HeartOfTheNightLaser : MonoBehaviour
    {
        private Animator anim;
        private Vector2 origin;
        private Vector2 direction;
        private float length;
        private float width;
        private int damage;
        private float warnTime;
        private float fireTime;
        private float damageTickInterval;

        private float timer;
        private bool firing;
        private bool fireTriggered;
        private float nextDamageTime;

        private const string OverlaySortingLayer = "freeLightLayer";
        private const int OverlaySortingOrder = 20;

        private void Awake()
        {
            anim = GetComponent<Animator>();
            if (anim == null) anim = GetComponentInChildren<Animator>();
            DrawInFrontOfPlayer();
        }

        private void DrawInFrontOfPlayer()
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingLayerName = OverlaySortingLayer;
                renderers[i].sortingOrder = OverlaySortingOrder;
            }
        }

        public void Configure(Vector2 beamOrigin, Vector2 beamDirection, float beamLength, float beamWidth,
                              int beamDamage, float warn, float fire, float tickInterval = 0.12f)
        {
            origin = beamOrigin;
            direction = beamDirection.sqrMagnitude > 0.0001f ? beamDirection.normalized : Vector2.up;
            length = beamLength;
            width = beamWidth;
            damage = beamDamage;
            // Luôn chờ ít nhất 1 frame / 0.05s trước khi Fire — tránh mất SetTrigger
            // cùng frame với Instantiate (bug lần đầu Animator chưa init).
            warnTime = Mathf.Max(0.05f, warn);
            fireTime = Mathf.Max(0.05f, fire);
            damageTickInterval = Mathf.Max(0.02f, tickInterval);

            timer = 0f;
            firing = false;
            fireTriggered = false;
            nextDamageTime = 0f;
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (!firing)
            {
                if (timer >= warnTime)
                {
                    firing = true;
                    timer = 0f;
                    TriggerFire();
                }
                return;
            }

            if (Time.time >= nextDamageTime)
            {
                ApplyDamage();
                nextDamageTime = Time.time + damageTickInterval;
            }

            if (timer >= fireTime) Destroy(gameObject);
        }

        private void TriggerFire()
        {
            if (fireTriggered) return;
            fireTriggered = true;

            if (anim == null)
            {
                anim = GetComponent<Animator>();
                if (anim == null) anim = GetComponentInChildren<Animator>();
            }

            if (anim == null) return;

            // Reset rồi set lại để chắc chắn trigger không bị “nuốt” lần đầu.
            anim.ResetTrigger("Fire");
            anim.SetTrigger("Fire");
        }

        private void ApplyDamage()
        {
            Vector2 center = origin + direction * (length * 0.5f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Vector2 size = new(length, width);

            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle);
            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];
                if (hit == null || EnemyCombatRules.IsEnemyCollider(hit)) continue;
                if (EnemyCombatRules.TryGetPlayerDamageable(hit, out var target))
                {
                    target.TakeDamage(damage);
                    break;
                }
            }
        }
    }
}
