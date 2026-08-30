using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeartOfTheNight.Rooms
{
    /// <summary>
    /// Đếm chìa Blue/Red trên RAM. Chỉ ghi file khi qua cửa checkpoint.
    /// Track từng KeyPickup bằng pickupId khi map có nhiều chìa cùng màu.
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

        public static bool IsPickupCollected(string pickupId)
        {
            if (string.IsNullOrEmpty(pickupId)) return false;
            var data = GetData();
            return data?.collectedKeyPickupIds != null && data.collectedKeyPickupIds.Contains(pickupId);
        }

        public static void MarkPickupCollected(string pickupId)
        {
            if (string.IsNullOrEmpty(pickupId)) return;

            var data = GetData();
            if (data == null) return;

            if (data.collectedKeyPickupIds == null)
                data.collectedKeyPickupIds = new List<string>();

            if (data.collectedKeyPickupIds.Contains(pickupId)) return;

            data.collectedKeyPickupIds.Add(pickupId);
            OnKeysChanged?.Invoke();
        }

        public static void Add(KeyType type, int amount = 1)
        {
            Add(type, null, amount);
        }

        public static void Add(KeyType type, string pickupId, int amount = 1)
        {
            if (type == KeyType.None || amount <= 0) return;

            var data = GetData();
            if (data == null)
            {
                Debug.LogWarning("[PlayerKeyInventory] DataManager chua san sang, khong the nhat chia.");
                return;
            }

            // Đã nhặt đúng pickup này rồi → không cộng trùng.
            if (!string.IsNullOrEmpty(pickupId) && IsPickupCollected(pickupId))
            {
                Debug.Log($"[PlayerKeyInventory] Pickup '{pickupId}' da nhat roi, bo qua.");
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

            if (!string.IsNullOrEmpty(pickupId))
            {
                if (data.collectedKeyPickupIds == null)
                    data.collectedKeyPickupIds = new List<string>();
                if (!data.collectedKeyPickupIds.Contains(pickupId))
                    data.collectedKeyPickupIds.Add(pickupId);
            }

            OnKeysChanged?.Invoke();
            Debug.Log($"[PlayerKeyInventory] +{amount} {type} (con {GetCount(type)})" +
                      (string.IsNullOrEmpty(pickupId) ? "" : $" pickupId={pickupId}"));
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
                data.unlockedDoors = new List<string>();

            if (!data.unlockedDoors.Contains(doorId))
                data.unlockedDoors.Add(doorId);

            OnKeysChanged?.Invoke();
        }

        /// <summary>
        /// Sang scene mới: tui chìa về 0. Chìa chỉ tồn tại trong scene nhặt.
        /// Pickup/cửa đã xử lý giữ theo ID (có prefix scene) — không đụng scene khác.
        /// </summary>
        public static void ClearKeyCountsForNewScene()
        {
            var data = GetData();
            if (data == null) return;

            data.blueKeys = 0;
            data.redKeys = 0;
            data.collectedBlueKey = false;
            data.collectedRedKey = false;
            OnKeysChanged?.Invoke();
            Debug.Log("[PlayerKeyInventory] Sang scene moi: tui chia ve 0.");
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
            if (data.collectedKeyPickupIds == null)
                data.collectedKeyPickupIds = new List<string>();
            else
                data.collectedKeyPickupIds.Clear();

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
