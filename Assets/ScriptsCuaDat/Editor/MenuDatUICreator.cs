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
/// Menu: Heart Of The Night / Auth / Scaffold MenuDat In Open Scene
/// Main Menu tạm cho scene MenuDat (Play / Settings+Account / Quit + Logout popup).
/// </summary>
public static class MenuDatUICreator
{
    private static readonly Color BgDark = new Color(0.06f, 0.05f, 0.05f, 1f);
    private static readonly Color RedSmear = new Color(0.55f, 0.08f, 0.08f, 0.85f);
    private static readonly Color WindowBg = new Color(0.12f, 0.11f, 0.11f, 0.96f);
    private static readonly Color TitleBar = new Color(0.18f, 0.16f, 0.16f, 1f);
    private static readonly Color TextCream = new Color(0.92f, 0.88f, 0.82f, 1f);
    private static readonly Color TextDim = new Color(0.65f, 0.6f, 0.55f, 1f);
    private static readonly Color Overlay = new Color(0f, 0f, 0f, 0.65f);

    [MenuItem("Heart Of The Night/Auth/Scaffold MenuDat In Open Scene")]
    public static void ScaffoldInOpenScene()
    {
        if (Object.FindFirstObjectByType<MenuDatUI>() != null)
        {
            if (!EditorUtility.DisplayDialog(
                    "MenuDat UI",
                    "Scene đã có MenuDatUI. Tạo thêm Canvas mới?",
                    "Tạo thêm",
                    "Hủy"))
                return;
        }

        EnsureEventSystem();

        var canvasGo = CreateCanvas("Canvas_MenuDat");
        var menu = canvasGo.AddComponent<MenuDatUI>();

        CreateBg(canvasGo.transform);

        var brand = CreateTmp("Txt_Brand", canvasGo.transform, "HEART OF THE NIGHT", 22, TextDim, TextAlignmentOptions.Center);
        PlaceTopCenter(brand.rectTransform, 700f, 32f, -80f);

        var title = CreateTmp("Txt_Title", canvasGo.transform, "MAIN MENU", 40, TextCream, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        PlaceTopCenter(title.rectTransform, 500f, 56f, -130f);

        var buttonsRoot = CreateRect("MainButtons", canvasGo.transform);
        PlaceCenter(buttonsRoot, 420f, 280f, new Vector2(0f, -20f));

        CreateMenuButton("Btn_Play", buttonsRoot, "▶  PLAY", new Vector2(0f, 80f), 360f);
        CreateMenuButton("Btn_Settings", buttonsRoot, "SETTINGS", new Vector2(0f, 20f), 360f);
        CreateMenuButton("Btn_Quit", buttonsRoot, "QUIT", new Vector2(0f, -40f), 360f);

        var settings = CreateSettingsPanel(canvasGo.transform);
        settings.SetActive(false);

        var logoutPopup = CreateLogoutPopup(canvasGo.transform);
        logoutPopup.SetActive(false);

        Wire(menu, buttonsRoot, settings.transform, logoutPopup.transform);

        var so = new SerializedObject(menu);
        so.FindProperty("mainButtonsRoot").objectReferenceValue = buttonsRoot.gameObject;
        so.FindProperty("settingsPanel").objectReferenceValue = settings;
        so.FindProperty("logoutConfirmPopup").objectReferenceValue = logoutPopup;
        so.FindProperty("signOutButton").objectReferenceValue = settings.transform.Find("Window/Body/Btn_SignOut")?.gameObject;
        so.FindProperty("linkGoogleButton").objectReferenceValue = settings.transform.Find("Window/Body/Btn_LinkGoogle")?.gameObject;
        so.FindProperty("accountEmailText").objectReferenceValue = settings.transform.Find("Window/Body/Txt_Email")?.GetComponent<TMP_Text>();
        so.FindProperty("accountStatusText").objectReferenceValue = settings.transform.Find("Window/Body/Txt_Status")?.GetComponent<TMP_Text>();
        so.FindProperty("playSceneName").stringValue = "SelectLvDat";
        so.FindProperty("authSceneName").stringValue = "AuthScene";
        so.FindProperty("treatAsGuest").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = canvasGo;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[MenuDat] Scaffold xong. Save scene. Auth → nextSceneName = MenuDat. Build: AuthScene → MenuDat → SelectLevel.");
    }

    private static void Wire(MenuDatUI menu, RectTransform buttonsRoot, Transform settings, Transform logout)
    {
        Persist(buttonsRoot.Find("Btn_Play")?.GetComponent<Button>(), menu, nameof(MenuDatUI.OnPlay));
        Persist(buttonsRoot.Find("Btn_Settings")?.GetComponent<Button>(), menu, nameof(MenuDatUI.OnOpenSettings));
        Persist(buttonsRoot.Find("Btn_Quit")?.GetComponent<Button>(), menu, nameof(MenuDatUI.OnQuit));

        Persist(settings.Find("Window/TitleBar/Btn_Close")?.GetComponent<Button>(), menu, nameof(MenuDatUI.OnCloseSettings));
        Persist(settings.Find("Window/Body/Btn_SignOut")?.GetComponent<Button>(), menu, nameof(MenuDatUI.OnSignOutClicked));
        Persist(settings.Find("Window/Body/Btn_LinkGoogle")?.GetComponent<Button>(), menu, nameof(MenuDatUI.OnLinkGoogle));

        Persist(logout.Find("Window/TitleBar/Btn_Close")?.GetComponent<Button>(), menu, nameof(MenuDatUI.OnLogoutNo));
        Persist(logout.Find("Window/Body/Actions/Btn_Yes")?.GetComponent<Button>(), menu, nameof(MenuDatUI.OnLogoutYes));
        Persist(logout.Find("Window/Body/Actions/Btn_No")?.GetComponent<Button>(), menu, nameof(MenuDatUI.OnLogoutNo));
    }

    private static void Persist(Button button, MenuDatUI menu, string method)
    {
        if (button == null || menu == null) return;
        var action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), menu, method);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static GameObject CreateSettingsPanel(Transform canvas)
    {
        var root = CreateRect("SettingsPanel", canvas);
        StretchFull(root);

        var dim = CreateImage("Dim", root, Overlay);
        StretchFull(dim.rectTransform);

        var win = CreateImage("Window", root, WindowBg);
        var winRt = win.rectTransform;
        winRt.anchorMin = winRt.anchorMax = new Vector2(0.5f, 0.5f);
        winRt.sizeDelta = new Vector2(720f, 460f);

        CreateWindowTitleBar(winRt, "settings");

        var body = CreateRect("Body", winRt);
        StretchFull(body);
        body.offsetMin = new Vector2(28f, 28f);
        body.offsetMax = new Vector2(-28f, -50f);

        var hint = CreateTmp("Txt_VolumeHint", body, "...volume / screen options...", 18, TextDim, TextAlignmentOptions.Left);
        PlaceTopLeft(hint.rectTransform, 400f, 28f, 0f, 0f);

        var line = CreateImage("Divider", body, TextDim);
        var lineRt = line.rectTransform;
        lineRt.anchorMin = new Vector2(0f, 1f);
        lineRt.anchorMax = new Vector2(1f, 1f);
        lineRt.pivot = new Vector2(0.5f, 1f);
        lineRt.sizeDelta = new Vector2(0f, 2f);
        lineRt.anchoredPosition = new Vector2(0f, -48f);

        var account = CreateTmp("Txt_AccountLabel", body, "ACCOUNT", 16, TextDim, TextAlignmentOptions.Left);
        PlaceTopLeft(account.rectTransform, 200f, 24f, 0f, -64f);

        var email = CreateTmp("Txt_Email", body, "GUEST (LOCAL SAVE)", 22, TextCream, TextAlignmentOptions.Left);
        PlaceTopLeft(email.rectTransform, 500f, 32f, 0f, -96f);

        var status = CreateTmp("Txt_Status", body, "STATUS: OFFLINE ACCOUNT", 16, TextDim, TextAlignmentOptions.Left);
        PlaceTopLeft(status.rectTransform, 500f, 24f, 0f, -136f);

        CreateMenuButton("Btn_SignOut", body, "▶  SIGN OUT", new Vector2(-180f, -80f), 220f);
        CreateMenuButton("Btn_LinkGoogle", body, "▶  LINK GOOGLE", new Vector2(-180f, -80f), 260f);
        // Guest mặc định: SignOut ẩn — runtime RefreshAccountRow xử lý
        body.Find("Btn_SignOut").gameObject.SetActive(false);

        var note = CreateTmp("Txt_GuestNote", body, "IF GUEST: show LINK GOOGLE instead", 14, TextDim, TextAlignmentOptions.Left);
        PlaceTopLeft(note.rectTransform, 420f, 22f, 0f, -200f);

        return root.gameObject;
    }

