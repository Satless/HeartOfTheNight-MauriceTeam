using UnityEngine;
using TMPro;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// DamagePopup - Hiển thị số sát thương kiểu Zero-GC.
    /// Tích hợp hệ thống Universal Object Pooling (không dùng Instantiate/Destroy).
    /// </summary>
    public enum DamageColorMode
    {
        Single,
        Random,
        GradientOverTime,
        RandomGradientOverTime
    }

    public enum DamageRotationMode
    {
        Fixed,
        Random
    }

    public enum DamageFontSizeMode
    {
        Fixed,
        Random
    }

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

        [Header("Màu sắc")]
        [Tooltip("Chế độ màu sắc: Đơn, Ngẫu nhiên, hay Đổi màu theo thời gian (Gradient)")]
        [SerializeField] private DamageColorMode colorMode = DamageColorMode.Single;
        
        [Tooltip("Màu đơn cố định (Nếu chọn Single)")]
        [SerializeField] private Color singleColor = Color.white;
        
        [Tooltip("Danh sách các màu để chọn ngẫu nhiên (Nếu chọn Random)")]
        [SerializeField] private Color[] randomColors;
        
        [Tooltip("Dải màu thay đổi theo tuổi thọ của số (Nếu chọn GradientOverTime)")]
        [SerializeField] private Gradient gradientColor;
        
        [Tooltip("Danh sách các dải màu ngẫu nhiên (Nếu chọn RandomGradientOverTime)")]
        [SerializeField] private Gradient[] randomGradients;
        
        [Header("Cỡ chữ (Font)")]
        [Tooltip("Chế độ cỡ chữ: Cố định hay Ngẫu nhiên")]
        [SerializeField] private DamageFontSizeMode fontSizeMode = DamageFontSizeMode.Fixed;
        
        [Tooltip("Cỡ chữ hiển thị (Nếu chọn Fixed)")]
        [SerializeField] private float fixedFontSize = 4f;
        
        [Tooltip("Cỡ chữ ngẫu nhiên trong khoảng [Min(X), Max(Y)] (Nếu chọn Random)")]
        [SerializeField] private Vector2 randomFontSizeRange = new Vector2(3f, 5f);

        [Header("Xoay (Rotation)")]
        [Tooltip("Chế độ xoay: Cố định góc Z hay Xoay nghiêng ngẫu nhiên")]
        [SerializeField] private DamageRotationMode rotationMode = DamageRotationMode.Fixed;
        
        [Tooltip("Góc xoay Z cố định (Nếu chọn Fixed)")]
        [SerializeField] private float fixedRotationZ = 0f;
        
        [Tooltip("Góc xoay nghiêng ngẫu nhiên trong khoảng [Min(X), Max(Y)] (Nếu chọn Random)")]
        [SerializeField] private Vector2 randomRotationRange = new Vector2(-15f, 15f);

        private TextMeshPro _tmp;
        private Transform _cachedTransform;

        private float _age;
        private Vector3 _velocity;
        private bool _isActive;
        private float _currentZRotation;
        private Gradient _currentGradient; // Lưu trữ gradient đang chạy của từng số

        private static Camera _mainCamera;
        private static Quaternion _cachedBillboardRotation;
        private static int _cachedFrame = -1;
        private static int _lastRotationSign = 1; // Biến nhớ dấu để đảo chiều qua lại

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
            
            if (fontSizeMode == DamageFontSizeMode.Fixed)
            {
                _tmp.fontSize = fixedFontSize;
            }
            else
            {
                _tmp.fontSize = Random.Range(randomFontSizeRange.x, randomFontSizeRange.y);
            }
            _cachedTransform.localScale = targetStartScale;

            // Thiết lập Rotation
            if (rotationMode == DamageRotationMode.Fixed)
            {
                _currentZRotation = fixedRotationZ;
            }
            else
            {
                // Ép lấy giá trị dương từ khoảng Min-Max
                float min = Mathf.Abs(randomRotationRange.x);
                float max = Mathf.Abs(randomRotationRange.y);
                float randomMag = Random.Range(Mathf.Min(min, max), Mathf.Max(min, max));
                
                // Đảo dấu luân phiên: Lần trước nghiêng trái thì lần này nghiêng phải
                _lastRotationSign = -_lastRotationSign; 
                _currentZRotation = randomMag * _lastRotationSign;
            }

            // Thiết lập Màu ban đầu
            switch (colorMode)
            {
                case DamageColorMode.Single:
                    _tmp.color = singleColor;
                    break;
                case DamageColorMode.Random:
                    if (randomColors != null && randomColors.Length > 0)
                        _tmp.color = randomColors[Random.Range(0, randomColors.Length)];
                    else
                        _tmp.color = Color.white;
                    break;
                case DamageColorMode.GradientOverTime:
                    _currentGradient = gradientColor;
                    if (_currentGradient != null)
                        _tmp.color = _currentGradient.Evaluate(0f);
                    break;
                case DamageColorMode.RandomGradientOverTime:
                    if (randomGradients != null && randomGradients.Length > 0)
                        _currentGradient = randomGradients[Random.Range(0, randomGradients.Length)];
                    else
                        _currentGradient = gradientColor; // Fallback
                        
                    if (_currentGradient != null)
                        _tmp.color = _currentGradient.Evaluate(0f);
                    break;
            }
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
            UpdateFade(t); // Tính toán alpha của hệ thống trước (dựa trên Fade Start)

            // Cập nhật Gradient theo thời gian
            if ((colorMode == DamageColorMode.GradientOverTime || colorMode == DamageColorMode.RandomGradientOverTime) && _currentGradient != null)
            {
                // Nhân alpha của Gradient với alpha của hệ thống Fade để kết hợp cả 2
                Color newColor = _currentGradient.Evaluate(t);
                newColor.a *= _tmp.alpha; 
                _tmp.color = newColor;
            }

            _cachedTransform.position += _velocity * dt;
            _velocity *= velocityDamping;

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
            // Áp dụng Billboard gốc Camera và cộng thêm góc xoay nghiêng cục bộ (Z Rotation)
            _cachedTransform.rotation = _cachedBillboardRotation * Quaternion.Euler(0f, 0f, _currentZRotation);
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
