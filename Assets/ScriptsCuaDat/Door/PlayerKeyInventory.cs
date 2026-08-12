using System;
using UnityEngine;

namespace HeartOfTheNight.Rooms
{
    /// <summary>
    /// Đếm chìa Blue/Red qua DataManager của Hưng (lưu local + cloud).
    /// </summary>
    public static class PlayerKeyInventory
    {
        public static event Action OnKeysChanged;

        public static int GetCount(KeyType type)
        {
            var data = GetData();
            if (data == null) return 0;

            return type switch
            {
                KeyType.Blue => data.blueKeys,
                KeyType.Red => data.redKeys,
                _ => 0
            };
        }

        public static bool Has(KeyType type) => type != KeyType.None && GetCount(type) > 0;

        /// <summary>Đã từng nhặt chìa màu này chưa (dùng cho HUD).</summary>
        public static bool HasCollected(KeyType type)
        {
            var data = GetData();
            if (data == null) return false;

            return type switch
            {
                KeyType.Blue => data.collectedBlueKey,
                KeyType.Red => data.collectedRedKey,
                _ => false
            };
        }

        public static void Add(KeyType type, int amount = 1)
        {
            if (type == KeyType.None || amount <= 0) return;

            var data = GetData();
            if (data == null)
            {
                Debug.LogWarning("[PlayerKeyInventory] DataManager chua san sang, khong the nhat chia.");
                return;
            }

            switch (type)
            {
                case KeyType.Blue:
                    data.blueKeys += amount;
                    data.collectedBlueKey = true;
                    break;
                case KeyType.Red:
                    data.redKeys += amount;
                    data.collectedRedKey = true;
                    break;
            }

            HeartOfTheNight.Hung.DataManager.Instance.SaveGame();
            OnKeysChanged?.Invoke();
            Debug.Log($"[PlayerKeyInventory] +{amount} {type} (con {GetCount(type)})");
        }

        public static bool TryConsume(KeyType type, int amount = 1)
        {
            if (type == KeyType.None || amount <= 0) return true;

            var data = GetData();
            if (data == null) return false;

            switch (type)
            {
                case KeyType.Blue:
                    if (data.blueKeys < amount) return false;
                    data.blueKeys -= amount;
                    break;
                case KeyType.Red:
                    if (data.redKeys < amount) return false;
                    data.redKeys -= amount;
                    break;
                default:
                    return false;
            }

            HeartOfTheNight.Hung.DataManager.Instance.SaveGame();
            OnKeysChanged?.Invoke();
            Debug.Log($"[PlayerKeyInventory] -{amount} {type} (con {GetCount(type)})");
            return true;
        }

        public static bool IsDoorUnlocked(string doorId)
        {
            if (string.IsNullOrEmpty(doorId)) return false;
            var data = GetData();
            return data != null && data.unlockedDoors != null && data.unlockedDoors.Contains(doorId);
        }

        public static void MarkDoorUnlocked(string doorId)
        {
            if (string.IsNullOrEmpty(doorId)) return;

            var data = GetData();
            if (data == null) return;

            if (data.unlockedDoors == null)
                data.unlockedDoors = new System.Collections.Generic.List<string>();

            if (!data.unlockedDoors.Contains(doorId))
                data.unlockedDoors.Add(doorId);

            HeartOfTheNight.Hung.DataManager.Instance.SaveGame();
            OnKeysChanged?.Invoke();
        }

        /// <summary>Goi khi load save xong de HUD cap nhat.</summary>
        public static void NotifyChanged() => OnKeysChanged?.Invoke();

        /// <summary>Xoa het chia (test / choi lai). Giu unlockedDoors neu can.</summary>
        public static void ClearAllKeys()
        {
            var data = GetData();
            if (data == null) return;

            data.blueKeys = 0;
            data.redKeys = 0;
            data.collectedBlueKey = false;
            data.collectedRedKey = false;
            HeartOfTheNight.Hung.DataManager.Instance.SaveGame();
            OnKeysChanged?.Invoke();
            Debug.Log("[PlayerKeyInventory] Da xoa het chia.");
        }

        private static HeartOfTheNight.Hung.GameData GetData()
        {
            return HeartOfTheNight.Hung.DataManager.Instance != null
                ? HeartOfTheNight.Hung.DataManager.Instance.Data
                : null;
        }
    }
}