    private static GameObject CreateLogoutPopup(Transform canvas)
    {
        var root = CreateRect("Popup_LogoutConfirm", canvas);
        StretchFull(root);

        var dim = CreateImage("DimOverlay", root, Overlay);
        StretchFull(dim.rectTransform);

        var win = CreateImage("Window", root, WindowBg);
        var winRt = win.rectTransform;
        winRt.anchorMin = winRt.anchorMax = new Vector2(0.5f, 0.5f);
        winRt.sizeDelta = new Vector2(780f, 360f);

        CreateWindowTitleBar(winRt, "logout");

        var body = CreateRect("Body", winRt);
        StretchFull(body);
        body.offsetMin = new Vector2(24f, 24f);
        body.offsetMax = new Vector2(-24f, -50f);

        var msg = CreateTmp(
            "Txt_Message",
            body,
            "SIGN OUT OF THIS ACCOUNT? LOCAL SAVES REMAIN ON DEVICE. CLOUD SAVES STAY WITH THIS ACCOUNT.",
            20,
            TextCream,
            TextAlignmentOptions.Center);
        msg.enableWordWrapping = true;
        var msgRt = msg.rectTransform;
        msgRt.anchorMin = new Vector2(0f, 0.35f);
        msgRt.anchorMax = new Vector2(1f, 1f);
        msgRt.offsetMin = Vector2.zero;
        msgRt.offsetMax = Vector2.zero;

        var actions = CreateRect("Actions", body);
        actions.anchorMin = actions.anchorMax = new Vector2(0.5f, 0f);
        actions.pivot = new Vector2(0.5f, 0f);
        actions.sizeDelta = new Vector2(320f, 40f);
        actions.anchoredPosition = new Vector2(0f, 16f);

        CreateMenuButton("Btn_Yes", actions, "▶  YES", new Vector2(-80f, 0f), 120f);
        CreateMenuButton("Btn_No", actions, "NO", new Vector2(80f, 0f), 100f);

        return root.gameObject;
    }

