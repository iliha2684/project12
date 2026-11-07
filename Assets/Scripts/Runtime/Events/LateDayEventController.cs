using System;
using ProjectED.Cases;
using ProjectED.Days;
using ProjectED.Examination;
using UnityEngine;
using System.Collections.Generic;

// Stores late-game events independently of dialogue panels or future scene UI.
[DisallowMultipleComponent]
public sealed class LateDayEventController : MonoBehaviour
{
    [Header("특수 NPC 데이터")]
    [SerializeField] private CitizenCaseData quackDoctorCase;
    [SerializeField] private CitizenCaseData pleadingCitizenCase;

    [Header("DAY 16~17 규정 위반 시 신뢰도 감소")]
    [Min(1)] [SerializeField] private int forcedExtractionTrustPenalty = 10;
    [Min(1)] [SerializeField] private int pleadingChoiceTrustPenalty = 10;

    [Header("스토리 진행 기록")]
    [SerializeField] private int lastDaySeen;
    [SerializeField] private bool directorArrived;
    [SerializeField] private bool deputyAnnouncedDirector;
    [SerializeField] private bool agencyAgentWarned;
    [SerializeField] private int lateRuleViolations;

    public CitizenCaseData PleadingCitizenCase => pleadingCitizenCase;
    public bool IsPleadingCitizen(CitizenCaseInstance citizen)
    {
        return pleadingCitizenCase != null && citizen != null &&
               citizen.SourceCase == pleadingCitizenCase;
    }

    public bool DirectorArrived => directorArrived;
    public bool DeputyAnnouncedDirector => deputyAnnouncedDirector;
    public bool AgencyAgentWarned => agencyAgentWarned;
    public int LateRuleViolations => lateRuleViolations;

    public event Action<int> LateDayStarted;
    public event Action<int, int> LateRuleViolated;

    private ExaminationController boundController;
    private EndingManager endings;
    private readonly HashSet<string> researchedTopics = new HashSet<string>();
    private readonly HashSet<string> processedVerdicts = new HashSet<string>();

    private void Awake()
    {
        endings = GetComponent<EndingManager>();
    }

    private void OnDestroy()
    {
        if (boundController != null)
            boundController.CitizenJudged -= OnCitizenJudged;
    }

    private void Update()
    {
        var campaign = CampaignManager.Instance;
        var game = GameManager.Instance;
        if (campaign == null || game == null)
            return;

        if (boundController != campaign.examinationController)
        {
            if (boundController != null)
                boundController.CitizenJudged -= OnCitizenJudged;
            boundController = campaign.examinationController;
            if (boundController != null)
                boundController.CitizenJudged += OnCitizenJudged;
        }

        if (lastDaySeen != game.currentDay)
            BeginDay(game.currentDay);
    }

    public void BeginDay(int day)
    {
        if (lastDaySeen == day)
            return;

        lastDaySeen = day;
        if (day == 15) agencyAgentWarned = true;
        if (day == 17) deputyAnnouncedDirector = true;
        if (day == 18) directorArrived = true;
        LateDayStarted?.Invoke(day);
        Debug.Log("[LateDayEventController] DAY " + day + " 이벤트 상태 갱신");
    }

    private void OnCitizenJudged(CitizenCaseInstance citizen, CaseDisposition choice, bool correct)
    {
        GameManager game = GameManager.Instance;
        if (game == null || citizen == null || game.currentDay < 14)
            return;

        // Only one record per citizen slot and day.
        string key = game.currentDay + ":" + citizen.SequenceIndex;
        if (!processedVerdicts.Add(key))
            return;

        if (quackDoctorCase != null && citizen.SourceCase == quackDoctorCase)
        {
            if (choice == CaseDisposition.ExtractionApproval ||
                (game.currentDay == 15 && choice == CaseDisposition.DocumentRejection))
            {
                GetEndings()?.RecordQuackDoctorFatalOutcome();
            }
        }

        // The specially designated pleading citizen must be detained.
        if (game.currentDay == 16 && IsPleadingCitizen(citizen) &&
            choice != CaseDisposition.Detention)
        {
            ApplyPenalty(game.currentDay, pleadingChoiceTrustPenalty);
        }
    }

    // Call from the rules/verdict layer once consent documents are validated.
    // Not called automatically because the current CitizenCaseInstance does not
    // expose a verified 'valid consent' decision in the files provided.
    public void RecordConsentRuleDecision(int day, bool consentValid, bool isPleadingCitizen,
        CaseDisposition actualDisposition)
    {
        if (day != 16 && day != 17)
            return;
        if (!consentValid || isPleadingCitizen)
            return;
        if (actualDisposition != CaseDisposition.ExtractionApproval)
            ApplyPenalty(day, forcedExtractionTrustPenalty);
    }

    private void ApplyPenalty(int day, int points)
    {
        GameManager game = GameManager.Instance;
        if (game == null)
            return;

        lateRuleViolations++;
        game.trust = Mathf.Max(0, game.trust - points);
        LateRuleViolated?.Invoke(day, points);
        Debug.Log("[LateDayEventController] DAY " + day + " 규정 위반, 신뢰도 -" + points);
        GetEndings()?.EvaluateCurrentState();
    }

    public bool RecordTerminalResearch(int day, TerminalResearchTopic topic)
    {
        GameManager game = GameManager.Instance;
        if (game == null || day != game.currentDay)
            return false;

        bool available = (topic == TerminalResearchTopic.WhiteOrganization && day >= 7) ||
            (topic == TerminalResearchTopic.DeathRegister && day >= 10) ||
            (topic == TerminalResearchTopic.IncidentArchive && day >= 10) ||
            (topic == TerminalResearchTopic.Priest && day >= 11) ||
            (topic == TerminalResearchTopic.Deputy && day >= 17);
        if (!available)
            return false;

        string key = day + ":" + topic;
        if (!researchedTopics.Add(key))
            return false;

        if (topic == TerminalResearchTopic.Deputy)
            game.deputyNameSearched = true;
        game.RecordTerminalClue(day); // GameManager counts at most one clue/day.
        return true;
    }

    public void RecordPriestCrossChoice(bool keepCross)
    {
        GameManager game = GameManager.Instance;
        if (game != null && game.currentDay == 12)
            game.RecordCrossDecision(keepCross);
    }

    public void RecordIllegalContactDiscovered()
    {
        GetEndings()?.RecordIllegalContactDiscovery();
    }

    private EndingManager GetEndings()
    {
        if (endings == null)
            endings = GetComponent<EndingManager>();
        return endings;
    }
}

public enum TerminalResearchTopic
{
    WhiteOrganization,
    DeathRegister,
    IncidentArchive,
    Priest,
    Deputy
}
