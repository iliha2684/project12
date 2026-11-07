using ProjectED.Days;
using ProjectED.Examination;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DAY 10 record-search interface. Uses actual Day10ArchiveRecords data (no fake results).
/// Installed as a separate child of the existing Canvas. Does not change judgement UI.
/// </summary>
[DisallowMultipleComponent]
public sealed class MemoryOfficeDay10TerminalUI : MonoBehaviour
{
    [SerializeField] private ExaminationController examination;
    [SerializeField] private Button monitorOpenButton;
    [SerializeField] private GameObject popup;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button deathSearchButton;
    [SerializeField] private Button incidentSearchButton;
    [SerializeField] private Button priestSearchButton;
    [SerializeField] private TMP_InputField citizenNumberInput;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text headingText;

    private int lastDay = -1;

    private void Awake()
    {
        if (popup != null) popup.SetActive(false);
        if (examination == null && CampaignManager.Instance != null)
            examination = CampaignManager.Instance.examinationController;
    }

    private void OnEnable()
    {
        if (monitorOpenButton != null) monitorOpenButton.onClick.AddListener(Open);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
        if (deathSearchButton != null) deathSearchButton.onClick.AddListener(SearchDeathRecords);
        if (incidentSearchButton != null) incidentSearchButton.onClick.AddListener(SearchIncidentRecords);
        if (priestSearchButton != null) priestSearchButton.onClick.AddListener(SearchPriestRecord);
        RefreshPriestSearchButtonVisibility();
    }

    private void OnDisable()
    {
        if (monitorOpenButton != null) monitorOpenButton.onClick.RemoveListener(Open);
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
        if (deathSearchButton != null) deathSearchButton.onClick.RemoveListener(SearchDeathRecords);
        if (incidentSearchButton != null) incidentSearchButton.onClick.RemoveListener(SearchIncidentRecords);
        if (priestSearchButton != null) priestSearchButton.onClick.RemoveListener(SearchPriestRecord);
    }

    private void Update()
    {
        var game = GameManager.Instance;
        if (game == null) return;
        if (lastDay == game.currentDay) return;
        lastDay = game.currentDay;
        RefreshPriestSearchButtonVisibility();
        if (popup != null && popup.activeSelf) Close();
    }

    /// <summary>
    /// Arrange the search buttons without leaving an obvious empty slot.
    /// DAY 11~17: three evenly spaced buttons. Other days: two wide, centered buttons.
    /// This changes presentation only; the existing search-day restrictions remain unchanged.
    /// </summary>
    private void RefreshPriestSearchButtonVisibility()
    {
        if (priestSearchButton == null) return;

        GameManager game = GameManager.Instance;
        bool showPriest = game != null && game.currentDay >= 11 && game.currentDay <= 17;
        if (priestSearchButton.gameObject.activeSelf != showPriest)
            priestSearchButton.gameObject.SetActive(showPriest);

        if (showPriest)
        {
            // DAY 17 unlocks deputy research without revealing a fourth button.
            TMP_Text personSearchLabel = priestSearchButton.GetComponentInChildren<TMP_Text>(true);
            if (personSearchLabel != null)
                personSearchLabel.text = game.currentDay == 17 ? "인물 기록 조회" : "성직자 기록 조회";
            // DAY 11~17: three evenly spaced controls.
            ArrangeSearchButton(deathSearchButton, -320f, 296f, 23f);
            ArrangeSearchButton(incidentSearchButton, 0f, 296f, 23f);
            ArrangeSearchButton(priestSearchButton, 320f, 296f, 23f);
        }
        else
        {
            // DAY 1~10 / DAY 18: return to the original two-wide-button layout.
            ArrangeSearchButton(deathSearchButton, -260f, 430f, 28f);
            ArrangeSearchButton(incidentSearchButton, 260f, 430f, 28f);
        }
    }

