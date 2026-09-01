using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace HeartOfTheNight.UI
{
    public class CursorManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image cursorImage;

        [Header("Cursor Sprites")]
        [SerializeField] private Sprite defaultSprite;
        [SerializeField] private Sprite actionSprite;

        private bool isHovering = false;
        private Canvas parentCanvas;

        private void Start()
        {
            if (cursorImage != null)
            {
                parentCanvas = cursorImage.GetComponentInParent<Canvas>();
                MakeVisualOnly(cursorImage);

                // Canvas gốc của prefab Cursor đã sortingOrder 999 — không gắn Canvas thêm
                // lên Image (popup Continue từng bị parent vào Canvas đó → kéo theo chuột).
                var raycaster = parentCanvas != null
                    ? parentCanvas.GetComponent<GraphicRaycaster>()
                    : null;
                if (raycaster != null)
                    raycaster.enabled = false;
            }

            if (parentCanvas == null)
            {
                Debug.LogError("[CursorManager] KHÔNG THỂ HOẠT ĐỘNG! Vui lòng kéo Component Image vào ô 'Cursor Image' trước.");
            }
        }

        private static void MakeVisualOnly(Image image)
        {
            image.raycastTarget = false;
            var group = image.GetComponent<CanvasGroup>();
            if (group == null)
                group = image.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        private void OnEnable()
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Confined;
        }

        private void OnDisable()
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void Update()
        {
            if (Mouse.current != null && cursorImage != null && parentCanvas != null)
            {
                RectTransform targetRect = cursorImage.rectTransform;
                if (targetRect.parent == null) return;

                Vector2 mousePosition = Mouse.current.position.ReadValue();

                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    targetRect.parent as RectTransform,
                    mousePosition,
                    parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : parentCanvas.worldCamera,
                    out Vector3 worldPoint);

                targetRect.position = worldPoint;

                bool isClicking = Mouse.current.leftButton.isPressed;

                if (isClicking || isHovering)
                    cursorImage.sprite = actionSprite;
                else
                    cursorImage.sprite = defaultSprite;
            }
        }

        public void SetHoverState(bool hover)
        {
            isHovering = hover;
        }
    }
}
