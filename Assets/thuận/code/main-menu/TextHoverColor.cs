using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class HorrorMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TMP_Text text;

    private Vector3 originalScale;
    private Color originalColor = Color.white;

    private void Awake()
    {
        originalScale = transform.localScale;
        if (text != null)
            originalColor = text.color;
    }

    private void OnDisable()
    {
        ResetVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (text != null)
            text.color = Color.red;
        transform.localScale = originalScale * 1.15f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ResetVisual();
    }

    private void ResetVisual()
    {
        if (text != null)
            text.color = originalColor;
        transform.localScale = originalScale;
    }
}