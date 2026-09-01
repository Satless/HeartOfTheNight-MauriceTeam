using System.Collections.Generic;
using HeartOfTheNight.Hung;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectLevelManager : MonoBehaviour
{
    [Header("Panel cũ — Chapter1 / Chapter2 / Chapter3")]
    [SerializeField] private Transform chapter1MissionsRoot;
    [SerializeField] private Transform chapter2MissionsRoot;
    [SerializeField] private Transform chapter3MissionsRoot;

    [Header("Continue / Bỏ màn đang chơi dở")]
    [SerializeField] private ContinueInProgressUI continuePopup;

    [Header("Nút cũ (giữ reference scene)")]
    public Button level1Button;
    public Button level2Button;
    public Button level3Button;
    public Button level4Button;
    public Button level5Button;
    public Button level6Button;

    [Header("Level scenes — Chapter 1")]
    [SerializeField] private string level1Scene = "Khanh_Level0-1";
    [SerializeField] private string level2Scene = "Khanh_Level1-1";
    [SerializeField] private string level3Scene = "Khanh_Level2-1";
    [SerializeField] private string level4Scene = "Khanh_Level3-1";
    [SerializeField] private string level5Scene = "Khanh_Level4-1";
    [SerializeField] private string level6Scene = "Khanh_Level5-1";

    private void Start()
    {
        var dm = DataManager.EnsureExists();
        if (dm != null && dm.Data != null && dm.Data.hasSave)
            ChapterProgress.ApplyFromSave(dm.Data);

        BindChapterButtons(chapter1MissionsRoot, ChapterProgress.Chapter1Scenes);
        BindChapterButtons(chapter2MissionsRoot, ChapterProgress.Chapter2Scenes);
        BindChapterButtons(chapter3MissionsRoot, ChapterProgress.Chapter3Scenes);
        RefreshUnlocks();
        ApplyChapterMenuLocks();
        EnsureContinuePopup().TryShowIfInProgress();
    }

    private ContinueInProgressUI EnsureContinuePopup()
    {
        if (continuePopup != null)
            return continuePopup;

        continuePopup = FindFirstObjectByType<ContinueInProgressUI>();
        if (continuePopup != null)
            return continuePopup;

        continuePopup = gameObject.AddComponent<ContinueInProgressUI>();
        return continuePopup;
    }

    private void BindChapterButtons(Transform panel, string[] scenes)
    {
        if (panel == null || scenes == null || scenes.Length == 0)
            return;

        var buttons = CollectLevelButtons(panel);
        if (buttons.Count == 0)
            return;

        var firstRt = buttons[0].transform as RectTransform;
        var lastRt = buttons[buttons.Count - 1].transform as RectTransform;
        float topY = firstRt != null ? firstRt.anchoredPosition.y : 0f;
        float bottomY = lastRt != null ? lastRt.anchoredPosition.y : 0f;
        int originalCount = buttons.Count;

        EnsureButtonCount(panel, buttons, scenes.Length);
        if (scenes.Length > originalCount)
            RelayoutUsedButtons(buttons, scenes.Length, topY, bottomY);

        for (int i = 0; i < buttons.Count; i++)
        {
            Button button = buttons[i];
            if (button == null)
                continue;

            bool used = i < scenes.Length;
            button.gameObject.SetActive(used);
            if (!used)
                continue;

            string scene = scenes[i];
            button.gameObject.name = ChapterProgress.DisplayName(scene);
            button.interactable = ChapterProgress.IsUnlocked(scene);
            MutePersistentClicks(button);
            button.onClick = new Button.ButtonClickedEvent();
            string captured = scene;
            button.onClick.AddListener(() => RequestEnterLevel(captured));

            var label = ChapterProgress.DisplayName(scene);
            var tmp = button.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
                tmp.text = label;
        }
    }

    private static List<Button> CollectLevelButtons(Transform panel)
    {
        var result = new List<Button>();
        var all = panel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
                continue;
            if (!all[i].gameObject.name.StartsWith("Level", System.StringComparison.OrdinalIgnoreCase))
                continue;
            result.Add(all[i]);
        }

        result.Sort((a, b) =>
        {
            var ra = a.transform as RectTransform;
            var rb = b.transform as RectTransform;
            float ya = ra != null ? ra.anchoredPosition.y : 0f;
            float yb = rb != null ? rb.anchoredPosition.y : 0f;
            return yb.CompareTo(ya);
        });
        return result;
    }

    private static void RelayoutUsedButtons(List<Button> buttons, int usedCount, float topY, float bottomY)
    {
        if (usedCount <= 1)
            return;

        for (int i = 0; i < usedCount; i++)
        {
            var rt = buttons[i].transform as RectTransform;
            if (rt == null)
                continue;
            float t = i / (float)(usedCount - 1);
            var pos = rt.anchoredPosition;
            pos.y = Mathf.Lerp(topY, bottomY, t);
            rt.anchoredPosition = pos;
        }
    }

    private static void EnsureButtonCount(Transform panel, List<Button> buttons, int needed)
    {
        if (buttons.Count >= needed)
            return;

        var template = buttons[buttons.Count - 1];
        float step = 173f;
        if (buttons.Count >= 2)
        {
            var a = buttons[buttons.Count - 2].transform as RectTransform;
            var b = template.transform as RectTransform;
            if (a != null && b != null)
            {
                float dy = Mathf.Abs(a.anchoredPosition.y - b.anchoredPosition.y);
                if (dy > 20f)
                    step = dy;
            }
        }

        while (buttons.Count < needed)
        {
            var clone = Object.Instantiate(template.gameObject, panel);
            clone.SetActive(true);
            var crt = clone.GetComponent<RectTransform>();
            var last = buttons[buttons.Count - 1].transform as RectTransform;
            if (crt != null && last != null)
                crt.anchoredPosition = last.anchoredPosition + Vector2.down * step;

            var button = clone.GetComponent<Button>();
            if (button == null)
                break;
            buttons.Add(button);
        }
    }

    private void ApplyChapterMenuLocks()
    {
        var gm = FindFirstObjectByType<GameMnagaer>();
        if (gm == null || gm.menuLevel == null)
            return;

        bool ch2 = ChapterProgress.IsUnlocked(ChapterProgress.Chapter2Scenes[0]);
        bool ch3 = ChapterProgress.IsUnlocked(ChapterProgress.Chapter3Scenes[0]);
        var buttons = gm.menuLevel.GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            string n = buttons[i].gameObject.name;
            if (n.StartsWith("CHAPTER 2", System.StringComparison.OrdinalIgnoreCase)
                || n.Equals("Chapter 2", System.StringComparison.OrdinalIgnoreCase))
                buttons[i].interactable = ch2;
            else if (n.StartsWith("CHAPTER 3", System.StringComparison.OrdinalIgnoreCase)
                     || n.Equals("Chapter 3", System.StringComparison.OrdinalIgnoreCase))
                buttons[i].interactable = ch3;
        }
    }

    private static void MutePersistentClicks(Button button)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        int count = button.onClick.GetPersistentEventCount();
        for (int i = 0; i < count; i++)
            button.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
    }

    private void RefreshUnlocks()
    {
        ApplyUnlock(level1Button, level1Scene);
        ApplyUnlock(level2Button, level2Scene);
        ApplyUnlock(level3Button, level3Scene);
        ApplyUnlock(level4Button, level4Scene);
        ApplyUnlock(level5Button, level5Scene);
        ApplyUnlock(level6Button, level6Scene);
    }

    private static void ApplyUnlock(Button button, string sceneName)
    {
        if (button == null)
            return;

        button.interactable = ChapterProgress.IsUnlocked(sceneName);
    }

    public void LoadLevel1() => Load(level1Scene);
    public void LoadLevel2() => Load(level2Scene);
    public void LoadLevel3() => Load(level3Scene);
    public void LoadLevel4() => Load(level4Scene);
    public void LoadLevel5() => Load(level5Scene);
    public void LoadLevel6() => Load(level6Scene);

    public void RequestEnterLevel(string sceneName) => Load(sceneName);

    public void Back()
    {
        if (SoundManager_New.Instance != null)
            SoundManager_New.Instance.PlaySound2DFromPath("UI/Buttons/Cancel");

        SceneManager.LoadScene("mainMenu");
    }

    private void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SelectLevel] Scene name trống.");
            return;
        }

        var dm = HeartOfTheNight.Hung.DataManager.EnsureExists();
        if (dm != null && dm.HasInProgress())
        {
            EnsureContinuePopup().TryShowIfInProgress();
            Debug.Log("[SelectLevel] Còn màn đang chơi dở — hiện popup Continue/Bỏ trước.");
            return;
        }

        if (dm != null)
        {
            dm.PrepareForNewScene(sceneName);
            LevelEntrance.ClearPendingSpawn();
        }

        if (ScreenFader.Instance != null)
            ScreenFader.Instance.LoadSceneWithLoading(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }
}
