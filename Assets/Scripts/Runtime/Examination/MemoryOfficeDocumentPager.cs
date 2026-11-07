using System;
using System.Collections.Generic;
using ProjectED.Days;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectED.Examination
{
    /// <summary>
    /// Displays two documents per sheet, with exactly two sheets.
    /// PaperText stays as the original data source for ExaminationController.
    /// Each document has its own scroll area when its text is long.
    /// </summary>
    public sealed class MemoryOfficeDocumentPager : MonoBehaviour
    {
        private const string Separator = "\n\n────────────\n\n";
        private const int TotalPages = 2;
        private const int DocumentsPerPage = 2;

        [SerializeField] private ExaminationController examination;
        [SerializeField] private TMP_Text paperText;
        [SerializeField] private GameObject navigationRoot;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private TMP_Text pageNumberText;
        [SerializeField] private TextMeshProUGUI upperDocumentText;
        [SerializeField] private TextMeshProUGUI lowerDocumentText;
        [SerializeField] private ScrollRect upperDocumentScroll;
        [SerializeField] private ScrollRect lowerDocumentScroll;

        private readonly List<string> documents = new List<string>();
        private CitizenCaseInstance observedCitizen;
        private string lastSourceText;
        private int currentPage;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (previousButton != null) previousButton.onClick.AddListener(PreviousPage);
            if (nextButton != null) nextButton.onClick.AddListener(NextPage);
            RefreshDocument();
        }

        private void OnDisable()
        {
            if (previousButton != null) previousButton.onClick.RemoveListener(PreviousPage);
            if (nextButton != null) nextButton.onClick.RemoveListener(NextPage);
        }

        private void LateUpdate()
        {
            if (paperText == null) return;
            var citizen = examination != null ? examination.CurrentCitizen : null;
            if (!ReferenceEquals(observedCitizen, citizen) ||
                !string.Equals(lastSourceText, paperText.text, StringComparison.Ordinal))
            {
                RefreshDocument();
            }
        }

        public void NextPage()
        {
            if (observedCitizen == null || currentPage >= TotalPages - 1) return;
            currentPage++;
            RenderPage();
        }

        public void PreviousPage()
        {
            if (observedCitizen == null || currentPage == 0) return;
            currentPage--;
            RenderPage();
        }

        private void RefreshDocument()
        {
            ResolveReferences();
            if (paperText == null) return;

            observedCitizen = examination != null ? examination.CurrentCitizen : null;
            lastSourceText = paperText.text ?? string.Empty;
            documents.Clear();
            currentPage = 0;

            if (observedCitizen != null && lastSourceText.Length > 0)
            {
                foreach (var section in lastSourceText.Split(new[] { Separator },
                    StringSplitOptions.None))
                {
                    // Do not discard missing-document placeholders: they contain
                    // gameplay-relevant information for the player's judgment.
                    documents.Add(section.Trim('\r', '\n'));
                }
            }
            RenderPage();
        }

        private void RenderPage()
        {
            bool visible = observedCitizen != null && documents.Count > 0;
            int first = currentPage * DocumentsPerPage;
            SetDocument(upperDocumentText, upperDocumentScroll,
                visible && first < documents.Count ? documents[first] : string.Empty);
            SetDocument(lowerDocumentText, lowerDocumentScroll,
                visible && first + 1 < documents.Count ? documents[first + 1] : string.Empty);

            if (navigationRoot != null) navigationRoot.SetActive(visible);
            if (previousButton != null) previousButton.interactable = visible && currentPage > 0;
            if (nextButton != null) nextButton.interactable = visible && currentPage < TotalPages - 1;
            if (pageNumberText != null)
                pageNumberText.text = visible ? (currentPage + 1) + " / " + TotalPages : string.Empty;
        }

        private void SetDocument(TextMeshProUGUI output, ScrollRect scroll, string value)
        {
            if (output == null) return;

            // ExaminationController may assign its Korean-compatible font at runtime.
            if (paperText != null && paperText.font != null && output.font != paperText.font)
                output.font = paperText.font;

            output.text = value;
            if (scroll == null || scroll.content == null) return;

            float width = scroll.viewport != null ? scroll.viewport.rect.width : 390f;
            if (width < 50f) width = 390f;
            float viewportHeight = scroll.viewport != null ? scroll.viewport.rect.height : 240f;
            if (viewportHeight < 50f) viewportHeight = 240f;
            float preferred = value.Length == 0 ? 0f :
                output.GetPreferredValues(value, Mathf.Max(1f, width - 12f), Mathf.Infinity).y;

            // Keep every line in the document: only the inner document area
            // scrolls if its content is longer than its allotted half-sheet.
            scroll.content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
                Mathf.Max(viewportHeight, preferred + 20f));
            scroll.StopMovement();
            scroll.verticalNormalizedPosition = 1f;
        }

        private void ResolveReferences()
        {
            if (paperText == null)
                paperText = transform.Find("PaperText")?.GetComponent<TMP_Text>();
            if (upperDocumentText == null)
                upperDocumentText = transform.Find("MemoryOffice_TopDocument/DocumentText")?.GetComponent<TextMeshProUGUI>();
            if (lowerDocumentText == null)
                lowerDocumentText = transform.Find("MemoryOffice_BottomDocument/DocumentText")?.GetComponent<TextMeshProUGUI>();
            if (upperDocumentScroll == null)
                upperDocumentScroll = transform.Find("MemoryOffice_TopDocument")?.GetComponent<ScrollRect>();
            if (lowerDocumentScroll == null)
                lowerDocumentScroll = transform.Find("MemoryOffice_BottomDocument")?.GetComponent<ScrollRect>();
            if (examination == null)
                examination = GetComponentInParent<ExaminationController>();
        }
    }
}
