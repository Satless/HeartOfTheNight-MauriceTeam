using UnityEngine;

/// <summary>Snapshot thống kê một màn lúc hiện Level Complete.</summary>
public struct LevelCompleteStats
{
    public int enemiesKilled;
    public int enemiesTotal;
    public int secretsFound;
    public int secretsTotal;
    public float timeSeconds;
    public string sceneName;

    public string EnemiesText => $"{Mathf.Max(0, enemiesKilled)}/{Mathf.Max(0, enemiesTotal)}";

    public string TimeText => HeartOfTheNight.Hung.DataManager.FormatLevelTime(timeSeconds);

    public string SecretsText => $"{Mathf.Max(0, secretsFound)}/{Mathf.Max(0, secretsTotal)}";
}
