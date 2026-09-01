using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using HeartOfTheNight.Hung;

public class LevelCompleteManager : MonoBehaviour
{
    [Header("Tên Scene")]
    [SerializeField] private string nextLevelScene;
    [SerializeField] private string homeScene = "mainMenu";

    [Header("Số liệu (để trống = tự tìm trong prefab)")]
    [SerializeField] private TextMeshProUGUI enemiesValueText;
    [SerializeField] private TextMeshProUGUI timeValueText;
    [SerializeField] private TextMeshProUGUI secretsValueText;

    private void Awake()
    {
        AutoBindValueTexts();
    }

    private void Start()
    {
        if (!string.IsNullOrEmpty(LevelStatsTracker.LastSnapshot.sceneName)
            || LevelStatsTracker.LastSnapshot.enemiesTotal > 0
            || LevelStatsTracker.LastSnapshot.timeSeconds > 0f)
        {
            ApplyStats(LevelStatsTracker.LastSnapshot);
        }
    }

    public void ApplyStats(LevelCompleteStats stats)
    {
        AutoBindValueTexts();
        if (enemiesValueText != null)
            enemiesValueText.text = stats.EnemiesText;
        if (timeValueText != null)
            timeValueText.text = stats.TimeText;
        if (secretsValueText != null)
            secretsValueText.text = stats.SecretsText;
    }

    public void NextLevel()
    {
        if (LevelCompleteUI.Instance != null && LevelCompleteUI.IsShowing)
        {
            LevelCompleteUI.Instance.ConfirmNextLevel();
            return;
        }

        if (!string.IsNullOrEmpty(nextLevelScene))
        {
            PrepareAndLoadNextLevel(nextLevelScene);
            return;
        }

        Debug.LogError("Chưa nhập tên Scene của Level tiếp theo!");
    }

    public void BackToHome()
    {
        if (LevelCompleteUI.Instance != null && LevelCompleteUI.IsShowing)
        {
            LevelCompleteUI.Instance.GoHome();
            return;
        }

        string scene = string.IsNullOrEmpty(homeScene) ? "mainMenu" : homeScene;
        if (DataManager.Instance != null)
            DataManager.Instance.CommitFinishedLevelAndLeave();
        LevelEntrance.ClearPendingSpawn();
        LoadScene(scene);
    }

    private static void PrepareAndLoadNextLevel(string next)
    {
        if (DataManager.Instance != null && DataManager.Instance.Data != null)
        {
            DataManager.Instance.Data.currentScene = next;
            DataManager.Instance.PrepareForNewScene(next);
            DataManager.Instance.ClearCheckpointAfterLeavingLevel();
        }

        LevelEntrance.SetPendingSpawn("");
        LoadScene(next);
    }

    private static void LoadScene(string sceneName)
    {
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.LoadSceneWithLoading(sceneName);
        else
            SceneManager.LoadScene(sceneName);
    }

    private void AutoBindValueTexts()
    {
        if (enemiesValueText != null && timeValueText != null && secretsValueText != null)
            return;

        Transform diem = FindNamed(transform, "điểm");
        if (diem == null)
            return;

        var labels = diem.GetComponentsInChildren<TextMeshProUGUI>(true);
        if (labels.Length < 3)
            return;

        if (enemiesValueText == null)
            enemiesValueText = labels[0];
        if (timeValueText == null)
            timeValueText = labels[1];
        if (secretsValueText == null)
            secretsValueText = labels[2];
    }

    private static Transform FindNamed(Transform root, string name)
    {
        if (root.name == name)
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            var found = FindNamed(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }
}
