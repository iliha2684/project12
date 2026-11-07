using UnityEngine;
using UnityEngine.EventSystems;

namespace ProjectED.Examination
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class DraggableUIItem : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        [SerializeField]
        private bool bringToFrontOnDrag = true;

        private RectTransform rectTransform;
        private Canvas rootCanvas;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            rootCanvas = GetComponentInParent<Canvas>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (bringToFrontOnDrag)
            {
                rectTransform.SetAsLastSibling();
            }
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (rootCanvas == null)
            {
                rootCanvas = GetComponentInParent<Canvas>();
            }

            var scaleFactor = rootCanvas != null ? rootCanvas.scaleFactor : 1f;
            rectTransform.anchoredPosition += eventData.delta / Mathf.Max(scaleFactor, 0.0001f);
        }
    }
}
