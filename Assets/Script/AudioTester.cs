using UnityEngine;

public class AudioTester : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start() 
    {
        MusicManager.Instance.PlayMusic("Bitter Reality");
    }

}
