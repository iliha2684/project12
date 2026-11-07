using UnityEngine;
using UnityEngine.UI;

namespace ProjectED.Examination
{
    /// <summary>
    /// 새 Canvas 이미지 위의 두 버튼만 연결한다.
    /// 기존 ExaminationController / DAY / NPC / .meta는 변경하지 않는다.
    /// </summary>
    public sealed class MemoryOfficeInstantDecisionBridge : MonoBehaviour
    {
        [SerializeField] private ExaminationController examinationController;
        [SerializeField] private Button approvalButton;
        [SerializeField] private Button rejectionButton;

        private void OnEnable()
        {
            if (approvalButton != null)
                approvalButton.onClick.AddListener(Approve);
            if (rejectionButton != null)
                rejectionButton.onClick.AddListener(Reject);
        }

        private void OnDisable()
        {
            if (approvalButton != null)
                approvalButton.onClick.RemoveListener(Approve);
            if (rejectionButton != null)
                rejectionButton.onClick.RemoveListener(Reject);
        }

        private void Update()
        {
            // 대화 중 / 결과 확인 중 / 게임 오버일 때 새 판정 버튼 클릭 금지.
            bool canDecide = CanDecide();
            if (approvalButton != null && approvalButton.interactable != canDecide)
                approvalButton.interactable = canDecide;
            if (rejectionButton != null && rejectionButton.interactable != canDecide)
                rejectionButton.interactable = canDecide;
        }

        private bool CanDecide()
        {
            if (examinationController == null || !examinationController.HasActiveCase)
                return false;

            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.IsGameOver()) return false;
                var endingManager = GameManager.Instance.GetComponent<EndingManager>();
                if (endingManager != null && endingManager.HasResolvedEnding) return false;
            }
            return true;
        }

        /// <summary>
        /// 새 UI에서는 승인/반려 버튼 하나로 판정 확정 후 다음 시민까지 진행한다.
        /// 기존 ExaminationController를 수정하지 않고, 공개된 API를 순서대로 호출한다.
        /// </summary>
        public void Approve()
        {
            Decide(true);
        }

        public void Reject()
        {
            Decide(false);
        }

        private void Decide(bool approve)
        {
            if (!CanDecide()) return;

            // 1) 기존 판정 및 보상/오류/NPC 이벤트 처리.
            if (approve)
                examinationController.SelectExtractionApproval();
            else
                examinationController.SelectDocumentRejection();

            examinationController.StampSelectedDisposition();

            // 판정 단계가 종료되지 않았다면, 안전을 위해 진행하지 않는다.
            if (examinationController.HasActiveCase) return;

            // 2) 엔딩이 확정됐다면 새 시민/다음 날짜를 로드하지 않는다.
            if (GameManager.Instance != null)
            {
                if (GameManager.Instance.IsGameOver()) return;
                var ending = GameManager.Instance.GetComponent<EndingManager>();
                if (ending != null && ending.HasResolvedEnding) return;
            }

            // 3) 기존 컨트롤러의 '결과 -> 다음 시민/다음 DAY' 동작을 그대로 사용.
            //    상인 제안 대기 중이면 컨트롤러가 이동을 거부하므로 해당 선택이 우선한다.
            examinationController.StampSelectedDisposition();
        }
    }
}
