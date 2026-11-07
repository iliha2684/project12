using System;
using System.Collections.Generic;
using ProjectED.Cases;
using ProjectED.Days;
using ProjectED.Examination;
using UnityEngine;

public class CampaignManager : MonoBehaviour
{
    public static CampaignManager Instance;

    [Header("심사 시스템")]
    public ExaminationController examinationController;

    [Header("DAY 데이터")]
    public DayDefinition[] days;

    [Header("조건부 특수 NPC - 에셋 연결 후 자동 배치")]
    [SerializeField] private CitizenCaseData whiteAgentCase;
    [SerializeField] private CitizenCaseData priestCase;
    [Header("DAY 12 십자가 요청 전용 성직자 사건 (선택, 미등록 시 기존 성직자 사용)")]
    [SerializeField] private CitizenCaseData priestDay12Case;
    [SerializeField] private CitizenCaseData quackDoctorCase;
    [SerializeField] private CitizenCaseData day15AgencyAgentCase;

    // A stable seed prevents NPCs from changing when the same DAY is reloaded.
    private const int BaseSeed = 1000;
    private int lastPriestRecordedDay;
    private int lastDoctorRecordedDay;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (examinationController != null)
            examinationController.CitizenJudged -= OnCitizenJudged;
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        LoadCurrentDay();
    }

    public void LoadCurrentDay()
    {
        GameManager game = GameManager.Instance;
        if (game == null)
        {
            Debug.LogError("GameManager를 찾을 수 없습니다.");
            return;
        }

        int currentDay = game.currentDay;

        // DAY 18 has no citizen inspections and must not call LoadDay().
        // Resolve it through the existing UI-independent ending manager.
        if (currentDay == 18)
        {
            var lateEvents = game.GetComponent<LateDayEventController>();
            if (lateEvents != null) lateEvents.BeginDay(18);

            var endings = game.GetComponent<EndingManager>();
            if (endings == null)
            {
                Debug.LogError("DAY 18: GameManager에 EndingManager가 없습니다.");
                return;
            }

            var result = endings.EvaluateCurrentState();
            if (result == MemoryOfficeEnding.None)
            {
                Debug.LogWarning("DAY 18: 아직 엔딩 조건이 확정되지 않았습니다. " +
                    "경고장을 받은 경우의 최종 분기는 기획서에 추가 정의가 필요합니다.");
            }
            else
            {
                Debug.Log("DAY 18 국장 이벤트, 엔딩: " + result);
            }
            return;
        }

        int index = currentDay - 1;

        if (days == null || index < 0 || index >= days.Length || days[index] == null)
        {
            Debug.LogError("DAY " + currentDay + "의 DayDefinition이 연결되지 않았습니다.");
            return;
        }

        if (examinationController == null)
        {
            Debug.LogError("ExaminationController가 연결되지 않았습니다.");
            return;
        }

        DayDefinition definition = days[index];
        int seed = BaseSeed + currentDay;
        DayEventManager dayEvents = GetComponent<DayEventManager>();

        // Run the DAY 9/14 review before citizens are loaded.
        StoryProgressController story = GetComponent<StoryProgressController>();
        if (story != null)
        {
            story.BeginDay(currentDay, examinationController);
            if (game.HasFatalStoryResult())
            {
                Debug.Log("DAY " + currentDay + " 차장 점검으로 진행 중단. 엔딩 연결 대기.");
                return;
            }
        }

        // Listen once for decisions that affect later NPC visits.
        examinationController.CitizenJudged -= OnCitizenJudged;
        examinationController.CitizenJudged += OnCitizenJudged;

        // The planner uses these overrides during ExaminationController.LoadDay().
        // No ScriptableObject asset is modified, so the setup survives UI replacement.
        ConfigureStorySchedule(definition, currentDay, seed, game, dayEvents);

        bool loaded = examinationController.LoadDay(definition, seed);
        if (!loaded)
        {
            Debug.LogError("DAY " + currentDay + " 로드 실패. 시민 수 / 비정상 조건 / NPC 데이터를 확인하세요.");
            return;
        }

        game.ProcessMerchantVisit(currentDay);

        if (dayEvents != null)
        {
            dayEvents.Bind(examinationController);
            dayEvents.BeginDay(currentDay, examinationController.TotalCitizenCount);
        }

        Debug.Log("DAY " + currentDay + " 데이터 및 NPC 대기열을 불러왔습니다.");
    }

    // The verdict engine needs the same case identity used by the schedule.
    // Do not rely on NPC names, which are editable in the Inspector.
    public bool IsPriestCase(CitizenCaseInstance citizen)
    {
        return citizen != null && citizen.IsStoryCase &&
            ((priestCase != null && citizen.SourceCase == priestCase) ||
             (priestDay12Case != null && citizen.SourceCase == priestDay12Case));
    }

    public bool IsQuackDoctorCase(CitizenCaseInstance citizen)
    {
        return citizen != null && citizen.IsStoryCase &&
            quackDoctorCase != null && citizen.SourceCase == quackDoctorCase;
    }

    private void OnCitizenJudged(CitizenCaseInstance citizen, CaseDisposition disposition, bool isCorrect)
    {
        GameManager game = GameManager.Instance;
        if (game == null || citizen == null) return;

        int day = game.currentDay;
        if (disposition == CaseDisposition.Detention)
            game.RecordDetention();

        // DAY 11: introduction only. Ordinary approval/rejection is not the cross choice.
        // DAY 12: verdict itself records the cross request and priest relationship.
        // DAY 13: no extra trust change unless the priest is detained.
        bool isPriest = (priestCase != null && citizen.SourceCase == priestCase) ||
                        (priestDay12Case != null && citizen.SourceCase == priestDay12Case);
        if (isPriest && day >= 11 && day <= 13 && lastPriestRecordedDay != day)
        {
            lastPriestRecordedDay = day;
            if (day == 12)
            {
                // Only the three authored dispositions change the priest relationship.
                // Treatment referral has no defined story effect in the design.
                if (disposition == CaseDisposition.ExtractionApproval ||
                    disposition == CaseDisposition.DocumentRejection ||
                    disposition == CaseDisposition.Detention)
                {
                    // Record the choice before a detention marks the priest unavailable.
                    game.RecordCrossDecision(disposition == CaseDisposition.ExtractionApproval);
                    game.RecordPriestResult(disposition.ToString());
                    Debug.Log("DAY 12 성직자 십자가 요청 처리: " + disposition +
                        ", 십자가 보관=" + game.hasCross + ", 성직자 구금=" + game.priestDetained);
                }
                else
                {
                    Debug.LogWarning("DAY 12 성직자: 치료 전환은 기획에 정의된 십자가 선택이 아닙니다.");
                }
            }
            else if (disposition == CaseDisposition.Detention)
            {
                game.RecordPriestResult(disposition.ToString());
                Debug.Log("DAY " + day + " 성직자 구금: 이후 등장하지 않습니다.");
            }
            else
            {
                Debug.Log("DAY " + day + " 성직자 만남 완료 (십자가 선택은 DAY 12에서 처리).");
            }
        }

        if (quackDoctorCase != null && citizen.SourceCase == quackDoctorCase &&
            (day == 14 || day == 15) && lastDoctorRecordedDay != day)
        {
            lastDoctorRecordedDay = day;
            game.RecordQuackDoctorResult(day, disposition.ToString());
        }
    }

    private void ConfigureStorySchedule(
        DayDefinition definition, int day, int seed,
        GameManager game, DayEventManager dayEvents)
    {
        var additions = new List<RuntimeStoryCaseEntry>();
        var suppressed = new List<CitizenCaseData>();

        // DAY 5: first appearance; DAY 6-10: seeded chance; DAY 11-12: story revisit.
        // If detained from DAY 7, the addict does not return.
        CitizenCaseData addictCase = dayEvents != null ? dayEvents.AddictCase : null;
        if (addictCase != null && day >= 5 && day <= 12)
        {
            bool shouldVisit = day == 5 ||
                (game.CanAddictReturn(day) &&
                 (day == 11 || day == 12 || SeededVisitChance(seed, 47)));

            if (shouldVisit)
                additions.Add(new RuntimeStoryCaseEntry(addictCase,
                    day == 5 ? 3 : day == 11 ? 4 : day == 12 ? 3 : 2));
            else
                suppressed.Add(addictCase);
        }

        // The white organization appears on DAY 7-13. Their final DAY 13 visit
        // is suppressed when the priest has already been detained.
        if (whiteAgentCase != null && day >= 7 && day <= 13)
        {
            if (day == 13 && game.priestDetained)
                suppressed.Add(whiteAgentCase);
            else
                additions.Add(new RuntimeStoryCaseEntry(whiteAgentCase, day == 7 ? 3 : 2));
        }

        // DAY 11 and 13 use the introductory priest case. DAY 12 can use
        // a separate CitizenCaseData asset so the cross-request dialogue differs.
        if (day >= 11 && day <= 13)
        {
            CitizenCaseData activePriestCase =
                (day == 12 && priestDay12Case != null) ? priestDay12Case : priestCase;

            // Suppress day-inappropriate authored slots to avoid duplicate priests.
            if (priestCase != null && activePriestCase != priestCase)
                suppressed.Add(priestCase);
            if (priestDay12Case != null && activePriestCase != priestDay12Case)
                suppressed.Add(priestDay12Case);

            if (game.priestDetained)
            {
                if (priestCase != null) suppressed.Add(priestCase);
                if (priestDay12Case != null) suppressed.Add(priestDay12Case);
            }
            else if (activePriestCase != null)
            {
                additions.Add(new RuntimeStoryCaseEntry(activePriestCase, 6));
                if (day == 12 && priestDay12Case == null)
                    Debug.LogWarning("DAY 12: Priest Day12 Case가 없어 기존 성직자 대사를 사용합니다.");
            }
            else
            {
                Debug.LogWarning("DAY " + day + ": CampaignManager에 Priest Case가 연결되지 않았습니다.");
            }
        }

        if (quackDoctorCase != null && (day == 14 || day == 15))
        {
            if (day == 14 || game.ShouldQuackDoctorReturn(day))
                additions.Add(new RuntimeStoryCaseEntry(quackDoctorCase, day == 14 ? 5 : 4));
            else
                suppressed.Add(quackDoctorCase);
        }

        if (day == 15 && day15AgencyAgentCase != null)
            additions.Add(new RuntimeStoryCaseEntry(day15AgencyAgentCase, 7));

        // DAY 16 exception: schedule the citizen who must be detained.
        // The story case is supplied by the LateDayEventController asset field.
        var late = game.GetComponent<LateDayEventController>();
        if (day == 16 && late != null && late.PleadingCitizenCase != null)
            additions.Add(new RuntimeStoryCaseEntry(late.PleadingCitizenCase, 5));

        RuntimeStoryCaseOverrides.Configure(definition, additions, suppressed);
    }

    private static bool SeededVisitChance(int seed, int percent)
    {
        // This does not consume UnityEngine.Random's shared random state.
        var random = new System.Random(unchecked(seed * 397 + 17011));
        return random.Next(100) < percent;
    }
}
