using UnityEngine;

public class GameMnagaer : MonoBehaviour
{
    [Header("Level")]
    public GameObject chapter1;
    public GameObject menuLevel;
    


    public void Openchapter1()
    {
        menuLevel.SetActive(false);
        chapter1.SetActive(true);
    }
    


    public void Closechapter1()
    {
        chapter1.SetActive(false);
        menuLevel.SetActive(true);
    }
}
