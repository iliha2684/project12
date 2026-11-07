using System;
using System.Collections.Generic;
using System.Linq;
using ProjectED.Cases;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ProjectED.Editor.Cases
{
    [CustomEditor(typeof(CitizenCaseData))]
    [CanEditMultipleObjects]
    public sealed class CitizenCaseDataEditor : UnityEditor.Editor
    {
        private const string StyleSheetPath = "Assets/Scripts/Editor/Cases/CitizenCaseDataEditor.uss";

        private Image portraitPreview;
        private Label titleLabel;
        private Label metaLabel;
        private Label dispositionBadge;
        private VisualElement validationContainer;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.AddToClassList("citizen-case-inspector");

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StyleSheetPath);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            root.Add(CreateHeader());

            if (targets.Length == 1)
            {
                var openWindowButton = new Button(() =>
                    CitizenCaseDataEditorWindow.Open((CitizenCaseData)target))
                {
                    text = "별도 Citizen Case 편집 창에서 열기"
                };
                openWindowButton.style.height = 28f;
                openWindowButton.style.marginTop = 4f;
                openWindowButton.style.marginBottom = 8f;
                root.Add(openWindowButton);
            }

            root.Add(CreateSection("사건 기본 정보", true,
                "caseId",
                "citizen",
                "category",
                "isFakeCitizen"));
            root.Add(CreateSection("등장 조건", true,
                "appearanceConditions.earliestDay",
                "appearanceConditions.latestDay",
                "appearanceConditions.selectionWeight",
                "appearanceConditions.allowRepeatInCampaign",
                "appearanceConditions.prerequisiteCase",
                "appearanceConditions.requirePreviousDisposition",
                "appearanceConditions.requiredPreviousDisposition"));
            root.Add(CreateSection("시민 및 대사", true,
                "portraitOverride",
                "dialogue"));
            root.Add(CreateSection("구형 대사 호환", false,
                "openingDialogue",
                "questionDialogue"));
            root.Add(CreateSection("시민증", false,
                "citizenIdCard.submitted",
                "citizenIdCard.facePhoto",
                "citizenIdCard.fullName",
                "citizenIdCard.citizenNumber",
                "citizenIdCard.birthDate",
                "citizenIdCard.expirationDate"));
            root.Add(CreateSection("기억 제거 신청서", false,
                "removalApplication.submitted",
                "removalApplication.applicantName",
                "removalApplication.citizenNumber",
                "removalApplication.targetEvent",
                "removalApplication.eventDate",
                "removalApplication.emotionCode",
                "removalApplication.signature"));
            root.Add(CreateSection("의사 처방전", false,
                "prescription.submitted",
                "prescription.doctorName",
                "prescription.licenseNumber",
                "prescription.prescriptionDate",
                "prescription.diagnosisCode",
                "prescription.removalScope",
                "prescription.hospitalName",
                "prescription.hospitalSeal"));
            root.Add(CreateSection("기억 구조 검사표", false,
                "memoryReport.submitted",
                "memoryReport.nodeCount",
                "memoryReport.selfConnection",
                "memoryReport.linkedPersonCount",
                "memoryReport.internalMemoryResponse",
                "memoryReport.equipmentNumber",
                "memoryReport.traceCode"));
            root.Add(CreateSection("관계인 동의서", false,
                "consentForm.submitted",
                "consentForm.people"));

            var judgmentSection = CreateSection("제작자 전용 판정 정보", true,
                "judgment.expectedDisposition",
                "judgment.violatedRuleIds",
                "judgment.judgmentReason");
            judgmentSection.AddToClassList("judgment-section");
            root.Add(judgmentSection);

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
            header.AddToClassList("case-header");

            portraitPreview = new Image
            {
                scaleMode = ScaleMode.ScaleToFit
            };
            portraitPreview.AddToClassList("portrait-preview");
            header.Add(portraitPreview);

            var summary = new VisualElement();
            summary.AddToClassList("case-summary");

            titleLabel = new Label("Citizen Case");
            titleLabel.AddToClassList("case-title");
            summary.Add(titleLabel);

            metaLabel = new Label();
            metaLabel.AddToClassList("case-meta");
            summary.Add(metaLabel);

            dispositionBadge = new Label();
            dispositionBadge.AddToClassList("disposition-badge");
            summary.Add(dispositionBadge);

            header.Add(summary);
            return header;
        }

        private Foldout CreateSection(string title, bool expanded, params string[] propertyPaths)
        {
            var foldout = new Foldout
            {
                text = title,
                value = expanded
            };
            foldout.AddToClassList("case-section");

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
                portraitPreview.sprite = null;
                titleLabel.text = $"Citizen Case {targets.Length}개 선택됨";
                metaLabel.text = "여러 사건의 공통 값을 편집합니다.";
                dispositionBadge.text = string.Empty;
                validationContainer.Clear();
                validationContainer.Add(new HelpBox("여러 사건을 선택한 상태에서는 개별 검증 결과를 표시하지 않습니다.", HelpBoxMessageType.Info));
                return;
            }

            var caseData = (CitizenCaseData)target;
            portraitPreview.sprite = caseData.Portrait;
            titleLabel.text = string.IsNullOrWhiteSpace(caseData.Citizen?.DisplayName)
                ? "이름 없는 시민 사건"
                : caseData.Citizen.DisplayName;

            var appearance = caseData.AppearanceConditions;
            var dayText = appearance == null
                ? "날짜 미설정"
                : appearance.EarliestDay == appearance.LatestDay
                    ? $"Day {appearance.EarliestDay:00}"
                    : $"Day {appearance.EarliestDay:00}-{appearance.LatestDay:00}";
            var caseIdText = string.IsNullOrWhiteSpace(caseData.CaseId) ? "ID 없음" : caseData.CaseId;
            metaLabel.text = $"{dayText}  •  {caseData.Category}  •  {caseIdText}";

            var disposition = caseData.Judgment?.ExpectedDisposition ?? CaseDisposition.ExtractionApproval;
            dispositionBadge.text = GetDispositionLabel(disposition);
            UpdateDispositionClass(disposition);
            RefreshValidation(caseData);
        }

        private void RefreshValidation(CitizenCaseData caseData)
        {
            validationContainer.Clear();
            var issues = CitizenCaseValidator.Validate(caseData).ToList();

            if (!string.IsNullOrWhiteSpace(caseData.CaseId) && HasDuplicateCaseId(caseData))
            {
                issues.Insert(0, new CaseValidationIssue(
                    CaseValidationSeverity.Error,
                    $"사건 ID '{caseData.CaseId}'를 사용하는 다른 CitizenCaseData가 있습니다.",
                    "caseId"));
            }

            var errorCount = issues.Count(issue => issue.Severity == CaseValidationSeverity.Error);
            var warningCount = issues.Count(issue => issue.Severity == CaseValidationSeverity.Warning);
            var infoCount = issues.Count(issue => issue.Severity == CaseValidationSeverity.Info);

            var heading = new Label($"데이터 검사  •  오류 {errorCount}  경고 {warningCount}  안내 {infoCount}");
            heading.AddToClassList("validation-title");
            validationContainer.Add(heading);

            if (issues.Count == 0)
            {
                validationContainer.Add(new HelpBox("데이터 검사를 통과했습니다.", HelpBoxMessageType.Info));
                return;
            }

            foreach (var issue in issues)
            {
                validationContainer.Add(new HelpBox(issue.Message, ToHelpBoxType(issue.Severity)));
            }
        }

        private bool HasDuplicateCaseId(CitizenCaseData current)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:CitizenCaseData"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var other = AssetDatabase.LoadAssetAtPath<CitizenCaseData>(path);
                if (other == null || other == current)
                {
                    continue;
                }

                if (string.Equals(other.CaseId, current.CaseId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateDispositionClass(CaseDisposition disposition)
        {
            var classNames = new[]
            {
                "disposition-approve",
                "disposition-reject",
                "disposition-detain",
                "disposition-treatment"
            };

            foreach (var className in classNames)
            {
                dispositionBadge.RemoveFromClassList(className);
            }

            dispositionBadge.AddToClassList(disposition switch
            {
                CaseDisposition.ExtractionApproval => "disposition-approve",
                CaseDisposition.DocumentRejection => "disposition-reject",
                CaseDisposition.Detention => "disposition-detain",
                CaseDisposition.TreatmentReferral => "disposition-treatment",
                _ => "disposition-approve"
            });
        }

        private static string GetDispositionLabel(CaseDisposition disposition)
        {
            return disposition switch
            {
                CaseDisposition.ExtractionApproval => "추출 승인",
                CaseDisposition.DocumentRejection => "서류 반려",
                CaseDisposition.Detention => "피검자 구금",
                CaseDisposition.TreatmentReferral => "치료 전환",
                _ => disposition.ToString()
            };
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
