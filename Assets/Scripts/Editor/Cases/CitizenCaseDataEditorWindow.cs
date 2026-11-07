using ProjectED.Cases;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectED.Editor.Cases
{
    public sealed class CitizenCaseDataEditorWindow : EditorWindow
    {
        private CitizenCaseData currentCase;
        private ObjectField caseField;
        private ScrollView inspectorScroll;
        private UnityEditor.Editor caseEditor;

        [MenuItem("Window/Project ED/Citizen Case Editor")]
        private static void OpenFromWindowMenu()
        {
            Open(Selection.activeObject as CitizenCaseData);
        }

        [MenuItem("Assets/Project ED/Citizen Case 별도 창에서 열기", false, 2000)]
        private static void OpenFromAssetsMenu()
        {
            Open(Selection.activeObject as CitizenCaseData);
        }

        [MenuItem("Assets/Project ED/Citizen Case 별도 창에서 열기", true)]
        private static bool ValidateOpenFromAssetsMenu()
        {
            return Selection.activeObject is CitizenCaseData;
        }

        public static void Open(CitizenCaseData caseData)
        {
            var window = GetWindow<CitizenCaseDataEditorWindow>();
            window.titleContent = new GUIContent("Citizen Case Editor");
            window.minSize = new Vector2(430f, 520f);
            window.Show();

            if (caseData != null)
            {
                window.SetCase(caseData);
            }
        }

        public void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            var toolbar = new Toolbar();
            var title = new Label("Citizen Case");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginLeft = 6f;
            title.style.marginRight = 8f;
            toolbar.Add(title);

            caseField = new ObjectField
            {
                objectType = typeof(CitizenCaseData),
                allowSceneObjects = false,
                value = currentCase
            };
            caseField.style.flexGrow = 1f;
            caseField.RegisterValueChangedCallback(evt => SetCase(evt.newValue as CitizenCaseData));
            toolbar.Add(caseField);
            rootVisualElement.Add(toolbar);

            inspectorScroll = new ScrollView(ScrollViewMode.Vertical);
            inspectorScroll.style.flexGrow = 1f;
            rootVisualElement.Add(inspectorScroll);

            if (currentCase == null && Selection.activeObject is CitizenCaseData selectedCase)
            {
                currentCase = selectedCase;
                caseField.SetValueWithoutNotify(currentCase);
            }

            RebuildInspector();
        }

        private void SetCase(CitizenCaseData caseData)
        {
            if (currentCase == caseData && caseEditor != null)
            {
                return;
            }

            currentCase = caseData;
            if (caseField != null)
            {
                caseField.SetValueWithoutNotify(currentCase);
            }

            titleContent = new GUIContent(currentCase == null
                ? "Citizen Case Editor"
                : $"Citizen Case - {currentCase.name}");
            RebuildInspector();
        }

        private void RebuildInspector()
        {
            if (inspectorScroll == null)
            {
                return;
            }

            inspectorScroll.Clear();
            DestroyCaseEditor();

            if (currentCase == null)
            {
                inspectorScroll.Add(new HelpBox(
                    "위의 필드에서 편집할 CitizenCaseData를 선택하세요.",
                    HelpBoxMessageType.Info));
                return;
            }

            caseEditor = UnityEditor.Editor.CreateEditor(currentCase);
            inspectorScroll.Add(new InspectorElement(caseEditor));
        }

        private void OnDisable()
        {
            DestroyCaseEditor();
        }

        private void DestroyCaseEditor()
        {
            if (caseEditor == null)
            {
                return;
            }

            DestroyImmediate(caseEditor);
            caseEditor = null;
        }
    }
}
