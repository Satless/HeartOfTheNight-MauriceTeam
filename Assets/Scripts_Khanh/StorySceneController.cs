using System.Collections;
using UnityEngine;
using TMPro;

public class StoryController : MonoBehaviour
{
    public TextMeshProUGUI storyText;
    public float paragraphDelay = 3f;

    private string fullStory;

    void Start()
    {
        // Lưu toàn bộ Story đã viết trong TextMeshPro
        fullStory = storyText.text;

        // Xóa Text để bắt đầu hiển thị từng đoạn
        storyText.text = "";

        StartCoroutine(ShowStory());
    }

    IEnumerator ShowStory()
    {
        // Chia Story thành từng đoạn bằng dòng trống
        string[] paragraphs = fullStory.Split(
            new string[] { "\n\n" },
            System.StringSplitOptions.RemoveEmptyEntries
        );

        foreach (string paragraph in paragraphs)
        {
            storyText.text += paragraph + "\n\n";

            yield return new WaitForSeconds(paragraphDelay);
        }
    }
}