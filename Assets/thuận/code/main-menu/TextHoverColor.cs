using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class HorrorMenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TMP_Text text;

    private Vector3 originalScale;

    private void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        text.color = Color.red;
        transform.localScale = originalScale * 1.15f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        text.color = Color.white;
        transform.localScale = originalScale;
    }
}