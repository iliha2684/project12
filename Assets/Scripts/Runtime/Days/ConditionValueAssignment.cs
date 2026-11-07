using System;
using UnityEngine;

namespace ProjectED.Days
{
    [Serializable]
    public sealed class ConditionValueAssignment
    {
        [SerializeField, InspectorName("대상 필드")]
        private GeneratedCitizenField field;

        [SerializeField, InspectorName("값 가져오기")]
        private ConditionValueSource valueSource = ConditionValueSource.FixedValue;

        [SerializeField, Tooltip("Copy From Field 방식일 때 복사할 원본 필드입니다."), InspectorName("복사할 원본 필드")]
        private GeneratedCitizenField sourceField;

        [SerializeField, InspectorName("값 형식")]
        private ConditionValueType valueType = ConditionValueType.Text;

        [SerializeField, TextArea(1, 4), InspectorName("문자열 값")]
        private string textValue;

        [SerializeField, InspectorName("정수 값")]
        private int integerValue;

        [SerializeField, InspectorName("참/거짓 값")]
        private bool booleanValue;

        [SerializeField, InspectorName("이미지 값")]
        private Sprite spriteValue;

        public GeneratedCitizenField Field => field;
        public ConditionValueSource ValueSource => valueSource;
        public GeneratedCitizenField SourceField => sourceField;
        public ConditionValueType ValueType => valueType;
        public string TextValue => textValue;
        public int IntegerValue => integerValue;
        public bool BooleanValue => booleanValue;
        public Sprite SpriteValue => spriteValue;

        public bool HasExpectedValueType => valueSource == ConditionValueSource.CopyFromField
            ? GeneratedCitizenFieldSchema.GetValueType(field) == GeneratedCitizenFieldSchema.GetValueType(sourceField)
            : GeneratedCitizenFieldSchema.GetValueType(field) == valueType;

        public object GetValue()
        {
            return valueType switch
            {
                ConditionValueType.Integer => integerValue,
                ConditionValueType.Boolean => booleanValue,
                ConditionValueType.Sprite => spriteValue,
                _ => textValue
            };
        }

        public bool TryResolveValue(CitizenCaseInstance citizen, out object value, out string error)
        {
            value = null;
            error = null;
            if (!HasExpectedValueType)
            {
                error = valueSource == ConditionValueSource.CopyFromField
                    ? $"'{sourceField}' 필드와 '{field}' 필드의 값 형식이 다릅니다."
                    : $"'{field}' 필드는 {GeneratedCitizenFieldSchema.GetValueType(field)} 형식이어야 합니다.";
                return false;
            }

            if (valueSource == ConditionValueSource.FixedValue)
            {
                value = GetValue();
                return true;
            }

            if (citizen == null || !citizen.Values.TryGetValue(sourceField, out value))
            {
                error = $"'{field}'에 복사할 원본 필드 '{sourceField}'가 아직 생성되지 않았습니다.";
                return false;
            }

            return true;
        }
    }

    public static class GeneratedCitizenFieldSchema
    {
        public static ConditionValueType GetValueType(GeneratedCitizenField field)
        {
            return field switch
            {
                GeneratedCitizenField.Portrait => ConditionValueType.Sprite,
                GeneratedCitizenField.IdCardFacePhoto => ConditionValueType.Sprite,

                GeneratedCitizenField.IdCardSubmitted => ConditionValueType.Boolean,
                GeneratedCitizenField.ApplicationSubmitted => ConditionValueType.Boolean,
                GeneratedCitizenField.PrescriptionSubmitted => ConditionValueType.Boolean,
                GeneratedCitizenField.MemoryReportSubmitted => ConditionValueType.Boolean,
                GeneratedCitizenField.ConsentFormSubmitted => ConditionValueType.Boolean,

                GeneratedCitizenField.MemoryReportNodeCount => ConditionValueType.Integer,
                GeneratedCitizenField.MemoryReportSelfConnection => ConditionValueType.Integer,
                GeneratedCitizenField.MemoryReportLinkedPersonCount => ConditionValueType.Integer,
                GeneratedCitizenField.MemoryReportInternalResponse => ConditionValueType.Integer,
                GeneratedCitizenField.ConsentFormSignedPersonCount => ConditionValueType.Integer,
                _ => ConditionValueType.Text
            };
        }
    }
}
