using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public GameObject deadScreen;
    public GameObject scoreScreen;

    private void Awake()
    {
        Instance = this;

        deadScreen.SetActive(false);
        scoreScreen.SetActive(false);
    }

    public void ShowDeadScreen()
    {
        Time.timeScale = 0f;
        deadScreen.SetActive(true);
    }

    public void ShowScoreScreen()
    {
        Time.timeScale = 0f;
        scoreScreen.SetActive(true);
    }
}