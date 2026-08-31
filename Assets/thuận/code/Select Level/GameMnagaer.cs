using UnityEngine;

public class GameMnagaer : MonoBehaviour
{
    [Header("Level")]
    public GameObject chapter1;
    public GameObject chapter2;
    public GameObject chapter3;
    public GameObject menuLevel;
    


    public void Openchapter1()
    {
        menuLevel.SetActive(false);
        chapter1.SetActive(true);
    }

    public void Openchapter2()
    {
        menuLevel.SetActive(false);
        chapter2.SetActive(true);
    }

    public void Openchapter3()
    {
        menuLevel.SetActive(false);
        chapter3.SetActive(true);
    }



    public void Closechapter1()
    {
        chapter1.SetActive(false);
        menuLevel.SetActive(true);
    }

    public void Closechapter2()
    {
        chapter2.SetActive(false);
        menuLevel.SetActive(true);
    }
    public void Closechapter3()
    {
        chapter3.SetActive(false);
        menuLevel.SetActive(true);
    }
}
