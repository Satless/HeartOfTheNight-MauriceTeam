// --- HeartOfTheNightLaser.cs ---
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
        private float nextDamageTime;

        private void Awake()
        {
            anim = GetComponent<Animator>();
        }

        public void Configure(Vector2 beamOrigin, Vector2 beamDirection, float beamLength, float beamWidth, int beamDamage, float warn, float fire, float tickInterval = 0.12f)
        {
            origin = beamOrigin;
            direction = beamDirection.sqrMagnitude > 0.0001f ? beamDirection.normalized : Vector2.up;
            length = beamLength;
            width = beamWidth;
            damage = beamDamage;
            warnTime = Mathf.Max(0f, warn);
            fireTime = Mathf.Max(0.05f, fire);
            damageTickInterval = Mathf.Max(0.02f, tickInterval);

            timer = 0f;
            firing = warnTime <= 0f;
            nextDamageTime = 0f;

            if (anim != null && firing)
            {
                anim.SetTrigger("Fire");
            }
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
                    if (anim != null) anim.SetTrigger("Fire");
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