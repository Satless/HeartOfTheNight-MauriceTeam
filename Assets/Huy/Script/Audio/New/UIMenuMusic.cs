using UnityEngine;

public class UIMenuMusic : MonoBehaviour
{
    [Header("Menu Music Settings")]
    [Tooltip("Tên track nhạc trong MusicLibrary_New dành riêng cho Menu này (VD: WinBGM, PauseBGM, GameOverBGM)")]
    [SerializeField] private string menuTrackName = "DefaultBGM";
    [SerializeField] private float fadeDuration = 0.3f;

    private string previousTrackName = "";

    private void OnEnable()
    {
        if (MusicManager_New.Instance != null)
        {
            // 1. Lưu lại thời gian bài nhạc màn chơi đang phát dở
            MusicManager_New.Instance.SaveCurrentMusicTime();
            previousTrackName = MusicManager_New.Instance.CurrentTrackName;
        }

        // 2. Chuyển sang nhạc riêng của Menu này
        AudioEvents.TriggerMusic(menuTrackName, fadeDuration);
    }

    private void OnDisable()
    {
        // 3. Khi đóng Menu, tiếp tục phát bài nhạc cũ ngay tại thời điểm đã dừng
        if (MusicManager_New.Instance != null && !string.IsNullOrEmpty(previousTrackName))
        {
            MusicManager_New.Instance.PlayMusicResume(previousTrackName, fadeDuration);
        }
    }
}