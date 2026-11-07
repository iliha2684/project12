using ProjectED.Examination;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 기존 NPC 이벤트의 수락/거절 함수를 Play 화면의 팝업에 연결한다.
/// 심사 판정(추출 승인/서류 반려)과는 독립된 선택 UI이다.
/// </summary>
public sealed class MemoryOfficeNpcOfferPrompt : MonoBehaviour
{
    private enum OfferKind { None, MerchantPainting, WhiteFinal }

    [SerializeField] private CanvasGroup popupGroup;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text acceptText;
    [SerializeField] private TMP_Text rejectText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private ExaminationController examinationController;

    private OfferKind visibleOffer = OfferKind.None;

    private void Awake()
    {
        if (popupGroup == null) popupGroup = GetComponent<CanvasGroup>();
        Show(OfferKind.None);
    }

    private void OnEnable()
    {
        if (acceptButton != null) acceptButton.onClick.AddListener(Accept);
        if (rejectButton != null) rejectButton.onClick.AddListener(Reject);
    }

    private void OnDisable()
    {
        if (acceptButton != null) acceptButton.onClick.RemoveListener(Accept);
        if (rejectButton != null) rejectButton.onClick.RemoveListener(Reject);
    }

    // DAY가 바뀌거나 NPC 이벤트가 발생하는 즉시 다음 프레임에 동기화한다.
    private void LateUpdate()
    {
        OfferKind upcoming = FindPendingOffer();
        if (upcoming != visibleOffer)
            Show(upcoming);

        // 그림은 최대 4개. 더 구매할 수 없을 때는 거절만 가능하다.
        if (upcoming == OfferKind.MerchantPainting && acceptButton != null)
        {
            GameManager game = GameManager.Instance;
            bool canBuy = game != null && game.merchantPresentToday &&
                          !game.merchantPaintingPurchasedToday && game.paintingCount < 4;
            acceptButton.interactable = canBuy;
            if (descriptionText != null && game != null)
            {
                descriptionText.text = canBuy
                    ? "상인이 그림을 제안했습니다.\n그림을 구매하시겠습니까?\n현재 소지: " + game.paintingCount + " / 4개"
                    : "상인이 그림을 제안했습니다.\n이미 그림을 4개 보유했거나 구매할 수 없습니다.\n거절하여 심사를 계속하세요.";
            }
        }
    }

    private OfferKind FindPendingOffer()
    {
        GameManager game = GameManager.Instance;
        if (game == null || game.IsGameOver()) return OfferKind.None;

        EndingManager ending = game.GetComponent<EndingManager>();
        if (ending != null && ending.HasResolvedEnding) return OfferKind.None;

        CampaignManager campaign = CampaignManager.Instance;
        if (campaign == null) return OfferKind.None;

        DayEventManager events = campaign.GetComponent<DayEventManager>();
        if (events != null && events.merchantOfferPending)
            return OfferKind.MerchantPainting;

        StoryProgressController story = campaign.GetComponent<StoryProgressController>();
        if (story != null && story.WaitingForWhiteFinalOffer)
            return OfferKind.WhiteFinal;

        return OfferKind.None;
    }

    private void Show(OfferKind offer)
    {
        visibleOffer = offer;
        if (popupGroup != null)
        {
            bool show = offer != OfferKind.None;
            popupGroup.alpha = show ? 1f : 0f;
            popupGroup.interactable = show;
            popupGroup.blocksRaycasts = show;
        }

        if (offer == OfferKind.None) return;
        if (acceptButton != null) acceptButton.interactable = true;
        if (rejectButton != null) rejectButton.interactable = true;

        if (offer == OfferKind.MerchantPainting)
        {
            if (titleText != null) titleText.text = "상인의 제안";
            if (acceptText != null) acceptText.text = "수락 (구매)";
            if (rejectText != null) rejectText.text = "거절";
        }
        else if (offer == OfferKind.WhiteFinal)
        {
            if (titleText != null) titleText.text = "백색서고의 최종 제안";
            if (descriptionText != null)
                descriptionText.text = "백색서고의 마지막 제안입니다.\n수락하거나 거절하시겠습니까?";
            if (acceptText != null) acceptText.text = "수락";
            if (rejectText != null) rejectText.text = "거절";
        }
    }

    public void Accept() { Resolve(true); }
    public void Reject() { Resolve(false); }

    private void Resolve(bool accept)
    {
        // 팝업이 떠 있는 현재 이벤트인지 확인해 중복/잘못된 판정을 방지한다.
        if (visibleOffer == OfferKind.None || FindPendingOffer() != visibleOffer)
            return;

        CampaignManager campaign = CampaignManager.Instance;
        if (campaign == null) return;

        if (visibleOffer == OfferKind.MerchantPainting)
        {
            DayEventManager events = campaign.GetComponent<DayEventManager>();
            if (events == null || !events.merchantOfferPending) return;

            if (accept)
            {
                GameManager game = GameManager.Instance;
                if (game == null || game.paintingCount >= 4 ||
                    game.merchantPaintingPurchasedToday) return;
                events.AcceptMerchantOffer();
            }
            else
            {
                events.RejectMerchantOffer();
            }

            if (events.merchantOfferPending) return;
            Show(OfferKind.None);
            Debug.Log(accept ? "[NPC 선택] 상인 제안 수락" : "[NPC 선택] 상인 제안 거절");

            // 상인 제안 때문에 멈춘 결과 화면에서 기존 '다음 시민' 흐름을 재개.
            // 판정 중이라면 HasActiveCase가 true이므로 중복 판정하지 않는다.
            if (examinationController != null && !examinationController.HasActiveCase)
                examinationController.StampSelectedDisposition();
        }
        else if (visibleOffer == OfferKind.WhiteFinal)
        {
            StoryProgressController story = campaign.GetComponent<StoryProgressController>();
            if (story == null || !story.WaitingForWhiteFinalOffer) return;
            if (!story.ResolveWhiteFinalOffer(accept)) return;
            Show(OfferKind.None);
            Debug.Log(accept ? "[NPC 선택] 백색서고 최종 제안 수락" : "[NPC 선택] 백색서고 최종 제안 거절");
        }
    }
}
