using HeartOfTheNight.Hung;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
        var dm = DataManager.EnsureExists();
        if (dm == null)
        {
            Debug.LogError("[MainMenu] Không tạo được DataManager.");
            return;
        }

        dm.SelectSlotAndEnter(DataManager.GetActiveSlotIndex());
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}