using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class RoomTransition : MonoBehaviour
{
    public enum TransitionType { SameScene, NextLevel }

    public int nextLevelIndex;

    [Header("Loại chuyển cảnh")]
    public TransitionType transitionType = TransitionType.SameScene;

    [Header("Nếu là Same Scene (Chuyển phòng)")]
    public Transform nextRoomSpawnPoint;
    public RoomDoor targetDoor;

    [Header("Nếu là Next Level (Chuyển Scene)")]
    public string nextSceneName;
    public string spawnIDInNextScene;

    [Header("Hiệu ứng màn hình")]
    [Tooltip("Legacy — ScreenFader dùng chung, không cần gán nữa.")]
    public Image blackScreen;
    [Tooltip("Thời gian fade đen / sáng (giây).")]
    public float fadeDuration = 0.5f;
    [Tooltip("Giữ màn đen ngắn sau khi scene mới load xong.")]
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

        yield return ScreenFader.Instance.FadeOut(fadeDuration);

        if (transitionType == TransitionType.SameScene)
        {
            if (nextRoomSpawnPoint == null)
            {
                Debug.LogError("Chưa gán Next Room Spawn Point!", this);
                yield return ScreenFader.Instance.FadeIn(fadeDuration);
                if (pRb != null) pRb.simulated = true;
                isTransitioning = false;
                yield break;
            }

            playerObj.transform.position = nextRoomSpawnPoint.position;
            if (Camera.main != null)
            {
                Vector3 camPos = Camera.main.transform.position;
                Camera.main.transform.position = new Vector3(
                    nextRoomSpawnPoint.position.x,
                    nextRoomSpawnPoint.position.y,
                    camPos.z);
            }

            if (targetDoor != null) targetDoor.Open(instant: true);

            yield return new WaitForSeconds(0.2f);
            yield return ScreenFader.Instance.FadeIn(fadeDuration);

            if (pRb != null) pRb.simulated = true;
            isTransitioning = false;
        }
        else if (transitionType == TransitionType.NextLevel)
        {
            if (string.IsNullOrEmpty(nextSceneName))
            {
                Debug.LogError("Chưa nhập tên Scene tiếp theo!", this);
                yield return ScreenFader.Instance.FadeIn(fadeDuration);
                if (pRb != null) pRb.simulated = true;
                isTransitioning = false;
                yield break;
            }

            var hp = playerObj.GetComponent<HeartOfTheNight.Player.PlayerHealth>();
            if (hp != null) hp.HealToFull();

            // Static pending không bị Firebase LoadGame ghi đè targetSpawnID trên RAM.
            LevelEntrance.SetPendingSpawn(spawnIDInNextScene);

            if (HeartOfTheNight.Hung.DataManager.Instance != null)
            {
                if (nextLevelIndex > HeartOfTheNight.Hung.DataManager.Instance.Data.maxUnlockedLevel)
                    HeartOfTheNight.Hung.DataManager.Instance.Data.maxUnlockedLevel = nextLevelIndex;

                HeartOfTheNight.Hung.DataManager.Instance.Data.currentScene = nextSceneName;
                HeartOfTheNight.Hung.DataManager.Instance.PrepareForNewScene();
            }

            // Continuation on ScreenFader — dùng timing prefab nếu muốn: truyền -1f.
            ScreenFader.Instance.LoadSceneWithLoading(nextSceneName, fadeDuration, delayBeforeFadeIn);
        }
    }
}
