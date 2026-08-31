using System.Collections;
using UnityEngine;

public class MusicManager_New : MonoBehaviour
{
    public static MusicManager_New Instance;

    // Property để các script khác (như UIMenuMusic) lấy được tên track đang phát
    public string CurrentTrackName => currentTrackName;

    [SerializeField] private MusicLibrary_New musicLibrary;
    [SerializeField] private AudioSource musicSource;

    private const float MusicVolume = 1f;

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
        if (musicSource == null || musicLibrary == null)
            return;
        if (currentTrackName == trackName && musicSource.isPlaying)
            return;

        AudioClip nextClip = musicLibrary.GetClipFromName(trackName);
        if (nextClip == null)
            return;

        currentTrackName = trackName;
        StartCrossfade(nextClip, fadeDuration, 0f);
    }

    // Phát nhạc tiếp tục từ thời điểm đã dừng (dùng cho khi tắt Pause Menu)
    public void PlayMusicResume(string trackName, float fadeDuration = 0.3f)
    {
        if (musicSource == null || musicLibrary == null)
            return;

        AudioClip nextClip = musicLibrary.GetClipFromName(trackName);
        if (nextClip == null)
            return;

        currentTrackName = trackName;
        StartCrossfade(nextClip, fadeDuration, savedTrackTime);
    }

    private void StartCrossfade(AudioClip nextClip, float fadeDuration, float startTime)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(AnimateMusicCrossfade(nextClip, fadeDuration, startTime));
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
        float fade = Mathf.Max(0.01f, fadeDuration);
        float from = musicSource.volume;
        float percent = 0f;

        while (percent < 1f)
        {
            percent += Time.unscaledDeltaTime / fade;
            musicSource.volume = Mathf.Lerp(from, 0f, percent);
            yield return null;
        }

        musicSource.clip = nextTrack;
        float maxTime = Mathf.Max(0f, nextTrack.length - 0.05f);
        musicSource.time = Mathf.Clamp(startTime, 0f, maxTime);
        musicSource.Play();

        percent = 0f;
        while (percent < 1f)
        {
            percent += Time.unscaledDeltaTime / fade;
            musicSource.volume = Mathf.Lerp(0f, MusicVolume, percent);
            yield return null;
        }

        musicSource.volume = MusicVolume;
        fadeCoroutine = null;
    }
}