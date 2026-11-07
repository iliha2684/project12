using System;
using UnityEngine;

// Endings are data, not scenes. A future UI can subscribe to EndingResolved.
public enum MemoryOfficeEnding
{
    None = 0,
    Bad1Dismissal = 1,
    Bad2WhiteOrganization = 2,
    Bad3Priest = 3,
    Bad4Arrest = 4,
    Normal = 5,
    Happy = 6,
    Obedience = 7
}

[DisallowMultipleComponent]
public sealed class EndingManager : MonoBehaviour
{
    [Header("임시 엔딩 수치 기준 - Inspector에서 조정 가능")]
    [Min(1)] [SerializeField] private int happyRequiredPaintings = 4;
    [Min(1)] [SerializeField] private int happyRequiredAwards = 2;
    [Min(1)] [SerializeField] private int happyRequiredTerminalClues = 3;
    [Range(0, 100)] [SerializeField] private int happyRequiredTrust = 70;
    [Range(0, 100)] [SerializeField] private int happyRequiredMental = 70;

    [Header("진행 결과 (읽기용)")]
    [SerializeField] private MemoryOfficeEnding resolvedEnding = MemoryOfficeEnding.None;
    [SerializeField] private string resolutionReason = "";
    [SerializeField] private bool quackDoctorFatal;
    [SerializeField] private bool illegalContactDiscovered;
    [SerializeField] private bool day13ReviewCompleted;

    public MemoryOfficeEnding ResolvedEnding => resolvedEnding;
    public string ResolutionReason => resolutionReason;
    public bool HasResolvedEnding => resolvedEnding != MemoryOfficeEnding.None;
    public bool Day13ReviewCompleted => day13ReviewCompleted;
    public event Action<MemoryOfficeEnding, string> EndingResolved;

    private void Update()
    {
        // Fatal outcomes may be triggered at any time, including a deputy review.
        // The director visit is the only point where non-bad endings are finalized.
        if (!HasResolvedEnding)
            EvaluateCurrentState();
    }

    public MemoryOfficeEnding EvaluateCurrentState()
    {
        if (HasResolvedEnding)
            return resolvedEnding;

        GameManager game = GameManager.Instance;
        if (game == null)
            return MemoryOfficeEnding.None;

        // Bad endings take priority over final-day endings.
        if (game.trust <= 0 || game.mental <= 0 || game.firedByReview || quackDoctorFatal)
            return Resolve(MemoryOfficeEnding.Bad1Dismissal, "해고 또는 돌팔이 의사 사건");

        if (game.arrestedByReview || illegalContactDiscovered)
            return Resolve(MemoryOfficeEnding.Bad4Arrest, "반정부 접촉 발각 또는 차장 점검 체포");

        if (game.currentDay >= 13 && day13ReviewCompleted &&
            game.whiteMissionCount >= 5 && game.priestRequestCompleted &&
            !game.priestDetained && game.whiteFinalOfferDecided && game.acceptedWhiteFinalOffer)
        {
            return Resolve(MemoryOfficeEnding.Bad2WhiteOrganization, "조직 임무 전부 수행 후 최종 제안 수락");
        }

        if (game.currentDay >= 13 && day13ReviewCompleted &&
            game.whiteMissionCount > 0 && game.whiteMissionCount < 5 &&
            game.priestRequestCompleted && !game.priestDetained)
        {
            return Resolve(MemoryOfficeEnding.Bad3Priest, "조직 임무 일부 수행 및 성직자 요구 수락");
        }

        // DAY 18 has no citizen cases. Arrival of the director resolves one of
        // the three non-bad endings. No ending is awarded on DAY 1-17.
        if (game.currentDay < 18)
            return MemoryOfficeEnding.None;

        bool noWarnings = game.warningCount == 0;
        bool foundTruth = game.paintingCount >= happyRequiredPaintings &&
                          game.terminalClueCount >= happyRequiredTerminalClues;
        bool rejectedWhite = game.whiteMissionCount == 0 &&
                             !game.helpedWhiteOrganization;
        bool highStatus = game.trust >= happyRequiredTrust &&
                          game.mental >= happyRequiredMental;
        bool hasAwards = game.specialAwardCount >= happyRequiredAwards;

        if (noWarnings && foundTruth && rejectedWhite && hasAwards && highStatus)
            return Resolve(MemoryOfficeEnding.Happy, "모든 고발 조건 충족");

        if (foundTruth && noWarnings && rejectedWhite && (!hasAwards || !highStatus))
            return Resolve(MemoryOfficeEnding.Obedience, "진실은 알았지만 고발 조건 부족");

        // The design does not specify a warning-bearing final route.
        // Do not mislabel it as the warning-free Normal ending.
        if (!noWarnings)
            return MemoryOfficeEnding.None;

        return Resolve(MemoryOfficeEnding.Normal, "경고 없이 근무 완료, 고발 조건 미달");
    }

    // Called once when DAY 13's final citizen has completed their inspection.
    // Bad2/Bad3 cannot be finalized until this point.
    public void CompleteDay13Review()
    {
        GameManager game = GameManager.Instance;
        if (day13ReviewCompleted || game == null || game.currentDay != 13)
            return;

        day13ReviewCompleted = true;
        Debug.Log("[EndingManager] DAY 13 심사 종료 확인. 성직자/백색서고 엔딩 판정 시작.");
        EvaluateCurrentState();
    }

    // Explicit story callbacks. They contain no UI/scene code.
    public void RecordQuackDoctorFatalOutcome()
    {
        quackDoctorFatal = true;
        EvaluateCurrentState();
    }

    public void RecordIllegalContactDiscovery()
    {
        illegalContactDiscovered = true;
        EvaluateCurrentState();
    }

    private MemoryOfficeEnding Resolve(MemoryOfficeEnding ending, string reason)
    {
        if (HasResolvedEnding)
            return resolvedEnding;

        resolvedEnding = ending;
        resolutionReason = reason;
        Debug.Log("[EndingManager] " + ending + ": " + reason);
        EndingResolved?.Invoke(ending, reason);
        return ending;
    }
}
