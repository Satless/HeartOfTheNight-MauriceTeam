using UnityEngine;

public class GameMnagaer : MonoBehaviour
{
    [Header("Level")]
    public GameObject level1;
    public GameObject menuLevel;


    public void OpenLevel1()
    {
        menuLevel.SetActive(false);
        level1.SetActive(true);
    }
    


    public void CloseLevel1()
    {
        level1.SetActive(false);
    }
}
