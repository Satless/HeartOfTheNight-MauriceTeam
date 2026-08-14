#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HeartOfTheNight.UI;

/// <summary>
/// Menu: Heart Of The Night / Auth / Scaffold Login UI In Open Scene
/// Dựng Canvas Login + Guest/Network popup theo Figma 01/02/04 (placeholder style).
/// </summary>
public static class AuthLoginUICreator
{
    private static readonly Color BgDark = new Color(0.06f, 0.05f, 0.05f, 1f);
    private static readonly Color RedSmear = new Color(0.55f, 0.08f, 0.08f, 0.85f);
    private static readonly Color WindowBg = new Color(0.12f, 0.11f, 0.11f, 0.96f);
    private static readonly Color TitleBar = new Color(0.18f, 0.16f, 0.16f, 1f);
    private static readonly Color TextCream = new Color(0.92f, 0.88f, 0.82f, 1f);
    private static readonly Color TextDim = new Color(0.65f, 0.6f, 0.55f, 1f);
    private static readonly Color Overlay = new Color(0f, 0f, 0f, 0.65f);

    [MenuItem("Heart Of The Night/Auth/Scaffold Login UI In Open Scene")]
    public static void ScaffoldInOpenScene()
    {
        if (Object.FindFirstObjectByType<AuthLoginUI>() != null)
        {
            if (!EditorUtility.DisplayDialog(
                    "Auth UI",
                    "Scene đã có AuthLoginUI. Tạo thêm Canvas mới?",
                    "Tạo thêm",
                    "Hủy"))
                return;
        }

        EnsureEventSystem();

        var canvasGo = CreateCanvas("Canvas_Auth");
        var auth = canvasGo.AddComponent<AuthLoginUI>();

        var loginRoot = CreateRect("LoginScreen", canvasGo.transform);
        StretchFull(loginRoot);

        CreateBgSmears(loginRoot);
        CreateBrand(loginRoot);
        CreateLoginWindow(loginRoot);

        var guestPopup = CreateConfirmPopup(
            canvasGo.transform,
            "Popup_GuestConfirm",
            "warning",
            "PLAY WITHOUT AN ACCOUNT? PROGRESS STAYS ON THIS DEVICE. SIGN IN LATER TO SYNC CLOUD SAVES.",
            "CONTINUE",
            "SIGN IN");
        guestPopup.SetActive(false);

        var networkPopup = CreateConfirmPopup(
            canvasGo.transform,
            "Popup_NetworkRequired",
            "network",
            "NETWORK CONNECTION REQUIRED TO SIGN IN WITH GOOGLE. CHECK YOUR CONNECTION AND RETRY.",
            "RETRY",
            "PLAY AS GUEST");
        networkPopup.SetActive(false);

        WireButtons(auth, loginRoot, guestPopup, networkPopup);

        // Serialize refs via SerializedObject
        var so = new SerializedObject(auth);
        so.FindProperty("loginRoot").objectReferenceValue = loginRoot.gameObject;
        so.FindProperty("guestConfirmPopup").objectReferenceValue = guestPopup;
        so.FindProperty("networkPopup").objectReferenceValue = networkPopup;
        so.FindProperty("nextSceneName").stringValue = "mainMenu";
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = canvasGo;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[Auth] Scaffold xong. Save scene (Ctrl+S), thêm AuthScene vào Build Settings (index 0).");
    }

    private static void WireButtons(AuthLoginUI auth, RectTransform loginRoot, GameObject guestPopup, GameObject networkPopup)
    {
        Button FindBtn(Transform root, string name)
        {
            var t = root.Find(name);
            return t != null ? t.GetComponent<Button>() : null;
        }

        PersistClick(FindBtn(loginRoot, "Window_Login/Body/Btn_Google"), auth, nameof(AuthLoginUI.OnSignInWithGoogle));
        PersistClick(FindBtn(loginRoot, "Window_Login/Body/Btn_Guest"), auth, nameof(AuthLoginUI.OnPlayAsGuest));

        WirePopup(guestPopup.transform, auth,
            nameof(AuthLoginUI.OnGuestContinue),
            nameof(AuthLoginUI.OnGuestSignInInstead),
            nameof(AuthLoginUI.OnClosePopup));
        WirePopup(networkPopup.transform, auth,
            nameof(AuthLoginUI.OnNetworkRetry),
            nameof(AuthLoginUI.OnNetworkPlayAsGuest),
            nameof(AuthLoginUI.OnClosePopup));
    }

