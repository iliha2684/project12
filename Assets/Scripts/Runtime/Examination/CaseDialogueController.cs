using System;
using System.Collections.Generic;
using ProjectED.Cases;
using ProjectED.Days;
using UnityEngine;

namespace ProjectED.Examination
{
    public sealed class CaseDialogueController : MonoBehaviour
    {
        [SerializeField]
        private SpeechBubbleView citizenBubble;

        [SerializeField]
        private SpeechBubbleView playerBubble;

        [SerializeField]
        private bool hideInactiveBubble = true;

        private readonly List<RuntimeDialogueLine> activeLines = new();
        private int currentLineIndex = -1;

        public event Action DialogueStarted;
        public event Action<DialogueSpeaker, string, int> LineChanged;
        public event Action DialogueCompleted;

        public bool IsPlaying { get; private set; }
        public int CurrentLineIndex => currentLineIndex;

        private void Awake()
        {
            ResolveSceneReferences();
            HideAllBubbles();
        }

        private void OnEnable()
        {
            ResolveSceneReferences();
            if (citizenBubble != null) citizenBubble.Clicked += HandleBubbleClicked;
            if (playerBubble != null) playerBubble.Clicked += HandleBubbleClicked;
        }

        private void OnDisable()
        {
            if (citizenBubble != null) citizenBubble.Clicked -= HandleBubbleClicked;
            if (playerBubble != null) playerBubble.Clicked -= HandleBubbleClicked;
        }

        public bool StartDialogue(CitizenCaseData caseData)
        {
            ResolveSceneReferences();
            BuildRuntimeLines(caseData);
            currentLineIndex = -1;
            IsPlaying = activeLines.Count > 0;
            HideAllBubbles();

            if (!IsPlaying)
            {
                return false;
            }

            DialogueStarted?.Invoke();
            AdvanceDialogue();
            return true;
        }

        public bool StartDialogue(CitizenCaseInstance citizen)
        {
            if (citizen?.SourceCase != null)
            {
                return StartDialogue(citizen.SourceCase);
            }

            ResolveSceneReferences();
            BuildRuntimeLines(citizen);
            currentLineIndex = -1;
            IsPlaying = activeLines.Count > 0;
            HideAllBubbles();

            if (!IsPlaying)
            {
                return false;
            }

            DialogueStarted?.Invoke();
            AdvanceDialogue();
            return true;
        }

        public void AdvanceDialogue()
        {
            if (!IsPlaying)
            {
                return;
            }

            currentLineIndex++;
            if (currentLineIndex >= activeLines.Count)
            {
                CompleteDialogue();
                return;
            }

            var line = activeLines[currentLineIndex];
            var activeBubble = line.Speaker == DialogueSpeaker.Citizen ? citizenBubble : playerBubble;
            var inactiveBubble = line.Speaker == DialogueSpeaker.Citizen ? playerBubble : citizenBubble;

            if (hideInactiveBubble)
            {
                inactiveBubble?.Hide();
            }

            activeBubble?.Show(line.Text);
            LineChanged?.Invoke(line.Speaker, line.Text, currentLineIndex);
        }

        public void StopDialogue(bool hideBubbles = true)
        {
            activeLines.Clear();
            currentLineIndex = -1;
            IsPlaying = false;
            if (hideBubbles)
            {
                HideAllBubbles();
            }
        }

        private void CompleteDialogue()
        {
            IsPlaying = false;
            HideAllBubbles();
            DialogueCompleted?.Invoke();
        }

        private void BuildRuntimeLines(CitizenCaseData caseData)
        {
            activeLines.Clear();
            if (caseData == null)
            {
                return;
            }

            foreach (var line in caseData.Dialogue)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.Text))
                {
                    continue;
                }

                activeLines.Add(new RuntimeDialogueLine(line.Speaker, line.Text));
            }

            if (activeLines.Count == 0 && !string.IsNullOrWhiteSpace(caseData.OpeningDialogue))
            {
                activeLines.Add(new RuntimeDialogueLine(DialogueSpeaker.Citizen, caseData.OpeningDialogue));
            }
        }

        private void BuildRuntimeLines(CitizenCaseInstance citizen)
        {
            activeLines.Clear();
            if (citizen == null)
            {
                return;
            }

            AddGeneratedLine(citizen, GeneratedCitizenField.OpeningDialogue, DialogueSpeaker.Citizen);
            AddGeneratedLine(citizen, GeneratedCitizenField.PlayerDialogue, DialogueSpeaker.Player);

            if (citizen.HasValue(GeneratedCitizenField.CitizenReplyDialogue))
            {
                AddGeneratedLine(citizen, GeneratedCitizenField.CitizenReplyDialogue, DialogueSpeaker.Citizen);
            }
            else
            {
                AddGeneratedLine(citizen, GeneratedCitizenField.QuestionDialogue, DialogueSpeaker.Citizen);
            }
        }

        private void AddGeneratedLine(
            CitizenCaseInstance citizen,
            GeneratedCitizenField field,
            DialogueSpeaker speaker)
        {
            var text = citizen.GetText(field);
            if (!string.IsNullOrWhiteSpace(text))
            {
                activeLines.Add(new RuntimeDialogueLine(speaker, text));
            }
        }

        private void ResolveSceneReferences()
        {
            if (citizenBubble == null)
            {
                citizenBubble = transform.Find("CitizenSpeechBubble")?.GetComponent<SpeechBubbleView>();
            }

            if (playerBubble == null)
            {
                playerBubble = transform.Find("PlayerSpeechBubble")?.GetComponent<SpeechBubbleView>();
            }
        }

        private void HideAllBubbles()
        {
            citizenBubble?.Hide();
            playerBubble?.Hide();
        }

        private void HandleBubbleClicked(SpeechBubbleView _)
        {
            AdvanceDialogue();
        }

        private readonly struct RuntimeDialogueLine
        {
            public RuntimeDialogueLine(DialogueSpeaker speaker, string text)
            {
                Speaker = speaker;
                Text = text;
            }

            public DialogueSpeaker Speaker { get; }
            public string Text { get; }
        }
    }
}
