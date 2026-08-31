using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private Image image;
    private Button button;

    [SerializeField] private float hoverScale = 1.15f;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.red;

    private void Start()
    {
        originalScale = transform.localScale;
        image = GetComponent<Image>();
        button = GetComponent<Button>();
        if (image != null)
            image.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && !button.interactable)
            return;

        transform.localScale = originalScale * hoverScale;
        if (image != null)
            image.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
        if (image != null)
            image.color = normalColor;
    }
}
