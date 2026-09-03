using UnityEngine;
using UnityEngine.Audio;

public class SoundManager_New : MonoBehaviour
{
    const int VoiceCount = 12;

    public static SoundManager_New Instance;

    [SerializeField] private SoundLibrary_New sfxLibrary;
    [SerializeField] private AudioSource sfxSource;

    private AudioSource[] _voices;
    private bool[] _inUse;
    private float[] _startedAt;
    private bool _isProxy;

    public AudioMixerGroup SfxMixerGroup
    {
        get
        {
            if (_isProxy && Instance != null && Instance != this)
                return Instance.SfxMixerGroup;
            return sfxSource != null ? sfxSource.outputAudioMixerGroup : null;
        }
    }

    public AudioClip GetSfxClip(string categoryID, string subCategoryID, string actionName)
    {
        if (_isProxy && Instance != null && Instance != this)
            return Instance.GetSfxClip(categoryID, subCategoryID, actionName);
        if (sfxLibrary == null)
            return null;
        return sfxLibrary.GetClipFromName(categoryID, subCategoryID, actionName);
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (sfxSource != null)
                sfxSource.ignoreListenerPause = true;
            BuildPool();
        }
        else if (Instance != this)
        {
            // Giữ object scene để EventTrigger (mainMenu) không trỏ vào instance đã Destroy.
            _isProxy = true;
        }
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
        AudioEvents.OnPlaySound2D += PlaySound2D;
        AudioEvents.OnPlaySound3D += PlaySound3D;
    }

    private void OnDisable()
    {
        if (_isProxy)
            return;
        AudioEvents.OnPlaySound2D -= PlaySound2D;
        AudioEvents.OnPlaySound3D -= PlaySound3D;
    }

    private void Update()
    {
        if (_isProxy || _voices == null)
            return;

        for (int i = 0; i < _voices.Length; i++)
        {
            if (!_inUse[i] || Time.unscaledTime <= _startedAt[i])
                continue;

            AudioSource source = _voices[i];
            if (source == null || !source.isPlaying)
                Release(i);
        }
    }

    public void PlaySound3D(string categoryID, string subCategoryID, string actionName, Vector3 pos)
    {
        if (_isProxy)
        {
            if (Instance != null && Instance != this)
                Instance.PlaySound3D(categoryID, subCategoryID, actionName, pos);
            return;
        }

        if (sfxLibrary == null)
            return;

        AudioClip clip = sfxLibrary.GetClipFromName(categoryID, subCategoryID, actionName);
        if (clip == null)
            return;

        AudioSource source = Rent();
        if (source == null)
            return;

        source.transform.position = pos;
        source.spatialBlend = 1f;
        source.clip = clip;
        source.Play();
    }

    public void PlaySound2D(string categoryID, string subCategoryID, string actionName)
    {
        if (_isProxy)
        {
            if (Instance != null && Instance != this)
                Instance.PlaySound2D(categoryID, subCategoryID, actionName);
            return;
        }

        if (sfxLibrary == null || sfxSource == null)
            return;

        AudioClip clip = sfxLibrary.GetClipFromName(categoryID, subCategoryID, actionName);
        if (clip != null)
            sfxSource.PlayOneShot(clip);
    }

    public void PlaySound2DFromPath(string fullPath)
    {
        if (_isProxy)
        {
            if (Instance != null && Instance != this)
                Instance.PlaySound2DFromPath(fullPath);
            return;
        }

        string[] parts = fullPath.Split('/');

        if (parts.Length == 3)
            PlaySound2D(parts[0], parts[1], parts[2]);
        else if (parts.Length == 2)
            PlaySound2D(parts[0], "Default", parts[1]);
        else
            Debug.LogWarning("[SoundManager] Sai định dạng chuỗi! Hãy nhập dạng 'Tầng1/Tầng2/Tầng3' (Ví dụ: UI/Button/Click)");
    }

    private void BuildPool()
    {
        if (sfxSource == null)
            return;

        _voices = new AudioSource[VoiceCount];
        _inUse = new bool[VoiceCount];
        _startedAt = new float[VoiceCount];

        for (int i = 0; i < VoiceCount; i++)
        {
            AudioSource voice = Instantiate(sfxSource, transform);
            voice.gameObject.name = "SfxVoice";
            voice.playOnAwake = false;
            voice.loop = false;
            voice.ignoreListenerPause = false;
            voice.spatialBlend = 1f;
            voice.dopplerLevel = 0f;
            voice.minDistance = 15f;
            voice.maxDistance = 50f;
            voice.rolloffMode = AudioRolloffMode.Linear;
            voice.Stop();
            _voices[i] = voice;
        }
    }

    private AudioSource Rent()
    {
        if (_voices == null)
            return null;

        int free = -1;
        int oldest = 0;
        float oldestTime = float.MaxValue;

        for (int i = 0; i < _voices.Length; i++)
        {
            if (!_inUse[i])
            {
                free = i;
                break;
            }

            if (_startedAt[i] < oldestTime)
            {
                oldestTime = _startedAt[i];
                oldest = i;
            }
        }

        int index = free >= 0 ? free : oldest;
        if (free < 0 && _voices[index] != null)
            _voices[index].Stop();

        _inUse[index] = true;
        _startedAt[index] = Time.unscaledTime;
        return _voices[index];
    }

    private void Release(int index)
    {
        _inUse[index] = false;
        _startedAt[index] = 0f;

        AudioSource source = _voices[index];
        if (source == null)
            return;

        source.Stop();
        source.clip = null;
    }
}
