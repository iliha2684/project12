using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectED.Cases
{
    [Serializable]
    public sealed class CitizenPortraitVariant
    {
        [SerializeField, InspectorName("표정 키")]
        private string key;

        [SerializeField, InspectorName("이미지")]
        private Sprite portrait;

        public string Key => key;
        public Sprite Portrait => portrait;
    }

    [CreateAssetMenu(fileName = "CitizenProfile_", menuName = "Project ED/Cases/Citizen Profile", order = 0)]
    public sealed class CitizenProfileData : ScriptableObject
    {
        [SerializeField, InspectorName("프로필 ID")]
        private string profileId;

        [SerializeField, InspectorName("표시 이름")]
        private string displayName;

        [SerializeField, InspectorName("기본 시민 이미지")]
        private Sprite defaultPortrait;

        [SerializeField, InspectorName("표정 이미지")]
        private List<CitizenPortraitVariant> portraitVariants = new();

        [SerializeField, TextArea(3, 8), InspectorName("제작자 메모")]
        private string designerNotes;

        public string ProfileId => profileId;
        public string DisplayName => displayName;
        public Sprite DefaultPortrait => defaultPortrait;
        public IReadOnlyList<CitizenPortraitVariant> PortraitVariants => portraitVariants;
        public string DesignerNotes => designerNotes;
    }
}
