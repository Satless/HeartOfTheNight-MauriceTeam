using UnityEngine;

public class LogoBlink : MonoBehaviour
{
    public CanvasGroup canvasGroup;

    public float speed = 2f;
    public float minAlpha = 0.6f;
    public float maxAlpha = 1f;

    void Update()
    {
        canvasGroup.alpha = Mathf.Lerp(
            minAlpha,
            maxAlpha,
            (Mathf.Sin(Time.time * speed) + 1) / 2
        );
    }
}