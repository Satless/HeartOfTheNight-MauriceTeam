using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectLevelManager : MonoBehaviour
{
    [Header("UI mới — Chapter 1 missions")]
    [SerializeField] private Transform chapter1MissionsRoot;

    [Header("Nút cũ (panel ẩn, giữ để không mất reference scene)")]
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
        BindChapter1Missions();
        RefreshUnlocks();
    }

    private void BindChapter1Missions()
    {
        var root = chapter1MissionsRoot != null
            ? chapter1MissionsRoot
            : FindChapter1MissionsRoot();

        if (root == null)
        {
            Debug.LogWarning("[SelectLevel] Không thấy Chapter 1 missions trên Select Level (1).");
            return;
        }

        var hovers = root.GetComponentsInChildren<MissionHover>(true);
        var scenes = ChapterProgress.Chapter1Scenes;
        var count = Mathf.Min(hovers.Length, scenes.Length);
        for (var i = 0; i < count; i++)
            hovers[i].Configure(scenes[i]);
    }

    private static Transform FindChapter1MissionsRoot()
    {
        var select = GameObject.Find("Select Level (1)");
        if (select == null)
            return null;

        return select.transform.Find("MenuLevel/Chapter/Chapter 1");
    }

    private void RefreshUnlocks()
    {
        ApplyUnlock(level1Button, level1Scene);
        ApplyUnlock(level2Button, level2Scene);
        ApplyUnlock(level3Button, level3Scene);
        ApplyUnlock(level4Button, level4Scene);
        ApplyUnlock(level5Button, level5Scene);

        var hovers = FindObjectsByType<MissionHover>(FindObjectsSortMode.None);
        for (var i = 0; i < hovers.Length; i++)
            hovers[i].RefreshLock();
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
        if (SoundManager_New.Instance != null)
        {
            SoundManager_New.Instance.PlaySound2DFromPath("UI/Buttons/Cancel");
        }

        SceneManager.LoadScene("mainMenu");
    }

    private static void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SelectLevel] Scene name trống.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
