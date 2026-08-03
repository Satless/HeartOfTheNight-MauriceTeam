using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectLevelManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button level1Button;
    public Button level2Button;
    public Button level3Button;

    private void Start()
    {
        /*// Level 1 luôn mở
        level1Button.interactable = true;

        // Nếu chưa mở khóa thì sẽ bị khóa
        level2Button.interactable = PlayerPrefs.GetInt("Level2Unlocked", 0) == 1;
        level3Button.interactable = PlayerPrefs.GetInt("Level3Unlocked", 0) == 1;*/
    }

    public void LoadLevel1()
    {
        SceneManager.LoadScene("Khanh_Level0-1");
    }

    public void LoadLevel2()
    {
        SceneManager.LoadScene("Level2");
    }

    public void LoadLevel3()
    {
        SceneManager.LoadScene("Level3");
    }

    public void Back()
    {
        SceneManager.LoadScene("mainMenu");
    }
}