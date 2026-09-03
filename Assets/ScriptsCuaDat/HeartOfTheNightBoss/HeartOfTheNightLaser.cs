using UnityEngine;

namespace HeartOfTheNight.Enemy
{
    public class HeartOfTheNightLaser : MonoBehaviour
    {
        private Animator anim;
        private SpriteRenderer[] sprites;
        private BoxCollider2D box;
        private Vector2 origin;
        private Vector2 direction;
        private float length;
        private float width;
        private int damage;
        private float warnTime;
        private float fireTime;
        private float damageTickInterval;
        private bool showTelegraph;

        private float timer;
        private bool firing;
        private bool fireTriggered;
        private float nextDamageTime;

        private static readonly Color WarnTint = new(1f, 0.35f, 0.35f, 1f);
        private static readonly Color FireTint = Color.white;

        private float hitWidth;
        private float hitLength;

        // Sprite có gai/glow ngoài lõi đỏ. Collider gốc prefab ~4.08 / 8.875 ≈ nửa khung.
        private const float HitboxWidthScale = 0.5f;
        private const string OverlaySortingLayer = "freeLightLayer";
        private const int OverlaySortingOrder = 20;
        private static readonly Collider2D[] Hits = new Collider2D[16];

        private void Awake()
        {
            anim = GetComponent<Animator>();
            if (anim == null) anim = GetComponentInChildren<Animator>();
            sprites = GetComponentsInChildren<SpriteRenderer>(true);
            box = GetComponent<BoxCollider2D>();
            DrawInFrontOfPlayer();
        }

        private void DrawInFrontOfPlayer()
        {
            if (sprites == null) return;
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i].sortingLayerName = OverlaySortingLayer;
                sprites[i].sortingOrder = OverlaySortingOrder;
            }
        }

        public void Configure(Vector2 beamOrigin, Vector2 beamDirection, float beamLength, float beamWidth,
                              int beamDamage, float warn, float fire, float tickInterval = 0.12f,
                              bool scaleVisualToStats = false)
        {
            origin = beamOrigin;
            direction = beamDirection.sqrMagnitude > 0.0001f ? beamDirection.normalized : Vector2.up;
            length = Mathf.Max(0.1f, beamLength);
            width = Mathf.Max(0.05f, beamWidth);
            damage = beamDamage;
            fireTime = Mathf.Max(0.05f, fire);
            damageTickInterval = Mathf.Max(0.02f, tickInterval);

            // 8 tia: telegraph thật. Cột lửa (warn ~0) bắn ngay sau vòng cảnh báo riêng.
            showTelegraph = warn >= 0.2f;
            warnTime = showTelegraph ? Mathf.Max(1f, warn) : Mathf.Max(0.05f, warn);

            timer = 0f;
            firing = false;
            fireTriggered = false;
            nextDamageTime = 0f;

            if (scaleVisualToStats)
                FitVisualToBeam();
            else
                UsePrefabVisualSize();

            if (box != null)
                box.enabled = false;

            if (showTelegraph)
                ApplyWarnVisual(0f);
            else
            {
                firing = true;
                TriggerFire();
            }
        }

        private SpriteRenderer MainSprite =>
            sprites != null && sprites.Length > 0 ? sprites[0] : null;

        /// <summary>
        /// Giữ scale prefab (tia to như art). Hitbox lấy đúng kích thước sprite đang hiện.
        /// </summary>
        private void UsePrefabVisualSize()
        {
            var sr = MainSprite;
            Sprite sprite = sr != null ? sr.sprite : null;
            if (sprite == null) return;

            float ppu = sprite.pixelsPerUnit > 0.01f ? sprite.pixelsPerUnit : 16f;
            float nativeW = sprite.rect.width / ppu;
            float nativeH = sprite.rect.height / ppu;
            if (nativeW < 0.01f || nativeH < 0.01f) return;

            Vector3 s = transform.localScale;
            width = nativeW * Mathf.Abs(s.x);
            length = nativeH * Mathf.Abs(s.y);
            hitWidth = width * HitboxWidthScale;
            hitLength = length;
            SyncHitboxCollider(nativeW, nativeH);
        }

        private void FitVisualToBeam()
        {
            var sr = MainSprite;
            Sprite sprite = sr != null ? sr.sprite : null;
            if (sprite == null) return;

            float ppu = sprite.pixelsPerUnit > 0.01f ? sprite.pixelsPerUnit : 16f;
            float nativeW = sprite.rect.width / ppu;
            float nativeH = sprite.rect.height / ppu;
            if (nativeW < 0.01f || nativeH < 0.01f) return;

            transform.localScale = new Vector3(width / nativeW, length / nativeH, 1f);
            hitWidth = width * HitboxWidthScale;
            hitLength = length;
            SyncHitboxCollider(nativeW, nativeH);
        }

        private void SyncHitboxCollider(float nativeW, float nativeH)
        {
            if (box == null) return;
            box.offset = new Vector2(0f, nativeH * 0.5f);
            box.size = new Vector2(nativeW * HitboxWidthScale, nativeH);
            box.enabled = false;
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (!firing)
            {
                ApplyWarnVisual(timer);
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

        private void ApplyWarnVisual(float elapsed)
        {
            // Nhấp mờ — đứng trong tia lúc này CHƯA mất máu.
            float pulse = 0.22f + 0.2f * (0.5f + 0.5f * Mathf.Sin(elapsed * 14f));
            SetSpriteColor(new Color(WarnTint.r, WarnTint.g, WarnTint.b, pulse));
        }

        private void ApplyFireVisual()
        {
            SetSpriteColor(FireTint);
        }

        private void SetSpriteColor(Color color)
        {
            if (sprites == null) return;
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                    sprites[i].color = color;
            }
        }

        private void TriggerFire()
        {
            if (fireTriggered) return;
            fireTriggered = true;

            ApplyFireVisual();

            if (anim == null)
            {
                anim = GetComponent<Animator>();
                if (anim == null) anim = GetComponentInChildren<Animator>();
            }

            if (anim == null) return;

            anim.ResetTrigger("Fire");
            anim.Play("Laser", 0, 0f);
            anim.SetTrigger("Fire");
        }

        private void ApplyDamage()
        {
            float w = hitWidth > 0.01f ? hitWidth : width;
            float h = hitLength > 0.01f ? hitLength : length;
            Vector2 center = origin + direction * (h * 0.5f);
            float angle = transform.eulerAngles.z;
            Vector2 size = new(w, h);

            int count = Physics2D.OverlapBoxNonAlloc(center, size, angle, Hits);
            for (int i = 0; i < count; i++)
            {
                var hit = Hits[i];
                if (hit == null || EnemyCombatRules.IsEnemyCollider(hit)) continue;
                if (EnemyCombatRules.TryGetPlayerDamageable(hit, out var target))
                {
                    target.TakeDamage(damage);
                    break;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            float w = hitWidth > 0.01f ? hitWidth : width;
            float h = hitLength > 0.01f ? hitLength : length;
            Gizmos.color = firing ? Color.red : Color.yellow;
            Vector3 center = origin + direction * (h * 0.5f);
            var old = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, Quaternion.Euler(0f, 0f, transform.eulerAngles.z), Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(w, h, 0.1f));
            Gizmos.matrix = old;
        }
#endif
    }
}
