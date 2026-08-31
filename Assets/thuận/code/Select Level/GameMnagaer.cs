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
        if (menuLevel != null) menuLevel.SetActive(false);
        if (chapter1 != null) chapter1.SetActive(true);
    }

    public void Openchapter2()
    {
        if (!ChapterProgress.IsUnlocked(ChapterProgress.Chapter2Scenes[0]))
            return;

        if (menuLevel != null) menuLevel.SetActive(false);
        if (chapter2 != null) chapter2.SetActive(true);
    }

    public void Openchapter3()
    {
        if (!ChapterProgress.IsUnlocked(ChapterProgress.Chapter3Scenes[0]))
            return;

        if (menuLevel != null) menuLevel.SetActive(false);
        if (chapter3 != null) chapter3.SetActive(true);
    }

    public void Closechapter1()
    {
        if (chapter1 != null) chapter1.SetActive(false);
        if (menuLevel != null) menuLevel.SetActive(true);
    }

    public void Closechapter2()
    {
        if (chapter2 != null) chapter2.SetActive(false);
        if (menuLevel != null) menuLevel.SetActive(true);
    }

    public void Closechapter3()
    {
        if (chapter3 != null) chapter3.SetActive(false);
        if (menuLevel != null) menuLevel.SetActive(true);
    }
}
