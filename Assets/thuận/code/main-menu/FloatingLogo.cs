using UnityEngine;

public class FloatingLogo : MonoBehaviour
{
    public float amplitude = 10f;
    public float speed = 1f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.localPosition;
    }

    void Update()
    {
        transform.localPosition =
            startPos +
            Vector3.up * Mathf.Sin(Time.time * speed) * amplitude;
    }
}