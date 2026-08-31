using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

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
    private bool isInitialized = false;
    private Coroutine enableCoroutine;

    private void Awake()
    {
        toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        isInitialized = false;

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnToggleChanged);
        }

        if (enableCoroutine != null)
            StopCoroutine(enableCoroutine);

        // Chạy Coroutine bằng WaitForSecondsRealtime thay cho Invoke
        enableCoroutine = StartCoroutine(EnableSFXRoutine());
    }

    private void OnDisable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(OnToggleChanged);
        }

        if (enableCoroutine != null)
            StopCoroutine(enableCoroutine);
    }

    private IEnumerator EnableSFXRoutine()
    {
        // WaitForSecondsRealtime giúp đếm đủ 0.05s kể cả khi Time.timeScale = 0
        yield return new WaitForSecondsRealtime(0.05f);
        isInitialized = true;
    }

    private void OnToggleChanged(bool isOn)
    {
        // Lúc này isInitialized đã = true, không bị chặn nữa
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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (toggle != null && toggle.interactable && !string.IsNullOrEmpty(hoverAction))
        {
            AudioEvents.TriggerSound2D(categoryID, subCategoryID, hoverAction);
        }
    }
}