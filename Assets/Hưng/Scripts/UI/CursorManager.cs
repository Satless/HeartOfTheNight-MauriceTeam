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
        [SerializeField] private Sprite defaultSprite;      // Hình Đỏ (Bình thường)
        [SerializeField] private Sprite actionSprite;       // Hình Trắng (Bóp cò / Tương tác)

        private bool isHovering = false; 
        
        private Canvas parentCanvas;

        private void Start()
        {
            if (cursorImage != null)
            {
                parentCanvas = cursorImage.GetComponentInParent<Canvas>();
            }
            
            if (parentCanvas == null)
            {
                Debug.LogError("[CursorManager] KHÔNG THỂ HOẠT ĐỘNG! Vui lòng kéo Component Image vào ô 'Cursor Image' trước.");
            }
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

                // Xử lý đổi hình ảnh khi click chuột (Đọc trực tiếp từ phần cứng con chuột)
                bool isClicking = Mouse.current.leftButton.isPressed;
                
                if (isClicking || isHovering)
                {
                    cursorImage.sprite = actionSprite;
                }
                else
                {
                    cursorImage.sprite = defaultSprite;
                }
            }
        }

        // Dành cho UI Button gọi vào khi rê chuột tới (Event Trigger: PointerEnter/PointerExit)
        public void SetHoverState(bool hover)
        {
            isHovering = hover;
        }
    }
}
