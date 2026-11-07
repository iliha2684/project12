using System;
using ProjectED.Cases;
using ProjectED.Days;
using ProjectED.Examination;
using UnityEngine;

// UI-independent story decisions and deputy review rules.
// Attach this component to the same GameObject as CampaignManager.
public enum DeputyReviewOutcome
{
    None,
    Award,
    Warning,
    Fired,
    Arrested
}

public class StoryProgressController : MonoBehaviour
{
    [Header("특수 NPC 참조")]
    [SerializeField] private CitizenCaseData whiteAgentCase;

    [Header("DAY 11 이후 성직자 단말기 조회 (의도적으로 부정확한 기록)")]
    [SerializeField, TextArea(2, 4)]
    private string priestTerminalRecord =
        "조회 결과: 등록된 특이 이력이 없습니다. 신원 조회상 별도 주의 사항이 확인되지 않습니다.";

    [Header("DAY 9 차장 점검 기준")]
    [Tooltip("오류 7회 이상일 때 경고장으로 처리할 구금 횟수 기준입니다.")]
    [Min(1)] [SerializeField] private int detentionsForWarning = 3;

    public int ActiveDay { get; private set; }
    public int OfferedWhiteMission { get; private set; }
    public DeputyReviewOutcome LastReviewOutcome { get; private set; }

    // Future UI can subscribe to these events without changing story rules.
    public event Action<int, int, bool> WhiteMissionResolved;
    public event Action<int, DeputyReviewOutcome> DeputyReviewResolved;
    public event Action<bool> WhiteFinalOfferResolved;

    private ExaminationController controller;
    private int lastReviewProcessedDay;
    private bool day13CompletionHandled;

    // DAY 13 completion has happened and the player must decide the final offer.
    // ExaminationController checks this before advancing to DAY 14.
    public bool WaitingForWhiteFinalOffer
    {
        get
        {
            GameManager game = GameManager.Instance;
            return game != null && game.currentDay == 13 && day13CompletionHandled &&
                game.whiteFinalOfferAvailable && !game.whiteFinalOfferDecided;
        }
    }

    private void OnDestroy()
    {
        if (controller != null)
        {
            controller.CitizenJudged -= OnCitizenJudged;
            controller.DayCompleted -= OnDayCompleted;
        }
    }

    public void BeginDay(int day, ExaminationController examinationController)
    {
        if (controller != examinationController)
        {
            if (controller != null)
            {
                controller.CitizenJudged -= OnCitizenJudged;
                controller.DayCompleted -= OnDayCompleted;
            }

            controller = examinationController;

            if (controller != null)
            {
                controller.CitizenJudged += OnCitizenJudged;
                controller.DayCompleted += OnDayCompleted;
            }
        }

        ActiveDay = day;
        OfferedWhiteMission = 0;
        LastReviewOutcome = DeputyReviewOutcome.None;

        GameManager game = GameManager.Instance;
        if (game == null)
        {
            Debug.LogError("StoryProgressController: GameManager를 찾을 수 없습니다.");
            return;
        }

        if (day == 9 || day == 14)
            ProcessDeputyReview(day, game);

        if (day >= 8 && day <= 13 && !game.priestDetained && game.whiteMissionCount < 5)
            OfferedWhiteMission = game.whiteMissionCount + 1;

        Debug.Log("DAY " + day + " 스토리 준비 완료. 백색서고 임무 번호: " + OfferedWhiteMission);
    }

    private void ProcessDeputyReview(int day, GameManager game)
    {
        if (lastReviewProcessedDay == day)
            return;
        lastReviewProcessedDay = day;

        DeputyReviewOutcome result = DeputyReviewOutcome.None;

        if (day == 9)
        {
            if (game.errorCount < 3)
                result = DeputyReviewOutcome.Award;
            else if (game.errorCount >= 7)
                result = game.detentionCount >= detentionsForWarning
                    ? DeputyReviewOutcome.Warning
                    : DeputyReviewOutcome.Fired;
            // Errors 3-6: no award and no penalty.
        }
        else if (day == 14)
        {
            if (game.whiteMissionCount < 2)
                result = DeputyReviewOutcome.Award;
            else if (game.whiteMissionCount >= 4)
                result = DeputyReviewOutcome.Arrested;
            // Accepted missions 2-3: no award and no penalty.
        }

        LastReviewOutcome = result;
        game.RecordReviewResult(
            day,
            result == DeputyReviewOutcome.Award,
            result == DeputyReviewOutcome.Warning,
            result == DeputyReviewOutcome.Fired,
            result == DeputyReviewOutcome.Arrested);

        Debug.Log("DAY " + day + " 차장 점검 결과: " + result);
        DeputyReviewResolved?.Invoke(day, result);
    }

