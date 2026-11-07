
using ProjectED.Cases;
using ProjectED.Days;
using ProjectED.Examination;
using UnityEngine;

public class DayEventManager : MonoBehaviour
{
    [Header("?Š¹?ˆ˜ NPC ?°?´?„°")]
    [SerializeField] private CitizenCaseData merchantCase;
    [SerializeField] private CitizenCaseData addictCase;

    public CitizenCaseData ActiveMerchantCase { get; private set; }
    public CitizenCaseData AddictCase => addictCase;

    // ?´?›„ CampaignManager?—?„œ ?‹¤? œ ?‹œë¯? ???ê¸°ì—´?— ?„£?„ ?•Œ ì°¸ì¡°?•œ?‹¤.
    public bool ShouldScheduleAddict(int day)
    {
        if (addictCase == null || GameManager.Instance == null) return false;
        if (day == 5) return true;
        return GameManager.Instance.CanAddictReturn(day);
    }

    [Header("?˜„?ž¬ DAY ?´ë²¤íŠ¸")]
    public int activeDay;
    public bool merchantOfferPending;
    public bool addictTriggeredToday;
    public bool day6AngerTriggered;

    [Header("?ˆ„?  ?´ë²¤íŠ¸ ê¸°ë¡")]
    public int addictEncounterCount;

    private ExaminationController examinationController;
    private int totalCitizens;
    private bool merchantTriggeredToday;

    public void Bind(ExaminationController controller)
    {
        if (examinationController == controller)
            return;

        if (examinationController != null)
        {
            examinationController.CitizenJudged -= OnCitizenJudged;
        }

        examinationController = controller;

        if (examinationController != null)
        {
            examinationController.CitizenJudged += OnCitizenJudged;
        }
    }

    private void OnDestroy()
    {
        if (examinationController != null)
        {
            examinationController.CitizenJudged -= OnCitizenJudged;
        }
    }

    public void BeginDay(int day, int citizenCount)
    {
        if (activeDay == day && totalCitizens == citizenCount)
            return;
        activeDay = day;
        totalCitizens = citizenCount;

        merchantTriggeredToday = false;
        merchantOfferPending = false;
        addictTriggeredToday = false;
        day6AngerTriggered = false;
        ActiveMerchantCase = null;

        Debug.Log("DAY " + day + " ?´ë²¤íŠ¸ ì¤?ë¹? ?™„ë£?");
    }

    private void OnCitizenJudged(
        CitizenCaseInstance citizen,
        CaseDisposition disposition,
        bool isCorrect)
    {
        if (examinationController == null)
            return;

        int position = examinationController.CurrentCitizenPosition;

        int middlePosition = Mathf.Max(
            1,
            Mathf.CeilToInt(totalCitizens / 2f)
        );

        // ?ƒ?¸ ?´ë²¤íŠ¸
        if (!merchantTriggeredToday &&
            GameManager.Instance != null &&
            GameManager.Instance.merchantPresentToday &&
            position == middlePosition)
        {
            merchantTriggeredToday = true;
            merchantOfferPending = true;
            ActiveMerchantCase = merchantCase;

            Debug.Log("?ƒ?¸ ê·¸ë¦¼ êµ¬ë§¤ ? œ?•ˆ ë°œìƒ");
        }

        // DAY 5-12 ì¤‘ë…?ž ?‚¬ê±? ?‹ë³?
        if (activeDay >= 5 && activeDay <= 12 &&
            !addictTriggeredToday &&
            addictCase != null &&
            citizen != null &&
            citizen.SourceCase == addictCase)
        {
            addictTriggeredToday = true;
            addictEncounterCount++;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RecordAddictResult(activeDay, disposition.ToString());
            }

            Debug.Log("ì¤‘ë…?ž ?‹¬?‚¬ ?™„ë£?");
            Debug.Log("ì¤‘ë…?ž ì²˜ë¦¬: " + disposition);
        }

        // DAY 6 ë§ˆì??ë§? ?‹œë¯? ë°˜ë ¤
        if (activeDay == 6 &&
            !day6AngerTriggered &&
            position == totalCitizens &&
            disposition == CaseDisposition.DocumentRejection)
        {
            day6AngerTriggered = true;
            Debug.Log("DAY 6 ë§ˆì??ë§? ?‹œë¯? ì§„ìƒ ?´ë²¤íŠ¸");
        }
    }

    [ContextMenu("?ƒ?¸ ? œ?•ˆ ?ˆ˜?½")]
    public void AcceptMerchantOffer()
    {
        if (!merchantOfferPending)
            return;

        bool success =
            GameManager.Instance != null &&
            GameManager.Instance.TryBuyMerchantPainting();

        merchantOfferPending = false;

        Debug.Log(success
            ? "ê·¸ë¦¼ ?š?“ ?„±ê³?"
            : "ê·¸ë¦¼ ?š?“ ?‹¤?Œ¨");
    }

    [ContextMenu("?ƒ?¸ ? œ?•ˆ ê±°ì ˆ")]
    public void RejectMerchantOffer()
    {
        if (!merchantOfferPending)
            return;

        merchantOfferPending = false;
        Debug.Log("?ƒ?¸ ? œ?•ˆ ê±°ì ˆ");
    }
}
