using System;

namespace ProjectED.Days
{
    [Flags]
    public enum DayFeatureFlags
    {
        None = 0,
        CitizenIdCard = 1 << 0,
        RemovalApplication = 1 << 1,
        DoctorPrescription = 1 << 2,
        MemoryStructureReport = 1 << 3,
        RelatedPersonConsent = 1 << 4,
        SearchTerminal = 1 << 5,
        DetailedSearch = 1 << 6,
        AdditionalRecords = 1 << 7
    }

    public enum CitizenConditionCategory
    {
        Identity,
        Occupation,
        VisitPurpose,
        TargetEvent,
        Emotion,
        Prescription,
        MemoryStructure,
        RelatedPersonConsent,
        SearchRecord,
        Dialogue,
        Custom
    }

    public enum GeneratedCitizenField
    {
        ProfileId,
        DisplayName,
        Portrait,
        Occupation,
        VisitPurpose,

        IdCardSubmitted,
        IdCardFacePhoto,
        IdCardFullName,
        IdCardCitizenNumber,
        IdCardBirthDate,
        IdCardExpirationDate,

        ApplicationSubmitted,
        ApplicationApplicantName,
        ApplicationCitizenNumber,
        ApplicationTargetEvent,
        ApplicationEventDate,
        ApplicationEmotionCode,
        ApplicationSignature,

        PrescriptionSubmitted,
        PrescriptionDoctorName,
        PrescriptionLicenseNumber,
        PrescriptionDate,
        PrescriptionDiagnosisCode,
        PrescriptionRemovalScope,
        PrescriptionHospitalName,
        PrescriptionHospitalSeal,

        MemoryReportSubmitted,
        MemoryReportNodeCount,
        MemoryReportSelfConnection,
        MemoryReportLinkedPersonCount,
        MemoryReportInternalResponse,
        MemoryReportEquipmentNumber,
        MemoryReportTraceCode,

        ConsentFormSubmitted,
        SearchRegisteredName,
        SearchCitizenNumber,
        SearchStatus,
        OpeningDialogue,
        QuestionDialogue,
        Custom,
        PlayerDialogue,
        CitizenReplyDialogue,

        ConsentFormSignedPersonCount,
        ConsentFormDetails
    }

    public enum ConditionValueType
    {
        Text,
        Integer,
        Boolean,
        Sprite
    }

    public enum ConditionValueSource
    {
        FixedValue,
        CopyFromField
    }

    public enum FakeMutationMode
    {
        ReplaceFromPool,
        UseFixedValue,
        ClearValue
    }

    public enum DayRegulationKind
    {
        ReferenceOnly,
        RelatedPersonConsent
    }

    public enum RegulationEvaluationStatus
    {
        NotApplicable,
        Compliant,
        Violated,
        MissingData
    }
}
