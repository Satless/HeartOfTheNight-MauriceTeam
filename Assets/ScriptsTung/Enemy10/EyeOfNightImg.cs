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