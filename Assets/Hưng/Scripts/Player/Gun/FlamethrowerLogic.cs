using HeartOfTheNight.Common;
using UnityEngine;

/// <summary>
/// Quản lý logic vùng sát thương của Súng phun lửa bằng Physics2D.OverlapBoxNonAlloc.
/// KHÔNG CẦN Collider trên Prefab.
/// </summary>
public class FlamethrowerLogic : MonoBehaviour
{
    [Header("Hitbox")]
    [Tooltip("Kích thước vùng lửa")]
    public Vector2 hitboxSize = new Vector2(5f, 2f);
    [Tooltip("Khoảng cách từ nòng súng đến tâm vùng lửa")]
    public Vector2 hitboxOffset = new Vector2(2.5f, 0f);
    [Tooltip("Layer mục tiêu")]
    public LayerMask targetLayer = ~0;

    [Header("Tag Filter")]
    [Tooltip("Danh sách tag được phép nhận sát thương lửa")]
    [TagSelector]
    [SerializeField] private string[] _targetTags;

    [Header("Debug Tracking")]
    [Tooltip("Hiệu ứng trạng thái (Cháy, Độc...) đang được gán cho luồng lửa này")]
    [SerializeField, ReadOnly] private StatusEffectData _statusEffect;

    private const string LoopCategory = "Weapons";
    private const string LoopSubCategory = "Flamethrower";
    private const string LoopAction = "Shoot";

    private AudioSource _loopSource;
    private int _directDamage;
    private float _directTickInterval = 0.2f;
    private float _directTickTimer;

    // Pre-allocated cho OverlapBoxNonAlloc (Zero-GC, giống PlayerMagnet / Bullet)
    private static readonly Collider2D[] _overlapBuffer = new Collider2D[20];
    private static readonly int[] _directHitIds = new int[20];

    public void Activate(StatusEffectData effectData, int directDamage = 0, float tickInterval = 0.2f)
    {
        _statusEffect = effectData;
        _directDamage = Mathf.Max(0, directDamage);
        _directTickInterval = Mathf.Max(0.05f, tickInterval);
        EnsureLoopSource();
        if (_loopSource != null && _loopSource.clip != null && !_loopSource.isPlaying)
            _loopSource.Play();
    }

    private void OnEnable()
    {
        EnsureLoopSource();
        _directTickTimer = 0f;
    }

    private void OnDisable()
    {
        if (_loopSource != null)
            _loopSource.Stop();
    }

    private void EnsureLoopSource()
    {
        if (_loopSource == null)
        {
            _loopSource = GetComponent<AudioSource>();
            if (_loopSource == null)
                _loopSource = gameObject.AddComponent<AudioSource>();

            _loopSource.playOnAwake = false;
            _loopSource.loop = true;
            _loopSource.spatialBlend = 0f;
            _loopSource.ignoreListenerPause = false;
        }

        var mgr = SoundManager_New.Instance;
        if (mgr == null)
            return;

        if (_loopSource.clip == null)
            _loopSource.clip = mgr.GetSfxClip(LoopCategory, LoopSubCategory, LoopAction);

        if (mgr.SfxMixerGroup != null)
            _loopSource.outputAudioMixerGroup = mgr.SfxMixerGroup;
    }

    private void Update()
    {
        if (_statusEffect == null && _directDamage <= 0) return;

        Vector2 centerPos = (Vector2)transform.position + (Vector2)(transform.right * hitboxOffset.x) + (Vector2)(transform.up * hitboxOffset.y);
        int count = Physics2D.OverlapBoxNonAlloc(centerPos, hitboxSize, transform.eulerAngles.z, _overlapBuffer, targetLayer);

        if (_statusEffect != null)
            ApplyBurn(count);

        if (_directDamage > 0)
        {
            _directTickTimer -= Time.deltaTime;
            if (_directTickTimer <= 0f)
            {
                _directTickTimer = _directTickInterval;
                ApplyDirectDamage(count);
            }
        }
    }

    private void ApplyBurn(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Collider2D col = _overlapBuffer[i];
            if (!HasTargetTag(col) || IsPlayer(col)) continue;

            StatusEffectReceiver receiver = col.GetComponent<StatusEffectReceiver>();
            if (receiver == null)
                receiver = col.GetComponentInParent<StatusEffectReceiver>();
            if (receiver != null)
                receiver.ApplyStatus(_statusEffect);
        }
    }

    private void ApplyDirectDamage(int count)
    {
        int hitCount = 0;
        for (int i = 0; i < count; i++)
        {
            Collider2D col = _overlapBuffer[i];
            if (!HasTargetTag(col) || IsPlayer(col)) continue;

            IDamageable target = col.GetComponent<IDamageable>();
            if (target == null)
                target = col.GetComponentInParent<IDamageable>();
            if (target == null) continue;

            Component host = target as Component;
            int id = host != null ? host.GetInstanceID() : 0;
            if (id != 0 && AlreadyHit(id, hitCount)) continue;
            if (id != 0 && hitCount < _directHitIds.Length)
                _directHitIds[hitCount++] = id;

            target.TakeDamage(_directDamage);
            HeartOfTheNight.UI.DamagePopup.Create(col.bounds.center, _directDamage);
        }
    }

    private static bool AlreadyHit(int id, int hitCount)
    {
        for (int i = 0; i < hitCount; i++)
        {
            if (_directHitIds[i] == id) return true;
        }
        return false;
    }

    private static bool IsPlayer(Collider2D col)
    {
        return col.CompareTag("Player") || col.transform.root.CompareTag("Player");
    }

    /// <summary>
    /// Kiểm tra xem collider có nằm trong danh sách tag cho phép không.
    /// Nếu _targetTags rỗng (chưa setup) → cho phép tất cả.
    /// </summary>
    private bool HasTargetTag(Collider2D col)
    {
        if (_targetTags == null || _targetTags.Length == 0) return true;

        for (int i = 0; i < _targetTags.Length; i++)
        {
            if (col.CompareTag(_targetTags[i])) return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Vector2 centerPos = (Vector2)transform.position + (Vector2)(transform.right * hitboxOffset.x) + (Vector2)(transform.up * hitboxOffset.y);

        Gizmos.matrix = Matrix4x4.TRS(centerPos, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, hitboxSize);
    }
}
