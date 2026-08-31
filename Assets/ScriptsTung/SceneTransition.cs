using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SceneTransition : MonoBehaviour
{
    [Header("Tên Scene muốn chuyển tới")]
    public string nextSceneName;

    [Header("Cài đặt Hiệu ứng")]
    [Tooltip("Legacy — ScreenFader dùng chung, không cần gán nữa.")]
    public Image blackScreen;

    [Tooltip("Thời gian màn hình tối đi / sáng lên (giây)")]
    public float fadeDuration = 0.5f;

    [Tooltip("Thời gian giữ màn hình đen trước khi sáng lên ở Scene mới (giây)")]
    public float delayBeforeFadeIn = 0.2f;

    private bool isTransitioning;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Transform root = collision.transform.root;
        if (!root.CompareTag("Player") || isTransitioning) return;

        StartCoroutine(TransitionRoutine(root.gameObject));
    }

    private IEnumerator TransitionRoutine(GameObject playerObj)
    {
        isTransitioning = true;

        Rigidbody2D pRb = playerObj.GetComponent<Rigidbody2D>();
        if (pRb != null)
        {
            pRb.linearVelocity = Vector2.zero;
            pRb.simulated = false;
        }

        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("Chưa nhập tên Scene tiếp theo!", this);
            isTransitioning = false;
            if (pRb != null) pRb.simulated = true;
            yield break;
        }

        yield return ScreenFader.Instance.FadeOut(fadeDuration);

        bool leavingLevel = HeartOfTheNight.Hung.DataManager.IsLevelScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        if (leavingLevel)
        {
            ChapterProgress.UnlockIfChapterScene(nextSceneName);
            var pending = new LevelCompletePending
            {
                nextSceneName = nextSceneName,
                fadeDuration = fadeDuration,
                delayBeforeFadeIn = delayBeforeFadeIn
            };

            if (LevelCompleteUI.TryShow(pending))
            {
                yield return ScreenFader.Instance.FadeIn(fadeDuration);
                yield break;
            }
        }

        if (HeartOfTheNight.Hung.DataManager.Instance != null
            && HeartOfTheNight.Hung.DataManager.Instance.Data != null)
        {
            HeartOfTheNight.Hung.DataManager.Instance.Data.currentScene = nextSceneName;
            HeartOfTheNight.Hung.DataManager.Instance.PrepareForNewScene(nextSceneName);
            HeartOfTheNight.Hung.DataManager.Instance.ClearCheckpointAfterLeavingLevel();
        }

        // Continuation on ScreenFader so loading + FadeIn survive scene unload.
        ScreenFader.Instance.LoadSceneWithLoading(nextSceneName, fadeDuration, delayBeforeFadeIn);
    }
}
