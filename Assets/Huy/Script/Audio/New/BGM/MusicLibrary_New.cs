using UnityEngine;

[System.Serializable]
public struct MusicTrack
{
    public string trackName;
    public AudioClip clip;
}

public class MusicLibrary_New : MonoBehaviour
{
    public MusicTrack[] tracks;

    public AudioClip GetClipFromName(string trackName)
    {
        if (tracks == null || string.IsNullOrEmpty(trackName))
            return null;

        foreach (var track in tracks)
        {
            if (track.trackName == trackName)
            {
                if (track.clip == null)
                    Debug.LogWarning($"[MusicLibrary] Track '{trackName}' không có AudioClip.");
                return track.clip;
            }
        }
        Debug.LogWarning($"[MusicLibrary] Không tìm thấy BGM: '{trackName}'.");
        return null;
    }
}