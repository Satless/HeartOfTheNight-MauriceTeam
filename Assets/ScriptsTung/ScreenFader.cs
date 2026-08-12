using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shared fullscreen fade + loading UI (DontDestroyOnLoad).
/// Prefer placing / Resources prefab "UI/ScreenFader" so logo, TMP text, spinner are editable in Inspector.
/// Falls back to runtime UI if prefab / refs are missing.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private const string ResourcesPrefabPath = "UI/ScreenFader";

    private static ScreenFader _instance;

    public static ScreenFader Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ScreenFader>();
                if (_instance == null)
                    _instance = TryInstantiateFromResources();
                if (_instance == null)
                {
                    var go = new GameObject("ScreenFader");
                    _instance = go.AddComponent<ScreenFader>();
                }
            }
            return _instance;
        }
    }

    [Header("UI Refs (kéo từ prefab — đừng để trống nếu dùng prefab)")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private GameObject loadingRoot;
    [SerializeField] private RectTransform spinner;
    [SerializeField] private Image logoImage;
    [SerializeField] private TextMeshProUGUI loadingLabel;

    [Header("Timing (chỉnh trên prefab)")]
    [Tooltip("Thời gian fade đen / sáng mặc định (giây).")]
    [SerializeField] private float defaultFadeDuration = 0.5f;
    [Tooltip("Giữ màn đen sau khi scene load xong, trước khi fade in.")]
    [SerializeField] private float defaultDelayBeforeFadeIn = 0.2f;
    [Tooltip("Loading hiện tối thiểu bao lâu (kể cả khi scene load rất nhanh).")]
    [SerializeField] private float minLoadingDisplayTime = 0.35f;
    [Tooltip("Thời gian 1 vòng xoay spinner (giây).")]
    [SerializeField] private float spinnerLoopSeconds = 1f;

    [Header("Loading copy")]
    [SerializeField] private string loadingText = "Loading...";

    public float DefaultFadeDuration => defaultFadeDuration;
    public float DefaultDelayBeforeFadeIn => defaultDelayBeforeFadeIn;
    public float MinLoadingDisplayTime => minLoadingDisplayTime;

    private Tween fadeTween;
    private Tween spinnerTween;
    private static Sprite _whiteSprite;

    private static ScreenFader TryInstantiateFromResources()
    {
        var prefab = Resources.Load<GameObject>(ResourcesPrefabPath);
        if (prefab == null) return null;
        var instance = Object.Instantiate(prefab);
        instance.name = "ScreenFader";
        return instance.GetComponent<ScreenFader>();
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureUi();
        SanitizeFadeSprite();
        ApplyLoadingText();
        SetFadeAlpha(0f, blockRaycasts: false);
        SetLoadingVisible(false);
    }

    private void OnDestroy()
    {
        fadeTween?.Kill();
        spinnerTween?.Kill();
        if (_instance == this) _instance = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (logoImage != null)
            logoImage.gameObject.SetActive(logoImage.sprite != null);
    }
#endif

    public IEnumerator FadeOut(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        EnsureUi();
        SanitizeFadeSprite();
        fadeImage.gameObject.SetActive(true);
        fadeImage.raycastTarget = true;

        fadeTween?.Kill();
        fadeTween = fadeImage.DOFade(1f, duration).SetEase(Ease.InOutSine).SetUpdate(true);
        yield return fadeTween.WaitForCompletion();

        SetFadeAlpha(1f, blockRaycasts: true);
    }

    public IEnumerator FadeIn(float duration = -1f)
    {
        if (duration < 0f) duration = defaultFadeDuration;
        EnsureUi();
        SanitizeFadeSprite();
        fadeImage.gameObject.SetActive(true);

        fadeTween?.Kill();
        fadeTween = fadeImage.DOFade(0f, duration).SetEase(Ease.InOutSine).SetUpdate(true);
        yield return fadeTween.WaitForCompletion();

        SetFadeAlpha(0f, blockRaycasts: false);
    }

    public void SetLoadingVisible(bool visible)
    {
        EnsureUi();
        ApplyLoadingText();

        if (loadingRoot != null)
            loadingRoot.SetActive(visible);

        if (visible)
        {
            fadeImage.gameObject.SetActive(true);
            SetFadeAlpha(1f, blockRaycasts: true);
        }

        spinnerTween?.Kill();
        if (visible && spinner != null)
        {
            float loop = Mathf.Max(0.1f, spinnerLoopSeconds);
            spinner.localRotation = Quaternion.identity;
            spinnerTween = spinner
                .DORotate(new Vector3(0f, 0f, -360f), loop, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart)
                .SetUpdate(true);
        }
    }

    /// <summary>
    /// Call after FadeOut + save logic. Runs on this DDOL object so loading + FadeIn survive scene unload.
    /// </summary>
    public void LoadSceneWithLoading(string sceneName, float fadeDuration = -1f, float delayBeforeFadeIn = -1f)
    {
        if (fadeDuration < 0f) fadeDuration = defaultFadeDuration;
        if (delayBeforeFadeIn < 0f) delayBeforeFadeIn = defaultDelayBeforeFadeIn;
        StartCoroutine(LoadSceneWithLoadingRoutine(sceneName, fadeDuration, delayBeforeFadeIn));
    }

    private IEnumerator LoadSceneWithLoadingRoutine(string sceneName, float fadeDuration, float delayBeforeFadeIn)
    {
        SetLoadingVisible(true);
        float loadingShownAt = Time.realtimeSinceStartup;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        if (asyncLoad == null)
        {
            Debug.LogError($"[ScreenFader] LoadSceneAsync failed for '{sceneName}'.");
            SetLoadingVisible(false);
            yield return FadeIn(fadeDuration);
            yield break;
        }

        while (!asyncLoad.isDone)
            yield return null;

        float elapsed = Time.realtimeSinceStartup - loadingShownAt;
        if (elapsed < minLoadingDisplayTime)
            yield return new WaitForSecondsRealtime(minLoadingDisplayTime - elapsed);

        yield return null;
        LevelEntrance.TryApplyAllPending();

        SetLoadingVisible(false);

        if (delayBeforeFadeIn > 0f)
            yield return new WaitForSecondsRealtime(delayBeforeFadeIn);

        yield return FadeIn(fadeDuration);
    }

    /// <summary>
    /// Prefab cũ từng gán Background.psd (bo góc) → bị răng cưa khi full-screen. Ép về sprite đặc.
    /// </summary>
    private void SanitizeFadeSprite()
    {
        if (fadeImage == null) return;

        Sprite solid = Resources.Load<Sprite>("UI/ScreenFaderWhite");
        if (solid == null) solid = GetWhiteSprite();

        bool badSprite = fadeImage.sprite == null
            || fadeImage.sprite.name.Contains("Background")
            || fadeImage.sprite.name.Contains("UISprite")
            || fadeImage.sprite.name.Contains("Knob");

        if (badSprite)
            fadeImage.sprite = solid;

        fadeImage.type = Image.Type.Simple;
        StretchFull(fadeImage.rectTransform);
    }

    private void ApplyLoadingText()
    {
        if (loadingLabel != null && !string.IsNullOrEmpty(loadingText))
            loadingLabel.text = loadingText;
    }

    private void SetFadeAlpha(float alpha, bool blockRaycasts)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = alpha;
        fadeImage.color = c;
        fadeImage.raycastTarget = blockRaycasts;
        if (alpha <= 0.001f && (loadingRoot == null || !loadingRoot.activeSelf))
            fadeImage.gameObject.SetActive(false);
        else
            fadeImage.gameObject.SetActive(true);
    }

    private void EnsureUi()
    {
        if (fadeImage != null && loadingRoot != null) return;

        Canvas canvas = GetComponentInChildren<Canvas>(true);
        if (canvas == null)
        {
            var canvasGo = new GameObject("ScreenFaderCanvas", typeof(RectTransform));
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 9999;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        if (fadeImage == null)
        {
            var fadeGo = new GameObject("FadeImage", typeof(RectTransform));
            fadeGo.transform.SetParent(canvas.transform, false);
            fadeImage = fadeGo.AddComponent<Image>();
            fadeImage.sprite = GetWhiteSprite();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            fadeImage.raycastTarget = false;
            StretchFull(fadeImage.rectTransform);
        }

        if (loadingRoot == null)
        {
            loadingRoot = new GameObject("LoadingRoot", typeof(RectTransform));
            loadingRoot.transform.SetParent(canvas.transform, false);
            StretchFull(loadingRoot.GetComponent<RectTransform>());

            // Logo slot (optional — gán sprite trên prefab)
            var logoGo = new GameObject("Logo", typeof(RectTransform));
            logoGo.transform.SetParent(loadingRoot.transform, false);
            var logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.sizeDelta = new Vector2(220f, 220f);
            logoRt.anchoredPosition = new Vector2(0f, 120f);
            logoImage = logoGo.AddComponent<Image>();
            logoImage.color = Color.white;
            logoImage.preserveAspect = true;
            logoImage.raycastTarget = false;
            logoGo.SetActive(false); // bật khi gán sprite trên prefab

            var spinnerGo = new GameObject("Spinner", typeof(RectTransform));
            spinnerGo.transform.SetParent(loadingRoot.transform, false);
            spinner = spinnerGo.GetComponent<RectTransform>();
            spinner.sizeDelta = new Vector2(72f, 72f);
            spinner.anchoredPosition = new Vector2(0f, -20f);
            var spinnerImg = spinnerGo.AddComponent<Image>();
            spinnerImg.sprite = GetWhiteSprite();
            spinnerImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            spinnerImg.raycastTarget = false;

            var textGo = new GameObject("LoadingText", typeof(RectTransform));
            textGo.transform.SetParent(loadingRoot.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(640f, 80f);
            textRt.anchoredPosition = new Vector2(0f, -110f);
            loadingLabel = textGo.AddComponent<TextMeshProUGUI>();
            loadingLabel.text = loadingText;
            loadingLabel.alignment = TextAlignmentOptions.Center;
            loadingLabel.color = Color.white;
            loadingLabel.fontSize = 42f;
            loadingLabel.raycastTarget = false;
            if (TMP_Settings.defaultFontAsset != null)
                loadingLabel.font = TMP_Settings.defaultFontAsset;
        }

        if (spinner == null && loadingRoot != null)
        {
            var existing = loadingRoot.transform.Find("Spinner");
            if (existing != null) spinner = existing.GetComponent<RectTransform>();
        }

        if (loadingLabel == null && loadingRoot != null)
            loadingLabel = loadingRoot.GetComponentInChildren<TextMeshProUGUI>(true);

        if (logoImage == null && loadingRoot != null)
        {
            var logo = loadingRoot.transform.Find("Logo");
            if (logo != null) logoImage = logo.GetComponent<Image>();
        }
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static Sprite GetWhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        var tex = Texture2D.whiteTexture;
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return _whiteSprite;
    }
}
