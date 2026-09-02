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

    [Header("Scene")]
    [Tooltip("Trống = tự theo tên scene (Story1→0-1, Story2→1-2, Story3→1-3).")]
    [SerializeField] private string nextLevelScene;
    [SerializeField] private string backSceneName = "mainMenu";

    private void Start()
    {
        if (continueButton != null)
            continueButton.SetActive(false);
        if (backButton != null)
            backButton.SetActive(false);

        if (storyText != null)
            storyText.text = "";

        StartCoroutine(PlayStory());
    }

    private IEnumerator PlayStory()
    {
        if (storyLines != null)
        {
            foreach (string line in storyLines)
            {
                yield return StartCoroutine(TypeLine(line));
                yield return new WaitForSeconds(delayBetweenLines);
            }
        }

        if (continueButton != null)
            continueButton.SetActive(true);
        if (backButton != null)
            backButton.SetActive(true);
    }

    private IEnumerator TypeLine(string line)
    {
        if (storyText == null || string.IsNullOrEmpty(line))
            yield break;

        foreach (char c in line)
        {
            storyText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        storyText.text += "\n\n";
    }

    public void Continue()
    {
        StoryFlow.ContinueFromStory(SceneManager.GetActiveScene().name, nextLevelScene);
    }

    public void BackToMenu()
    {
        StoryFlow.RememberSpawnForNextLevel("");
        string scene = string.IsNullOrEmpty(backSceneName) ? "mainMenu" : backSceneName;
        StoryFlow.LoadScene(scene);
    }
}
