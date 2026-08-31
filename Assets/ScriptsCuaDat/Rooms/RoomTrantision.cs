using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("Secret")]
    [Tooltip("Bật trên cửa VÀO phòng secret. Đi qua cửa này = tìm thấy 1 SECRET.")]
    [SerializeField] private bool countsAsSecret;

    [Header("Checkpoint")]
    [Tooltip("Next Level: bật = lưu checkpoint khi sang map. Same Scene không dùng ô này.")]
    [SerializeField] private bool saveAsCheckpoint;
    [Tooltip("Same Scene: mặc định luôn lưu checkpoint lúc qua cửa. Bật = không lưu (cửa giả / trap).")]
    [SerializeField] private bool skipCheckpoint;
    [Tooltip("Same Scene: ID LevelEntrance bên kia cửa (nếu có). Để trống = hồi sinh đúng nextRoomSpawnPoint.")]
    [SerializeField] private string checkpointSpawnID;

    [Header("Hiệu ứng màn hình")]
    [Tooltip("Legacy — ScreenFader dùng chung, không cần gán nữa.")]
    public Image blackScreen;
    [Tooltip("Thời gian fade đen / sáng (giây).")]
    public float fadeDuration = 0.5f;
    [Tooltip("Giữ màn đen ngắn sau khi scene mới load xong.")]
    public float delayBeforeFadeIn = 0.2f;

    private bool isTransitioning;

    public bool CountsAsSecret => countsAsSecret;

    public string SecretId => SceneManager.GetActiveScene().name + "_" + gameObject.name;

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

            RegisterSecretIfNeeded();

            if (!skipCheckpoint)
            {
                TrySaveCheckpoint(
                    playerObj,
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().name,
                    checkpointSpawnID,
                    nextRoomSpawnPoint.position);
            }

            yield return new WaitForSeconds(0.2f);
            yield return ScreenFader.Instance.FadeIn(fadeDuration);

            if (pRb != null) pRb.simulated = true;
            isTransitioning = false;
        }
        else if (transitionType == TransitionType.NextLevel)
        {
            string next = ResolveNextLevelSceneName();
            if (string.IsNullOrEmpty(next))
            {
                Debug.LogError("Chưa nhập tên Scene tiếp theo!", this);
                yield return ScreenFader.Instance.FadeIn(fadeDuration);
                if (pRb != null) pRb.simulated = true;
                isTransitioning = false;
                yield break;
            }

            var hp = playerObj.GetComponent<HeartOfTheNight.Player.PlayerHealth>();
            if (hp != null && !saveAsCheckpoint)
                hp.HealToFull();

            ChapterProgress.UnlockOnLeavingLevel(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
            RegisterSecretIfNeeded();

            var pending = new LevelCompletePending
            {
                nextSceneName = next,
                spawnIDInNextScene = spawnIDInNextScene,
                fadeDuration = fadeDuration,
                delayBeforeFadeIn = delayBeforeFadeIn,
                nextLevelIndex = nextLevelIndex,
                saveAsCheckpoint = saveAsCheckpoint,
                playerHealth = hp != null ? hp.GetCurrentHealth() : -1
            };

            if (LevelCompleteUI.TryShow(pending))
            {
                yield return ScreenFader.Instance.FadeIn(fadeDuration);
                yield break;
            }

            if (HeartOfTheNight.Hung.DataManager.Instance != null)
            {
                HeartOfTheNight.Hung.DataManager.Instance.Data.currentScene = next;
                HeartOfTheNight.Hung.DataManager.Instance.PrepareForNewScene(next);

                if (saveAsCheckpoint)
                    TrySaveCheckpoint(playerObj, next, spawnIDInNextScene, Vector3.zero);
                else
                    HeartOfTheNight.Hung.DataManager.Instance.ClearCheckpointAfterLeavingLevel();
            }

            LevelEntrance.SetPendingSpawn(spawnIDInNextScene);
            ScreenFader.Instance.LoadSceneWithLoading(next, fadeDuration, delayBeforeFadeIn);
        }
    }

    private string ResolveNextLevelSceneName()
    {
        if (!string.IsNullOrEmpty(nextSceneName))
            return nextSceneName;

        var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        var idx = ChapterProgress.IndexOf(current);
        if (idx < 0)
            return nextSceneName;

        if (idx + 1 < ChapterProgress.TotalSceneCount)
            return ChapterProgress.GetSceneAt(idx + 1);

        return HeartOfTheNight.Hung.DataManager.SelectLevelScene;
    }

    private void RegisterSecretIfNeeded()
    {
        if (!countsAsSecret)
            return;

        LevelStatsTracker.DiscoverSecret(SecretId);
    }

    private void TrySaveCheckpoint(GameObject playerObj, string sceneName, string spawnId, Vector3 worldPosition)
    {
        if (HeartOfTheNight.Hung.DataManager.Instance == null) return;

        var hp = playerObj != null ? playerObj.GetComponent<HeartOfTheNight.Player.PlayerHealth>() : null;
        int health = hp != null ? hp.GetCurrentHealth() : -1;
        HeartOfTheNight.Hung.DataManager.Instance.SaveCheckpoint(sceneName, spawnId, worldPosition, health);
    }
}
