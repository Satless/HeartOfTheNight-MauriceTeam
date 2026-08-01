    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class EyeOfNightImg : MonoBehaviour
    {
        [Header("Hoạt ảnh")]
        public Animator anim;/////////

        [Header("Chỉ số Sinh tồn")]
        public int maxHealth = 150;
        private int currentHealth;

        [Header("Cài đặt Kỹ năng Khiên")]
        public GameObject shieldPrefab;
        public float shieldDuration = 5f;
        public float cooldown = 10f;

        // Lưu trữ khiên và danh tính (Tag) gốc của quái
        private List<GameObject> activeShields = new List<GameObject>();
        private Dictionary<GameObject, string> originalTags = new Dictionary<GameObject, string>();
        private bool isDead = false;

        void Start()
        {
            if (anim == null) anim = GetComponentInChildren<Animator>();

            currentHealth = maxHealth;
            StartCoroutine(ShieldLoop());
        }

        IEnumerator ShieldLoop()
        {
            while (!isDead)
            {
                yield return new WaitForSeconds(cooldown);
                if (isDead) break;

                ActivateShield();

                yield return new WaitForSeconds(shieldDuration);

                DeactivateShield();
            }
        }

    void ActivateShield()
    {
        Debug.Log("Mắt Đêm: Bắt đầu Buff Khiên!");

        // Dọn dẹp dữ liệu cũ cho an toàn
        originalTags.Clear();
        activeShields.Clear();

        List<GameObject> allTargets = new List<GameObject>();
        allTargets.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));
        allTargets.AddRange(GameObject.FindGameObjectsWithTag("Boss"));

        foreach (GameObject target in allTargets)
        {
            if (target != null && target != this.gameObject)
            {
                // Cất Tag cũ đi và đổi thành Untagged
                originalTags.Add(target, target.tag);
                target.tag = "Untagged";

                // Ép hình ảnh cái khiên bám vào người quái
                if (shieldPrefab != null)
                {
                    GameObject shieldClone = Instantiate(shieldPrefab, target.transform.position, Quaternion.identity, target.transform);

                    // =======================================================
                    // THUẬT TOÁN AUTO-FIT (TỰ ĐỘNG BỌC KHÍT KHIÊN VÀO QUÁI)
                    // =======================================================
                    Collider2D targetCol = target.GetComponent<Collider2D>();
                    SpriteRenderer shieldSr = shieldClone.GetComponent<SpriteRenderer>();

                    if (targetCol != null && shieldSr != null)
                    {
                        // 1. CHỈNH TÂM: Dời cái khiên từ gót chân (transform gốc) lên đúng tâm của Collider
                        Vector3 offsetToCenter = targetCol.bounds.center - target.transform.position;
                        // Phải chia cho localScale của quái vật đề phòng con quái đó bị lật mặt (scale X = -1)
                        offsetToCenter.x /= target.transform.localScale.x;
                        offsetToCenter.y /= target.transform.localScale.y;
                        shieldClone.transform.localPosition = offsetToCenter;

                        // 2. CO GIÃN: Tính toán kích thước để phóng to/thu nhỏ khiên
                        float doRongQuai = targetCol.bounds.size.x;
                        float doCaoQuai = targetCol.bounds.size.y;

                        float doRongKhien = shieldSr.sprite.bounds.size.x;
                        float doCaoKhien = shieldSr.sprite.bounds.size.y;

                        // Tính tỷ lệ cần phóng to (Nhân thêm 1.3f để tạo độ hở padding bọc ngoài quái cho đẹp)
                        float scaleX = (doRongQuai / doRongKhien) * 1.3f;
                        float scaleY = (doCaoQuai / doCaoKhien) * 1.3f;

                        // Dùng số lớn hơn để đảm bảo khiên vẫn giữ được form tròn (không bị méo thành hình oval)
                        float finalScale = Mathf.Max(scaleX, scaleY);

                        // Chia lại cho scale gốc của quái để khiên không bị phóng to gấp đôi nếu bản thân quái đang scale to
                        shieldClone.transform.localScale = new Vector3(
                            finalScale / Mathf.Abs(target.transform.localScale.x),
                            finalScale / Mathf.Abs(target.transform.localScale.y),
                            1f
                        );
                    }
                    // =======================================================

                    activeShields.Add(shieldClone);
                }
            }
        }
    }

    void DeactivateShield()
        {
            Debug.Log("Mắt Đêm: Thu hồi Khiên!");

            // Trả lại Tag gốc cho quái để Player chém trúng lại
            foreach (var kvp in originalTags)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.tag = kvp.Value;
                }
            }
            originalTags.Clear();

            // Xóa sổ hình ảnh khiên
            foreach (GameObject shield in activeShields)
            {
                if (shield != null) Destroy(shield);
            }
            activeShields.Clear();
        }

        public void TakeDamage(int damage)
        {
            if (isDead) return;
            currentHealth -= damage;
            if (currentHealth <= 0) Die();
        }

        void Die()
        {
            isDead = true;

            // 1. Thu hồi toàn bộ khiên và trả Tag lại trước khi Mắt chết
            DeactivateShield();

            Debug.Log("Eye of the Night đã bị tiêu diệt!");

            // 2. Chạy Animation chết
            if (anim != null)
            {
                anim.enabled = true;
                anim.SetTrigger("Dead");
            }

            // 3. Chờ 1.5s để chạy hết Animation rồi mới xóa hẳn khỏi màn hình
            Destroy(gameObject, 0.5f);
        }
    }