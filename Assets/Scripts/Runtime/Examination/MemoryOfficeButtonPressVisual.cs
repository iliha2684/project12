using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectED.Examination
{
    /// <summary>기존 PNG 기본/눌림 레이어를 전환하며 이미지 파일은 수정하지 않는다.</summary>
    public sealed class MemoryOfficeButtonPressVisual : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private Image normalImage;
        [SerializeField] private Image pressedImage;
        private Button button;

        private void Awake() => button = GetComponent<Button>();

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && button.IsActive() && button.IsInteractable())
                SetPressed(true);
        }

        public void OnPointerUp(PointerEventData eventData) => SetPressed(false);
        public void OnPointerExit(PointerEventData eventData) => SetPressed(false);
        private void OnDisable() => SetPressed(false);

        private void SetPressed(bool isPressed)
        {
            if (normalImage != null) normalImage.gameObject.SetActive(!isPressed);
            if (pressedImage != null) pressedImage.gameObject.SetActive(isPressed);
        }
    }
}
