using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectLevelManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button level1Button;
    public Button level2Button;
    public Button level3Button;
    public Button level4Button;
    public Button level5Button;

    [Header("Level scenes — Chapter 1 / Floor 1")]
    [SerializeField] private string level1Scene = "Khanh_Level0-1";
    [SerializeField] private string level2Scene = "Khanh_Level1-1";
    [SerializeField] private string level3Scene = "Khanh_Level2-1";
    [SerializeField] private string level4Scene = "Khanh_Level3-1";
    [SerializeField] private string level5Scene = "Khanh_Level4-1";

    private void Start()
    {
        EnsureFifthButton();
        RelayoutChapter1Buttons();
        RefreshUnlocks();
    }

    private void RefreshUnlocks()
    {
        ApplyUnlock(level1Button, level1Scene);
        ApplyUnlock(level2Button, level2Scene);
        ApplyUnlock(level3Button, level3Scene);
        ApplyUnlock(level4Button, level4Scene);
        ApplyUnlock(level5Button, level5Scene);
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

    public void Back()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("mainMenu");
    }

    private static void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SelectLevel] Scene name trống.");
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }

    private void EnsureFifthButton()
    {
        if (level5Button != null || level4Button == null)
            return;

        var parent = level4Button.transform.parent;
        var existing = parent.Find("Level 1-5");
        if (existing != null)
        {
            level5Button = existing.GetComponent<Button>();
            SetButtonLabel(level5Button, "LEVEL 1-5");
            WireLoadLevel5(level5Button);
            return;
        }

        var clone = Instantiate(level4Button.gameObject, parent);
        clone.name = "Level 1-5";
        clone.transform.SetSiblingIndex(level4Button.transform.GetSiblingIndex() + 1);

        level5Button = clone.GetComponent<Button>();
        SetButtonLabel(level5Button, "LEVEL 1-5");
        WireLoadLevel5(level5Button);
    }

    private void WireLoadLevel5(Button button)
    {
        if (button == null)
            return;

        button.onClick = new Button.ButtonClickedEvent();
        button.onClick.AddListener(LoadLevel5);
    }

    private void RelayoutChapter1Buttons()
    {
        var buttons = new[] { level1Button, level2Button, level3Button, level4Button, level5Button };
        var labels = new[] { "LEVEL 1-1", "LEVEL 1-2", "LEVEL 1-3", "LEVEL 1-4", "LEVEL 1-5" };

        const float startY = 210f;
        const float spacing = 118f;
        const float height = 88f;

        for (var i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            var rt = buttons[i].GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, startY - i * spacing);
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
            SetButtonLabel(buttons[i], labels[i]);
        }
    }

    private static void SetButtonLabel(Button button, string text)
    {
        if (button == null)
            return;

        var tmp = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
            tmp.text = text;
    }
}
