using UnityEngine;

public class GlitchLogo : MonoBehaviour
{
    Vector3 origin;

    public float intensity = 3f;
    public float interval = 0.05f;

    float timer;

    void Start()
    {
        origin = transform.localPosition;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= interval)
        {
            timer = 0;

            transform.localPosition =
                origin + (Vector3)Random.insideUnitCircle * intensity;
        }
    }
}