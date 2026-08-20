using UnityEngine;
using TMPro;

public class BlinkText : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private float blinkSpeed = 2f;

    private void Start()
    {
        if (text == null)
            text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (text == null) return;

        float alpha = (Mathf.Sin(Time.unscaledTime * blinkSpeed) + 1f) / 2f;

        Color color = text.color;
        color.a = Mathf.Lerp(0.2f, 1f, alpha);
        text.color = color;
    }
}