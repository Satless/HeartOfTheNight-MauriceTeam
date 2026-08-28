using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// THÊM: IPointerEnterHandler để Unity nhận diện sự kiện Hover chuột
public class UIToggleSFX : MonoBehaviour, IPointerEnterHandler
{
    [Header("Category / SubCategory")]
    [SerializeField] private string categoryID = "UI";
    [SerializeField] private string subCategoryID = "Buttons";

    [Header("Action Names")]
    [SerializeField] private string onAction = "ToggleOn";
    [SerializeField] private string offAction = "ToggleOff";
    [SerializeField] private string hoverAction = "Hover";

    private Toggle toggle;
    private bool isInitialized = false; // Biến chặn phát âm thanh khi mới load UI

    private Coroutine enableCoroutine;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
    }

    //private void OnEnable()
    //{
    //    // Đánh dấu chưa khởi tạo để tránh phát tiếng khi OnEnable set trạng thái On/Off
    //    isInitialized = false;

    //    if (toggle != null)
    //    {
    //        toggle.onValueChanged.AddListener(OnToggleChanged);
    //    }

    //    // Cho phép phát âm thanh ở các khung hình tiếp theo (sau khi UI ổn định)
    //    Invoke(nameof(EnableSFX), 0.05f);
    //}

    private void OnEnable()
    {
        isInitialized = false;

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        if (enableCoroutine != null) StopCoroutine(enableCoroutine);

        // Thay Invoke(...) bằng Coroutine chạy thời gian thực
        enableCoroutine = StartCoroutine(EnableSFXRoutine());
    }

    //private void OnDisable()
    //{
    //    if (toggle != null)
    //    {
    //        toggle.onValueChanged.RemoveListener(OnToggleChanged);
    //    }
    //}

    private void OnDisable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }

        if (enableCoroutine != null) StopCoroutine(enableCoroutine);
    }

    private void EnableSFX()
    {
        isInitialized = true;
    }

    private void OnToggleChanged(bool isOn)
    {
        // Nếu UI vừa mở lên, không phát tiếng ngay
        if (!isInitialized) return;

        if (isOn)
        {
            AudioEvents.TriggerSound2D(categoryID, subCategoryID, onAction);
        }
        else
        {
            AudioEvents.TriggerSound2D(categoryID, subCategoryID, offAction);
        }
    }

    // Bắt sự kiện Hover chuột
    public void OnPointerEnter(PointerEventData eventData)
    {
        // Kiểm tra Toggle có bấm được không (Interactable) và tên Action không rỗng
        if (toggle != null && toggle.interactable && !string.IsNullOrEmpty(hoverAction))
        {
            AudioEvents.TriggerSound2D(categoryID, subCategoryID, hoverAction);
        }
    }

    private System.Collections.IEnumerator EnableSFXRoutine()
    {
        yield return new WaitForSecondsRealtime(0.05f);
        isInitialized = true;
    }
}