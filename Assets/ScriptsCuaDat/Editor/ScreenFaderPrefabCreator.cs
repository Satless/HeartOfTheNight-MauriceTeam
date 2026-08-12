#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Menu: Heart Of The Night / Create or Rebuild ScreenFader Prefab
/// </summary>
public static class ScreenFaderPrefabCreator
{
    private const string PrefabFolder = "Assets/Resources/UI";
    private const string PrefabPath = PrefabFolder + "/ScreenFader.prefab";
    private const string SpinnerPath = PrefabFolder + "/ScreenFaderSpinner.png";
    private const string WhitePath = PrefabFolder + "/ScreenFaderWhite.png";

    [InitializeOnLoadMethod]
    private static void AutoCreateOnce()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                return;

            try { CreatePrefab(silent: true); }
            catch (System.Exception e)
            {
                Debug.LogError($"[ScreenFader] Auto-create prefab failed: {e.Message}\n{e}");
            }
        };
    }

    [MenuItem("Heart Of The Night/Create or Rebuild ScreenFader Prefab")]
    public static void CreatePrefabMenu() => CreatePrefab(silent: false);

    public static void CreatePrefabBatch() => CreatePrefab(silent: true);

    public static void CreatePrefab(bool silent)
    {
        EnsureFolders();
        Sprite whiteSprite = EnsureWhiteSprite();
        Sprite spinnerSprite = EnsureSpinnerSprite();

        var root = new GameObject("ScreenFader");
        var fader = root.AddComponent<ScreenFader>();

        var canvasGo = new GameObject("ScreenFaderCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(root.transform, false);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Full-screen solid black (KHÔNG dùng Background.psd bo góc)
        var fadeGo = new GameObject("FadeImage", typeof(RectTransform));
        fadeGo.transform.SetParent(canvasGo.transform, false);
        var fadeImage = fadeGo.AddComponent<Image>();
        fadeImage.sprite = whiteSprite;
        fadeImage.type = Image.Type.Simple;
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;
        StretchFull(fadeGo.GetComponent<RectTransform>());

        var loadingRoot = new GameObject("LoadingRoot", typeof(RectTransform));
        loadingRoot.transform.SetParent(canvasGo.transform, false);
        StretchFull(loadingRoot.GetComponent<RectTransform>());

        var logoGo = new GameObject("Logo", typeof(RectTransform));
        logoGo.transform.SetParent(loadingRoot.transform, false);
        var logoRt = logoGo.GetComponent<RectTransform>();
        logoRt.anchorMin = logoRt.anchorMax = new Vector2(0.5f, 0.5f);
        logoRt.sizeDelta = new Vector2(200f, 200f);
        logoRt.anchoredPosition = new Vector2(0f, 90f);
        var logoImage = logoGo.AddComponent<Image>();
        logoImage.color = Color.white;
        logoImage.preserveAspect = true;
        logoImage.raycastTarget = false;
        logoGo.SetActive(false);

        var spinnerGo = new GameObject("Spinner", typeof(RectTransform));
        spinnerGo.transform.SetParent(loadingRoot.transform, false);
        var spinnerRt = spinnerGo.GetComponent<RectTransform>();
        spinnerRt.anchorMin = spinnerRt.anchorMax = new Vector2(0.5f, 0.5f);
        spinnerRt.sizeDelta = new Vector2(56f, 56f);
        spinnerRt.anchoredPosition = new Vector2(0f, -20f);
        var spinnerImg = spinnerGo.AddComponent<Image>();
        spinnerImg.sprite = spinnerSprite;
        spinnerImg.color = new Color(0.92f, 0.92f, 0.92f, 0.95f);
        spinnerImg.raycastTarget = false;
        spinnerImg.preserveAspect = true;

        var textGo = new GameObject("LoadingText", typeof(RectTransform));
        textGo.transform.SetParent(loadingRoot.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = textRt.anchorMax = new Vector2(0.5f, 0.5f);
        textRt.sizeDelta = new Vector2(720f, 60f);
        textRt.anchoredPosition = new Vector2(0f, -90f);
        var label = textGo.AddComponent<TextMeshProUGUI>();
        label.text = "Loading...";
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 28f;
        label.fontStyle = FontStyles.Normal;
        label.characterSpacing = 6f;
        label.color = new Color(0.85f, 0.85f, 0.85f, 0.9f);
        label.raycastTarget = false;
        if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;

        loadingRoot.SetActive(false);

        var so = new SerializedObject(fader);
        so.FindProperty("fadeImage").objectReferenceValue = fadeImage;
        so.FindProperty("loadingRoot").objectReferenceValue = loadingRoot;
        so.FindProperty("spinner").objectReferenceValue = spinnerRt;
        so.FindProperty("logoImage").objectReferenceValue = logoImage;
        so.FindProperty("loadingLabel").objectReferenceValue = label;
        so.FindProperty("defaultFadeDuration").floatValue = 0.5f;
        so.FindProperty("defaultDelayBeforeFadeIn").floatValue = 0.2f;
        so.FindProperty("minLoadingDisplayTime").floatValue = 0.35f;
        so.FindProperty("spinnerLoopSeconds").floatValue = 0.85f;
        so.FindProperty("loadingText").stringValue = "Loading...";
        so.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (!silent)
        {
            Selection.activeObject = prefab;
            EditorGUIUtility.PingObject(prefab);
        }

        Debug.Log($"[ScreenFader] Prefab rebuilt at {PrefabPath} (fullscreen solid fade).");
    }

    private static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "UI");
    }

    private static Sprite EnsureWhiteSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
        if (existing != null) return existing;

        if (!File.Exists(WhitePath))
        {
            var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false);
            var pixels = new Color[64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(WhitePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }

        AssetDatabase.ImportAsset(WhitePath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(WhitePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(WhitePath);
    }

    private static Sprite EnsureSpinnerSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(SpinnerPath);
        if (existing != null) return existing;

        AssetDatabase.ImportAsset(SpinnerPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(SpinnerPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100f;
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(SpinnerPath);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
#endif
