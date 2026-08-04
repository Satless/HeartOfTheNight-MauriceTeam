using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private Image image;

    [SerializeField] private float hoverScale = 1.15f;

    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.red;

    private void Start()
    {
        originalScale = transform.localScale;
        image = GetComponent<Image>();
        image.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        transform.localScale = originalScale * hoverScale;
        image.color = hoverColor;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = originalScale;
        image.color = normalColor;
    }
}