    private void OnCitizenJudged(CitizenCaseInstance citizen, CaseDisposition disposition, bool isCorrect)
    {
        GameManager game = GameManager.Instance;
        if (game == null || citizen == null || whiteAgentCase == null || citizen.SourceCase != whiteAgentCase)
            return;

        int day = game.currentDay;
        if (day < 8 || day > 13)
            return; // DAY 7 is introductory; no mission yet.

        // A single white-agent verdict counts at most once per day.
        if (game.lastWhiteAgentJudgedDay == day)
            return;
        game.lastWhiteAgentJudgedDay = day;

        int missionNumber = game.whiteMissionCount + 1;
        if (missionNumber <= 5)
        {
            bool accepted = disposition == CaseDisposition.ExtractionApproval;
            if (accepted)
            {
                game.RecordWhiteMission(missionNumber, true);
            }
            else
            {
                game.whiteMissionRefusedCount++;
            }

            Debug.Log("DAY " + day + " 백색서고 임무 " + missionNumber +
                      (accepted ? " 수락" : " 거절"));
            WhiteMissionResolved?.Invoke(day, missionNumber, accepted);
        }

        // DAY 13 final offer is opened by OnDayCompleted, NOT by this verdict.
    }

    // The last verdict has been fully processed when this event fires.
    // Do not resolve a DAY 13 ending just because the calendar says 13.
    private void OnDayCompleted(DayPlan plan)
    {
        GameManager game = GameManager.Instance;
        if (plan == null || plan.Definition == null || plan.Definition.DayNumber != 13 ||
            game == null || game.currentDay != 13 || day13CompletionHandled)
            return;

        day13CompletionHandled = true;
        game.whiteFinalOfferAvailable = game.whiteMissionCount >= 5 &&
            game.priestRequestCompleted && !game.priestDetained;

        EndingManager endings = game.GetComponent<EndingManager>();
        if (endings != null)
            endings.CompleteDay13Review();
        else
            Debug.LogError("DAY 13: GameManager에 EndingManager가 연결되지 않았습니다.");

        if (game.whiteFinalOfferAvailable)
            Debug.Log("DAY 13 업무 종료: 백색서고 최종 제안 선택 대기 (수락/거절 선택은 아직 자동 처리하지 않음).");
        else
            Debug.Log("DAY 13 업무 종료: 최종 제안 조건 불충족. 엔딩 조건 확인 완료.");
    }

    // UI-independent terminal lookup. The record is deliberately misleading;
    // the final wording can be edited from this component's Inspector.
    // Search button integration is not performed here.
    public bool TrySearchPriestRecord(out string displayedRecord)
    {
        displayedRecord = string.Empty;
        GameManager game = GameManager.Instance;
        if (game == null || game.currentDay < 11 || game.currentDay > 17)
            return false;

        displayedRecord = priestTerminalRecord;
        var lateEvents = game.GetComponent<LateDayEventController>();
        if (lateEvents != null)
            lateEvents.RecordTerminalResearch(game.currentDay, TerminalResearchTopic.Priest);
        else
            Debug.LogWarning("성직자 검색 기록: LateDayEventController가 없어 단서 획득은 기록되지 않았습니다.");
        return true;
    }

    // Call this later from any UI or story trigger. Does not show a popup/scene.
    public bool ResolveWhiteFinalOffer(bool accept)
    {
        GameManager game = GameManager.Instance;
        EndingManager endings = game != null ? game.GetComponent<EndingManager>() : null;
        if (game == null || game.currentDay != 13 || endings == null ||
            !endings.Day13ReviewCompleted ||
            !game.whiteFinalOfferAvailable || game.whiteFinalOfferDecided)
            return false;

        game.whiteFinalOfferDecided = true;
        game.acceptedWhiteFinalOffer = accept;
        WhiteFinalOfferResolved?.Invoke(accept);
        Debug.Log("DAY 13 백색서고 최종 제안: " + (accept ? "수락" : "거절"));

        // Accepting may resolve Bad2 immediately after DAY 13 is completed.
        endings.EvaluateCurrentState();
        if (!accept && !endings.HasResolvedEnding)
        {
            // The DAY-advance was paused by ExaminationController.
            // A refusal resumes the regular DAY 14 campaign progression.
            game.GoToNextDay();
            if (CampaignManager.Instance != null)
                CampaignManager.Instance.LoadCurrentDay();
            else
                Debug.LogError("DAY 13: CampaignManager가 없어 다음 날을 불러오지 못했습니다.");
        }
        return true;
    }

    // Temporary Unity Editor test shortcuts. No dialogue or UI scene is added.
    [ContextMenu("DAY 13 테스트 - 백색서고 최종 제안 수락")]
    private void TestAcceptWhiteFinalOffer() => ResolveWhiteFinalOffer(true);

    [ContextMenu("DAY 13 테스트 - 백색서고 최종 제안 거절")]
    private void TestRejectWhiteFinalOffer() => ResolveWhiteFinalOffer(false);
}
