using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    /// <summary>
    /// Tia laze tu sinh hinh anh (LineRenderer). Gom 2 pha:
    ///  - Warn:  tia mong, mo, KHONG gay sat thuong (canh bao).
    ///  - Fire:  tia day, sang, GAY sat thuong len Player moi nhip.
    /// Dung chung cho State 2 (laze 8 huong) va State 3 (cot lua thang dung).
    /// Goi Configure(...) ngay sau khi Instantiate.
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class HeartOfTheNightLaser : MonoBehaviour
    {
        private LineRenderer line;

        private Vector2 origin;
        private Vector2 direction;
        private float length;
        private float width;
        private int damage;
        private float warnTime;
        private float fireTime;
        private float damageTickInterval = 0.12f;

        private float timer;
        private bool firing;
        private float nextDamageTime;

        private static readonly Color WarnColor = new(1f, 0.25f, 0.35f, 0.35f);
        private static readonly Color FireColor = new(1f, 0.15f, 0.2f, 1f);

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            EnsureMaterial();
            line.useWorldSpace = true;
            line.numCapVertices = 4;
            line.positionCount = 2;
        }

        private void EnsureMaterial()
        {
            if (line.sharedMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader != null) line.material = new Material(shader);
            }
        }

        /// <param name="beamOrigin">Diem bat dau tia.</param>
        /// <param name="beamDirection">Huong tia (se duoc normalize).</param>
        public void Configure(Vector2 beamOrigin, Vector2 beamDirection, float beamLength,
                              float beamWidth, int beamDamage, float warn, float fire,
                              float tickInterval = 0.12f)
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

            ApplyVisual(firing);
            UpdateLinePositions();
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (!firing)
            {
                PulseWarn();
                if (timer >= warnTime)
                {
                    firing = true;
                    timer = 0f;
                    ApplyVisual(true);
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

        private void UpdateLinePositions()
        {
            Vector3 end = (Vector3)(origin + direction * length);
            line.SetPosition(0, origin);
            line.SetPosition(1, end);
        }

        private void ApplyVisual(bool fire)
        {
            Color c = fire ? FireColor : WarnColor;
            float w = fire ? width : width * 0.25f;
            line.startColor = c;
            line.endColor = new Color(c.r, c.g, c.b, c.a * 0.6f);
            line.startWidth = w;
            line.endWidth = w;
        }

        private void PulseWarn()
        {
            float t = warnTime > 0f ? Mathf.Clamp01(timer / warnTime) : 1f;
            float w = Mathf.Lerp(width * 0.1f, width * 0.4f, t);
            line.startWidth = w;
            line.endWidth = w;
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
