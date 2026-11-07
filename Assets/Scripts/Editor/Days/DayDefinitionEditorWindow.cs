using ProjectED.Days;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectED.Editor.Days
{
    public sealed class DayDefinitionEditorWindow : EditorWindow
    {
        private DayDefinition currentDay;
        private ObjectField dayField;
        private ScrollView inspectorScroll;
        private UnityEditor.Editor dayEditor;

        [MenuItem("Window/Project ED/DAY Editor")]
        private static void OpenFromWindowMenu()
        {
            Open(Selection.activeObject as DayDefinition);
        }

        [MenuItem("Assets/Project ED/DAY 별도 창에서 열기", false, 2001)]
        private static void OpenFromAssetsMenu()
        {
            Open(Selection.activeObject as DayDefinition);
        }

        [MenuItem("Assets/Project ED/DAY 별도 창에서 열기", true)]
        private static bool ValidateOpenFromAssetsMenu()
        {
            return Selection.activeObject is DayDefinition;
        }

        public static void Open(DayDefinition day)
        {
            var window = GetWindow<DayDefinitionEditorWindow>();
            window.titleContent = new GUIContent("DAY Editor");
            window.minSize = new Vector2(460f, 560f);
            window.Show();

            if (day != null)
            {
                window.SetDay(day);
            }
        }

        public void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            var toolbar = new Toolbar();
            var title = new Label("DAY");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginLeft = 6f;
            title.style.marginRight = 8f;
            toolbar.Add(title);

            dayField = new ObjectField
            {
                objectType = typeof(DayDefinition),
                allowSceneObjects = false,
                value = currentDay
            };
            dayField.style.flexGrow = 1f;
            dayField.RegisterValueChangedCallback(evt => SetDay(evt.newValue as DayDefinition));
            toolbar.Add(dayField);
            rootVisualElement.Add(toolbar);

            inspectorScroll = new ScrollView(ScrollViewMode.Vertical);
            inspectorScroll.style.flexGrow = 1f;
            rootVisualElement.Add(inspectorScroll);

            if (currentDay == null && Selection.activeObject is DayDefinition selectedDay)
            {
                currentDay = selectedDay;
                dayField.SetValueWithoutNotify(currentDay);
            }

            RebuildInspector();
        }

        private void SetDay(DayDefinition day)
        {
            if (currentDay == day && dayEditor != null)
            {
                return;
            }

            currentDay = day;
            dayField?.SetValueWithoutNotify(currentDay);
            titleContent = new GUIContent(currentDay == null ? "DAY Editor" : $"DAY - {currentDay.name}");
            RebuildInspector();
        }

        private void RebuildInspector()
        {
            if (inspectorScroll == null)
            {
                return;
            }

            inspectorScroll.Clear();
            DestroyDayEditor();

            if (currentDay == null)
            {
                inspectorScroll.Add(new HelpBox("위의 필드에서 편집할 DayDefinition을 선택하세요.", HelpBoxMessageType.Info));
                return;
            }

            dayEditor = UnityEditor.Editor.CreateEditor(currentDay);
            inspectorScroll.Add(new InspectorElement(dayEditor));
        }

        private void OnDisable()
        {
            DestroyDayEditor();
        }

        private void DestroyDayEditor()
        {
            if (dayEditor == null)
            {
                return;
            }

            DestroyImmediate(dayEditor);
            dayEditor = null;
        }
    }
}
