using UnityEngine;

public class UIMenuMusic : MonoBehaviour
{
    [Header("Menu Music Settings")]
    [Tooltip("Tên track nhạc trong MusicLibrary_New dành cho Menu này (VD: PauseBGM, WinBGM...)")]
    [SerializeField] private string menuTrackName = "PauseBGM";
    [SerializeField] private float fadeDuration = 0.3f;

    private string previousTrackName = "";
    private bool _switched;

    private void OnEnable()
    {
        if (MusicManager_New.Instance != null)
        {
            MusicManager_New.Instance.SaveCurrentMusicTime();
            previousTrackName = MusicManager_New.Instance.CurrentTrackName;
        }

        if (!string.IsNullOrEmpty(previousTrackName) && previousTrackName == menuTrackName)
            return;

        AudioEvents.TriggerMusic(menuTrackName, fadeDuration);
        _switched = true;
    }

    private void OnDisable()
    {
        if (!_switched)
            return;

        _switched = false;

        if (MusicManager_New.Instance != null
            && !string.IsNullOrEmpty(previousTrackName)
            && previousTrackName != menuTrackName)
        {
            MusicManager_New.Instance.PlayMusicResume(previousTrackName, fadeDuration);
        }
    }
}