    private static void CreateWindowTitleBar(RectTransform win, string title)
    {
        var titleBar = CreateImage("TitleBar", win, TitleBar);
        var tbRt = titleBar.rectTransform;
        tbRt.anchorMin = new Vector2(0f, 1f);
        tbRt.anchorMax = new Vector2(1f, 1f);
        tbRt.pivot = new Vector2(0.5f, 1f);
        tbRt.sizeDelta = new Vector2(0f, 42f);
        tbRt.anchoredPosition = Vector2.zero;

        var titleTmp = CreateTmp("Title", tbRt, title, 20, TextCream, TextAlignmentOptions.Left);
        var titleRt = titleTmp.rectTransform;
        titleRt.anchorMin = Vector2.zero;
        titleRt.anchorMax = Vector2.one;
        titleRt.offsetMin = new Vector2(16f, 0f);
        titleRt.offsetMax = new Vector2(-80f, 0f);

        var close = CreateMenuButton("Btn_Close", tbRt, "X", Vector2.zero, 36f);
        var closeRt = close.GetComponent<RectTransform>();
        closeRt.anchorMin = closeRt.anchorMax = new Vector2(1f, 0.5f);
        closeRt.pivot = new Vector2(1f, 0.5f);
        closeRt.sizeDelta = new Vector2(36f, 36f);
        closeRt.anchoredPosition = new Vector2(-8f, 0f);
    }

    private static void CreateBg(Transform canvas)
    {
        var bg = CreateImage("Bg", canvas, BgDark);
        StretchFull(bg.rectTransform);

        var left = CreateImage("Bg_RedSmearL", canvas, RedSmear);
        var leftRt = left.rectTransform;
        leftRt.anchorMin = new Vector2(0f, 0f);
        leftRt.anchorMax = new Vector2(0f, 1f);
        leftRt.pivot = new Vector2(0f, 0.5f);
        leftRt.offsetMin = new Vector2(0f, 0f);
        leftRt.offsetMax = new Vector2(520f, 0f);

        var right = CreateImage("Bg_RedSmearR", canvas, RedSmear);
        var rightRt = right.rectTransform;
        rightRt.anchorMin = new Vector2(1f, 0f);
        rightRt.anchorMax = new Vector2(1f, 1f);
        rightRt.pivot = new Vector2(1f, 0.5f);
        rightRt.offsetMin = new Vector2(-580f, 0f);
        rightRt.offsetMax = new Vector2(0f, 0f);
    }

    private static GameObject CreateCanvas(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        Undo.RegisterCreatedObjectUndo(go, "Create MenuDat Canvas");
        return go;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
    }

    private static Button CreateMenuButton(string name, RectTransform parent, string label, Vector2 pos, float width)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, 40f);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.02f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(1f, 0.85f, 0.7f, 0.15f);
        colors.pressedColor = new Color(1f, 0.7f, 0.5f, 0.25f);
        btn.colors = colors;

        var tmp = CreateTmp("Label", rt, label, 20, TextCream, TextAlignmentOptions.Center);
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
        string name, Transform parent, string text, float size, Color color, TextAlignmentOptions align)
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

    private static void PlaceTopCenter(RectTransform rt, float w, float h, float yFromTop)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(0f, yFromTop);
    }

    private static void PlaceCenter(RectTransform rt, float w, float h, Vector2 pos)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = pos;
    }

    private static void PlaceTopLeft(RectTransform rt, float w, float h, float x, float yFromTop)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, yFromTop);
    }
}
#endif
