using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HeartOfTheNight.Common; // 1. GỌI BỘ LUẬT CHUNG CỦA TEAM VÀO ĐÂY

// 2. KẾT NỐI VỚI INTERFACE IDamageable
public class EyeOfNightImg : MonoBehaviour, IDamageable
{
    [Header("Hoạt ảnh")]
    public Animator anim;

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

                if (shieldPrefab != null)
                {
                    // Sinh khiên ra ngoài không gian tự do
                    GameObject shieldClone = Instantiate(shieldPrefab, Vector3.zero, Quaternion.identity);

                    // CHÌA KHÓA Ở ĐÂY: Dùng InChildren để lục tìm chắc chắn có hình ảnh
                    SpriteRenderer targetSr = target.GetComponentInChildren<SpriteRenderer>();
                    SpriteRenderer shieldSr = shieldClone.GetComponentInChildren<SpriteRenderer>();
                    Collider2D targetCol = target.GetComponentInChildren<Collider2D>(); // Lấy thêm Collider để canh tâm

                    if (targetSr != null && shieldSr != null && targetCol != null)
                    {
                        // 1. CHỈNH TÂM TUYỆT ĐỐI (Dùng Collider để tránh bị lệch xuống bóng dưới chân)
                        shieldClone.transform.position = targetCol.bounds.center;

                        // 2. KÍCH THƯỚC: Đo theo ảnh thật của quái
                        float chieuRongQuai = targetSr.bounds.size.x;
                        float chieuCaoQuai = targetSr.bounds.size.y;
                        float maxKichThuocQuai = Mathf.Max(chieuRongQuai, chieuCaoQuai);

                        // Lấy kích thước gốc của bức ảnh khiên
                        float kichThuocGocKhien = shieldSr.sprite.bounds.size.x;

                        // Ép Scale (Nhân thêm 1.3f để tạo khoảng hở bọc ngoài)
                        if (kichThuocGocKhien > 0)
                        {
                            float worldScale = (maxKichThuocQuai / kichThuocGocKhien) * 1.3f;
                            shieldClone.transform.localScale = new Vector3(worldScale, worldScale, 1f);
                        }

                        // 3. NHÉT VÀO LÀM CON CỦA QUÁI
                        shieldClone.transform.SetParent(target.transform, true);
                    }
                    else
                    {
                        // Nếu lỡ prefab bị lỗi thiếu cái gì đó, gắn tạm vào gót chân
                        shieldClone.transform.position = target.transform.position;
                        shieldClone.transform.SetParent(target.transform, true);
                    }

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

    // 3. HÀM NÀY ĐÃ CHUẨN ĐỂ NHẬN SÁT THƯƠNG TỪ PLAYER
    public void TakeDamage(int damage)
    {
        if (isDead) return;
        currentHealth -= damage;
        Debug.Log("Mắt đêm bị chém! Máu: " + currentHealth + "/" + maxHealth);

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;

        DeactivateShield();

        Debug.Log("Eye of the Night đã bị tiêu diệt!");

        if (anim != null)
        {
            anim.enabled = true;
            anim.SetTrigger("Dead");
        }

        Destroy(gameObject, 0.5f);
    }
}