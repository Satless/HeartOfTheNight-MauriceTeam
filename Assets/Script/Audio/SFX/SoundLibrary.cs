using UnityEngine;

[System.Serializable]
public struct SoundEffect
{
    public string groupID;
    public AudioClip[] clips;
}

public class SoundLibrary : MonoBehaviour
{
    public SoundEffect[] soundEffects;

    public AudioClip GetClipFromName(string name)
    {
        foreach (var soundEffect in soundEffects)
        {
            if (soundEffect.groupID == name)
            {
                return soundEffect.clips[Random.Range(0, soundEffect.clips.Length)];
            }
        }

        return null;
    }
}

//change the volume with this 
// https://assetstore.unity.com/packages/tools/audio/easy-audio-cutter-316085
//thank god i dont have to use another app or website to just simply cut out and adjust the volume of the files

//oh also use: 
// SoundManager.Instance.PlaySound3D("Name", transform.position);
//to play the sfx where you want, put it somewhere ideal