    private static void WirePopup(Transform popupRoot, AuthLoginUI auth, string primary, string secondary, string close)
    {
        var win = popupRoot.Find("Window");
        if (win == null) return;

        PersistClick(win.Find("Body/Actions/Btn_Primary")?.GetComponent<Button>(), auth, primary);
        PersistClick(win.Find("Body/Actions/Btn_Secondary")?.GetComponent<Button>(), auth, secondary);
        PersistClick(win.Find("TitleBar/Btn_Close")?.GetComponent<Button>(), auth, close);
    }

    private static void PersistClick(Button button, AuthLoginUI auth, string methodName)
    {
        if (button == null || auth == null) return;
        UnityAction action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), auth, methodName);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static GameObject CreateCanvas(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(go, "Create Auth Canvas");
        return go;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
            return;

        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    private static void CreateBgSmears(RectTransform parent)
    {
        var bg = CreateImage("Bg", parent, BgDark);
        StretchFull(bg.rectTransform);

        var left = CreateImage("Bg_RedSmearL", parent, RedSmear);
        var leftRt = left.rectTransform;
        leftRt.anchorMin = new Vector2(0f, 0f);
        leftRt.anchorMax = new Vector2(0f, 1f);
        leftRt.pivot = new Vector2(0f, 0.5f);
        leftRt.sizeDelta = new Vector2(520f, 0f);
        leftRt.anchoredPosition = Vector2.zero;
        leftRt.offsetMin = new Vector2(0f, 0f);
        leftRt.offsetMax = new Vector2(520f, 0f);

        var right = CreateImage("Bg_RedSmearR", parent, RedSmear);
        var rightRt = right.rectTransform;
        rightRt.anchorMin = new Vector2(1f, 0f);
        rightRt.anchorMax = new Vector2(1f, 1f);
        rightRt.pivot = new Vector2(1f, 0.5f);
        rightRt.anchoredPosition = Vector2.zero;
        rightRt.offsetMin = new Vector2(-580f, 0f);
        rightRt.offsetMax = new Vector2(0f, 0f);
    }

    private static void CreateBrand(RectTransform parent)
    {
        var brand = CreateTmp("Txt_Brand", parent, "HEART OF THE NIGHT", 18, TextDim, TextAlignmentOptions.Center);
        var brandRt = brand.rectTransform;
        brandRt.anchorMin = brandRt.anchorMax = new Vector2(0.5f, 1f);
        brandRt.pivot = new Vector2(0.5f, 1f);
        brandRt.sizeDelta = new Vector2(600f, 28f);
        brandRt.anchoredPosition = new Vector2(0f, -90f);

        var account = CreateTmp("Txt_ACCOUNT", parent, "ACCOUNT", 36, TextCream, TextAlignmentOptions.Center);
        var accountRt = account.rectTransform;
        accountRt.anchorMin = accountRt.anchorMax = new Vector2(0.5f, 1f);
        accountRt.pivot = new Vector2(0.5f, 1f);
        accountRt.sizeDelta = new Vector2(400f, 48f);
        accountRt.anchoredPosition = new Vector2(0f, -130f);
        account.fontStyle = FontStyles.Bold;
    }

    private static void CreateLoginWindow(RectTransform parent)
    {
        var win = CreateImage("Window_Login", parent, WindowBg);
        var winRt = win.rectTransform;
        winRt.anchorMin = winRt.anchorMax = new Vector2(0.5f, 0.5f);
        winRt.sizeDelta = new Vector2(690f, 430f);
        winRt.anchoredPosition = new Vector2(0f, -20f);

        var titleBar = CreateImage("TitleBar", winRt, TitleBar);
        var tbRt = titleBar.rectTransform;
        tbRt.anchorMin = new Vector2(0f, 1f);
        tbRt.anchorMax = new Vector2(1f, 1f);
        tbRt.pivot = new Vector2(0.5f, 1f);
        tbRt.sizeDelta = new Vector2(0f, 42f);
        tbRt.anchoredPosition = Vector2.zero;

        var title = CreateTmp("Title", tbRt, "login", 20, TextCream, TextAlignmentOptions.Left);
        var titleRt = title.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 0f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = new Vector2(16f, 0f);
        titleRt.offsetMax = new Vector2(-80f, 0f);

        CreateTitleBarClose(tbRt);

        var body = CreateRect("Body", winRt);
        StretchFull(body);
        body.offsetMin = new Vector2(0f, 0f);
        body.offsetMax = new Vector2(0f, -42f);

        var google = CreateMenuButton("Btn_Google", body, "▶  SIGN IN WITH GOOGLE", new Vector2(0f, 60f), 420f);
        var guest = CreateMenuButton("Btn_Guest", body, "PLAY AS GUEST", new Vector2(0f, 0f), 280f);

        var line = CreateImage("Divider", body, TextDim);
        var lineRt = line.rectTransform;
        lineRt.anchorMin = lineRt.anchorMax = new Vector2(0.5f, 0.5f);
        lineRt.sizeDelta = new Vector2(450f, 2f);
        lineRt.anchoredPosition = new Vector2(0f, -50f);

        var note = CreateTmp(
            "Txt_Note",
            body,
            "NETWORK REQUIRED TO SIGN IN. SAVE IS STORED ON DEVICE; CLOUD SYNCS WHEN ONLINE.",
            16,
            TextDim,
            TextAlignmentOptions.Center);
        note.enableWordWrapping = true;
        var noteRt = note.rectTransform;
        noteRt.anchorMin = noteRt.anchorMax = new Vector2(0.5f, 0.5f);
        noteRt.sizeDelta = new Vector2(600f, 70f);
        noteRt.anchoredPosition = new Vector2(0f, -110f);
    }

    private static GameObject CreateConfirmPopup(
        Transform canvas,
        string rootName,
        string title,
        string message,
        string primaryLabel,
        string secondaryLabel)
    {
        var root = CreateRect(rootName, canvas);
        StretchFull(root);

        var dim = CreateImage("DimOverlay", root, Overlay);
        StretchFull(dim.rectTransform);
        dim.raycastTarget = true;

        var win = CreateImage("Window", root, WindowBg);
        var winRt = win.rectTransform;
        winRt.anchorMin = winRt.anchorMax = new Vector2(0.5f, 0.5f);
        winRt.sizeDelta = new Vector2(780f, 360f);

        var titleBar = CreateImage("TitleBar", winRt, TitleBar);
        var tbRt = titleBar.rectTransform;
        tbRt.anchorMin = new Vector2(0f, 1f);
        tbRt.anchorMax = new Vector2(1f, 1f);
        tbRt.pivot = new Vector2(0.5f, 1f);
        tbRt.sizeDelta = new Vector2(0f, 42f);
        tbRt.anchoredPosition = Vector2.zero;

        var titleTmp = CreateTmp("Title", tbRt, title, 20, TextCream, TextAlignmentOptions.Left);
        var titleRt = titleTmp.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 0f);
        titleRt.anchorMax = new Vector2(1f, 1f);
        titleRt.offsetMin = new Vector2(16f, 0f);
        titleRt.offsetMax = new Vector2(-80f, 0f);

        CreateTitleBarClose(tbRt);

        var body = CreateRect("Body", winRt);
        StretchFull(body);
        body.offsetMin = new Vector2(24f, 24f);
        body.offsetMax = new Vector2(-24f, -42f);

        var msg = CreateTmp("Txt_Message", body, message, 20, TextCream, TextAlignmentOptions.Center);
        msg.enableWordWrapping = true;
        var msgRt = msg.rectTransform;
        msgRt.anchorMin = new Vector2(0f, 0.35f);
        msgRt.anchorMax = new Vector2(1f, 1f);
        msgRt.offsetMin = Vector2.zero;
        msgRt.offsetMax = Vector2.zero;

        var actions = CreateRect("Actions", body);
        var actRt = actions;
        actRt.anchorMin = actRt.anchorMax = new Vector2(0.5f, 0f);
        actRt.pivot = new Vector2(0.5f, 0f);
        actRt.sizeDelta = new Vector2(500f, 40f);
        actRt.anchoredPosition = new Vector2(0f, 20f);

        CreateTextButton("Btn_Primary", actRt, "▶  " + primaryLabel, new Vector2(-110f, 0f), 200f);
        CreateTextButton("Btn_Secondary", actRt, secondaryLabel, new Vector2(110f, 0f), 200f);

        return root.gameObject;
    }

    private static void CreateTitleBarClose(RectTransform titleBar)
    {
        var btn = CreateTextButton("Btn_Close", titleBar, "X", Vector2.zero, 36f);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.sizeDelta = new Vector2(36f, 36f);
        rt.anchoredPosition = new Vector2(-8f, 0f);
    }

    private static Button CreateMenuButton(string name, RectTransform parent, string label, Vector2 pos, float width)
    {
        var btn = CreateTextButton(name, parent, label, pos, width);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(width, 40f);
        return btn;
    }

    private static Button CreateTextButton(string name, RectTransform parent, string label, Vector2 pos, float width)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, 36f);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.02f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.7f, 0.15f);
        colors.pressedColor = new Color(1f, 0.7f, 0.5f, 0.25f);
        btn.colors = colors;

        var tmp = CreateTmp("Label", rt, label, 18, TextCream, TextAlignmentOptions.Center);
        StretchFull(tmp.rectTransform);
        return btn;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    private static TextMeshProUGUI CreateTmp(
        string name,
        Transform parent,
        string text,
        float size,
        Color color,
        TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
    }
}
#endif
