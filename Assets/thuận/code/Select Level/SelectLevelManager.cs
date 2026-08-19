using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectLevelManager : MonoBehaviour
{
    [Header("Buttons")]
    public Button level1Button;
    public Button level2Button;
    public Button level3Button;
    public Button level4Button;

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
        SceneManager.LoadScene("Khanh_Level1-1");
    }

    public void LoadLevel2()
    {
        SceneManager.LoadScene("Khanh_Level2-1");
    }

    public void LoadLevel3()
    {
        SceneManager.LoadScene("Khanh_Level3-1");
    }
    public void LoadLevel4()
    {
        SceneManager.LoadScene("Khanh_Level4-1");
    }

    public void Back()
    {
        if (SoundManager_New.Instance != null)
        {
            SoundManager_New.Instance.PlaySound2DFromPath("UI/Buttons/Cancel");
        }

        SceneManager.LoadScene("mainMenu");
    }
}