using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectED.Cases
{
    [Serializable]
    public sealed class CitizenIdCardData
    {
        [SerializeField, InspectorName("제출됨")]
        private bool submitted = true;

        [SerializeField, InspectorName("증명사진")]
        private Sprite facePhoto;

        [SerializeField, InspectorName("이름")]
        private string fullName;

        [SerializeField, InspectorName("시민번호")]
        private string citizenNumber;

        [SerializeField, InspectorName("생년월일")]
        private string birthDate;

        [SerializeField, InspectorName("유효기간")]
        private string expirationDate;

        public bool Submitted => submitted;
        public string FullName => fullName;
        public string CitizenNumber => citizenNumber;
        public string BirthDate => birthDate;
        public string ExpirationDate => expirationDate;
    }

    [Serializable]
    public sealed class MemoryRemovalApplicationData
    {
        [SerializeField, InspectorName("제출됨")]
        private bool submitted = true;

        [SerializeField, InspectorName("신청자 이름")]
        private string applicantName;

        [SerializeField, InspectorName("시민번호")]
        private string citizenNumber;

        [SerializeField, TextArea(2, 5), InspectorName("대상 사건")]
        private string targetEvent;

        [SerializeField, InspectorName("사건일")]
        private string eventDate;

        [SerializeField, InspectorName("감정 코드")]
        private string emotionCode;

        [SerializeField, InspectorName("서명")]
        private string signature;

        public bool Submitted => submitted;
        public string ApplicantName => applicantName;
        public string CitizenNumber => citizenNumber;
        public string TargetEvent => targetEvent;
        public string EventDate => eventDate;
        public string EmotionCode => emotionCode;
        public string Signature => signature;
    }

    [Serializable]
    public sealed class DoctorPrescriptionData
    {
        [SerializeField, InspectorName("제출됨")]
        private bool submitted = true;

        [SerializeField, InspectorName("의사 이름")]
        private string doctorName;

        [SerializeField, InspectorName("면허번호")]
        private string licenseNumber;

        [SerializeField, InspectorName("처방일")]
        private string prescriptionDate;

        [SerializeField, InspectorName("진단 코드")]
        private string diagnosisCode;

        [SerializeField, TextArea(2, 5), InspectorName("제거 범위")]
        private string removalScope;

        [SerializeField, InspectorName("병원")]
        private string hospitalName;

        [SerializeField, InspectorName("병원 직인")]
        private string hospitalSeal;

        public bool Submitted => submitted;
        public string DoctorName => doctorName;
        public string LicenseNumber => licenseNumber;
        public string PrescriptionDate => prescriptionDate;
        public string DiagnosisCode => diagnosisCode;
        public string RemovalScope => removalScope;
        public string HospitalName => hospitalName;
        public string HospitalSeal => hospitalSeal;
    }

    [Serializable]
    public sealed class MemoryStructureReportData
    {
        [SerializeField, InspectorName("제출됨")]
        private bool submitted = true;

        [SerializeField, Min(0), InspectorName("노드 수")]
        private int nodeCount;

        [SerializeField, Min(0), InspectorName("자아 연결도")]
        private int selfConnection;

        [SerializeField, Min(0), InspectorName("타인 연결자 수")]
        private int linkedPersonCount;

        [SerializeField, Min(0), InspectorName("내부기억 반응")]
        private int internalMemoryResponse;

        [SerializeField, InspectorName("검사 장비번호")]
        private string equipmentNumber;

        [SerializeField, InspectorName("흔적 코드")]
        private string traceCode;

        public bool Submitted => submitted;
        public int NodeCount => nodeCount;
        public int SelfConnection => selfConnection;
        public int LinkedPersonCount => linkedPersonCount;
        public int InternalMemoryResponse => internalMemoryResponse;
        public string EquipmentNumber => equipmentNumber;
        public string TraceCode => traceCode;
    }

    [Serializable]
    public sealed class RelatedPersonConsentEntry
    {
        [SerializeField, InspectorName("이름")]
        private string fullName;

        [SerializeField, InspectorName("시민번호")]
        private string citizenNumber;

        [SerializeField, InspectorName("관계")]
        private string relationship;

        [SerializeField, InspectorName("서명")]
        private string signature;

        [SerializeField, InspectorName("동의일")]
        private string consentDate;

        public string FullName => fullName;
        public string CitizenNumber => citizenNumber;
        public string Relationship => relationship;
        public string Signature => signature;
        public string ConsentDate => consentDate;
    }

    [Serializable]
    public sealed class RelatedPersonConsentData
    {
        [SerializeField, InspectorName("제출됨")]
        private bool submitted = true;

        [SerializeField, InspectorName("관계인 목록")]
        private List<RelatedPersonConsentEntry> people = new();

        public bool Submitted => submitted;
        public IReadOnlyList<RelatedPersonConsentEntry> People => people;
    }
}
