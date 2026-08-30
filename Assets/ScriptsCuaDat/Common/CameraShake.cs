using UnityEngine;

namespace HeartOfTheNight.Common
{
    /// <summary>
    /// Rung camera sau khi Cinemachine ghi vị trí (tránh bị Brain ghi đè).
    /// </summary>
    [DefaultExecutionOrder(10000)]
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [SerializeField] private float defaultAmplitude = 0.4f;
        [SerializeField] private float defaultDuration = 1f;
        [SerializeField] private float defaultFrequency = 26f;

        private float amplitude;
        private float duration;
        private float frequency;
        private float elapsed;
        private Transform cam;

        public static void Shake(float amplitude = -1f, float duration = -1f, float frequency = -1f)
        {
            EnsureExists();
            Instance.Play(amplitude, duration, frequency);
        }

        private static void EnsureExists()
        {
            if (Instance != null) return;

            var go = new GameObject(nameof(CameraShake));
            DontDestroyOnLoad(go);
            go.AddComponent<CameraShake>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Play(float amp, float dur, float freq)
        {
            amplitude = amp > 0f ? amp : defaultAmplitude;
            duration = dur > 0f ? dur : defaultDuration;
            frequency = freq > 0f ? freq : defaultFrequency;
            elapsed = 0f;
            CacheCamera();
        }

        private void CacheCamera()
        {
            cam = Camera.main != null ? Camera.main.transform : null;
        }

        private void LateUpdate()
        {
            if (elapsed >= duration || amplitude <= 0f) return;

            if (cam == null) CacheCamera();
            if (cam == null) return;

            elapsed += Time.deltaTime;
            float falloff = 1f - Mathf.Clamp01(elapsed / duration);
            falloff *= falloff;

            float mag = amplitude * falloff;
            float t = Time.time * frequency;
            float x = (Mathf.PerlinNoise(t, 0.17f) * 2f - 1f) * mag;
            float y = (Mathf.PerlinNoise(0.63f, t) * 2f - 1f) * mag;
            cam.position += new Vector3(x, y, 0f);
        }
    }
}
