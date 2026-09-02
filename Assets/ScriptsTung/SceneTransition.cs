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

        string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        string loadScene = StoryFlow.ResolveLoadAfterLevel(current, nextSceneName);
        bool leavingLevel = HeartOfTheNight.Hung.DataManager.IsLevelScene(current);
        if (leavingLevel)
        {
            ChapterProgress.UnlockOnLeavingLevel(current);
            var pending = new LevelCompletePending
            {
                nextSceneName = loadScene,
                fadeDuration = fadeDuration,
                delayBeforeFadeIn = delayBeforeFadeIn
            };

            if (LevelCompleteUI.TryShow(pending))
            {
                yield return ScreenFader.Instance.FadeIn(fadeDuration);
                yield break;
            }
        }

        StoryFlow.ApplyDestinationSave(loadScene, "", false, -1);

        // Continuation on ScreenFader so loading + FadeIn survive scene unload.
        ScreenFader.Instance.LoadSceneWithLoading(loadScene, fadeDuration, delayBeforeFadeIn);
    }
}
