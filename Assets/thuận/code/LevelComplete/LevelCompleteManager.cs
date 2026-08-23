using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelCompleteManager : MonoBehaviour
{
    [Header("Tên Scene")]
    [SerializeField] private string nextLevelScene;
    [SerializeField] private string homeScene ;

    // Nút NEXT LEVEL
    public void NextLevel()
    {
        if (!string.IsNullOrEmpty(nextLevelScene))
        {
            SceneManager.LoadScene(nextLevelScene);
        }
        else
        {
            Debug.LogError("Chưa nhập tên Scene của Level tiếp theo!");
        }
    }

    // Nút BACK TO HOME
    public void BackToHome()
    {
        SceneManager.LoadScene(homeScene);
    }
}