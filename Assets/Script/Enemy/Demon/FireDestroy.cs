using UnityEngine;

public class FireDestroy : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    [SerializeField] private float fireDuration = 1.0f;
    void Start()
    {
        Destroy(gameObject, fireDuration);
    }
}