    private static void ArrangeSearchButton(Button button, float x, float width, float labelSize)
    {
        if (button == null) return;
        RectTransform rect = button.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = new Vector2(x, 0f);
            rect.sizeDelta = new Vector2(width, 72f);
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.fontSize = labelSize;
            RectTransform labelRect = label.GetComponent<RectTransform>();
            if (labelRect != null)
                labelRect.sizeDelta = new Vector2(width - 24f, 62f);
        }
    }

    private bool IsAvailable(out string reason)
    {
        reason = string.Empty;
        GameManager game = GameManager.Instance;
        if (game == null)
        {
            reason = "GameManager를 찾지 못했습니다.";
            return false;
        }
        if (game.IsGameOver())
        {
            reason = "게임이 종료되어 단말기를 사용할 수 없습니다.";
            return false;
        }
        var ending = game.GetComponent<EndingManager>();
        if (ending != null && ending.HasResolvedEnding)
        {
            reason = "엔딩이 확정되어 단말기를 사용할 수 없습니다.";
            return false;
        }
        var campaign = CampaignManager.Instance;
        if (examination == null && campaign != null)
            examination = campaign.examinationController;
        if (examination == null || examination.CurrentCitizen == null ||
            examination.CurrentDayPlan == null)
        {
            reason = "심사 중인 시민이 없습니다.";
            return false;
        }
        if (!examination.CurrentDayPlan.Definition.HasFeature(DayFeatureFlags.SearchTerminal))
        {
            reason = "오늘은 검색 단말기를 사용할 수 없습니다.";
            return false;
        }
        if (campaign != null)
        {
            var events = campaign.GetComponent<DayEventManager>();
            var story = campaign.GetComponent<StoryProgressController>();
            if ((events != null && events.merchantOfferPending) ||
                (story != null && story.WaitingForWhiteFinalOffer))
            {
                reason = "먼저 NPC의 제안을 처리해 주세요.";
                return false;
            }
        }
        return true;
    }

    public void Open()
    {
        string reason;
        if (!IsAvailable(out reason))
        {
            Debug.Log("[검색 단말기] " + reason);
            return;
        }
        if (popup == null) return;
        RefreshPriestSearchButtonVisibility();
        popup.SetActive(true);
        // Ensure this overlay renders above the artwork, and below nothing during use.
        transform.SetAsLastSibling();
        if (citizenNumberInput != null) citizenNumberInput.text = string.Empty;
        int day = GameManager.Instance.currentDay;
        SetResult(day == 10
            ? "시민번호를 입력하고 사망자 명부 또는 과거 사건 기록을 선택하세요."
            : day >= 11 && day <= 17
                ? (day == 17
                    ? "인물 기록은 '성직자' 또는 '차장'을 입력해 조회하세요."
                    : "성직자 기록은 검색어 '성직자'를 입력하고 조회하세요.")
                : "DAY 10부터 기록 보관소를 조회할 수 있습니다.");
        if (headingText != null)
            headingText.text = "기억관리국 · 기록 검색 | DAY " +
                GameManager.Instance.currentDay.ToString("00");
    }

    public void Close()
    {
        if (popup != null) popup.SetActive(false);
    }

    public void SearchDeathRecords()
    {
        Search(true);
    }

    public void SearchIncidentRecords()
    {
        Search(false);
    }

    /// <summary>
    /// Searches the existing deliberately unreliable priest entry.
    /// The priest case currently has no unique citizen number ("미정"),
    /// so require the unambiguous role keyword instead of matching a placeholder.
    /// </summary>
    public void SearchPriestRecord()
    {
        string reason;
        if (!IsAvailable(out reason))
        {
            SetResult(reason);
            return;
        }

        int day = GameManager.Instance.currentDay;
        if (day < 11 || day > 17)
        {
            SetResult("성직자 기록은 DAY 11~17에만 조회할 수 있습니다.");
            return;
        }

        string keyword = citizenNumberInput != null
            ? citizenNumberInput.text.Trim() : string.Empty;
        // DAY 17: search the deputy by role rather than fabricating a name.
        if (day == 17 && keyword == "차장")
        {
            var lateEvents = GameManager.Instance.GetComponent<LateDayEventController>();
            if (lateEvents == null)
            {
                SetResult("차장 검색 기록 시스템을 찾지 못했습니다.");
                return;
            }
            lateEvents.RecordTerminalResearch(day, TerminalResearchTopic.Deputy);
            SetResult("[차장 기록] 검색 내역을 기록했습니다.\n" +
                "인물의 상세 등록 자료는 아직 준비되지 않았습니다.");
            return;
        }

        if (keyword != "성직자")
        {
            SetResult(day == 17
                ? "인물 검색어: 성직자 또는 차장 (인물 고유번호 미등록)"
                : "성직자 기록 검색어는 '성직자'입니다. 시민번호는 아직 지정되지 않았습니다.");
            return;
        }

        CampaignManager campaign = CampaignManager.Instance;
        StoryProgressController story = campaign != null
            ? campaign.GetComponent<StoryProgressController>() : null;
        if (story == null)
        {
            SetResult("성직자 기록 검색 시스템을 찾지 못했습니다. CampaignManager를 확인하세요.");
            Debug.LogError("[검색 단말기] StoryProgressController를 찾지 못했습니다.");
            return;
        }

        string displayedRecord;
        bool found = story.TrySearchPriestRecord(out displayedRecord);
        SetResult(found
            ? "[성직자 기록] 조회 결과\n" + displayedRecord
            : "[성직자 기록] 현재 검색할 수 없습니다.");
    }

    private void Search(bool death)
    {
        string reason;
        if (!IsAvailable(out reason))
        {
            SetResult(reason);
            return;
        }
        var game = GameManager.Instance;
        if (game.currentDay != 10)
        {
            SetResult("이 기록의 검색 대상 날짜가 아닙니다. (DAY 10 전용)");
            return;
        }
        var records = game.GetComponent<Day10ArchiveRecords>();
        if (records == null || !records.HasPreparedDay10Records)
        {
            SetResult("DAY 10 기록이 아직 준비되지 않았습니다. Console과 DAY 데이터 설정을 확인하세요.");
            return;
        }
        string number = citizenNumberInput != null ? citizenNumberInput.text.Trim() : "";
        if (string.IsNullOrWhiteSpace(number))
        {
            SetResult("조회할 시민번호를 입력해 주세요.");
            return;
        }

        string message;
        bool found;
        if (death)
        {
            string note;
            found = records.TryFindDeathRecord(number, out note);
            message = found
                ? "[사망자 명부] 등록 기록 발견\n시민번호: " + number +
                  "\n기록: " + (string.IsNullOrWhiteSpace(note) ? "사망자 명부에 등록됨" : note)
                : "[사망자 명부] 검색 결과 없음\n시민번호: " + number;
        }
        else
        {
            string recordedName;
            string eventName;
            found = records.TryFindIncidentRecord(number, out recordedName, out eventName);
            message = found
                ? "[과거 사건 기록] 등록 기록 발견\n시민번호: " + number +
                  (string.IsNullOrWhiteSpace(recordedName) ? "" : "\n기록된 이름: " + recordedName) +
                  (string.IsNullOrWhiteSpace(eventName) ? "" : "\n기록된 사건: " + eventName)
                : "[과거 사건 기록] 검색 결과 없음\n시민번호: " + number;
        }
        SetResult(message);

        // The existing clue system accepts at most one terminal clue per day.
        if (found)
        {
            var lateEvents = game.GetComponent<LateDayEventController>();
            if (lateEvents != null)
            {
                lateEvents.RecordTerminalResearch(game.currentDay,
                    death ? TerminalResearchTopic.DeathRegister : TerminalResearchTopic.IncidentArchive);
            }
        }
    }

    private void SetResult(string message)
    {
        if (resultText != null) resultText.text = message;
    }
}
