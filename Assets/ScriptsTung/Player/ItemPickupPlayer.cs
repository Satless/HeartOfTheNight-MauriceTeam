using HeartOfTheNight.Player;
using System.Collections;
using UnityEngine;

public class ItemPickupPlayer : MonoBehaviour
{
    public enum ItemType { HealHP, Shield, SpeedBuff, JumpBuff }

    [Header("Cài đặt Item")]
    public ItemType itemType;
    public float buffDuration = 5f;

    [Header("Chỉ số")]
    public int healAmount = 30;
    public float speedMultiplier = 1.5f;
    public float jumpMultiplier = 1.5f;

    [Header("Hiệu ứng (Tùy chọn)")]
    public GameObject pickupVFX;
    public GameObject shieldVisualPrefab;

    private bool isCollected = false;

    // 🔥 BAO TRỌN GÓI: Player đi xuyên qua (Trigger) hay đụng vật lý (Collision) đều nhặt được hết!
    private void OnTriggerEnter2D(Collider2D collision)
    {
        XuLyNhatDo(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        XuLyNhatDo(collision.gameObject);
    }

    // Tách riêng logic nhặt đồ ra một hàm để dùng chung cho cả 2 trường hợp trên
    private void XuLyNhatDo(GameObject doiTuongVaCham)
    {
        if (isCollected) return;

        // Tìm file PlayerHealth từ object va chạm hoặc cha của nó
        HeartOfTheNight.Player.PlayerHealth playerHp = doiTuongVaCham.GetComponentInParent<HeartOfTheNight.Player.PlayerHealth>();

        if (playerHp != null)
        {
            if (itemType == ItemType.HealHP)
            {
                AntiHeal buaCamMau = doiTuongVaCham.GetComponentInParent<AntiHeal>();

                if (buaCamMau != null && buaCamMau.thoiGianConLai > 0)
                {
                    Debug.Log("⛔ Đang dính Anti-Heal! Đi ngang qua cục máu không thèm nhặt!");
                    return;
                }

                if (playerHp.GetCurrentHealth() >= playerHp.MaxHealth)
                {
                    return;
                }
            }

            isCollected = true;

            // Dừng hiệu ứng lơ lửng của item (nếu có script ItemFloating)
            if (TryGetComponent(out ItemFloating floatingScript))
            {
                floatingScript.StopFloating();
            }

            // Sinh hiệu ứng ăn đồ
            if (pickupVFX != null) Instantiate(pickupVFX, transform.position, Quaternion.identity);

            // Tắt Rigidbody và toàn bộ hình ảnh, va chạm của Item
            if (TryGetComponent(out Rigidbody2D rb)) rb.simulated = false;

            SpriteRenderer[] allSprites = GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in allSprites) sr.enabled = false;

            Collider2D[] cols = GetComponents<Collider2D>();
            foreach (var c in cols) c.enabled = false;

            // Gắn vào Player để đếm thời gian buff
            transform.SetParent(playerHp.transform);
            transform.localPosition = Vector3.zero;

            // Kích hoạt buff cho các loại item
            StartCoroutine(ApplyBuffRoutine(playerHp.gameObject));
        }
    }

    private IEnumerator ApplyBuffRoutine(GameObject player)
    {
        switch (itemType)
        {
            case ItemType.HealHP:
                HeartOfTheNight.Player.PlayerHealth hpScript = player.GetComponentInParent<HeartOfTheNight.Player.PlayerHealth>();
                if (hpScript != null)
                {
                    hpScript.Heal(healAmount);
                    Debug.Log("💚 Đã hồi máu!");
                }
                Destroy(gameObject);
                yield break;

            case ItemType.Shield:
                HeartOfTheNight.Player.PlayerHealth shieldScript = player.GetComponentInParent<HeartOfTheNight.Player.PlayerHealth>();
                if (shieldScript != null)
                {
                    shieldScript.hasShield = true;

                    GameObject shieldInstance = null;
                    if (shieldVisualPrefab != null)
                    {
                        shieldInstance = Instantiate(shieldVisualPrefab, player.transform.position, Quaternion.identity, player.transform);
                    }

                    Debug.Log("🛡️ Bật Khiên!");
                    yield return new WaitForSeconds(buffDuration);

                    shieldScript.hasShield = false;
                    if (shieldInstance != null) Destroy(shieldInstance);
                    Debug.Log("Hết Khiên!");
                }
                break;

            case ItemType.SpeedBuff:
                HeartOfTheNight.Player.PlayerMovement speedScript = player.GetComponentInParent<HeartOfTheNight.Player.PlayerMovement>();
                if (speedScript != null)
                {
                    speedScript.moveSpeedMultiplier = speedMultiplier;
                    Debug.Log("⚡ Tăng tốc độ chạy!");

                    yield return new WaitForSeconds(buffDuration);

                    speedScript.moveSpeedMultiplier = 1f;
                    Debug.Log("Hết buff tốc độ.");
                }
                break;

            case ItemType.JumpBuff:
                HeartOfTheNight.Player.PlayerMovement jumpScript = player.GetComponentInParent<HeartOfTheNight.Player.PlayerMovement>();
                if (jumpScript != null)
                {
                    jumpScript.jumpForceMultiplier = jumpMultiplier;
                    Debug.Log("🦘 Tăng lực nhảy!");

                    yield return new WaitForSeconds(buffDuration);

                    jumpScript.jumpForceMultiplier = 1f;
                    Debug.Log("Hết buff lực nhảy.");
                }
                break;
        }

        Destroy(gameObject);
    }
}