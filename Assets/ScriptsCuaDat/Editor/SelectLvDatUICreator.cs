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
/// Menu: Heart Of The Night / Auth / Scaffold SelectLvDat In Open Scene
/// </summary>
public static class SelectLvDatUICreator
{
    private static readonly Color BgDark = new Color(0.06f, 0.05f, 0.05f, 1f);
    private static readonly Color RedSmear = new Color(0.55f, 0.08f, 0.08f, 0.85f);
    private static readonly Color WindowBg = new Color(0.12f, 0.11f, 0.11f, 0.96f);
    private static readonly Color TitleBar = new Color(0.18f, 0.16f, 0.16f, 1f);
    private static readonly Color TextCream = new Color(0.92f, 0.88f, 0.82f, 1f);
    private static readonly Color TextDim = new Color(0.65f, 0.6f, 0.55f, 1f);
    private static readonly Color SlotBg = new Color(0.16f, 0.14f, 0.14f, 0.95f);

    [MenuItem("Heart Of The Night/Auth/Scaffold SelectLvDat In Open Scene")]
    public static void ScaffoldInOpenScene()
    {
        if (Object.FindFirstObjectByType<SelectLvDatUI>() != null)
        {
            if (!EditorUtility.DisplayDialog(
                    "SelectLvDat UI",
                    "Scene đã có SelectLvDatUI. Tạo thêm Canvas mới?",
                    "Tạo thêm",
                    "Hủy"))
                return;
        }

        EnsureEventSystem();

        var canvasGo = CreateCanvas("Canvas_SelectLvDat");
        var ui = canvasGo.AddComponent<SelectLvDatUI>();

        CreateBg(canvasGo.transform);

        var brand = CreateTmp("Txt_Brand", canvasGo.transform, "HEART OF THE NIGHT", 20, TextDim, TextAlignmentOptions.Center);
        PlaceTopCenter(brand.rectTransform, 700f, 28f, -70f);

        var title = CreateTmp("Txt_Title", canvasGo.transform, "SELECT LEVEL", 40, TextCream, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        PlaceTopCenter(title.rectTransform, 600f, 52f, -115f);

        var grid = CreateRect("LevelGrid", canvasGo.transform);
        PlaceCenter(grid, 900f, 320f, new Vector2(0f, -10f));

        CreateLevelSlot(grid, "Btn_Level1", "LEVEL 1", "DatScene", new Vector2(-330f, 40f));
        CreateLevelSlot(grid, "Btn_Level2", "LEVEL 2", "Floor I", new Vector2(-110f, 40f));
        CreateLevelSlot(grid, "Btn_Level3", "LEVEL 3", "Floor II", new Vector2(110f, 40f));
        CreateLevelSlot(grid, "Btn_Level4", "LEVEL 4", "Floor III", new Vector2(330f, 40f));

        CreateMenuButton("Btn_Back", canvasGo.transform, "◀  BACK", new Vector2(0f, -280f), 220f);

        Persist(grid.Find("Btn_Level1")?.GetComponent<Button>(), ui, nameof(SelectLvDatUI.LoadLevel1));
        Persist(grid.Find("Btn_Level2")?.GetComponent<Button>(), ui, nameof(SelectLvDatUI.LoadLevel2));
        Persist(grid.Find("Btn_Level3")?.GetComponent<Button>(), ui, nameof(SelectLvDatUI.LoadLevel3));
        Persist(grid.Find("Btn_Level4")?.GetComponent<Button>(), ui, nameof(SelectLvDatUI.LoadLevel4));
        Persist(canvasGo.transform.Find("Btn_Back")?.GetComponent<Button>(), ui, nameof(SelectLvDatUI.OnBack));

        var so = new SerializedObject(ui);
        so.FindProperty("level1Scene").stringValue = "DatScene";
        so.FindProperty("level2Scene").stringValue = "Khanh_Level1-1";
        so.FindProperty("level3Scene").stringValue = "Khanh_Level2-1";
        so.FindProperty("level4Scene").stringValue = "Khanh_Level3-1";
        so.FindProperty("backSceneName").stringValue = "MenuDat";
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = canvasGo;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[SelectLvDat] Scaffold xong. Save scene. MenuDat Play → SelectLvDat. Thêm scene vào Build Settings.");
    }

    private static void Persist(Button button, SelectLvDatUI ui, string method)
    {
        if (button == null || ui == null) return;
        var action = (UnityAction)System.Delegate.CreateDelegate(typeof(UnityAction), ui, method);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static void CreateLevelSlot(RectTransform parent, string name, string title, string subtitle, Vector2 pos)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200f, 220f);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = SlotBg;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.45f, 0.2f, 0.18f, 1f);
        colors.pressedColor = new Color(0.35f, 0.12f, 0.12f, 1f);
        btn.colors = colors;

        var titleTmp = CreateTmp("Title", rt, title, 22, TextCream, TextAlignmentOptions.Center);
        titleTmp.fontStyle = FontStyles.Bold;
        var titleRt = titleTmp.rectTransform;
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.5f);
        titleRt.sizeDelta = new Vector2(180f, 40f);
        titleRt.anchoredPosition = new Vector2(0f, 30f);

        var sub = CreateTmp("Subtitle", rt, subtitle, 16, TextDim, TextAlignmentOptions.Center);
        var subRt = sub.rectTransform;
        subRt.anchorMin = subRt.anchorMax = new Vector2(0.5f, 0.5f);
        subRt.sizeDelta = new Vector2(180f, 30f);
        subRt.anchoredPosition = new Vector2(0f, -10f);
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

    private static Button CreateMenuButton(string name, Transform parent, string label, Vector2 pos, float width)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, 44f);
        rt.anchoredPosition = pos;

        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.04f);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var tmp = CreateTmp("Label", rt, label, 20, TextCream, TextAlignmentOptions.Center);
        StretchFull(tmp.rectTransform);
        return btn;
    }

    private static GameObject CreateCanvas(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(go, "Create SelectLvDat Canvas");
        return go;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;
        var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
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
}
#endif
