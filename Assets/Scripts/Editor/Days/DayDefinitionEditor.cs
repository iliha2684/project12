using System;
using System.Linq;
using ProjectED.Cases;
using ProjectED.Days;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ProjectED.Editor.Days
{
    [CustomEditor(typeof(DayDefinition))]
    [CanEditMultipleObjects]
    public sealed class DayDefinitionEditor : UnityEditor.Editor
    {
        private const string StyleSheetPath = "Assets/Scripts/Editor/Days/DayDefinitionEditor.uss";

        private Label titleLabel;
        private Label populationLabel;
        private Label featureLabel;
        private VisualElement validationContainer;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.AddToClassList("day-inspector");

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            root.Add(CreateHeader());

            if (targets.Length == 1)
            {
                var openWindowButton = new Button(() => DayDefinitionEditorWindow.Open((DayDefinition)target))
                {
                    text = "별도 DAY 편집 창에서 열기"
                };
                openWindowButton.AddToClassList("open-window-button");
                root.Add(openWindowButton);
            }

            root.Add(CreateSection("DAY 기본 정보", true,
                "dayId",
                "dayNumber",
                "displayTitle"));
            root.Add(CreateSection("하루 인원", true,
                "totalCitizenCount",
                "fakeCitizenCount",
                "storyCases"));
            root.Add(CreateSection("해금 기능", true,
                "unlockedFeatures"));
            root.Add(CreateSection("일반 시민 조건 풀", true,
                "activeConditionPools"));
            root.Add(CreateSection("가짜 시민 조건", true,
                "availableFakeConditions"));
            root.Add(CreateSection("일자별 규정", true,
                "activeRegulations"));
            root.Add(CreateSection("제작자 메모", false,
                "designerNotes"));

            validationContainer = new VisualElement();
            validationContainer.AddToClassList("validation-container");
            root.Add(validationContainer);

            root.TrackSerializedObjectValue(serializedObject, _ => RefreshOverview());
            root.schedule.Execute(RefreshOverview).ExecuteLater(1);
            return root;
        }

        private VisualElement CreateHeader()
        {
            var header = new VisualElement();
            header.AddToClassList("day-header");

            titleLabel = new Label("DAY");
            titleLabel.AddToClassList("day-title");
            header.Add(titleLabel);

            populationLabel = new Label();
            populationLabel.AddToClassList("population-summary");
            header.Add(populationLabel);

            featureLabel = new Label();
            featureLabel.AddToClassList("feature-summary");
            header.Add(featureLabel);
            return header;
        }

        private Foldout CreateSection(string title, bool expanded, params string[] propertyPaths)
        {
            var foldout = new Foldout
            {
                text = title,
                value = expanded
            };
            foldout.AddToClassList("day-section");

            foreach (var propertyPath in propertyPaths)
            {
                var property = serializedObject.FindProperty(propertyPath);
                if (property == null)
                {
                    foldout.Add(new HelpBox($"프로퍼티를 찾을 수 없습니다: {propertyPath}", HelpBoxMessageType.Error));
                    continue;
                }

                foldout.Add(new PropertyField(property));
            }

            return foldout;
        }

        private void RefreshOverview()
        {
            if (titleLabel == null || validationContainer == null)
            {
                return;
            }

            if (targets.Length != 1)
            {
                titleLabel.text = $"DAY {targets.Length}개 선택됨";
                populationLabel.text = "여러 DAY의 공통 값을 편집합니다.";
                featureLabel.text = string.Empty;
                validationContainer.Clear();
                validationContainer.Add(new HelpBox("여러 DAY를 선택한 상태에서는 개별 계산 및 검증 결과를 표시하지 않습니다.", HelpBoxMessageType.Info));
                return;
            }

            var day = (DayDefinition)target;
            titleLabel.text = string.IsNullOrWhiteSpace(day.DisplayTitle)
                ? $"DAY {day.DayNumber:00}"
                : $"DAY {day.DayNumber:00}  ·  {day.DisplayTitle}";
            populationLabel.text =
                $"전체 {day.TotalCitizenCount}명  |  중요 {day.StoryCitizenCount}명  |  일반 {day.GeneralCitizenCount}명\n" +
                $"가짜 일반 {day.FakeCitizenCount}명  |  정상 일반 {day.NormalGeneralCitizenCount}명";
            featureLabel.text = day.UnlockedFeatures == DayFeatureFlags.None
                ? $"해금 기능 없음  |  활성 규정 {day.ActiveRegulations.Count}개"
                : $"해금 기능  ·  {day.UnlockedFeatures}  |  활성 규정 {day.ActiveRegulations.Count}개";

            RefreshValidation(day);
        }

        private void RefreshValidation(DayDefinition day)
        {
            validationContainer.Clear();
            var issues = DayDefinitionValidator.Validate(day).ToList();

            if (!string.IsNullOrWhiteSpace(day.DayId) && HasDuplicateDayId(day))
            {
                issues.Insert(0, new CaseValidationIssue(
                    CaseValidationSeverity.Error,
                    $"DAY ID '{day.DayId}'를 사용하는 다른 DayDefinition이 있습니다.",
                    "dayId"));
            }

            var errorCount = issues.Count(issue => issue.Severity == CaseValidationSeverity.Error);
            var warningCount = issues.Count(issue => issue.Severity == CaseValidationSeverity.Warning);
            var infoCount = issues.Count(issue => issue.Severity == CaseValidationSeverity.Info);
            var heading = new Label($"데이터 검사  ·  오류 {errorCount}  경고 {warningCount}  안내 {infoCount}");
            heading.AddToClassList("validation-title");
            validationContainer.Add(heading);

            if (issues.Count == 0)
            {
                validationContainer.Add(new HelpBox("DAY 설정을 사용할 수 있습니다.", HelpBoxMessageType.Info));
                return;
            }

            foreach (var issue in issues)
            {
                validationContainer.Add(new HelpBox(issue.Message, ToHelpBoxType(issue.Severity)));
            }
        }

        private static bool HasDuplicateDayId(DayDefinition current)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:DayDefinition"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var other = AssetDatabase.LoadAssetAtPath<DayDefinition>(path);
                if (other == null || other == current)
                {
                    continue;
                }

                if (string.Equals(other.DayId, current.DayId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static HelpBoxMessageType ToHelpBoxType(CaseValidationSeverity severity)
        {
            return severity switch
            {
                CaseValidationSeverity.Error => HelpBoxMessageType.Error,
                CaseValidationSeverity.Warning => HelpBoxMessageType.Warning,
                _ => HelpBoxMessageType.Info
            };
        }
    }
}
