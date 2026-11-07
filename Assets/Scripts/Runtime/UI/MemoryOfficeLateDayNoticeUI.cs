using ProjectED.Examination;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Neutral system notices for already-authored DAY 14, 15 and 17 events.
/// This does not invent NPC dialogue or alter encounter rules.
/// </summary>
[DisallowMultipleComponent]
public sealed class MemoryOfficeLateDayNoticeUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text heading;
    [SerializeField] private TMP_Text body;
    [SerializeField] private Button closeButton;

    private int lastNotifiedDay = -1;

    private void Awake()
    {
        if (panel == null) panel = GetComponent<CanvasGroup>();
        SetVisible(false);
    }

    private void OnEnable()
    {
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnDisable()
    {
        if (closeButton != null) closeButton.onClick.RemoveListener(Close);
    }

    private void Update()
    {
        GameManager game = GameManager.Instance;
        if (game == null || lastNotifiedDay == game.currentDay) return;

        var ending = game.GetComponent<EndingManager>();
        if (ending != null && ending.HasResolvedEnding) return;
        CampaignManager campaign = CampaignManager.Instance;
        if (campaign == null) return;

        int day = game.currentDay;
        if (day != 14 && day != 15 && day != 17)
        {
            lastNotifiedDay = day;
            return;
        }
        if (day == 14)
        {
            StoryProgressController story = campaign.GetComponent<StoryProgressController>();
            if (story == null || story.ActiveDay != day) return;
            if (heading != null) heading.text = "DAY 14 · 차장 점검";
            if (body != null)
            {
                switch (story.LastReviewOutcome)
                {
                    case DeputyReviewOutcome.Award:
                        body.text = "점검 결과: 특별상이 지급되었습니다.";
                        break;
                    case DeputyReviewOutcome.Arrested:
                        body.text = "점검 결과: 체포 처리되었습니다.";
                        break;
                    case DeputyReviewOutcome.Fired:
                    case DeputyReviewOutcome.Warning:
                        body.text = "점검 결과: 해고 처리되었습니다.";
                        break;
                    default:
                        body.text = "차장 점검이 완료되었습니다. 추가 처분은 없습니다.";
                        break;
                }
            }
        }
        else if (day == 15)
        {
            // Don't spoil the agency-agent encounter at the start of the day.
            var examination = campaign.examinationController;
            var citizen = examination != null ? examination.CurrentCitizen : null;
            if (citizen == null || !citizen.IsStoryCase || citizen.SourceCase == null ||
                citizen.SourceCase.CaseId != "DAY15_AGENCY_AGENT") return;
            if (heading != null) heading.text = "DAY 15 · 기관 요원";
            if (body != null) body.text = "기관 요원이 방문했습니다. 서류와 안내를 확인하세요.";
        }
        else
        {
            LateDayEventController late = game.GetComponent<LateDayEventController>();
            if (late == null || !late.DeputyAnnouncedDirector) return;
            if (heading != null) heading.text = "DAY 17 · 차장 공지";
            if (body != null) body.text = "국장이 DAY 18에 방문할 예정입니다.";
        }
        lastNotifiedDay = day;
        SetVisible(true);
    }

    public void Close() => SetVisible(false);

    private void SetVisible(bool show)
    {
        if (panel == null) return;
        panel.alpha = show ? 1f : 0f;
        panel.interactable = show;
        panel.blocksRaycasts = show;
    }
}
