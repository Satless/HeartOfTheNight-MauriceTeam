using System.Collections;
using HeartOfTheNight.Enemy;
using UnityEngine;
using UnityEngine.UI;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// Thanh máu HUD của boss Heart Of The Night.
    /// Prefab có thể đứng độc lập trong scene (không cần là con của boss).
    /// </summary>
    public class HeartOfTheNightHealthBar : MonoBehaviour
    {
        [Header("Boss")]
        [SerializeField] private HeartOfTheNightBoss boss;

        [Header("UI")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private Image fillImage;

        [Header("Màu")]
        [SerializeField] private Color highHealthColor = Color.green;
        [SerializeField] private Color mediumHealthColor = Color.yellow;
        [SerializeField] private Color lowHealthColor = Color.red;
        [SerializeField] private Color hitFlashColor = Color.white;

        [Header("Chuyển động")]
        [SerializeField] private float catchupSpeed = 10f;

        private CanvasGroup canvasGroup;
        private Quaternion startRotation;
        private Vector3 originalCanvasScale;
        private float targetFill = 1f;
        private Color currentBaseColor;
        private bool isFlashing;
        private bool isDying;
        private int lastHealth = -1;

        private void Awake()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (fillImage == null)
            {
                var fill = transform.Find("BackgroundKhung/Fill");
                if (fill != null) fill.TryGetComponent(out fillImage);
            }

            if (canvas == null) return;

            startRotation = canvas.transform.rotation;
            originalCanvasScale = canvas.transform.localScale;
            if (originalCanvasScale.x <= 0.0001f || originalCanvasScale.y <= 0.0001f)
                originalCanvasScale = new Vector3(0.3f, 0.3f, 0.3f);

            canvas.overrideSorting = true;
            canvas.sortingLayerName = "UI";
            canvas.sortingOrder = 20;

            canvasGroup = canvas.GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = canvas.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            HeartOfTheNightBoss.OnBossReady += Bind;
            if (boss != null) Bind(boss);
            else TryFindBoss();
        }

        private void OnDisable()
        {
            HeartOfTheNightBoss.OnBossReady -= Bind;
            Unbind();
        }

        private void TryFindBoss()
        {
            Bind(FindFirstObjectByType<HeartOfTheNightBoss>());
        }

        private void Bind(HeartOfTheNightBoss target)
        {
            if (target == null || target.IsDead) return;
            if (boss == target && lastHealth >= 0 && !isDying) return;

            Unbind();
            StopAllCoroutines();
            boss = target;
            isDying = false;
            isFlashing = false;
            lastHealth = -1;
            boss.OnHealthChanged += HandleHealthChanged;
            HandleHealthChanged(boss.CurrentHealth, boss.MaxHealth);
        }

        private void Unbind()
        {
            if (boss == null) return;
            boss.OnHealthChanged -= HandleHealthChanged;
        }

        private void HandleHealthChanged(int currentHealth, int maxHealth)
        {
            if (maxHealth <= 0) return;

            float newFill = Mathf.Clamp01((float)currentHealth / maxHealth);
            bool firstSync = lastHealth < 0;
            bool tookHit = lastHealth >= 0 && currentHealth < lastHealth;
            lastHealth = currentHealth;

            if (firstSync && fillImage != null)
                fillImage.fillAmount = newFill;

            if (currentHealth <= 0)
            {
                targetFill = 0f;
                if (!isDying) StartCoroutine(DeathRoutine());
                return;
            }

            ShowBar();
            targetFill = newFill;

            if (tookHit && !isFlashing)
                StartCoroutine(HitFlashRoutine());
        }

        private void Update()
        {
            if (boss == null)
            {
                TryFindBoss();
                return;
            }

            if (fillImage == null || isDying) return;

            if (targetFill > 0.6f) currentBaseColor = highHealthColor;
            else if (targetFill > 0.3f) currentBaseColor = mediumHealthColor;
            else currentBaseColor = lowHealthColor;

            fillImage.fillAmount = Mathf.Lerp(
                fillImage.fillAmount, targetFill, Time.deltaTime * catchupSpeed);

            if (!isFlashing) fillImage.color = currentBaseColor;
        }

        private void LateUpdate()
        {
            if (canvas == null || canvasGroup == null || canvasGroup.alpha <= 0f) return;
            canvas.transform.rotation = startRotation;
        }

        private void ShowBar()
        {
            if (canvas == null || canvasGroup == null) return;
            canvas.transform.localScale = originalCanvasScale;
            canvasGroup.alpha = 1f;
        }

        private IEnumerator HitFlashRoutine()
        {
            isFlashing = true;
            if (fillImage != null) fillImage.color = hitFlashColor;
            yield return new WaitForSeconds(0.08f);
            isFlashing = false;
        }

        private IEnumerator DeathRoutine()
        {
            isDying = true;
            ShowBar();

            if (fillImage != null)
                fillImage.fillAmount = 0f;

            float timer = 0f;
            Vector3 startScale = canvas != null ? canvas.transform.localScale : originalCanvasScale;
            Vector3 flatScale = new Vector3(startScale.x, 0f, startScale.z);

            while (timer < 0.1f)
            {
                timer += Time.deltaTime;
                if (canvas != null)
                    canvas.transform.localScale = Vector3.Lerp(startScale, flatScale, timer / 0.1f);
                yield return null;
            }

            timer = 0f;
            Vector3 dotScale = new Vector3(0f, 0f, startScale.z);
            while (timer < 0.15f)
            {
                timer += Time.deltaTime;
                if (canvas != null)
                    canvas.transform.localScale = Vector3.Lerp(flatScale, dotScale, timer / 0.15f);
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }
    }
}
