using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField]
    private SoundLibrary sfxLibrary;
    [SerializeField]
    private AudioSource sfxSource;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    // play 3D sfx based on AudioClip 
    public void PlaySound3D(AudioClip clip, Vector3 pos)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, pos);
        }
    }

    // 3d sfx follows Target_Name và SFX_Name
    public void PlaySound3D(string categoryID, string soundName, Vector3 pos)
    {
        AudioClip clip = sfxLibrary.GetClipFromName(categoryID, soundName);
        PlaySound3D(clip, pos);
    }

    // need input for Target_Name và SFX_Name
    public void PlaySound2D(string categoryID, string soundName)
    {
        AudioClip clip = sfxLibrary.GetClipFromName(categoryID, soundName);
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlaySound2DButton(string fullPath)
    {
        // split chain follwing '/' (ex: "UI/Click" -> categoryID = "UI", soundName = "Click")
        string[] parts = fullPath.Split('/');
        if (parts.Length == 2)
        {
            PlaySound2D(parts[0], parts[1]);
        }
        else
        {
            Debug.LogWarning("Enter following the right order: 'CategoryID/SoundName' (Ex: UI/Click)");
        }
    }
}