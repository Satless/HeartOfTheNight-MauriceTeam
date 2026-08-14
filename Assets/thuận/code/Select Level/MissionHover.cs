using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class MissionHover : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
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
    [SerializeField] private float hoverScale = 1.15f;

    private TMP_Text missionText;
    private Vector3 originalScale;

    private void Awake()
    {
        missionText = GetComponent<TMP_Text>();
        originalScale = transform.localScale;

        if (missionText != null)
        {
            missionText.color = normalColor;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Đổi ảnh Preview
        if (previewImage != null && missionImage != null)
        {
            previewImage.sprite = missionImage;
            previewImage.enabled = true;
        }

        // Đổi màu chữ
        if (missionText != null)
        {
            missionText.color = hoverColor;
        }

        // Phóng to chữ
        transform.localScale = originalScale * hoverScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Trả ảnh Preview về ảnh mặc định
        if (previewImage != null && defaultImage != null)
        {
            previewImage.sprite = defaultImage;
        }

        // Trả màu chữ
        if (missionText != null)
        {
            missionText.color = normalColor;
        }

        // Trả kích thước ban đầu
        transform.localScale = originalScale;
    }
}