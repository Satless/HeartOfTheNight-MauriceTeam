using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameData
{
    public int playerHealth = 100;
    public string currentScene = "Level_1_1";
    public string targetSpawnID = ""; // Thêm dòng này
    public List<string> clearedRooms = new List<string>();
    //sau này thêm các dữ liệu tiếp theo...
}

// Class quản lý sống xuyên Scene
public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public GameData Data = new GameData();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Chuẩn bị sẵn API cho Firebase
    public string ExportForFirebase() => JsonUtility.ToJson(Data);
    public void ImportFromFirebase(string json) => Data = JsonUtility.FromJson<GameData>(json);
}