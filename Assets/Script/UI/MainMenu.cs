using UnityEngine;
using UnityEngine.Audio;

public class MainMenu : MonoBehaviour
{
    public AudioMixer audioMixer;

    private void Start()
    {
        MusicManager.Instance.PlayMusic("MainMenu");
    }

    public void Play()
    {
        LevelManager.Instance.LoadScene("HuyTestScene", "CircleWipe");
        MusicManager.Instance.PlayMusic("Menu");
    }

    public void Settings() 
    {

    }
    public void Quit()
    {
        Application.Quit();
    }

} 