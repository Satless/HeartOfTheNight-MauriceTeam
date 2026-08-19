using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIToggleSFX : MonoBehaviour
{
    [Header("Category / SubCategory")]
    [SerializeField] private string categoryID = "UI";
    [SerializeField] private string subCategoryID = "Toggle";

    [Header("Action Names")]
    [SerializeField] private string onAction = "TurnOn";   // Tên action khi BẬT
    [SerializeField] private string offAction = "TurnOff"; // Tên action khi TẮT
    [SerializeField] private string hoverAction = "Hover"; // Tên action khi Hover

    private Toggle toggle;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        // Lắng nghe sự kiện thay đổi trạng thái On/Off của Toggle
        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (isOn)
        {
            // Phát tiếng Bật
            AudioEvents.TriggerSound2D(categoryID, subCategoryID, onAction);
        }
        else
        {
            // Phát tiếng Tắt
            AudioEvents.TriggerSound2D(categoryID, subCategoryID, offAction);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!string.IsNullOrEmpty(hoverAction))
        {
            AudioEvents.TriggerSound2D(categoryID, subCategoryID, hoverAction);
        }
    }
}
