
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class EndGameStory : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text storyText;
    [SerializeField] private Image fadePanel;

    [Header("Background")]
    [SerializeField] private Sprite background1;
    [SerializeField] private Sprite background2;

    [Header("Story Settings")]
    [SerializeField] private float textSpeed = 0.03f;
    [SerializeField] private float delayAfterStory = 2f;
    [SerializeField] private float fadeDuration = 1.5f;

    [Header("Scene")]
    [SerializeField] private string mainMenuScene = "MainMenu";

    private string story1 =
        "Sau một trận chiến dài, bạn cuối cùng cũng đánh bại được Heart Of The Night. " +
        "Nhưng ngay sau đó, toàn bộ công trình bắt đầu rung chuyển. " +
        "Những cỗ máy ngừng hoạt động, các bức tường sụp đổ và nguồn năng lượng bên trong dần mất kiểm soát. " +
        "Bạn lập tức chạy qua những khu vực đang sụp đổ để tìm đường thoát. " +
        "Cuối cùng, bạn thoát ra ngoài ngay trước khi toàn bộ công trình phát nổ. " +
        "Nguồn năng lượng tạo ra màn đêm biến mất. " +
        "Bầu trời dần trở lại bình thường. " +
        "Màn đêm bất tận cuối cùng cũng chấm dứt. " +
        "Nhưng quê hương của bạn đã trở thành một vùng đất hoang tàn. " +
        "Quái vật vẫn tồn tại, môi trường bị ô nhiễm và không còn ai để trở về. " +
        "Không còn nơi nào để đi, bạn tìm thấy một con tàu và bắt đầu hành trình trên đại dương.";

    private string story2 =
        "Sau một thời gian dài, một vùng đất mới xuất hiện ở phía chân trời. " +
        "Bạn tiến về phía đó, hy vọng tìm được một nơi để bắt đầu lại. " +
        "Nhưng khi đến gần, bạn nhận ra nơi đây cũng đang chìm trong một màn đêm bí ẩn. " +
        "Bạn siết chặt vũ khí và nhìn về phía trước. " +
        "Cuộc hành trình chưa kết thúc.";

    private void Start()
    {
        StartCoroutine(PlayEndStory());
    }

    private IEnumerator PlayEndStory()
    {
        // Background đầu tiên
        background.sprite = background1;

        // Đảm bảo FadePanel trong suốt
        SetFadeAlpha(0f);

        // Xóa text
        storyText.text = "";

        // Chạy story 1
        yield return StartCoroutine(TypeText(story1));

        // Chờ một chút
        yield return new WaitForSeconds(delayAfterStory);

        // Fade sang background 2
        yield return StartCoroutine(Fade(0f, 1f));

        // Đổi background
        background.sprite = background2;

        // Xóa story cũ
        storyText.text = "";

        // Fade trở lại
        yield return StartCoroutine(Fade(1f, 0f));

        // Chạy story 2
        yield return StartCoroutine(TypeText(story2));

        // Chờ
        yield return new WaitForSeconds(delayAfterStory);

        // Hiện nút
        ShowButtons();
    }

    private IEnumerator TypeText(string text)
    {
        storyText.text = "";

        foreach (char letter in text)
        {
            storyText.text += letter;

            yield return new WaitForSeconds(textSpeed);
        }
    }

    private IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float timer = 0f;

        Color color = fadePanel.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(
                startAlpha,
                endAlpha,
                timer / fadeDuration
            );

            color.a = alpha;
            fadePanel.color = color;

            yield return null;
        }

        color.a = endAlpha;
        fadePanel.color = color;
    }

    private void SetFadeAlpha(float alpha)
    {
        Color color = fadePanel.color;
        color.a = alpha;
        fadePanel.color = color;
    }

    private void ShowButtons()
    {
        GameObject buttons = GameObject.Find("Buttons");

        if (buttons != null)
        {
            buttons.SetActive(true);
        }
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("mainMenu");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");

        Application.Quit();
    }
}

