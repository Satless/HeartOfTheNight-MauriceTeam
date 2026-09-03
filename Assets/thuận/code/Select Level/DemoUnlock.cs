using HeartOfTheNight.Hung;
using HeartOfTheNight.Player;
using HeartOfTheNight.Rooms;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nút DEMO trên Select Level + toast. Mở hết màn để hội đồng chọn scene.
/// Vào màn sau khi bật DEMO: đủ súng + chìa để không kẹt cửa khóa.
/// </summary>
public static class DemoUnlock
{
    public const KeyCode Hotkey = KeyCode.F8;
    public const int DemoKeyCount = 9;

    private const string LegacyArmedPrefsPrefix = "DemoUnlock.Armed.";

    public static bool IsArmed
    {
        get
        {
            var data = DataManager.Instance != null ? DataManager.Instance.Data : null;
            return data != null && data.hasSave && data.demoArmed;
        }
    }

    public static void Arm()
    {
        var dm = DataManager.Instance;
        if (dm?.Data == null)
            return;

        dm.Data.demoArmed = true;
        ClearLegacyArmedPrefs(dm.ActiveSlotIndex);
    }

    public static void DisarmForSlot(int slotIndex)
    {
        ClearLegacyArmedPrefs(slotIndex);
        var dm = DataManager.Instance;
        if (dm != null && dm.ActiveSlotIndex == slotIndex && dm.Data != null)
            dm.Data.demoArmed = false;
    }

    public static void EnsureDemoKeys()
    {
        TopUp(KeyType.Blue, DemoKeyCount);
        TopUp(KeyType.Red, DemoKeyCount);
    }

    public static void ApplyLiveWeapons()
    {
        var attack = Object.FindFirstObjectByType<PlayerAttack>();
        if (attack == null)
            return;

        for (int i = 1; i <= 4; i++)
            attack.UnlockWeapon(i);
    }

    public static void EnsureSelectLevelButton(SelectLevelManager owner)
    {
        if (owner == null)
            return;

        var canvas = FindUiCanvas();
        if (canvas == null)
            return;

        if (canvas.transform.Find("DemoUnlockButton") != null)
            return;

        var go = new GameObject("DemoUnlockButton", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(canvas.transform, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(300f, 58f);
        rt.anchoredPosition = new Vector2(-36f, 36f);

        var image = go.GetComponent<Image>();
        image.color = new Color(0.42f, 0.12f, 0.14f, 0.94f);

        var button = go.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(owner.UnlockAllForDemo);

        var textGo = new GameObject("Text", typeof(RectTransform));
        textGo.transform.SetParent(go.transform, false);
        var textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "DEMO  ·  MỞ HẾT MÀN  (F8)";
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.96f, 0.9f, 0.82f, 1f);
        tmp.raycastTarget = false;
    }

    public static void ShowToast(MonoBehaviour host, string message)
    {
        if (host == null)
        {
            Debug.Log("[DemoUnlock] " + message);
            return;
        }

        var canvas = FindUiCanvas();
        if (canvas == null)
        {
            Debug.Log("[DemoUnlock] " + message);
            return;
        }

        var existing = canvas.transform.Find("DemoUnlockToast");
        if (existing != null)
            Object.Destroy(existing.gameObject);

        var go = new GameObject("DemoUnlockToast", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(canvas.transform, false);
        go.transform.SetAsLastSibling();

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.12f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(720f, 56f);
        rt.anchoredPosition = Vector2.zero;

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = message;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.95f, 0.86f, 0.55f, 1f);
        tmp.raycastTarget = false;

        host.StartCoroutine(FadeToast(go.GetComponent<CanvasGroup>()));
    }

    private static void TopUp(KeyType type, int count)
    {
        int have = PlayerKeyInventory.GetCount(type);
        if (have < count)
            PlayerKeyInventory.Add(type, count - have);
    }

    private static void ClearLegacyArmedPrefs(int slotIndex)
    {
        PlayerPrefs.DeleteKey(LegacyArmedPrefsPrefix + Mathf.Clamp(slotIndex, 1, DataManager.SlotCount));
        PlayerPrefs.Save();
    }

    private static System.Collections.IEnumerator FadeToast(CanvasGroup group)
    {
        if (group == null)
            yield break;

        group.alpha = 1f;
        yield return new WaitForSecondsRealtime(2.2f);
        float t = 0f;
        while (t < 0.45f && group != null)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = 1f - t / 0.45f;
            yield return null;
        }

        if (group != null)
            Object.Destroy(group.gameObject);
    }

    private static Canvas FindUiCanvas()
    {
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Canvas fallback = null;
        for (int i = 0; i < canvases.Length; i++)
        {
            Canvas c = canvases[i];
            if (c == null || !c.isActiveAndEnabled)
                continue;
            if (c.renderMode != RenderMode.ScreenSpaceOverlay)
                continue;
            if (c.GetComponentInChildren<HeartOfTheNight.UI.CursorManager>(true) != null)
                continue;
            if (c.name.IndexOf("Cursor", System.StringComparison.OrdinalIgnoreCase) >= 0)
                continue;
            if (c.sortingOrder >= 900)
                continue;

            if (c.name == "Canvas")
                return c;
            fallback = c;
        }

        return fallback;
    }
}
