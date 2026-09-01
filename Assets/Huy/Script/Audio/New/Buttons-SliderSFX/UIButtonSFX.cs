using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonSFX : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Audio Settings")]
    [SerializeField] private string categoryID = "UI";
    [SerializeField] private string subCategoryID = "Buttons";

    [Header("Actions")]
    [SerializeField] private string hoverAction = "Hover";
    [SerializeField] private string clickAction = "Click";

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    // Sự kiện Hover (Di chuột vào nút)
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.interactable && !string.IsNullOrEmpty(hoverAction))
        {
            AudioEvents.TriggerSound2D(categoryID, subCategoryID, hoverAction);
        }
    }

    // Sự kiện Click (Bấm vào nút)
    public void OnPointerClick(PointerEventData eventData)
    {
        if (button != null && button.interactable && !string.IsNullOrEmpty(clickAction))
        {
            AudioEvents.TriggerSound2D(categoryID, subCategoryID, clickAction);
        }
    }
}