using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class ButtonHoverEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Text")]
    [SerializeField] private TMP_Text buttonText;

    [Header("Hover Settings")]
    [SerializeField] private float hoverScale = 1.15f;
    [SerializeField] private float animationSpeed = 8f;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.red;

    private Vector3 normalScale;
    private Vector3 targetScale;

    private void Start()
    {
        normalScale = transform.localScale;
        targetScale = normalScale;

        if (buttonText != null)
        {
            buttonText.color = normalColor;
        }
    }

    private void Update()
    {
        // Phóng to / thu nhỏ mượt
        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            Time.unscaledDeltaTime * animationSpeed
        );
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = normalScale * hoverScale;

        if (buttonText != null)
        {
            buttonText.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = normalScale;

        if (buttonText != null)
        {
            buttonText.color = normalColor;
        }
    }
}