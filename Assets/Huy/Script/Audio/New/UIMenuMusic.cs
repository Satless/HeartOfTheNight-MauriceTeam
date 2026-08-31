using UnityEngine;

public class UIMenuMusic : MonoBehaviour
{
    [Header("Menu Music Settings")]
    [Tooltip("Tên track nhạc trong MusicLibrary_New dành cho Menu này (VD: PauseBGM, WinBGM...)")]
    [SerializeField] private string menuTrackName = "PauseBGM";
    [SerializeField] private float fadeDuration = 0.3f;

    private string previousTrackName = "";

    private void OnEnable()
    {
        if (MusicManager_New.Instance != null)
        {
            // 1. Lưu lại thời điểm bài nhạc màn chơi đang chạy dở
            MusicManager_New.Instance.SaveCurrentMusicTime();
            previousTrackName = MusicManager_New.Instance.CurrentTrackName;
        }

        // 2. Chuyển sang nhạc của Menu này
        AudioEvents.TriggerMusic(menuTrackName, fadeDuration);
    }

    private void OnDisable()
    {
        // 3. Khi đóng Menu, tiếp tục phát bài nhạc màn chơi từ vị trí đã dừng
        if (MusicManager_New.Instance != null && !string.IsNullOrEmpty(previousTrackName))
        {
            MusicManager_New.Instance.PlayMusicResume(previousTrackName, fadeDuration);
        }
    }
}