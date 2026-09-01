using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HeartOfTheNight.Common;

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

    [Range(0f, 1f)]
    public float shieldOpacity = 0.5f;

    [Header("Cài đặt Chết")]
    [Tooltip("Chỉnh số này bằng đúng thời gian chạy của Animation Dead")]
    public float deathAnimDuration = 2f;
    [Tooltip("Kéo vị trí của Animation chết (nhập số âm để kéo tụt xuống đất)")]
    public float deathYOffset = 0f; // 🔥 Đã thêm biến kéo vị trí chết

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

        originalTags.Clear();
        activeShields.Clear();

        AudioEvents.TriggerSound3D("Enemy", "EyeOfNight", "ShieldActivate", transform.position);

        List<GameObject> allTargets = new List<GameObject>();
        allTargets.AddRange(GameObject.FindGameObjectsWithTag("Enemy"));
        allTargets.AddRange(GameObject.FindGameObjectsWithTag("Boss"));

        foreach (GameObject target in allTargets)
        {
            if (target != null && target != this.gameObject)
            {
                originalTags.Add(target, target.tag);
                target.tag = "Untagged";

                if (shieldPrefab != null)
                {
                    GameObject shieldClone = Instantiate(shieldPrefab, Vector3.zero, Quaternion.identity);

                    SpriteRenderer targetSr = target.GetComponentInChildren<SpriteRenderer>();
                    SpriteRenderer shieldSr = shieldClone.GetComponentInChildren<SpriteRenderer>();
                    Collider2D targetCol = target.GetComponentInChildren<Collider2D>();

                    if (targetSr != null && shieldSr != null && targetCol != null)
                    {
                        Color shieldColor = shieldSr.color;
                        shieldColor.a = shieldOpacity;
                        shieldSr.color = shieldColor;

                        shieldClone.transform.position = targetCol.bounds.center;

                        float chieuRongQuai = targetSr.bounds.size.x;
                        float chieuCaoQuai = targetSr.bounds.size.y;
                        float maxKichThuocQuai = Mathf.Max(chieuRongQuai, chieuCaoQuai);

                        float kichThuocGocKhien = shieldSr.sprite.bounds.size.x;

                        if (kichThuocGocKhien > 0)
                        {
                            float worldScale = (maxKichThuocQuai / kichThuocGocKhien) * 1.3f;
                            shieldClone.transform.localScale = new Vector3(worldScale, worldScale, 1f);
                        }

                        shieldClone.transform.SetParent(target.transform, true);
                    }
                    else
                    {
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

        AudioEvents.TriggerSound3D("Enemy", "EyeOfNight", "ShieldDeactivate", transform.position);

        foreach (var kvp in originalTags)
        {
            if (kvp.Key != null)
            {
                kvp.Key.tag = kvp.Value;
            }
        }
        originalTags.Clear();

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
        Debug.Log("Mắt đêm bị chém! Máu: " + currentHealth + "/" + maxHealth);

        if (currentHealth <= 0) Die();
    }

    void Die()
    {
        isDead = true;
        DeactivateShield();
        Debug.Log("Eye of the Night đã bị tiêu diệt!");

        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        // 🔥 CẬP NHẬT: Dịch chuyển vị trí chết theo trục Y
        transform.position = new Vector3(transform.position.x, transform.position.y + deathYOffset, transform.position.z);

        if (anim != null)
        {
            anim.enabled = true;
            anim.SetTrigger("Dead");
        }

        StartCoroutine(XuLyXoaXac());
    }

    IEnumerator XuLyXoaXac()
    {
        // Đợi theo đúng số bác chỉnh ở inspector (ví dụ 2 giây)
        yield return new WaitForSeconds(deathAnimDuration);

        // 🔥 FIX: Quét sạch sành sanh TẤT CẢ các ảnh con (mắt, bóng đổ, hiệu ứng...)
        SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>();
        foreach (SpriteRenderer sr in srs)
        {
            if (sr != null) sr.enabled = false;
        }

        // Xóa hoàn toàn object sau nửa giây
        Destroy(gameObject);
    }
}