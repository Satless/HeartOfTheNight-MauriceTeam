using UnityEngine;

/// <summary>
/// Component quản lý và tiếp nhận các hiệu ứng trạng thái (Cháy, Độc, Làm chậm...).
/// Gắn vào bất kỳ đối tượng nào (Player, Quái, Boss) có cài Interface NhanSatThuong.
/// </summary>
[RequireComponent(typeof(NhanSatThuong))]
public class StatusEffectReceiver : MonoBehaviour
{
    [System.Serializable]
    private struct ActiveStatus
    {
        public StatusEffectData data;
        public float durationLeft;
        public float tickTimer;
        public GameObject vfxInstance;
        public bool isActive;
    }

    [Header("Debug Tracking")]
    [Tooltip("Danh sách tối đa 4 hiệu ứng trạng thái đang bám trên người")]
    [SerializeField, ReadOnly] private ActiveStatus[] _activeStatuses = new ActiveStatus[4];
    private NhanSatThuong _healthComponent;

    private void Awake()
    {
        _healthComponent = GetComponent<NhanSatThuong>();
    }

    private void Update()
    {
        // Xử lý Status Effect (Zero-GC loop)
        for (int i = 0; i < _activeStatuses.Length; i++)
        {
            if (_activeStatuses[i].isActive)
            {
                _activeStatuses[i].durationLeft -= Time.deltaTime;
                _activeStatuses[i].tickTimer -= Time.deltaTime;

                if (_activeStatuses[i].tickTimer <= 0)
                {
                    _activeStatuses[i].tickTimer = _activeStatuses[i].data.tickInterval;
                    _healthComponent.TakeDamage(_activeStatuses[i].data.damagePerTick);
                }

                // Cập nhật vị trí bám theo tâm của Parent, không nhận tỷ lệ Scale của Parent để chống méo hình
                if (_activeStatuses[i].vfxInstance != null)
                {
                    _activeStatuses[i].vfxInstance.transform.position = transform.position;
                }

                if (_activeStatuses[i].durationLeft <= 0)
                {
                    _activeStatuses[i].isActive = false;
                    if (_activeStatuses[i].vfxInstance != null)
                    {
                        _activeStatuses[i].vfxInstance.Despawn();
                        _activeStatuses[i].vfxInstance = null;
                    }
                }
            }
        }
    }

    public void ApplyStatus(StatusEffectData statusData)
    {
        if (statusData == null) return;

        int emptySlot = -1;

        for (int i = 0; i < _activeStatuses.Length; i++)
        {
            if (_activeStatuses[i].isActive && _activeStatuses[i].data == statusData)
            {
                _activeStatuses[i].durationLeft = statusData.duration;
                return;
            }
            if (!_activeStatuses[i].isActive && emptySlot == -1)
            {
                emptySlot = i;
            }
        }

        if (emptySlot != -1)
        {
            _activeStatuses[emptySlot].data = statusData;
            _activeStatuses[emptySlot].durationLeft = statusData.duration;
            _activeStatuses[emptySlot].tickTimer = statusData.tickInterval;
            _activeStatuses[emptySlot].isActive = true;

            if (statusData.effectVfxPrefab != null)
            {
                // Lấy VFX từ Pool (thay vì Instantiate)
                _activeStatuses[emptySlot].vfxInstance = statusData.effectVfxPrefab.Spawn(transform.position, Quaternion.identity);
            }
        }
    }

    private void OnDisable()
    {
        // Trả VFX về Pool nếu object bị tắt/xóa
        for (int i = 0; i < _activeStatuses.Length; i++)
        {
            if (_activeStatuses[i].isActive && _activeStatuses[i].vfxInstance != null)
            {
                _activeStatuses[i].vfxInstance.Despawn();
                _activeStatuses[i].vfxInstance = null;
            }
            _activeStatuses[i].isActive = false;
        }
    }
}
