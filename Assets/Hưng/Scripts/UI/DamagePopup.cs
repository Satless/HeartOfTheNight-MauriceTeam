using UnityEngine;
using TMPro;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// DamagePopup - Hiển thị số sát thương kiểu Zero-GC.
    /// Tích hợp hệ thống Universal Object Pooling (không dùng Instantiate/Destroy).
    /// </summary>
    [RequireComponent(typeof(TextMeshPro))]
    public class DamagePopup : MonoBehaviour
    {
        [Header("Thời gian & Chuyển động")]
        [Tooltip("Tổng thời gian tồn tại của số sát thương (giây) trước khi biến mất")]
        [SerializeField] private float lifeTime;
        
        [Tooltip("Bật nếu muốn số sát thương vẫn bay bình thường khi game bị Pause (Time.timeScale = 0)")]
        [SerializeField] private bool useUnscaledTime;
        
        [Tooltip("Bán kính nảy ngẫu nhiên quanh tâm (để các số không đè chặt lên nhau)")]
        [SerializeField] private float spawnJitterRadius;
        
        [Tooltip("Tốc độ bay thẳng lên trời ban đầu")]
        [SerializeField] private float moveUpSpeed;
        
        [Tooltip("Lực nảy ngẫu nhiên sang hai bên trái/phải lúc mới xuất hiện")]
        [SerializeField] private float moveSidewaysRandom;
        
        [Tooltip("Hệ số cản (Ma sát). Càng nhỏ số dừng lại càng nhanh (0.96 = giảm 4% vận tốc mỗi frame)")]
        [SerializeField] private float velocityDamping;

        [Header("Scale (tính theo % lifetime)")]
        [Tooltip("Kích thước lúc vừa sinh ra (nên = 0 để có hiệu ứng phình to mượt, chống chớp giật)")]
        [SerializeField] private float startScale;
        
        [Tooltip("Kích thước bự nhất lúc bị đánh trúng (Tạo cảm giác lực đập mạnh)")]
        [SerializeField] private float punchScale;
        
        [Tooltip("Kích thước ổn định sau cú nảy (Thường là 1)")]
        [SerializeField] private float normalScale;
        
        [Tooltip("Thời gian phình to từ Start -> Punch (Đơn vị: 0.15 = 15% của LifeTime)")]
        [SerializeField] private float punchDurationPercent;
        
        [Tooltip("Thời gian thu nhỏ từ Punch -> Normal (Đơn vị: 0.1 = 10% của LifeTime)")]
        [SerializeField] private float settleDurationPercent;

        [Header("Fade")]
        [Tooltip("Thời điểm bắt đầu mờ dần (0.6 = giữ rõ nét 60% thời gian đầu, 40% cuối mờ dần đi)")]
        [SerializeField] private float fadeStartPercent;

        [Header("Màu & Cỡ chữ")]
        [Tooltip("Màu chữ mặc định (Sát thương bình thường)")]
        [SerializeField] private Color normalColor = Color.white;
        
        [Tooltip("Cỡ chữ hiển thị mặc định")]
        [SerializeField] private float normalFontSize;

        private TextMeshPro _tmp;
        private Transform _cachedTransform;

        private float _age;
        private Vector3 _velocity;
        private bool _isActive;

        private static Camera _mainCamera;
        private static Quaternion _cachedBillboardRotation;
        private static int _cachedFrame = -1;

        // --- Liên kết Universal Pooling ---
        private static GameObject _prefab;

        public static void SetupPrefab(GameObject prefab)
        {
            _prefab = prefab;
            // Tự động làm nóng kho (Prewarm) khoảng 20 chữ để tránh giật lag lúc mới bắn
            if (_prefab != null)
            {
                // Gọi API của Universal Pool (nếu có hàm Prewarm riêng, hoặc tạm thời bỏ qua)
                // Hiện tại Universal Pool tự cấp phát khi thiếu.
            }
        }

        public static void Create(Vector3 worldPosition, float damageAmount)
        {
            if (_prefab == null)
            {
                Debug.LogWarning("[DamagePopup] Chưa gán Prefab. Hãy gán vào HUDManager!");
                return;
            }

            // Dùng Universal Pooling .Spawn() thay vì DamagePopupPool.Get()
            GameObject go = _prefab.Spawn(worldPosition);
            if (go == null) return;

            DamagePopup popup = go.GetComponent<DamagePopup>();
            if (popup != null)
            {
                popup.Initialize(worldPosition, damageAmount);
            }
        }

        private void Awake()
        {
            _tmp = GetComponent<TextMeshPro>();
            _cachedTransform = transform;
        }

        private void Initialize(Vector3 worldPosition, float damageAmount)
        {
            // Cộng thêm một chút nhiễu loạn ngẫu nhiên để các số không đè khít lên nhau
            Vector2 randomOffset = Random.insideUnitCircle * spawnJitterRadius;
            _cachedTransform.position = worldPosition + (Vector3)randomOffset;

            _cachedTransform.localScale = Vector3.one * startScale;

            Vector3 targetStartScale = _cachedTransform.localScale;
            _cachedTransform.localScale = Vector3.one;
            _tmp.text = FormatDamageText(damageAmount);
            _tmp.fontSize = normalFontSize;
            _cachedTransform.localScale = targetStartScale;

            _tmp.color = normalColor;
            _tmp.alpha = 1f;

            float sideways = Random.Range(-moveSidewaysRandom, moveSidewaysRandom);
            _velocity = new Vector3(sideways, moveUpSpeed, 0f);

            _age = 0f;
            _isActive = true;
            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isActive) return;

            float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            _age += dt;

            float t = _age / lifeTime;
            if (t >= 1f)
            {
                Despawn();
                return;
            }

            UpdateScale(t);

            _cachedTransform.position += _velocity * dt;
            _velocity *= velocityDamping;

            UpdateFade(t);
            UpdateBillboard();
        }

        private void UpdateScale(float t)
        {
            float scale;
            if (t < punchDurationPercent)
            {
                scale = Mathf.Lerp(startScale, punchScale, t / punchDurationPercent);
            }
            else if (t < punchDurationPercent + settleDurationPercent)
            {
                float settleT = (t - punchDurationPercent) / settleDurationPercent;
                scale = Mathf.Lerp(punchScale, normalScale, settleT);
            }
            else
            {
                scale = normalScale;
            }
            _cachedTransform.localScale = Vector3.one * scale;
        }

        private void UpdateFade(float t)
        {
            if (t < fadeStartPercent)
            {
                if (_tmp.alpha != 1f) _tmp.alpha = 1f;
                return;
            }

            float fadeT = Mathf.InverseLerp(fadeStartPercent, 1f, t);
            _tmp.alpha = 1f - fadeT;
        }

        private void UpdateBillboard()
        {
            int frame = Time.frameCount;
            if (frame != _cachedFrame)
            {
                if (_mainCamera == null) _mainCamera = Camera.main;
                if (_mainCamera != null)
                {
                    _cachedBillboardRotation = _mainCamera.transform.rotation;
                    _cachedFrame = frame;
                }
            }
            _cachedTransform.rotation = _cachedBillboardRotation;
        }

        private void Despawn()
        {
            _isActive = false;
            // Trả về Universal Pool
            gameObject.Despawn();
        }

        private static string FormatDamageText(float damageAmount)
        {
            if (Mathf.Approximately(damageAmount, Mathf.Round(damageAmount)))
                return ((int)damageAmount).ToString(System.Globalization.CultureInfo.InvariantCulture);

            return damageAmount.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
