using System;
using System.Collections.Generic;
using ProjectED.Days;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ProjectED.Editor.Days
{
    [CustomEditor(typeof(CitizenConditionPoolData))]
    public sealed class CitizenConditionPoolDataEditor : UnityEditor.Editor
    {
        private Label summaryLabel;
        private VisualElement validationContainer;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Scripts/Editor/Days/DayDefinitionEditor.uss");
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            var header = new VisualElement();
            header.AddToClassList("pool-header");
            var title = new Label("일반 시민 조건 풀");
            title.AddToClassList("day-title");
            header.Add(title);
            summaryLabel = new Label();
            summaryLabel.AddToClassList("feature-summary");
            header.Add(summaryLabel);
            root.Add(header);

            root.Add(new PropertyField(serializedObject.FindProperty("poolId")));
            root.Add(new PropertyField(serializedObject.FindProperty("displayName")));
            root.Add(new PropertyField(serializedObject.FindProperty("category")));
            root.Add(new PropertyField(serializedObject.FindProperty("requiredFeatures")));
            root.Add(new PropertyField(serializedObject.FindProperty("requiredForEveryCitizen")));

            var options = new Foldout
            {
                text = "무작위 선택지",
                value = true
            };
            options.Add(new HelpBox(
                "일반 시민마다 호환되는 선택지 하나를 가중치에 따라 뽑습니다. 필요/금지/제공 태그로 조건 간 연결을 만드세요.",
                HelpBoxMessageType.Info));
            options.Add(new PropertyField(serializedObject.FindProperty("options")));
            root.Add(options);

            validationContainer = new VisualElement();
            validationContainer.AddToClassList("validation-container");
            root.Add(validationContainer);

            root.TrackSerializedObjectValue(serializedObject, _ => Refresh());
            root.schedule.Execute(Refresh).ExecuteLater(1);
            return root;
        }

        private void Refresh()
        {
            var pool = (CitizenConditionPoolData)target;
            summaryLabel.text = $"{pool.Category}  ·  선택지 {pool.Options.Count}개  ·  필요 기능 {pool.RequiredFeatures}";

            validationContainer.Clear();
            var messages = Validate(pool);
            if (messages.Count == 0)
            {
                validationContainer.Add(new HelpBox("조건 풀을 사용할 수 있습니다.", HelpBoxMessageType.Info));
                return;
            }

            foreach (var message in messages)
            {
                validationContainer.Add(new HelpBox(message, HelpBoxMessageType.Error));
            }
        }

        private static List<string> Validate(CitizenConditionPoolData pool)
        {
            var messages = new List<string>();
            if (string.IsNullOrWhiteSpace(pool.PoolId))
            {
                messages.Add("조건 풀 ID가 비어 있습니다.");
            }

            if (pool.Options.Count == 0)
            {
                messages.Add("무작위로 선택할 항목을 하나 이상 추가해야 합니다.");
                return messages;
            }

            var optionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < pool.Options.Count; index++)
            {
                var option = pool.Options[index];
                if (option == null)
                {
                    messages.Add($"선택지 {index + 1}번이 비어 있습니다.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(option.OptionId))
                {
                    messages.Add($"선택지 {index + 1}번의 ID가 비어 있습니다.");
                }
                else if (!optionIds.Add(option.OptionId))
                {
                    messages.Add($"선택지 ID '{option.OptionId}'가 중복되었습니다.");
                }

                if (option.Values.Count == 0)
                {
                    messages.Add($"선택지 '{option.DisplayName ?? option.OptionId}'에 적용할 값이 없습니다.");
                }

                var assignedFields = new HashSet<GeneratedCitizenField>();
                foreach (var assignment in option.Values)
                {
                    if (assignment == null)
                    {
                        messages.Add($"선택지 '{option.DisplayName ?? option.OptionId}'에 비어 있는 필드 값이 있습니다.");
                        continue;
                    }

                    if (!assignment.HasExpectedValueType)
                    {
                        messages.Add(
                            $"필드 '{assignment.Field}'의 값 형식은 " +
                            $"{GeneratedCitizenFieldSchema.GetValueType(assignment.Field)}이어야 합니다.");
                    }

                    if (!assignedFields.Add(assignment.Field))
                    {
                        messages.Add($"선택지 '{option.DisplayName ?? option.OptionId}'에서 필드 '{assignment.Field}'가 중복 지정되었습니다.");
                    }
                }
            }

            return messages;
        }
    }
}
