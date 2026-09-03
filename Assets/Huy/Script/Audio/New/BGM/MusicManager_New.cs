using System.Collections;
using UnityEngine;

public class MusicManager_New : MonoBehaviour
{
    public static MusicManager_New Instance;

    public string CurrentTrackName => currentTrackName;

    [SerializeField] private MusicLibrary_New musicLibrary;
    [SerializeField] private AudioSource musicSource;

    private const float MusicVolume = 1f;

    private Coroutine fadeCoroutine;
    private string currentTrackName = "";
    private float savedTrackTime = 0f;
    private bool _isProxy;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Không Destroy cả GameObject — cùng prefab với SoundManager (EventTrigger mainMenu).
            _isProxy = true;
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource != null)
            musicSource.ignoreListenerPause = true;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void OnEnable()
    {
        if (_isProxy)
            return;
        AudioEvents.OnPlayMusic += OnPlayMusicEvent;
    }

    private void OnDisable()
    {
        if (_isProxy)
            return;
        AudioEvents.OnPlayMusic -= OnPlayMusicEvent;
    }

    private void OnPlayMusicEvent(string trackName, float fadeDuration)
    {
        PlayMusic(trackName, fadeDuration);
    }

    public bool PlayMusic(string trackName, float fadeDuration = 0.5f)
    {
        if (_isProxy)
            return Instance != null && Instance != this && Instance.PlayMusic(trackName, fadeDuration);

        if (musicSource == null || musicLibrary == null)
            return false;
        if (currentTrackName == trackName && musicSource.isPlaying)
            return true;

        AudioClip nextClip = musicLibrary.GetClipFromName(trackName);
        if (nextClip == null)
            return false;

        currentTrackName = trackName;
        StartCrossfade(nextClip, fadeDuration, 0f);
        return true;
    }

    public bool PlayMusicResume(string trackName, float fadeDuration = 0.3f)
    {
        if (_isProxy)
            return Instance != null && Instance != this && Instance.PlayMusicResume(trackName, fadeDuration);

        if (musicSource == null || musicLibrary == null)
            return false;

        AudioClip nextClip = musicLibrary.GetClipFromName(trackName);
        if (nextClip == null)
            return false;

        currentTrackName = trackName;
        StartCrossfade(nextClip, fadeDuration, savedTrackTime);
        return true;
    }

    private void StartCrossfade(AudioClip nextClip, float fadeDuration, float startTime)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(AnimateMusicCrossfade(nextClip, fadeDuration, startTime));
    }

    public void SaveCurrentMusicTime()
    {
        if (_isProxy)
        {
            if (Instance != null && Instance != this)
                Instance.SaveCurrentMusicTime();
            return;
        }

        if (musicSource != null && musicSource.isPlaying)
            savedTrackTime = musicSource.time;
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
