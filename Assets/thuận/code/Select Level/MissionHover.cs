using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MissionHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Ảnh của Mission")]
    [SerializeField] private Sprite missionImage;

    [Header("Ảnh Preview")]
    [SerializeField] private Image previewImage;

    [Header("Ảnh mặc định")]
    [SerializeField] private Sprite defaultImage;

    [Header("Hiệu ứng chữ")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color hoverColor = Color.red;
    [SerializeField] private Color lockedColor = new Color(0.45f, 0.45f, 0.45f, 1f);
    [SerializeField] private float hoverScale = 1.15f;

    [Header("Load scene (trống = chưa mở / Chapter sau)")]
    [SerializeField] private string sceneName;

    private TMP_Text missionText;
    private Button missionButton;
    private Vector3 originalScale;

    public bool CanEnter =>
        !string.IsNullOrEmpty(sceneName) && ChapterProgress.IsUnlocked(sceneName);

    private Color IdleColor => CanEnter ? normalColor : lockedColor;

    private void Awake()
    {
        missionText = GetComponent<TMP_Text>();
        missionButton = GetComponent<Button>();
        originalScale = transform.localScale;
        StripDuplicateButtonClicks();
        ApplyIdleVisual();
    }

    public void Configure(string levelSceneName)
    {
        sceneName = levelSceneName;
        StripDuplicateButtonClicks();
        RefreshLock();
    }

    public void RefreshLock()
    {
        StripDuplicateButtonClicks();
        if (missionButton != null)
            missionButton.interactable = CanEnter;
        ApplyIdleVisual();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (previewImage != null && missionImage != null)
        {
            previewImage.sprite = missionImage;
            previewImage.enabled = true;
        }

        if (missionText != null)
            missionText.color = hoverColor;

        transform.localScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (previewImage != null && defaultImage != null)
            previewImage.sprite = defaultImage;

        ApplyIdleVisual();
        transform.localScale = originalScale;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!CanEnter)
            return;

        var select = FindFirstObjectByType<SelectLevelManager>();
        if (select != null)
        {
            select.RequestEnterLevel(sceneName);
            return;
        }

        SceneManager.LoadScene(sceneName);
    }

    private void StripDuplicateButtonClicks()
    {
        if (missionButton == null)
            missionButton = GetComponent<Button>();
        if (missionButton == null)
            return;

        missionButton.onClick = new Button.ButtonClickedEvent();
    }

    private void ApplyIdleVisual()
    {
        if (missionText != null)
            missionText.color = IdleColor;
    }
}
