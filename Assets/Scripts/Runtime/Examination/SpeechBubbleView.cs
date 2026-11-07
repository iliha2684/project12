using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectED.Examination
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SpeechBubbleView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField]
        private RectTransform mainPanel;

        [SerializeField]
        private RectTransform tail;

        [SerializeField]
        private TMP_Text dialogueText;

        [SerializeField, Min(1f), InspectorName("최대 가로 길이")]
        private float maxWidth = 410f;

        [SerializeField, Min(1f), InspectorName("최소 세로 길이")]
        private float minHeight = 150f;

        [SerializeField, Min(0f), InspectorName("세로 여백")]
        private float verticalPadding = 24f;

        [SerializeField, InspectorName("대사 색상")]
        private Color textColor = new(0.08f, 0.07f, 0.06f, 1f);

        private RectTransform bubbleRoot;
        private bool tailOffsetCached;
        private float tailOffsetFromBottom;

        public event Action<SpeechBubbleView> Clicked;

        public TMP_Text DialogueText => dialogueText;
        public float MaxWidth => maxWidth;

        private void Awake()
        {
            EnsureReferences();
        }

        public void Show(string message)
        {
            gameObject.SetActive(true);
            EnsureReferences();

            if (dialogueText == null || mainPanel == null)
            {
                return;
            }

            dialogueText.textWrappingMode = TextWrappingModes.Normal;
            dialogueText.overflowMode = TextOverflowModes.Overflow;
            dialogueText.color = textColor;
            dialogueText.text = message ?? string.Empty;

            dialogueText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
            var preferredHeight = dialogueText.GetPreferredValues(dialogueText.text, maxWidth, 0f).y;
            var targetHeight = Mathf.Max(minHeight, preferredHeight + verticalPadding);

            dialogueText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            mainPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
            mainPanel.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
            bubbleRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
            bubbleRoot.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);

            if (tail != null)
            {
                var position = tail.anchoredPosition;
                position.y = -targetHeight * 0.5f + tailOffsetFromBottom;
                tail.anchoredPosition = position;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(mainPanel);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button == PointerEventData.InputButton.Left)
            {
                Clicked?.Invoke(this);
            }
        }

        private void EnsureReferences()
        {
            if (bubbleRoot == null) bubbleRoot = (RectTransform)transform;
            if (mainPanel == null) mainPanel = transform.Find("Main") as RectTransform;
            if (tail == null) tail = transform.Find("Tail") as RectTransform;
            if (dialogueText == null) dialogueText = GetComponentInChildren<TMP_Text>(true);

            if (!tailOffsetCached && tail != null && mainPanel != null)
            {
                tailOffsetFromBottom = tail.anchoredPosition.y + mainPanel.rect.height * 0.5f;
                tailOffsetCached = true;
            }
        }
    }
}
