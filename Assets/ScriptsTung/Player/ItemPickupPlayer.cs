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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isCollected) return;

        // Dùng GetComponentInParent để tìm file PlayerHealth từ object va chạm hoặc cha của nó
        HeartOfTheNight.Player.PlayerHealth playerHp = collision.GetComponentInParent<HeartOfTheNight.Player.PlayerHealth>();

        if (playerHp != null)
        {
       
            if (itemType == ItemType.HealHP)
            {
                if (playerHp.GetCurrentHealth() >= playerHp.MaxHealth)
                {
                    return; // Đầy máu rồi thì thoát hàm luôn, không nhặt và không xóa item
                }
            }

            isCollected = true;

            // Hiệu ứng ăn đồ
            if (pickupVFX != null) Instantiate(pickupVFX, transform.position, Quaternion.identity);

            // Ẩn vật phẩm đi
            if (TryGetComponent(out SpriteRenderer sr)) sr.enabled = false;

            // Tắt hết toàn bộ Collider của viên Item để tránh va chạm lần 2
            Collider2D[] cols = GetComponents<Collider2D>();
            foreach (var c in cols) c.enabled = false;

            transform.SetParent(playerHp.transform);
            transform.localPosition = Vector3.zero;

            // Kích hoạt coroutine buff (truyền thẳng GameObject gốc của Player vào)
            StartCoroutine(ApplyBuffRoutine(playerHp.gameObject));
        }
    }

    private IEnumerator ApplyBuffRoutine(GameObject player)
    {
        switch (itemType)
        {
            case ItemType.HealHP:
                // Tìm file máu từ object bị đụng hoặc cha của nó
                HeartOfTheNight.Player.PlayerHealth hpScript = player.GetComponentInParent<HeartOfTheNight.Player.PlayerHealth>();
                if (hpScript != null)
                {
                    hpScript.Heal(healAmount);
                    Debug.Log("Đã hồi máu!");
                }
                else
                {
                    Debug.LogWarning("⚠️ Đụng Player rồi nhưng không tìm thấy file PlayerHealth!");
                }

                Destroy(gameObject); // Ăn máu xong là hủy viên máu liền
                yield break;

            case ItemType.Shield:
                HeartOfTheNight.Player.PlayerHealth shieldScript = player.GetComponentInParent<HeartOfTheNight.Player.PlayerHealth>();
                if (shieldScript != null)
                {
                    shieldScript.hasShield = true; // Bật cờ vô địch

                    GameObject shieldInstance = null;
                    if (shieldVisualPrefab != null)
                    {
                        shieldInstance = Instantiate(shieldVisualPrefab, player.transform.position, Quaternion.identity, player.transform);
                    }

                    Debug.Log("🛡️ Bật Khiên! Quái chém vẫn ra đòn nhưng không mất máu.");
                    yield return new WaitForSeconds(buffDuration);

                    shieldScript.hasShield = false; // Tắt cờ vô địch
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

        // Hủy viên Item sau khi buff xong (Áp dụng cho Shield, Speed, Jump vì nó phải dùng yield return đợi hết giờ)
        Destroy(gameObject);
    }
}