using System;
using UnityEngine;

namespace ProjectED.Cases
{
    public enum DialogueSpeaker
    {
        Citizen,
        Player
    }

    [Serializable]
    public sealed class CaseDialogueLine
    {
        [SerializeField, InspectorName("화자")]
        private DialogueSpeaker speaker;

        [SerializeField, TextArea(2, 6), InspectorName("대사")]
        private string text;

        public CaseDialogueLine(DialogueSpeaker speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }

        public DialogueSpeaker Speaker => speaker;
        public string Text => text;
    }
}
