using UnityEngine;

public static class AudioBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (SoundManager_New.Instance == null)
        {
            var audioPrefab = Resources.Load<GameObject>("Audio/AudioManager(New)");
            if (audioPrefab != null)
                Object.Instantiate(audioPrefab);
            else
                Debug.LogWarning("[AudioBootstrap] Không tìm thấy Resources/Audio/AudioManager(New).");
        }

        if (MusicLevelPlay.Instance == null)
        {
            var musicPrefab = Resources.Load<GameObject>("Audio/BackgroundMusicForLevels");
            if (musicPrefab != null)
                Object.Instantiate(musicPrefab);
            else
                Debug.LogWarning("[AudioBootstrap] Không tìm thấy Resources/Audio/BackgroundMusicForLevels.");
        }
    }
}
