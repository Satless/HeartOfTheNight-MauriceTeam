using UnityEngine;
using System.Collections.Generic;
using System.Reflection; // BẮT BUỘC CÓ: Dùng để soi code của đồng đội (Fallback)
using HeartOfTheNight.Enemy;

[System.Serializable]
public class LootItem
{
    public string itemName = "Vật phẩm";
    public GameObject itemPrefab;
    [Range(0f, 100f)]
    public float dropChance = 50f;
}

public class EnemyLootDrop : MonoBehaviour
{
    [Header("Cài đặt Rớt Đồ")]
    public bool dropMultipleItems = true;
    public List<LootItem> lootTable = new List<LootItem>();

    [Header("Lớp bảo vệ thứ 6: Tên biến tự gõ (Tùy chọn)")]
    [Tooltip("Gõ chính xác tên biến bool/int/float nếu đồng đội đặt tên dị (VD: daNgum, healtPoint...)")]
    public string customVariableName = "";

    private bool hasDropped = false;
    private MonoBehaviour targetScript;
    private FieldInfo customField;
    private FieldInfo isDeadField;
    private FieldInfo healthField;
    private Collider2D enemyCollider;
    private bool isQuitting = false;
    private IEnemyStatus enemyStatus;

    void Start()
    {
        enemyCollider = GetComponent<Collider2D>();

        // [LỚP BẢO VỆ MỚI - TỐI ƯU ZERO-GC]: Ưu tiên dùng Interface chuẩn
        enemyStatus = GetComponent<IEnemyStatus>();
        if (enemyStatus != null) return; // Nếu quái đã hỗ trợ Interface thì nghỉ luôn, không cần soi code!

        MonoBehaviour[] allScripts = GetComponents<MonoBehaviour>();

        // Điệp viên bắt đầu đi soi các script khác gắn trên cùng con quái (Fallback chậm)
        foreach (MonoBehaviour script in allScripts)
        {
            if (script == this) continue; // Bỏ qua bản thân nó
            System.Type type = script.GetType();

            // Ưu tiên tìm cái tên biến Dị hợm mà bác gõ trong Inspector
            if (!string.IsNullOrEmpty(customVariableName))
            {
                customField = type.GetField(customVariableName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (customField != null) { targetScript = script; return; }
            }

            // [LỚP BẢO VỆ 1]: Tìm các biến bool báo tử phổ thông
            isDeadField = type.GetField("isDead", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                       ?? type.GetField("IsDead", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                       ?? type.GetField("dead", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (isDeadField != null) { targetScript = script; return; }

            // [LỚP BẢO VỆ 2]: Tìm các biến máu phổ thông
            healthField = type.GetField("currentHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                       ?? type.GetField("health", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                       ?? type.GetField("hp", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (healthField != null) { targetScript = script; return; }
        }
    }

    void Update()
    {
        // Đã rớt đồ rồi thì thôi, không làm gì nữa
        if (hasDropped) return;

        bool isEnemyDead = false;

        // Nếu có Interface chuẩn -> Dùng luôn (Zero-GC, cực nhanh)
        if (enemyStatus != null)
        {
            isEnemyDead = enemyStatus.IsDead;
        }
        // Nếu không có, Fallback về trò soi lén Reflection của Tùng (Gây GC Boxing)
        else if (targetScript != null)
        {
            if (customField != null)
            {
                object val = customField.GetValue(targetScript);
                if (val is bool b) isEnemyDead = b;
                else if (val is int i) isEnemyDead = (i <= 0);
                else if (val is float f) isEnemyDead = (f <= 0);
            }
            else if (isDeadField != null)
            {
                isEnemyDead = (bool)isDeadField.GetValue(targetScript);
            }
            else if (healthField != null)
            {
                object hpValue = healthField.GetValue(targetScript);
                if (hpValue is int intHp) isEnemyDead = (intHp <= 0);
                else if (hpValue is float floatHp) isEnemyDead = (floatHp <= 0);
            }
        }
        else
        {
            // [LỚP BẢO VỆ 3]: Check xem Tag có bị đổi thành Untagged không
            bool tagChanged = gameObject.CompareTag("Untagged");

            // [LỚP BẢO VỆ 4]: Check xem Collider có bị tắt đi không
            bool colliderTurnedOff = (enemyCollider != null && !enemyCollider.enabled);

            isEnemyDead = tagChanged || colliderTurnedOff;
        }

        // Nếu 1 trong 4 lớp trên báo tử -> Thả đồ!
        if (isEnemyDead)
        {
            TriggerLootDrop();
        }
    }

    // 🔥 HÀM THẢ ĐỒ (Để Public để bác có thể dùng Animation Event gọi ép rớt đồ nếu thích)
    public void TriggerLootDrop()
    {
        if (hasDropped) return;
        hasDropped = true; // Chốt hạ là đã thả, tránh văng đồ x2 x3

        if (lootTable == null || lootTable.Count == 0) return;

        if (dropMultipleItems)
        {
            // Quay gacha cho từng món (Có thể rớt cả cục vàng + bình máu)
            foreach (LootItem loot in lootTable)
            {
                if (Random.Range(0f, 100f) <= loot.dropChance) SpawnItem(loot.itemPrefab);
            }
        }
        else
        {
            // Rớt tối đa 1 món duy nhất
            float roll = Random.Range(0f, 100f);
            float currentChance = 0f;
            foreach (LootItem loot in lootTable)
            {
                currentChance += loot.dropChance;
                if (roll <= currentChance)
                {
                    SpawnItem(loot.itemPrefab);
                    break;
                }
            }
        }
    }

    void SpawnItem(GameObject prefab)
    {
        if (prefab == null) return;
        // Cho đồ văng lệch ra một chút cho tự nhiên
        float randomX = Random.Range(-0.8f, 0.8f);
        float randomY = Random.Range(0f, 0.5f);
        Instantiate(prefab, transform.position + new Vector3(randomX, randomY, 0f), Quaternion.identity);
    }

    // Hàm báo hiệu Game đang tắt hoặc đang chuyển màn chơi
    void OnApplicationQuit() { isQuitting = true; }

    // [LỚP BẢO VỆ 5]: Quái bị Destroy đột ngột (Xóa xổ khỏi trần đời)
    void OnDestroy()
    {
        // Né trường hợp người chơi tắt game mà đồ vẫn rớt lả tả
        if (isQuitting || !gameObject.scene.isLoaded) return;

        // Quái bị xóa mà chưa kịp rớt đồ -> Ép rớt ngay tắp lự!
        TriggerLootDrop();
    }
}