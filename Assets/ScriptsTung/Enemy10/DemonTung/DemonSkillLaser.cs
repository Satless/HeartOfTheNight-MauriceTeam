using UnityEngine;
using HeartOfTheNight.Common;
using HeartOfTheNight.Enemy;

namespace HeartOfTheNight.Enemy
{
    public class DemonSkillLaser : MonoBehaviour
    {
        [Header("Thông Số Kích Thước (Hitbox Ảo)")]
        public float length = 10f;          // Độ cao cột lửa
        public float width = 2f;            // Bề ngang cột lửa

        [Header("Thông Số Sát Thương")]
        public int damagePerTick = 15;      // Sát thương mỗi lần giật
        public float damageTickInterval = 0.12f; // Tần suất giật máu (0.12s giật 1 lần)

        [Header("Thời Gian Chiêu Thức")]
        [Tooltip("Bao lâu sau khi sinh ra thì bắt đầu kích nổ Laze")]
        public float warnTime = 1.5f;
        [Tooltip("Laze cháy trong bao nhiêu giây trước khi tự biến mất")]
        public float fireTime = 2.0f;

        private Animator anim;
        private Vector2 origin;
        private Vector2 direction = Vector2.up; // Bắn thẳng lên trời

        private float timer;
        private bool isFiring;
        private bool fireTriggered;
        private float nextDamageTime;

        private void Awake()
        {
            anim = GetComponent<Animator>();
            if (anim == null) anim = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            // Chốt tọa độ tâm để tính toán Hitbox
            origin = transform.position;

            timer = 0f;
            isFiring = false;
            fireTriggered = false;
            nextDamageTime = 0f;

            // Chống người nhập sai thông số âm
            warnTime = Mathf.Max(0.05f, warnTime);
            fireTime = Mathf.Max(0.05f, fireTime);
            damageTickInterval = Mathf.Max(0.02f, damageTickInterval);
        }

        private void Update()
        {
            timer += Time.deltaTime;

            // GIAI ĐOẠN 1: CẢNH BÁO
            if (!isFiring)
            {
                if (timer >= warnTime)
                {
                    isFiring = true;
                    timer = 0f; // Reset đồng hồ cho giai đoạn cháy
                    TriggerFire();
                }
                return;
            }

            // GIAI ĐOẠN 2: BÙM! LỬA CHÁY VÀ TRỪ MÁU
            if (Time.time >= nextDamageTime)
            {
                ApplyDamage();
                nextDamageTime = Time.time + damageTickInterval;
            }

            // GIAI ĐOẠN 3: TỰ HỦY
            if (timer >= fireTime)
            {
                Destroy(gameObject);
            }
        }

        private void TriggerFire()
        {
            if (fireTriggered) return;
            fireTriggered = true;

            if (anim != null)
            {
                // Gọi tới Animator để chuyển sang Animation nổ lửa
                anim.ResetTrigger("Fire");
                anim.SetTrigger("Fire");
            }
        }

        private void ApplyDamage()
        {
            // Dùng toán học vẽ Box tàng hình, không cần Collider vật lý!
            Vector2 center = origin + direction * (length * 0.5f);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Vector2 size = new Vector2(length, width);

            // Quét trúng đứa nào thì bỏ vô mảng hits
            Collider2D[] hits = Physics2D.OverlapBoxAll(center, size, angle);

            for (int i = 0; i < hits.Length; i++)
            {
                var hit = hits[i];

                if (hit == null) continue;

                // Trượt quái, trượt tường, chỉ tìm Player
                if (EnemyCombatRules.IsEnemyCollider(hit)) continue;

                // Nếu tìm thấy hệ thống máu của Player thì trừ máu
                if (EnemyCombatRules.TryGetPlayerDamageable(hit, out var target))
                {
                    target.TakeDamage(damagePerTick);
                    Debug.Log("🔥 Laze quét trúng Player: " + damagePerTick + " máu!");
                    break;
                }
            }
        }

        // ==========================================
        // VẼ HITBOX MÀU ĐỎ ĐỂ SẾP CĂN CHỈNH KÍCH THƯỚC TRÊN SCENE
        // ==========================================
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, 0.5f);
            Vector2 startPos = Application.isPlaying ? origin : (Vector2)transform.position;
            Vector2 drawCenter = startPos + direction * (length * 0.5f);
            Vector3 size3D = new Vector3(width, length, 1f);

            // Vẽ hộp ảo theo thông số Length và Width sếp nhập
            Gizmos.DrawWireCube(drawCenter, size3D);
        }
    }
}