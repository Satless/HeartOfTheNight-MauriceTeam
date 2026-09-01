using Unity.Cinemachine;
using UnityEngine;

namespace HeartOfTheNight.Common
{
    /// <summary>
    /// Rung camera sau khi Cinemachine Brain ghi pose (CameraUpdatedEvent).
    /// Không dùng Camera.main — scene có thể có thêm camera tag MainCamera (test).
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
        private float holdFraction;
        private float elapsed;
        private Vector3 offset;
        private int offsetFrame = -1;

        public static void Shake(float amplitude = -1f, float duration = -1f, float frequency = -1f)
        {
            EnsureExists();
            Instance.Play(amplitude, duration, frequency, 0f);
        }

        /// <summary>
        /// Rung giữ cường độ đến holdFraction (0–1), rồi mới fade. Dùng cho death cinematic dài.
        /// </summary>
        public static void ShakeHeld(float amplitude, float duration, float holdFraction = 0.85f, float frequency = -1f)
        {
            EnsureExists();
            Instance.Play(amplitude, duration, frequency, holdFraction);
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

        private void OnEnable()
        {
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCinemachineCameraUpdated);
        }

        private void OnDisable()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCinemachineCameraUpdated);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Play(float amp, float dur, float freq, float hold)
        {
            amplitude = amp > 0f ? amp : defaultAmplitude;
            duration = dur > 0f ? dur : defaultDuration;
            frequency = freq > 0f ? freq : defaultFrequency;
            holdFraction = Mathf.Clamp01(hold);
            elapsed = 0f;
            offsetFrame = -1;
        }

        private void OnCinemachineCameraUpdated(CinemachineBrain brain)
        {
            if (!IsShaking || brain == null) return;

            RefreshOffsetIfNeeded();

            Camera output = brain.OutputCamera;
            if (output != null)
                output.transform.position += offset;
        }

        private void LateUpdate()
        {
            if (!IsShaking) return;

            // Scene không có Cinemachine Brain — fallback.
            if (CinemachineBrain.ActiveBrainCount > 0) return;

            RefreshOffsetIfNeeded();
            Transform t = ResolveFallbackCamera();
            if (t != null)
                t.position += offset;
        }

        private bool IsShaking => elapsed < duration && amplitude > 0f && duration > 0f;

        private void RefreshOffsetIfNeeded()
        {
            if (offsetFrame == Time.frameCount) return;

            offsetFrame = Time.frameCount;
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float falloff;
            if (holdFraction <= 0f)
            {
                falloff = 1f - t;
                falloff *= falloff;
            }
            else if (t < holdFraction)
            {
                falloff = 1f;
            }
            else
            {
                float fade = 1f - (t - holdFraction) / Mathf.Max(0.0001f, 1f - holdFraction);
                falloff = fade * fade;
            }

            float mag = amplitude * falloff;
            float noiseT = Time.time * frequency;
            offset = new Vector3(
                (Mathf.PerlinNoise(noiseT, 0.17f) * 2f - 1f) * mag,
                (Mathf.PerlinNoise(0.63f, noiseT) * 2f - 1f) * mag,
                0f);
        }

        private static Transform ResolveFallbackCamera()
        {
            if (Camera.main != null) return Camera.main.transform;
            return null;
        }
    }
}
