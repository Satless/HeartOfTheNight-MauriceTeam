using System.Collections;
using HeartOfTheNight.Enemy;
using HeartOfTheNight.Player;
using UnityEngine;
using UnityEngine.UI;

namespace HeartOfTheNight.UI
{
    /// <summary>
    /// Thanh máu boss cuối kiểu HUD: dính đỉnh màn hình, tự hiện khi boss vào trận.
    /// Prefab có thể nằm trong boss; runtime sẽ tách ra Canvas Overlay.
    /// </summary>
    public class HeartOfTheNightHealthBar : MonoBehaviour
    {
        [Header("Boss")]
        [SerializeField] private HeartOfTheNightBoss boss;

        [Header("UI")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform barFrame;
        [SerializeField] private Image fillImage;

        [Header("Layout (1920x1080)")]
        [SerializeField] private Vector2 barSize = new(720f, 32f);
        [SerializeField] private float topOffset = 110f;
        [SerializeField] private float horizontalOffset = 80f;
        [SerializeField] private int sortingOrder = 40;

        [Header("Màu")]
        [SerializeField] private Color highHealthColor = new(0.82f, 0.14f, 0.16f, 1f);
        [SerializeField] private Color mediumHealthColor = new(0.92f, 0.48f, 0.12f, 1f);
        [SerializeField] private Color lowHealthColor = new(0.55f, 0.05f, 0.08f, 1f);
        [SerializeField] private Color hitFlashColor = Color.white;

        [Header("Chuyển động")]
        [SerializeField] private float catchupSpeed = 8f;
        [SerializeField] private float fadeInDuration = 0.35f;

        private CanvasGroup canvasGroup;
        private Vector3 barBaseScale = Vector3.one;
        private float targetFill = 1f;
        private Color currentBaseColor;
        private bool isFlashing;
        private bool isDying;
        private bool isShowing;
        private bool hideForPlayerDeath;
        private int lastHealth = -1;
        private PlayerHealth playerHealth;

        private void Awake()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            if (barFrame == null)
            {
                var khung = transform.Find("BackgroundKhung");
                if (khung != null) barFrame = khung as RectTransform;
            }
            if (fillImage == null && barFrame != null)
            {
                var fill = barFrame.Find("Fill");
                if (fill != null) fill.TryGetComponent(out fillImage);
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;

            ConfigureAsScreenHud();
        }

        private void ConfigureAsScreenHud()
        {
            if (transform.parent != null)
                transform.SetParent(null, false);

            var root = transform as RectTransform;
            if (root != null)
            {
                root.anchorMin = Vector2.zero;
                root.anchorMax = Vector2.one;
                root.pivot = new Vector2(0.5f, 0.5f);
                root.offsetMin = Vector2.zero;
                root.offsetMax = Vector2.zero;
                root.localScale = Vector3.one;
                root.localRotation = Quaternion.identity;
            }

            if (canvas != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.overrideSorting = true;
                canvas.sortingOrder = sortingOrder;
            }

            var scaler = GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
                scaler.referencePixelsPerUnit = 100f;
            }

            if (barFrame != null)
            {
                barFrame.anchorMin = new Vector2(0.5f, 1f);
                barFrame.anchorMax = new Vector2(0.5f, 1f);
                barFrame.pivot = new Vector2(0.5f, 1f);
                barFrame.anchoredPosition = new Vector2(horizontalOffset, -topOffset);
                barFrame.sizeDelta = barSize;
                barFrame.localScale = Vector3.one;
                barFrame.localRotation = Quaternion.identity;
                barBaseScale = Vector3.one;
            }
        }

        private void OnEnable()
        {
            HeartOfTheNightBoss.OnBossReady += Bind;
            SubscribePlayerDeath();
            if (hideForPlayerDeath) return;
            if (boss != null) Bind(boss);
            else TryFindBoss();
        }

        private void OnDisable()
        {
            HeartOfTheNightBoss.OnBossReady -= Bind;
            UnsubscribePlayerDeath();
            Unbind();
        }

        private void SubscribePlayerDeath()
        {
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (playerHealth == null) return;

            playerHealth.OnDeath -= HideBecausePlayerDied;
            playerHealth.OnDeath += HideBecausePlayerDied;
            if (playerHealth.IsDead)
                HideBecausePlayerDied();
        }

        private void UnsubscribePlayerDeath()
        {
            if (playerHealth == null) return;
            playerHealth.OnDeath -= HideBecausePlayerDied;
        }

        private void HideBecausePlayerDied()
        {
            if (hideForPlayerDeath) return;
            hideForPlayerDeath = true;

            StopAllCoroutines();
            isShowing = false;
            isDying = false;

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (canvas != null) canvas.enabled = false;
        }

        private void TryFindBoss()
        {
            Bind(FindFirstObjectByType<HeartOfTheNightBoss>());
        }

        private void Bind(HeartOfTheNightBoss target)
        {
            if (hideForPlayerDeath) return;
            if (target == null || target.IsDead) return;
            if (boss == target && lastHealth >= 0 && !isDying) return;

            Unbind();
            StopAllCoroutines();
            boss = target;
            isDying = false;
            isFlashing = false;
            isShowing = false;
            lastHealth = -1;
            if (barFrame != null) barFrame.localScale = barBaseScale;
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
            if (hideForPlayerDeath || maxHealth <= 0) return;

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
            if (playerHealth == null)
                SubscribePlayerDeath();

            if (hideForPlayerDeath) return;

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

        private void ShowBar()
        {
            if (hideForPlayerDeath || canvasGroup == null || isShowing) return;
            isShowing = true;
            if (barFrame != null) barFrame.localScale = barBaseScale;
            StartCoroutine(FadeInRoutine());
        }

        private IEnumerator FadeInRoutine()
        {
            float duration = Mathf.Max(0.05f, fadeInDuration);
            float timer = 0f;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                if (canvasGroup != null)
                    canvasGroup.alpha = Mathf.Clamp01(timer / duration);
                yield return null;
            }
            if (canvasGroup != null) canvasGroup.alpha = 1f;
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
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            if (fillImage != null) fillImage.fillAmount = 0f;

            float timer = 0f;
            Vector3 startScale = barFrame != null ? barFrame.localScale : barBaseScale;
            Vector3 flatScale = new Vector3(startScale.x, 0f, startScale.z);

            while (timer < 0.12f)
            {
                timer += Time.deltaTime;
                if (barFrame != null)
                    barFrame.localScale = Vector3.Lerp(startScale, flatScale, timer / 0.12f);
                yield return null;
            }

            timer = 0f;
            while (timer < 0.2f)
            {
                timer += Time.deltaTime;
                if (canvasGroup != null)
                    canvasGroup.alpha = 1f - Mathf.Clamp01(timer / 0.2f);
                yield return null;
            }

            if (canvasGroup != null) canvasGroup.alpha = 0f;
            Destroy(gameObject);
        }
    }
}
