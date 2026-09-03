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

    // ==============================
    // BIẾN SKIP
    // ==============================

    private Coroutine storyCoroutine;

    // Đang chạy chữ
    private bool isTyping = false;

    // Cho biết người chơi vừa click
    private bool skipRequested = false;

    // Dòng hiện tại
    private string currentLine = "";

    private void Start()
    {
        if (continueButton != null)
            continueButton.SetActive(false);

        if (backButton != null)
            backButton.SetActive(false);

        if (storyText != null)
            storyText.text = "";

        storyCoroutine = StartCoroutine(PlayStory());
    }

    private void Update()
    {
        // Click chuột trái
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    private void HandleMouseClick()
    {
        // Nếu đang gõ chữ
        if (isTyping)
        {
            // Lần click đầu tiên:
            // Hiện toàn bộ dòng ngay lập tức
            skipRequested = true;
        }
    }

    private IEnumerator PlayStory()
    {
        if (storyLines != null)
        {
            foreach (string line in storyLines)
            {
                currentLine = line;

                // Reset trạng thái skip
                skipRequested = false;

                // Gõ dòng
                yield return StartCoroutine(TypeLine(line));

                // Chờ giữa các dòng
                // Nhưng vẫn cho phép click để bỏ qua thời gian chờ
                float timer = 0f;

                while (timer < delayBetweenLines)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        // Skip luôn thời gian chờ
                        break;
                    }

                    timer += Time.deltaTime;
                    yield return null;
                }
            }
        }

        // Đã hết story
        if (continueButton != null)
            continueButton.SetActive(true);

        if (backButton != null)
            backButton.SetActive(true);
    }

    private IEnumerator TypeLine(string line)
    {
        if (storyText == null || string.IsNullOrEmpty(line))
            yield break;

        isTyping = true;
        storyText.text = "";

        foreach (char c in line)
        {
            // Nếu click chuột trong lúc chữ đang chạy
            if (skipRequested)
            {
                // Hiện toàn bộ dòng
                storyText.text = line;

                isTyping = false;
                skipRequested = false;

                yield break;
            }

            storyText.text += c;

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        // Xuống dòng
        storyText.text += "\n\n";
    }

    // ==============================
    // CONTINUE
    // ==============================

    public void Continue()
    {
        StoryFlow.ContinueFromStory(
            SceneManager.GetActiveScene().name,
            nextLevelScene
        );
    }

    // ==============================
    // BACK TO MENU
    // ==============================

    public void BackToMenu()
    {
        StoryFlow.RememberSpawnForNextLevel("");

        string scene = string.IsNullOrEmpty(backSceneName)
            ? "mainMenu"
            : backSceneName;

        StoryFlow.LoadScene(scene);
    }
}