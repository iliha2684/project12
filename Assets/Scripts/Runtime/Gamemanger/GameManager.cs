using UnityEngine;

public class GameManager : MonoBehaviour
{
    // GameManager를 다른 스크립트에서도 쉽게 사용할 수 있게 함
    public static GameManager Instance;

    [Header("게임 진행")]
    public int currentDay = 1;

    [Header("플레이어 상태")]
    public int errorCount = 0;
    public int trust = 50;
    public int mental = 50;

    [Header("보상")]
    public int paintingCount = 0;
    [Header("상인 시스템")]
    public int merchantVisitCount = 0;
    public bool merchantPresentToday = false;
    public bool merchantPaintingPurchasedToday = false;

    private int lastMerchantProcessedDay = 0;
    public int specialAwardCount = 0;

    [Header("백색말소")]
    public int whiteMissionCount = 0;
    public bool helpedWhiteOrganization = false;

    [Header("성직자")]
    public int priestTrust = 0;
    public bool priestDetained = false;
    [Header("중독자 진행 기록")]
    public bool addictMet = false;
    public int addictVisitCount = 0;
    public int addictLastVisitDay = 0;
    public string addictLastDisposition = "";
    public bool addictDetained = false;
    public bool hasCross = false;

    [Header("이벤트 아이템")]
    public bool hasGun = false;
    public bool hasLetter = false;

    [Header("단말기")]
    public int terminalClueCount = 0;

    [Header("스토리 선택 기록")]
    public int detentionCount = 0;
    public int warningCount = 0;
    public bool hasConfidentialFile = false;
    public bool passedWhiteAgent = false;
    public bool gaveRegulationToWhiteAgent = false;
    public bool acceptedWhiteFinalOffer = false;
    public bool whiteFinalOfferAvailable = false;
    public bool whiteFinalOfferDecided = false;
    public int whiteMissionRefusedCount = 0;
    public int lastWhiteAgentJudgedDay = 0;
    public bool priestRequestCompleted = false;
    public bool quackDoctorRejected = false;
    public bool quackDoctorDetained = false;
    public bool deputyNameSearched = false;
    public bool firedByReview = false;
    public bool arrestedByReview = false;
    [Header("차장 특별상 능력치 보상 (임시 밸런스, Inspector에서 조정)")]
    [Min(0)] [SerializeField] private int awardTrustRecovery = 10;
    [Min(0)] [SerializeField] private int awardMentalRecovery = 10;

    private int lastReviewedDay = 0;
    private int lastTerminalClueDay = 0;


    private void Awake()
    {
        // GameManager가 이미 존재하면 새로 생긴 것은 제거
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Scene이 바뀌어도 GameManager 유지
        DontDestroyOnLoad(gameObject);
    }


    // 다음 날로 이동
    public void GoToNextDay()
    {
        if (currentDay >= 18)
        {
            Debug.Log("18일차가 끝났습니다.");
            return;
        }

        currentDay++;

        Debug.Log("현재 DAY : " + currentDay);
    }


    // 심사 결과 기록
    public void ApplyExaminationResult(bool isCorrect)
    {
        // 정답이면 아무 패널티 없음
        if (isCorrect)
        {
            Debug.Log("정상 처리");
            return;
        }

        // 오답이면 오류 횟수 증가
        errorCount++;

        // 신뢰도 감소
        trust--;

        // 신뢰도가 0 아래로 내려가지 않게 함
        if (trust < 0)
        {
            trust = 0;
        }

        Debug.Log("오류 횟수 : " + errorCount);
        Debug.Log("현재 신뢰도 : " + trust);

        if (IsGameOver())
        {
            Debug.Log("게임 오버 조건 충족");
        }
    }
    public bool IsGameOver()
    {
        return trust <= 0 || mental <= 0 || firedByReview || arrestedByReview;
    }

    
    public void RecordAddictResult(int day, string disposition)
    {
        if (addictLastVisitDay == day)
        {
            return;
        }

        addictMet = true;
        addictVisitCount++;
        addictLastVisitDay = day;
        addictLastDisposition = disposition;

        if (day >= 7 && (disposition == "Detention" || disposition == "EmergencyDetention"))
        {
            addictDetained = true;
        }

        Debug.Log("중독자 방문 횟수: " + addictVisitCount);
        Debug.Log("중독자 마지막 처분: " + disposition);
    }

