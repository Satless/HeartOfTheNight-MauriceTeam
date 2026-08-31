using System.Collections;
using UnityEngine;

public class MusicManager_New : MonoBehaviour
{
    public static MusicManager_New Instance;

    // Property để các script khác (như UIMenuMusic) lấy được tên track đang phát
    public string CurrentTrackName => currentTrackName;

    [SerializeField] private MusicLibrary_New musicLibrary;
    [SerializeField] private AudioSource musicSource;

    private Coroutine fadeCoroutine;
    private string currentTrackName = "";
    private float savedTrackTime = 0f;

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); }
        else { Instance = this; DontDestroyOnLoad(gameObject); }

        // Đảm bảo MusicSource không bị ảnh hưởng bởi AudioListener.pause
        if (musicSource != null)
        {
            musicSource.ignoreListenerPause = true;
        }
    }

    private void OnEnable() => AudioEvents.OnPlayMusic += PlayMusic;
    private void OnDisable() => AudioEvents.OnPlayMusic -= PlayMusic;

    // Phát nhạc mới (phát từ đầu)
    public void PlayMusic(string trackName, float fadeDuration = 0.5f)
    {
        if (currentTrackName == trackName && musicSource.isPlaying) return;

        AudioClip nextClip = musicLibrary.GetClipFromName(trackName);
        if (nextClip == null) return;

        currentTrackName = trackName;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(AnimateMusicCrossfade(nextClip, fadeDuration, 0f));
    }

    // Phát nhạc tiếp tục từ thời điểm đã dừng (dùng cho khi tắt Pause Menu)
    public void PlayMusicResume(string trackName, float fadeDuration = 0.3f)
    {
        AudioClip nextClip = musicLibrary.GetClipFromName(trackName);
        if (nextClip == null) return;

        currentTrackName = trackName;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(AnimateMusicCrossfade(nextClip, fadeDuration, savedTrackTime));
    }

    // Lưu vị trí thời gian của bài nhạc hiện tại trước khi tạm dừng
    public void SaveCurrentMusicTime()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            savedTrackTime = musicSource.time;
        }
    }

    private IEnumerator AnimateMusicCrossfade(AudioClip nextTrack, float fadeDuration, float startTime)
    {
        float percent = 0;
        float startVolume = musicSource.volume;

        // Fade Out (dùng unscaledDeltaTime để chạy bình thường ngay cả khi Time.timeScale = 0)
        while (percent < 1)
        {
            percent += Time.unscaledDeltaTime / fadeDuration;
            musicSource.volume = Mathf.Lerp(startVolume, 0f, percent);
            yield return null;
        }

        musicSource.clip = nextTrack;
        musicSource.time = Mathf.Clamp(startTime, 0f, nextTrack.length - 0.1f);
        musicSource.Play();

        percent = 0;
        // Fade In
        while (percent < 1)
        {
            percent += Time.unscaledDeltaTime / fadeDuration;
            musicSource.volume = Mathf.Lerp(0f, 1f, percent);
            yield return null;
        }

        musicSource.volume = 1f;
    }
}