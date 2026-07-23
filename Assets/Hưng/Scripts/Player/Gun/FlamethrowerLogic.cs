using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý logic vùng sát thương của Súng phun lửa.
/// Gắn script này vào Prefab chứa Particle System phun lửa.
/// Yêu cầu Prefab phải có BoxCollider2D (Is Trigger = true).
/// </summary>
public class FlamethrowerLogic : MonoBehaviour
{
    private StatusEffectData _statusEffect;
    
    // Danh sách lưu quái đang đứng trong lửa để tránh gọi GetComponent liên tục (Zero-GC)
    private List<StatusEffectReceiver> _victimsInFire = new List<StatusEffectReceiver>();

    /// <summary>
    /// PlayerAttack gọi hàm này khi bắt đầu bắn súng lửa để truyền Data vào
    /// </summary>
    public void Activate(StatusEffectData effectData)
    {
        _statusEffect = effectData;
    }

    private void OnDisable()
    {
        // Khi người chơi nhả nút bắn (Tắt lửa), dọn dẹp danh sách
        _victimsInFire.Clear();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        StatusEffectReceiver victim = other.GetComponent<StatusEffectReceiver>();
        if (victim != null && !_victimsInFire.Contains(victim))
        {
            _victimsInFire.Add(victim);
            // Áp dụng ngay hiệu ứng cháy khi vừa chạm lửa
            if (_statusEffect != null)
            {
                victim.ApplyStatus(_statusEffect);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        StatusEffectReceiver victim = other.GetComponent<StatusEffectReceiver>();
        if (victim != null)
        {
            _victimsInFire.Remove(victim);
        }
    }

    private void Update()
    {
        if (_statusEffect == null || _victimsInFire.Count == 0) return;

        // Liên tục refresh thời gian cháy cho những con quái đang đứng TẬN TRONG luồng lửa
        for (int i = _victimsInFire.Count - 1; i >= 0; i--)
        {
            // Kiểm tra null an toàn cho Interface (vì Interface không overload == null như MonoBehaviour)
            if (_victimsInFire[i] == null || _victimsInFire[i] as Object == null)
            {
                _victimsInFire.RemoveAt(i);
            }
            else
            {
                _victimsInFire[i].ApplyStatus(_statusEffect);
            }
        }
    }
}