    public bool CanAddictReturn(int day)
    {
        return addictMet &&
            !addictDetained &&
            day >= 6 &&
            day <= 12 &&
            day > addictLastVisitDay;
    }

    
    public void ProcessMerchantVisit(int day)
    {
        if (lastMerchantProcessedDay == day)
        {
            return;
        }

        lastMerchantProcessedDay = day;
        merchantPresentToday = false;
        merchantPaintingPurchasedToday = false;

        // DAY 4 이전과 DAY 18에는 상인이 방문하지 않는다.
        if (day < 4 || day > 17)
        {
            return;
        }

        // DAY 4는 고정 방문
        bool willVisit = day == 4;

        if (day > 4)
        {
            // DAY 17까지 최소 5번 방문하도록 보장
            int remainingDays = 18 - day;
            int neededVisits = Mathf.Max(0, 5 - merchantVisitCount);

            willVisit = neededVisits >= remainingDays ||
                        Random.value < 0.35f;
        }

        merchantPresentToday = willVisit;

        if (merchantPresentToday)
        {
            merchantVisitCount++;
            Debug.Log("상인 방문: DAY " + day);
            Debug.Log("누적 방문 횟수: " + merchantVisitCount);
        }
    }

    public bool TryBuyMerchantPainting()
    {
        // 상인이 없으면 그림을 받을 수 없다.
        if (!merchantPresentToday)
        {
            return false;
        }

        // 같은 날 두 번 획득할 수 없다.
        if (merchantPaintingPurchasedToday)
        {
            return false;
        }

        // 우선 엔딩 조건인 그림 4개까지만 획득
        if (paintingCount >= 4)
        {
            return false;
        }

        paintingCount++;
        merchantPaintingPurchasedToday = true;

        Debug.Log("그림 획득 완료");
        Debug.Log("현재 그림 개수: " + paintingCount);

        return true;
    }

    // UI와 무관한 상태 기록 API. 화면은 추후 이 함수만 호출하면 된다.
    public void RecordDetention()
    {
        detentionCount++;
    }

    public void RecordWhiteMission(int missionNumber, bool accepted)
    {
        if (!accepted) return;
        if (missionNumber < 1 || missionNumber > 5) return;

        // 미션은 순서대로 한 번씩만 수락 가능
        if (missionNumber != whiteMissionCount + 1) return;
        whiteMissionCount++;
        helpedWhiteOrganization = true;
        switch (missionNumber)
        {
            case 1: hasGun = true; break;
            case 2: hasLetter = true; break;
            case 3: passedWhiteAgent = true; break;
            case 4: gaveRegulationToWhiteAgent = true; break;
            case 5: hasConfidentialFile = true; break;
        }
    }

    public void RecordPriestResult(string disposition)
    {
        if (priestDetained) return;
        if (disposition == "EmergencyDetention" || disposition == "Detention")
        {
            priestDetained = true;
            priestTrust = Mathf.Max(0, priestTrust - 1);
        }
        else if (disposition == "DocumentRejection")
        {
            priestTrust = Mathf.Max(0, priestTrust - 1);
        }
        else
        {
            priestTrust++;
        }
    }

    public void RecordCrossDecision(bool kept)
    {
        if (priestDetained) return;
        hasCross = kept;
        priestRequestCompleted = kept;
    }

    public void RecordTerminalClue(int day)
    {
        // 같은 날 중복 입력 방지. 여러 단서를 하루에 얻어야 하면 확장 필요.
        if (lastTerminalClueDay == day) return;
        lastTerminalClueDay = day;
        terminalClueCount++;
    }

    public void RecordQuackDoctorResult(int day, string disposition)
    {
        if (day != 14 && day != 15) return;
        if (disposition == "EmergencyDetention" || disposition == "Detention")
            quackDoctorDetained = true;
        else if (disposition == "DocumentRejection")
            quackDoctorRejected = true;
    }

    public bool ShouldQuackDoctorReturn(int day)
    {
        return day == 15 && quackDoctorRejected && !quackDoctorDetained;
    }

    // 점검 결과는 DAY별 한 번만 반영. DAY 9/14 세부 조건은 별도 구현.
    public void RecordReviewResult(int day, bool award, bool warning, bool fired, bool arrested)
    {
        if (lastReviewedDay == day) return;
        if (day != 9 && day != 14) return;
        lastReviewedDay = day;
        if (award)
        {
            specialAwardCount++;
            // Without any recovery, the initial 50/50 stats could never reach
            // the existing 70/70 Happy ending requirements. Keep these
            // temporary amounts configurable rather than hard-coding story rules.
            trust = Mathf.Clamp(trust + awardTrustRecovery, 0, 100);
            mental = Mathf.Clamp(mental + awardMentalRecovery, 0, 100);
            Debug.Log("DAY " + day + " 특별상: 신뢰도 +" + awardTrustRecovery +
                ", 정신력 +" + awardMentalRecovery);
        }
        if (warning) warningCount++;
        firedByReview |= fired || warning;

        arrestedByReview |= arrested;
    }

    public bool HasFatalStoryResult()
    {
        return IsGameOver() || firedByReview || arrestedByReview;
    }
}