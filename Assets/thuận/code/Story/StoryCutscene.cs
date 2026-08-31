using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryCutscene : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text storyText;
    [SerializeField] private GameObject continueButton;
    [SerializeField] private GameObject backButton;

    [Header("Story")]
    [TextArea(2, 5)]
    [SerializeField] private string[] storyLines;

    [Header("Typing")]
    [SerializeField] private float typingSpeed = 0.03f;
    [SerializeField] private float delayBetweenLines = 1.2f;

    private void Start()
    {
        continueButton.SetActive(false);
        backButton.SetActive(false);

        storyText.text = "";

        StartCoroutine(PlayStory());
    }

    private IEnumerator PlayStory()
    {
        foreach (string line in storyLines)
        {
            yield return StartCoroutine(TypeLine(line));

            yield return new WaitForSeconds(delayBetweenLines);
        }

        continueButton.SetActive(true);
        backButton.SetActive(true);
    }

    private IEnumerator TypeLine(string line)
    {
        foreach (char c in line)
        {
            storyText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        storyText.text += "\n\n";
    }

    public void Continue()
    {
        const string next = "khanh_level1-2";
        if (HeartOfTheNight.Hung.DataManager.Instance != null
            && HeartOfTheNight.Hung.DataManager.Instance.Data != null)
        {
            HeartOfTheNight.Hung.DataManager.Instance.Data.currentScene = next;
            HeartOfTheNight.Hung.DataManager.Instance.PrepareForNewScene(next);
            HeartOfTheNight.Hung.DataManager.Instance.ClearCheckpointAfterLeavingLevel();
        }

        SceneManager.LoadScene(next);
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("mainMenu");
    }
}