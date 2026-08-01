using UnityEngine;

[System.Serializable]
public struct MusicTrack
{
    public string trackName;
    public AudioClip clip;
}

public class MusicLibrary : MonoBehaviour
{
    public MusicTrack[] tracks;

    public AudioClip GetClipFromName(string trackName)
    {
        foreach (var track in tracks)
        {
            if (track.trackName == trackName)
            {
                return track.clip;
            }
        }
        return null;
    }
}

//remember to set load type of the background music to "Streaming"
//or else it will eat up your memory while running

//use this to play the music in specific stages/level to your likings:
// MusicManager.Instance.PlayMusic("Track name");